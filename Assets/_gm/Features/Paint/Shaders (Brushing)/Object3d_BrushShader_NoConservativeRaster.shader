//THIS IS A COPY OF Object3d_BrushShader.shader
// Has no comments because your are meant to adjust that shader instead, and paste here.
// MAKE SURE TO KEEP THIS COMMENT.
//
// The only difference is that here we must NOT have the 'Conservative True' here.
// This allows to work on old GPUs like AMD RX560 (GTX 1060 equivalent)
// With that setting, the shaders would malfunction, so we have exact copy here.

Shader "Custom/Object3d_BrushShader"{
    Properties{
        _ScreenAspectRatio("Screen Aspect Ratio", Float) = 1

        //DON'T remove. Helps us to ensure it's zero when not provided from c#.
        _ExtraVisibility("ExtraVisibility", Float) = 0 

        [Header(Brush Properties)]
        _BrushStamp ("Brush Texture (circular gradient, etc)", 2D) = "white" {}
        _BrushStampStronger("BrushStampStronger", Float) = 0.0
        _BrushStrength ("Brush Strength [-1,1] (x:prev, y:new, zw:0)", Vector) = (1,1,0,0)
        _PrevNewBrushScreenCoord ("Stroke Coordinates (prev, new)", Vector) = (0,0,0,0)
        _BrushSize_andFirstFrameFlag("BrushSize (prev, new, isFirstFrame, 0)", Vector) = (1,1,0,0)
    }
    SubShader{
        Tags { "RenderType"="Opaque" } 
        LOD 100
        Cull Off

        Pass{ 
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //for SV_RenderTargetArrayIndex

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24
            
            #define USING_TEXTURE_ARRAY //<---define, not using multi_compile, because always using arrays.

            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"


            sampler2D _LastCameraDepthTexture;

            //uv space texture.  
            //If non-zero, a texel is visible by the projection camera.
            // R: With fade-effect applied to edges of model.
            // G: without any fade effect. True (real visibility) of texel to the projector camera. 
            //    Helps to identify front-facing reverse side of 3d models.
            DECLARE_TEXTURE_OR_ARRAY(_ProjVisibility);  
            float _ExtraVisibility;//usually zero, but if don't have visibility texture, we can set this to 1.

            // Only contains the brush-dab from previous frame. Usually looks like a "worm" on a black texture.
            // We will paste these values over some specific mask ('_CurrentBrushMask') which we are currently painting, soon.
            // Has range [0,1]
            DECLARE_TEXTURE_OR_ARRAY(_PrevBrushPathTex);
             
            float _ScreenAspectRatio;
            sampler2D _BrushStamp;
            float _BrushStampStronger;//when Erasing masks, we have to enhance the effect.
            float4 _BrushStrength;
            float4 _PrevNewBrushScreenCoord;
            float4 _BrushSize_andFirstFrameFlag; //(previous size, new size, isFirstFrame, 0)
            
            float _FadeByNormal; //0 or 1.  1 will prevent brushing on surfaces that face away from camera.
                                 // We want to fade by normal when adding stuff, and disable this when erasing.

            struct VertexInput{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;   
                float3 normal : normal;
            };

            struct GeomInput{
                float2 uv : TEXCOORD0;
                float4 objVertex : TEXCOORD1;
                float3 objNormal : TEXCOORD2;
                float3 objViewDir : TEXCOORD3;
            };

            struct PixelInput{
                float2 uv : TEXCOORD0;
                float4 fragScreenSpaceUV : TEXCOORD1;
                float4 screenPos_SV : SV_POSITION;
                uint renderIx : SV_RenderTargetArrayIndex;

                float3 objNormal : TEXCOORD2;
                float3 objViewDir : TEXCOORD3;
            };


            GeomInput vert(VertexInput i){
                GeomInput g;
                g.objVertex  = i.vertex;
                g.objNormal  = i.normal;
                g.objViewDir = ObjSpaceViewDir(i.vertex);
                g.uv =  i.uv;
                return g;
            }


            PixelInput Init_pixelInput(in GeomInput g, uint renderIx){
                PixelInput pix;
                
                pix.renderIx = renderIx;
                g.uv = loopUV(g.uv);//so that UDIMs can sample textures like usual, in [0,1] space.
                pix.uv = g.uv;

                pix.fragScreenSpaceUV = UnityObjectToClipPos(g.objVertex); // Transform the vertex position to clip space
                pix.fragScreenSpaceUV = ComputeScreenPos(pix.fragScreenSpaceUV);

                g.uv.y =  1.0 - g.uv.y; // Invert the Y-coordinate of the UV
                float2 clipSpaceUV =  g.uv*2 - 1; // Convert UVs from [0, 1] range to [-1, 1] (clip space)
                pix.screenPos_SV =  float4(clipSpaceUV, 0, 1); // Set z to zero and w to one for correct depth

                pix.objNormal  = g.objNormal;
                pix.objViewDir = g.objViewDir;

                return pix;
            }


            // Helps to do a manual backface cull.
            // We want to prevent current (this) projection from affecting the reverse side, and putting black color there.
            // Very useful, if users have a clone of same 3D model but rotated 180 degrees, and the back side of clone is facing us.
            // NOTICE: we can't rely on vertex normals, because polygon smoothing groups might be used, even if model is low-poly.
            [maxvertexcount(3)]
            void geom(triangle GeomInput vertices[3],  inout TriangleStream<PixelInput> triStream){
                // Calculate the normal of the triangle
                float3 edge1 = vertices[1].objVertex - vertices[0].objVertex;
                float3 edge2 = vertices[2].objVertex - vertices[0].objVertex;
                float3 normal = cross(edge1, edge2);//normalize not needed.

                float3 viewDir = vertices[0].objViewDir*0.3333333f 
                               + vertices[1].objViewDir*0.3333333f 
                               + vertices[2].objViewDir*0.3333333f;
                viewDir = viewDir;
                // Discard the triangle if it's facing away
                bool isFacing =  dot(normal, viewDir) >= 0;
                if( !isFacing ){ return; }

                uint renderIx = max(0, uv_to_renderTargIX(vertices[0].uv));

                for (int i=0; i<3; i++){
                    PixelInput pix = Init_pixelInput(vertices[i], renderIx);
                    triStream.Append(pix);
                }
            } 


            #include "Assets/_gm/_Core/Shader_Includes/BrushEffects.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/ShaderEffects.cginc"
             
            //checks if Projector Camera sees this texel.
            //Otherwise we will ignore any brush operations, if our current camera can observe the texel.
            float isVisibleToProjCamera(float3 objUV_withSlice){
                //0 if not visible to its projectorCamera, else visible:
                float projVsibil = SAMPLE_TEXTURE_OR_ARRAY(_ProjVisibility, objUV_withSlice).g;
                      projVsibil += _ExtraVisibility;
                return projVsibil > 0;
            }



            float frag (PixelInput i) : SV_Target{
                i.fragScreenSpaceUV.xyz /= i.fragScreenSpaceUV.w;

                float3 uv_withSliceIx =  float3(i.uv.xy, i.renderIx);

                // Only allow painting the texels that are visible to the projector camera.
                // This is important, to prevent user from "painting" on the reverse side of projection.
                // Projection would still be invisible back there, but it would cause all other projections 
                // to become invisible there too. (due to Equalization of mask weights)
                float isVis     = isVisibleToProjCamera(uv_withSliceIx);
                
                //prevent painting on surfaces that are behind the camera:
                float isInFront = isOutsideScreen_or_behind(i.fragScreenSpaceUV) ? 0 : 1;

                // notice, '0.001f' was causing painting-through the belt of leather bag.
                // It depends on Near & Far clip planes. [0.25, 1000] needs 0.00003 (three-hundred-thousandth), it's a good value.
                // I tested it on objects of different import-scale factors (0.0001 to 1000 ratio), 
                // that were then resized into a 3x3x3 bounding box.  It also works well with projection-shader.
                // NOTICE: brining far plane closer WILL WORSEN THE QUALITY. Depth is non-linearly distributed.
                // BY BRINING FAR-PLANE CLOSER, YOU ARE SHIFTING LOW PRECISION CLOSER TO THE OBJECT. Default far plane is 1000 and is good.
                //
                // NOTICE: it's also affected by directional light. 
                // It must exist in the scene (WITH SHADOWS ENABLED), else camera depth will be severely degraded by unity.
                float depthOffset = 0.00003;
                float depth   = Linear01Depth(tex2D(_LastCameraDepthTexture, i.fragScreenSpaceUV).r);// Sample the depth texture.
                float myDepth = Linear01Depth(i.fragScreenSpaceUV.z) - depthOffset;
                float notObscured = myDepth <= depth ?  1 : 0;

                // Not visible. NOTICE, discard, - don't optimize it!!
                // Useful when there is a clone version of objects (allows users to bake two sides at once)
                if(isVis * isInFront * notObscured == 0){ discard; }

                PaintInBrushStroke_Input pibs_input;
                pibs_input.screenAspectRatio  = _ScreenAspectRatio;
                pibs_input.fragScreenSpaceUV  = i.fragScreenSpaceUV.xy;
                pibs_input.PrevNewBrushScreenCoord      = _PrevNewBrushScreenCoord;
                pibs_input.BrushSizes_andFirstFrameFlag = _BrushSize_andFirstFrameFlag;
                pibs_input.BrushStrength01 = abs(_BrushStrength.xy);
                pibs_input.BrushStamp      = _BrushStamp;
                pibs_input.brushStampStronger = _BrushStampStronger; 

                // Sample the obj-mask-texture with the texture space UVs:
                pibs_input.currentBrushPath01  =  SAMPLE_TEXTURE_OR_ARRAY(_PrevBrushPathTex, uv_withSliceIx).r;
                
                pibs_input.normalDotView =   _FadeByNormal==0?  1 : dot(normalize(i.objNormal), normalize(i.objViewDir));

                return PaintInBrushStroke(pibs_input); //[0,1]
            }
            ENDCG
        }//end Pass
    }
    FallBack "Diffuse"
} 
