
// draws color inside UV chunks using vertex colors (users asked about vertex colors)
// Can be used as a starting step, to give "base color" to UV chunks, while having alpha 0 elsewhere.
// Rest of colors remains as is (if you render through a Camera, not through a blit).
// This allows you to know where UV chunks are, and where is empty space 
// (will be useful during dilation etc)
// Works with any format, even R8 (single-channel texture).
Shader "Unlit/Color_UV_Chunks"
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

        Pass{
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //so that the 'SV_RenderTargetArrayIndex' works

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ VERTEX_COLORS
            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24

            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"

            float4 _COL_UVCH_Color;

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

            fixed4 frag (v2f i) : SV_Target{
                
                #ifdef VERTEX_COLORS
                    // Even if model has no vertColors, their alpha is 1, which makes UV chunks black.
                    // This is bad because usually people want to export projections on transparent parts.
                    // (transparent everywhere else, possibly even inside uv-chunks). So, making alpha 0.
                    //
                    // But if vertex colors are not black, then allow alpha to be "as-is"".
                    // This allows user to export the texture with vertex colors, even if there are no projections.
                    i.vertColor.a =  dot(i.vertColor.rgb, 1) ==0?  0 : i.vertColor.a;
                    return i.vertColor; 
                #endif
                return _COL_UVCH_Color;
            }
            ENDCG
        }
    }
}
 