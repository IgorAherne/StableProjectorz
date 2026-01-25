// Used during Graphics.Blit().
// fills up to 16 slices of a target texture-array.  GTX 1050 is limited to 16 sampler2D.
// For more slices you can invoke this shader several times.
Shader "Custom/TextureArray_FillSlices"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma require setrtarrayindexfromanyshader //for SV_RenderTargetArrayIndex
            #pragma require geometry

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex0;
            sampler2D _MainTex1;
            sampler2D _MainTex2;
            sampler2D _MainTex3;
            
            sampler2D _MainTex4;
            sampler2D _MainTex5;
            sampler2D _MainTex6;
            sampler2D _MainTex7;

            sampler2D _MainTex8;
            sampler2D _MainTex9;
            sampler2D _MainTex10;
            sampler2D _MainTex11;

            sampler2D _MainTex12;
            sampler2D _MainTex13;
            sampler2D _MainTex14;
            sampler2D _MainTex15;

            int _NumInsertSlices;
            int _InsertSlicesStart;


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
                uint texIndex : TEXCOORD1;
            };


            v2g vert (appdata v){
                v2g o;
                o.uv = v.uv;
                return o;
            }

            [maxvertexcount(16*4)]
            void geom(point v2g input[1], inout TriangleStream<g2f> outStream){

                #if UNITY_UV_STARTS_AT_TOP //prevents texture from being flipped upside down during Blit, on DirectX
                    float2 uvs[4] = { float2(0,0), float2(1,0), float2(0,1), float2(1,1) };
                #else
                    float2 uvs[4] = { float2(0,1), float2(1,1), float2(0,0), float2(1,0) };
                #endif

                float4 vertices[4] = {  
                    float4(-1, 1, 0, 1), float4(1, 1, 0, 1),  float4(-1,-1, 0, 1), float4(1,-1, 0, 1)  
                };
                
                // Generate a quad for each slice
                g2f o;
                for (uint slice = 0; slice < (uint)_NumInsertSlices; ++slice){

                    o.vertex = vertices[0];
                    o.uv = uvs[0];
                    o.slice = (uint)_InsertSlicesStart + slice;
                    o.texIndex = (uint)slice;
                    outStream.Append(o);

                    o.vertex = vertices[1];
                    o.uv = uvs[1];
                    o.slice = (uint)_InsertSlicesStart + slice;
                    o.texIndex = (uint)slice;
                    outStream.Append(o);

                    o.vertex = vertices[2];
                    o.uv = uvs[2];
                    o.slice = (uint)_InsertSlicesStart + slice;
                    o.texIndex = (uint)slice;
                    outStream.Append(o);

                    o.vertex = vertices[3];
                    o.uv = uvs[3];
                    o.slice = (uint)_InsertSlicesStart + slice;
                    o.texIndex = (uint)slice;
                    outStream.Append(o);

                    outStream.RestartStrip();
                }
            } 

            fixed4 frag (g2f i) : SV_Target{

                switch(i.texIndex){
                    case 0:
                        return tex2D(_MainTex0, i.uv);
                    case 1:
                        return tex2D(_MainTex1, i.uv);
                    case 2:
                        return tex2D(_MainTex2, i.uv);
                    case 3:
                        return tex2D(_MainTex3, i.uv);

                    case 4:
                        return tex2D(_MainTex4, i.uv);
                    case 5:
                        return tex2D(_MainTex5, i.uv);
                    case 6:
                        return tex2D(_MainTex6, i.uv);
                    case 7:
                        return tex2D(_MainTex7, i.uv);

                    case 8:
                        return tex2D(_MainTex8, i.uv);
                    case 9:
                        return tex2D(_MainTex9, i.uv);
                    case 10:
                        return tex2D(_MainTex10, i.uv);
                    case 11:
                        return tex2D(_MainTex11, i.uv);

                    case 12:
                        return tex2D(_MainTex12, i.uv);
                    case 13:
                        return tex2D(_MainTex13, i.uv);
                    case 14:
                        return tex2D(_MainTex14, i.uv);
                    case 15:
                        return tex2D(_MainTex15, i.uv);
                }
                return tex2D(_MainTex0, i.uv);
            }//end()

            ENDCG
        }//end Pass
    }
}