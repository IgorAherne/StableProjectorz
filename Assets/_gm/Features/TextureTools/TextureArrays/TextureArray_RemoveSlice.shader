Shader "Custom/TextureArray_RemoveSlice"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //for SV_RenderTargetArrayIndex

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24
            
            #include "UnityCG.cginc"

            #define USING_TEXTURE_ARRAY //<---define, not using multi_compile, because always using arrays.
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"


            DECLARE_TEXTURE_OR_ARRAY(_MainTex); //depends on  'multi_compile USING_TEXTURE_ARRAY'
            int _SliceToSkip;
            int _NumSlicesOld;

            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g{
                float2 uv : TEXCOORD0;
            };

            struct g2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                uint slice : SV_RenderTargetArrayIndex;
            };

            v2g vert (appdata v){
                v2g o;
                o.uv = v.uv; 
                return o;
            }

            #if defined(NUM_SLICES_UPTO_24)
                [maxvertexcount(24*4)]
            #elif defined(NUM_SLICES_UPTO_16)
                [maxvertexcount(16*4)]
            #else
                [maxvertexcount(8*4)]
            #endif
            void geom(point v2g input[1], inout TriangleStream<g2f> outStream)
            {
                #if UNITY_UV_STARTS_AT_TOP //prevents texture from being flipped upside down during Blit, on DirectX
                    float2 uvs[4] = { float2(0,0), float2(1,0), float2(0,1), float2(1,1) };
                #else
                    float2 uvs[4] = { float2(0,1), float2(1,1), float2(0,0), float2(1,0) };
                #endif

                float4 vertices[4] = {
                    float4(-1, 1, 0, 1), float4(1, 1, 0, 1), float4(-1,-1, 0, 1), float4( 1,-1, 0, 1)
                };

                uint outputSlice = 0;

                // First loop: from 0 to _SliceToSkip
                {for (uint slice=0;  slice<(uint)_SliceToSkip;  ++slice){
                    g2f output;
                    output.slice = outputSlice;
                    // Generate a quad for each slice
                    output.vertex = vertices[0];
                    output.uv = uvs[0];
                    outStream.Append(output);

                    output.vertex = vertices[1];
                    output.uv = uvs[1];
                    outStream.Append(output);

                    output.vertex = vertices[2];
                    output.uv = uvs[2];
                    outStream.Append(output);

                    output.vertex = vertices[3];
                    output.uv = uvs[3];
                    outStream.Append(output);

                    outStream.RestartStrip();
                    outputSlice++;
                }}

                // Second loop: from _SliceToSkip + 1 to the end.
                //NOTICE: ussing {} around the loop, to avoid warnings that slice is defined in same scope twice.
                {for (uint slice=(uint)_SliceToSkip+1;  slice<(uint)_NumSlicesOld;  ++slice){
                    g2f output;
                    output.slice = outputSlice;
                    // Generate a quad for each slice
                    output.vertex = vertices[0];
                    output.uv = uvs[0];
                    outStream.Append(output);

                    output.vertex = vertices[1];
                    output.uv = uvs[1];
                    outStream.Append(output);

                    output.vertex = vertices[2];
                    output.uv = uvs[2];
                    outStream.Append(output);

                    output.vertex = vertices[3];
                    output.uv = uvs[3];
                    outStream.Append(output);

                    outStream.RestartStrip();
                    outputSlice++;
                }}
            }

            fixed4 frag (g2f i) : SV_Target{
                //depends on 'multi_compile USING_TEXTURE_ARRAY':
                return  SAMPLE_TEXTURE_OR_ARRAY(_MainTex, float3(i.uv, i.slice));
            }
            ENDCG
        }
    }
}