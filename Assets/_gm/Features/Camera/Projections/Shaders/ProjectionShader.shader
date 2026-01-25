
//Can grab the texture from screen and land it in correct uv location of an object.
Shader "Custom/ProjectionShader"
{
    Properties{
        _ScreenArtTexture("ScreenSpace-Art Texture", 2D) = "white"{}  //what will be projected on top.
        _TintColorCurrProjection("ScreenSpace-Art TintColor and Opacity", Color) = (1.0, 1.0, 1.0, 1.0)
        _ScreenMaskTexture("ScreenSpace Mask", 2D) = "white"{} //if user was using mask for inpaint.
        
        _HSV_and_Contrast("HueShift, Saturation, Value, Contrast", Vector) = (0,1,1,1)
    }

    SubShader{
        Tags { "RenderType"="Transparent" }
        
        LOD 100
        ZTest Always
        Cull Off

        // Setup blending, for blending new stuff on top of other already existing projections.
        // I need an alpha which is to be added to the already existing alpha.
        // Yet, the current alpha also needs to "lerp" its RGB.
        BlendOp Add //<--the source and destination values will be added together.
        Blend SrcAlpha OneMinusSrcAlpha, One One//<--two separate equations, one for RGB, one for Alpha.

        Pass{
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //so that the 'SV_RenderTargetArrayIndex' works

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24

            // for the  DECLARE_TEXTURE_OR_ARRAY:
            #define USING_TEXTURE_ARRAY//#define, not #multi_compile, because always used for projection

            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"

            //uv space texture. 
            //If non-zero, a texel is visible by the projection camera.
            // R: With fade-effect applied to edges of model.
            // G: without any fade effect. True (real visibility) of texel to the projector camera. 
            //    Helps to identify front-facing reverse side of 3d models.
            DECLARE_TEXTURE_OR_ARRAY(_ProjVisibility); 

            //if user wants to paint on 3d model to further mask the projected art:
            DECLARE_TEXTURE_OR_ARRAY(_uvMask);

            sampler2D _ScreenArtTexture;
            fixed4 _TintColorCurrProjection;
            
            sampler2D _ScreenMaskTexture;

            float4 _HSV_and_Contrast;


            #ifdef WITH_CURSOR_MASK
              float _ScreenAspectRatio;
              sampler2D _BrushStamp;
              float4 _PrevNewBrushScreenCoord;
              float4 _BrushSize_andFirstFrameFlag;
            #endif
             

            struct VertexInput{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;    
            };


            struct GeomInput{
                float3 objVertex : TEXCOORD0;
                float3 objViewDir : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };


            struct PixelInput{
                float4 screenPos_SV : SV_POSITION; //to land the vertex into the screen space
                float2 uv : TEXCOORD0;
                float4 fragScreenSpacePos : TEXCOORD1;
                uint renderIx : SV_RenderTargetArrayIndex;
            };



            GeomInput vert(VertexInput i){
                GeomInput g;
                g.objVertex = i.vertex;
                g.objViewDir = ObjSpaceViewDir(i.vertex);
                g.uv =  i.uv;
                return g;
            }


            PixelInput Init_pixelInput(in GeomInput g, uint renderIx){
                PixelInput pix;
                
                pix.renderIx = renderIx;
                g.uv = loopUV(g.uv);//so that UDIMs can sample textures like usual, in [0,1] space.
                pix.uv = g.uv;

                pix.fragScreenSpacePos = UnityObjectToClipPos(g.objVertex); // Transform the vertex position to clip space
                pix.fragScreenSpacePos = ComputeScreenPos(pix.fragScreenSpacePos);

                g.uv.y =  1.0 - g.uv.y; // Invert the Y-coordinate of the UV
                float2 clipSpaceUV =  g.uv*2 - 1; // Convert UVs from [0, 1] range to [-1, 1] (clip space)
                pix.screenPos_SV =  float4(clipSpaceUV, 0, 1); // Set z to zero and w to one for correct depth

                return pix;
            }


            // Helps to do a manual backface cull.
            // We want to prevent current (this) projection from affecting the reverse side, and putting black color there.
            // Very useful, if users have a clone of same 3D model but rotated 180 degrees, and the back side of clone is facing us.
            // NOTICE: we can't rely on vertex normals, because polygon smoothing groups might be used, even if model is low-poly.
            [maxvertexcount(3)]
            void geom( triangle GeomInput vertices[3],  inout TriangleStream<PixelInput> triStream ){
                // Calculate the normal of the triangle
                float3 edge1 = vertices[1].objVertex - vertices[0].objVertex;
                float3 edge2 = vertices[2].objVertex - vertices[0].objVertex;
                float3 normal = cross(edge1, edge2);//normalize not needed.

                float3 viewDir = vertices[0].objViewDir*0.3333333f 
                               + vertices[1].objViewDir*0.3333333f 
                               + vertices[2].objViewDir*0.3333333f;
                // Discard the triangle if it's facing away
                bool isFacing =  dot(normal, viewDir) >= 0;
                if( !isFacing ){ return; }

                uint renderIx =  max(0, uv_to_renderTargIX(vertices[0].uv));

                for (int i=0; i<3; i++){
                    PixelInput pix = Init_pixelInput(vertices[i], renderIx);
                    triStream.Append(pix);
                }
                triStream.RestartStrip();
            }


            #include "Assets/_gm/_Core/Shader_Includes/ShaderEffects.cginc"


            float4 frag(PixelInput i) : SV_Target{    
                
                i.fragScreenSpacePos /= i.fragScreenSpacePos.w;

                if( isOutsideScreen_or_behind(i.fragScreenSpacePos) ){ discard;}

                //check if the coordinate is behind the camera. 
                //This ensures we don't project on surfaces behind us, when rendering with the frustum culling off:
                if(i.fragScreenSpacePos.z < 0){ discard;}

                //now there are two things:  i.fragScreenSpacePos  and  i.uv

                float3 uv_withSliceIx =  float3(i.uv.xy, i.renderIx);

                
                float artOpacity  = _TintColorCurrProjection.a;

                //soft inpaint was generating at every pixel, even if mask only was like 10%.
                //It would still do 10% denoising strength. So the image might be modified in those regions.
                //Don't cut-off the mask, don't blend any more by it, to avoid "double fading". Either 1 or 0 even where its blurry:
                float msk = tex2D(_ScreenMaskTexture, i.fragScreenSpacePos.xy).r;
                float screenMask =  msk>0?  1 : 0; 

                float4 screenArtColor = tex2D(_ScreenArtTexture, i.fragScreenSpacePos.xy);// Sample the screen texture with the texture space UVs
                float4 newScreenArtColor = EffectsPostProcess(screenArtColor, _HSV_and_Contrast);
                       newScreenArtColor.rgb *= _TintColorCurrProjection.rgb;

                float2 visibility = SAMPLE_TEXTURE_OR_ARRAY(_ProjVisibility, uv_withSliceIx);
                float visibilityFaded = visibility.r;//Fades to black closer to edges of 3d surfaces. Doesn't know about front or reverse side.
                float visibilityReal  = visibility.g;//front-facing side of geometry will have 1, reverse side will have 0.
                float invisibility =  1.0 - visibilityFaded; //[0,1], greater when visibilityFaded is darker.


                float uvFinetuneMask =  SAMPLE_TEXTURE_OR_ARRAY(_uvMask, uv_withSliceIx).r * 2; //[0,1] --> [0,2]. Anything above 1 is for fighting the invisibility.
                float uvMask_vs_invisibility = saturate(uvFinetuneMask - invisibility );

                // Slap our new projection on top of everything rendered before.
                // Control the amount by the lerp factor. Remember blending is Transparent, with special blending (see the Blend above)
                float lerpFactor01 =  screenMask * artOpacity * uvMask_vs_invisibility * visibilityReal;
                return float4(newScreenArtColor.rgb,  lerpFactor01);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"

    
}