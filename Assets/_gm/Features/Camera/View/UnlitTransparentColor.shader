Shader "Unlit/Transparent Color" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100

        CGPROGRAM
        #pragma surface surf NoLighting alpha

        fixed4 _Color;

        struct Input {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            o.Albedo = _Color.rgb;
            o.Alpha = _Color.a;
        }

        fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten) {
            return fixed4(s.Albedo, s.Alpha);
        }
        ENDCG
    }
    FallBack "Diffuse"
}