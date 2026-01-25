// Spreads the color outwards onto transparent texels that are surrounded by opaque neighboring texels.
// This process is called Dilation (or Erosion when shrinking) and helps to solve the issue with UV seams being visible.
Shader "Unlit/TextureDilation"
{
    SubShader
    {        
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader // For SV_RenderTargetArrayIndex

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma multi_compile  ALPHA_R  ALPHA_G  ALPHA_B  ALPHA_A  SEPARATE_UV_CHUNKS_TEX_R8
            #pragma multi_compile _ SHRINK // Enable this for shrinking (erosion)
            #pragma multi_compile _ AVERAGE_THE_COLORS
            #pragma multi_compile _ USING_TEXTURE_ARRAY
            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24
            
            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"

            DECLARE_TEXTURE2D_OR_ARRAY(_SrcTex)
            float4 _SrcTex_invSize;

            // Optional texture for separate UV chunks
            DECLARE_TEXTURE2D_OR_ARRAY(_Separate_UV_Chunks_R8);

            SamplerState sampler_point_clamp;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0; 
            };
 
            struct v2g {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            struct g2f {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
                uint slice    : SV_RenderTargetArrayIndex;
            };

            v2g vert(appdata v) {
                v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv; 
                return o;
            }

            // Geometry function to handle texture arrays
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_GeomFunc_ForBlitSlices.cginc"
              
            float get_myAlpha(float4 myCol, float3 uv) {
                #ifdef ALPHA_R
                    return myCol.r;
                #elif ALPHA_G
                    return myCol.g;
                #elif ALPHA_B
                    return myCol.b;
                #elif ALPHA_A
                    return myCol.a;
                #elif SEPARATE_UV_CHUNKS_TEX_R8
                    return SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_Separate_UV_Chunks_R8, uv, _point_clamp).r;
                #else
                    return myCol.a;
                #endif
            }

            void getSample(float alphaThresh, float3 uv, inout float4 sum, inout int count) {
                float4 adjacentCol = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_SrcTex, uv, _point_clamp);
                float adjAlpha = get_myAlpha(adjacentCol, uv);

                #ifdef SHRINK
                    if (adjAlpha < alphaThresh) {
                        // For shrinking, we care if any neighbor is background
                        count++;
                    }
                #else // Expand outwards, which is usual for dilation
                    if (adjAlpha > alphaThresh) {
                        sum += adjacentCol;
                        count++;
                    }
                #endif
            }

            float4 avgCol_shrink(float4 myCol, float alphaThresh, float3 uv, float3 tex_invSize) {
                for (int x = -1; x <= 1; x++) {
                    for (int y = -1; y <= 1; y++) {
                        if (x == 0 && y == 0) continue; // Skip the center pixel
                        float3 offset = float3(x, y, 0) * tex_invSize;
                        float3 uv_offset = uv + offset;

                        float4 neighborCol = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_SrcTex, uv_offset, _point_clamp);
                        float adjAlpha = get_myAlpha(neighborCol, uv_offset);

                        if (adjAlpha < alphaThresh) {
                            // Found a background neighbor, set the current pixel to background
                            return float4(0, 0, 0, 0);
                        }
                    }
                }
                // No background neighbors found, keep the current pixel as is
                return myCol;
            }

            float4 avgCol_expand(float4 myCol, float alphaThresh, float3 uv, float3 tex_invSize) {
                float4 sum = float4(0, 0, 0, 0);
                int count = 0;
                for (int x = -1; x <= 1; x++) {
                    for (int y = -1; y <= 1; y++) {
                        float3 offset = float3(x, y, 0) * tex_invSize;
                        getSample(alphaThresh, uv + offset, sum, count);
                    }
                }
                count = max(1, count);
                sum /= count; // Averaging the colors
                return sum;
            }

            float4 nearbyCol_shrink(float4 myCol, float alphaThresh, float3 uv, float3 tex_invSize) {
                float3 offsets[4] = {
                    float3(-1,  0, 0) * tex_invSize,
                    float3( 1,  0, 0) * tex_invSize,
                    float3( 0,  1, 0) * tex_invSize,
                    float3( 0, -1, 0) * tex_invSize
                };

                for (int i = 0; i < 4; i++) {
                    float3 uv_offset = uv + offsets[i];
                    float4 neighborCol = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_SrcTex, uv_offset, _point_clamp);
                    float adjAlpha = get_myAlpha(neighborCol, uv_offset);
                    if (adjAlpha < alphaThresh) {
                        // Found a background neighbor, set the current pixel to background
                        return float4(0, 0, 0, 0);
                    }
                }
                // No background neighbors found, keep the current pixel as is
                return myCol;
            }

            float4 nearbyCol_expand(float4 myCol, float alphaThresh, float3 uv, float3 tex_invSize) {
                float4 sum = float4(0, 0, 0, 0);
                int count = 0;
                float3 offsets[4] = {
                    float3(-1,  0, 0) * tex_invSize,
                    float3( 1,  0, 0) * tex_invSize,
                    float3( 0,  1, 0) * tex_invSize,
                    float3( 0, -1, 0) * tex_invSize
                };

                for (int i = 0; i < 4; i++) {
                    getSample(alphaThresh, uv + offsets[i], sum, count);
                    if (count > 0) {
                        // Found a suitable neighbor
                        return sum / count;
                    }
                }
                return myCol;
            }

            float4 search_square_9(float4 myCol, float alphaThresh, float3 uv, float3 tex_invSize) {
                #ifdef SHRINK
                    return avgCol_shrink(myCol, alphaThresh, uv, tex_invSize);
                #else
                    return avgCol_expand(myCol, alphaThresh, uv, tex_invSize);
                #endif
            }

            float4 search_cross_4(float4 myCol, float alphaThresh, float3 uv, float3 tex_invSize) {
                #ifdef SHRINK
                    return nearbyCol_shrink(myCol, alphaThresh, uv, tex_invSize);
                #else
                    return nearbyCol_expand(myCol, alphaThresh, uv, tex_invSize);
                #endif
            }

            float4 frag(g2f i) : SV_Target {
                float alphaThresh = 0.01f;
                float3 tex_invSize = float3(_SrcTex_invSize.xy, 1); 
                float3 uv_withSlice = float3(i.uv, i.slice);

                float4 myCol = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_SrcTex, uv_withSlice, _point_clamp);
                float myAlpha = get_myAlpha(myCol, uv_withSlice);

                #if defined(SHRINK)
                    if (myAlpha < alphaThresh) {
                        // Already a background pixel, return as is
                        return myCol;
                    }
                #else
                    if (myAlpha > alphaThresh) {
                        // Already a foreground pixel, return as is
                        return myCol;
                    }
                #endif

                #ifdef AVERAGE_THE_COLORS
                    float4 col = search_square_9(myCol, alphaThresh, uv_withSlice, tex_invSize);
                #else
                    float4 col = search_cross_4(myCol, alphaThresh, uv_withSlice, tex_invSize);
                #endif

                return col;
            }
            ENDCG
        } // End Pass
    }
}
