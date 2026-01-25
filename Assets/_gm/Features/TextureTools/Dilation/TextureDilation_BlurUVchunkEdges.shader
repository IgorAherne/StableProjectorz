// Works with a _MainTexture (black and white) which shows where there are edges of UV-chunks.
// Blurs texels, but only those specified in _UVchunksBorders_Tex (edges are shown as 1).
// This significantly will improve Dilation that will be done later on, by another shader.
Shader "Unlit/TextureDilation_BlurUVchunkBorders"
{
    SubShader{
        ZTest Always
        ZWrite Off
        Cull Off

        Pass{
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //for SV_RenderTargetArrayIndex
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile _ BORDERS_WIDER_BLUR  
            #pragma multi_compile _ USING_TEXTURE_ARRAY
            #pragma multi_compile NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24

            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            

            DECLARE_TEXTURE_OR_ARRAY(_CurrentTexture);
            DECLARE_TEXTURE_OR_ARRAY(_UVchunksBorders_Tex);
            float4 _BordersTex_invSize;


            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g{
                float4 vertex : SV_POSITION; 
                float2 uv : TEXCOORD0;
            };

            struct g2f{
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                uint slice : SV_RenderTargetArrayIndex;
            };


            v2g vert(appdata v){
                v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; 
                return o;
            }


            //geom func. Spawns quads for every slice of the destination textureArra.
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_GeomFunc_ForBlitSlices.cginc"


            fixed4 averageSurroundingColor(float3 uv, fixed alphaThresh, float3 tex_invSize){
                fixed4 totalColor = fixed4(0, 0, 0, 0);
                int count = 0;

                #ifdef BORDERS_WIDER_BLUR
                  #define spread 2  //NOTICE, don't Include ';'
                #else
                  #define spread 1
                #endif

                for (int x = -spread; x <= spread; x++) {
                    for (int y = -spread; y <= spread; y++){

                        float3 offset = float3(x, y, 0) * tex_invSize;

                        if(SAMPLE_TEXTURE_OR_ARRAY(_UVchunksBorders_Tex, uv+offset).r > 0.5){ 
                            continue; }//texel belongs to an border, don't consider it.

                        fixed4 neighborColor = SAMPLE_TEXTURE_OR_ARRAY(_CurrentTexture, uv+offset);
                        if (neighborColor.a < alphaThresh){ continue; }

                        totalColor += neighborColor;
                        count++;
                    }
                }
                // return average, else return a default color (e.g., black) if no texels meet the threshold
                return count>0?  (totalColor/count) : fixed4(0,0,0,0);
            }


            fixed4 frag(g2f i) : SV_Target {

                float3 uv_withSlice  = float3(i.uv, i.slice);

                fixed4 originalColor = SAMPLE_TEXTURE_OR_ARRAY(_CurrentTexture, uv_withSlice);
                
                if(SAMPLE_TEXTURE_OR_ARRAY(_UVchunksBorders_Tex, uv_withSlice).r < 0.5){
                    return originalColor; //not an border, keep as is (don't blur)
                }

                fixed alphaThresh = 0.1f;
                float3 tex_invSize = float3(_BordersTex_invSize.xy, 1); 
                fixed4 avgColor = averageSurroundingColor(uv_withSlice, alphaThresh, tex_invSize);

                fixed4 col = fixed4(avgColor.rgb, originalColor.a);// Preserve existing alpha
                return col;
            }
            ENDCG
        }//end Pass
    }
}
