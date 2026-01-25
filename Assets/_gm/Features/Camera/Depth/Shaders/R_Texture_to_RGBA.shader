Shader "Custom/R_Texture_to_RGBA" {

    Properties {
        _MainTex("MainTexture", 2D) = "white"{}
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _ForceAlpha1;
            float _ForceFullWhite;

            struct appdata {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v) {
                v2f o;
                o.uv = v.uv;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                float r = tex2D( _MainTex,  i.uv ).r;
                      r = _ForceFullWhite>0? 1 : r;

                float alpha = _ForceAlpha1>0? 1 : r;

                return float4(r,r,r,alpha);
            }
            ENDCG
        }
    }
}
