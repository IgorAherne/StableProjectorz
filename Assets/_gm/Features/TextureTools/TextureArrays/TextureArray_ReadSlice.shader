Shader "Custom/TextureArray_ReadSlice"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma require setrtarrayindexfromanyshader //for SV_RenderTargetArrayIndex

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile  _  SAMPLER_POINT //Point-filtering (for pixelated look), else Bilinear
            #pragma multi_compile  __  RGBA  RRR1

            #include "UnityCG.cginc"

            #define USING_TEXTURE_ARRAY //<---define, not using multi_compile, because always using arrays.
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"


            DECLARE_TEXTURE2D_OR_ARRAY(_MainTex); //depends on  'multi_compile USING_TEXTURE_ARRAY'
            int _SliceIx;//which slice to read and output


             #ifdef SAMPLER_POINT
              SamplerState sampler_PointClamp;
            #else
              SamplerState sampler_LinearClamp;
            #endif


            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                uint slice : SV_RenderTargetArrayIndex;
            };

           
            v2f vert (appdata v){
                v2f o;
                o.uv = v.uv;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.slice = _SliceIx;
                return o;
            }


            fixed4 frag (v2f i) : SV_Target{
                //depends on 'multi_compile USING_TEXTURE_ARRAY':
                #ifdef SAMPLER_POINT 
                  float4 col = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_MainTex, float3(i.uv, i.slice), _PointClamp);
                #else 
                  float4 col = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_MainTex, float3(i.uv, i.slice), _LinearClamp);
                #endif

                #ifdef RRR1
                col = float4(col.rrr,1);
                #endif
                return col;
            }
            ENDCG
        }
    }
}