//sets blends between currentAlpha and wantedAlpha, based on the R8 mask.
Shader "Unlit/Blit_SetAlpha_where_Masked"
{
    SubShader{
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            Texture2D _SrcTex;
            Texture2D _MaskTex;

            float _WantedAlpha;

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

            fixed4 frag (v2f i) : SV_Target{
                float4 srcCol = _SrcTex.Sample(sampler_point_clamp, i.uv);
                float mask01  = _MaskTex.Sample(sampler_point_clamp, i.uv).r;

                float4 wanted  = float4(srcCol.rgb, _WantedAlpha);

                return lerp(srcCol, wanted, mask01);
            } 
            ENDCG
        }
    }//end SubShader
}
