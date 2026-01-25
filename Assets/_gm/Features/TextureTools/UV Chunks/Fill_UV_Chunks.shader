
// Used by Bucket Fill tool.
// Draws color inside UV chunks using vertex colors.
// Rest of colors remains as is (if you render through a Camera, not through a blit).
// This allows you to affect regions only for currently selected (isolated) meshes.
// Works with any format, even R8 (single-channel texture).
Shader "Unlit/Fill_UV_Chunks"
{
    Properties{
        _COL_UVCH_Color("_COL_UVCH_Color", Color) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        ZTest Always
        Cull Off

        Conservative True  //otherwise getting seams around uv chunk borders

        Pass{
            CGPROGRAM
            #pragma target 3.5
            #pragma require setrtarrayindexfromanyshader //so that the 'SV_RenderTargetArrayIndex' works in vert()

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile  __ USE_VISIBIL_TEX
            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24

            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"


            #ifdef USE_VISIBIL_TEX
                //uv space texture. 
                //If non-zero, a texel is visible by the projection camera.
                // R: With fade-effect applied to edges of model.
                // G: without any fade effect. True (real visibility) of texel to the projector camera. 
                //    Helps to identify front-facing reverse side of 3d models.
                DECLARE_TEXTURE_OR_ARRAY(_ProjVisibility); 
            #endif


            float4 _COL_UVCH_Color; //what to fill with.


            struct VertexInput{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;   
                float4 vertColor : COLOR;
            };


            struct v2f{
                float4 uv : SV_POSITION;
                float4 vertColor : TEXCOORD0;
                uint renderIx : SV_RenderTargetArrayIndex;
            };


            v2f vert (VertexInput v){ 
                v2f o;
                o.renderIx =  max(0, uv_to_renderTargIX(v.uv));
                
                v.uv = loopUV(v.uv); //so that any udims will all land into [0,1] range.

                v.uv.y =  1.0 - v.uv.y; // Invert the Y-coordinate of the UV
                float2 clipSpaceUV =  v.uv*2 - 1; // Convert UVs from [0, 1] range to [-1, 1] (clip space)
                o.uv =  float4(clipSpaceUV, 0, 1); // Set z to zero and w to one for correct depth
                
                o.vertColor = v.vertColor;
                return o;
            }


            fixed4 frag(v2f i) : SV_Target{
                i.uv /= i.uv.w;
                float3 uv_withSliceIx =  float3(i.uv.xy,  i.renderIx );

                #ifdef USE_VISIBIL_TEX
                   float2 visibility  = SAMPLE_TEXTURE_OR_ARRAY(_ProjVisibility, uv_withSliceIx);
                   float visibilFaded = visibility.r;//Fades to black closer to edges of 3d surfaces. Doesn't know about front or reverse side.
                   float visibilReal  = visibility.g;//front-facing side of geometry will have 1, reverse side will have 0.
                   return min(_COL_UVCH_Color, float4(visibilFaded.rrrr));
                #endif

                return _COL_UVCH_Color;
            }
            ENDCG
        }
    }
}
 