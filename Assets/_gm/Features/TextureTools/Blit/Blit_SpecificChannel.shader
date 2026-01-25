Shader "Custom/Blit_SpecificChannel"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _  BLIT_R  BLIT_G  BLIT_B  BLIT_A
            #pragma multi_compile _  USING_TEXTURE_ARRAY
            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            
            DECLARE_TEXTURE2D_OR_ARRAY(_SrcTex)
            SamplerState sampler_point_clamp;

            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v){
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            } 

            fixed4 frag (v2f i) : SV_Target
            {
                float sample = 0;
                float3 uv_xyz = float3(i.uv.xy, 0);

                #if defined(BLIT_R)
                    sample = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_SrcTex, uv_xyz, _point_clamp).r;
                #elif defined(BLIT_G)
                    sample = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_SrcTex, uv_xyz, _point_clamp).g;
                #elif defined(BLIT_B)
                    sample = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_SrcTex, uv_xyz, _point_clamp).b;
                #elif defined(BLIT_A)
                    sample = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_SrcTex, uv_xyz, _point_clamp).a;
                #else
                    sample = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_SrcTex, uv_xyz, _point_clamp).r;
                #endif

                return float4(sample, sample, sample, sample);
            }
            ENDCG
        }
    }
}