Shader "Custom/Make_Uniform_Noise_RGB"
{
    Properties
    {
        _Seed ("Random Seed", Float) = 12345
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float _Seed;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            // A better pseudo-random function for uniform distribution
            float3 random3(float3 c) {
                float j = 4096.0*sin(dot(c,float3(17.0, 59.4, 15.0)));
                float3 r;
                r.z = frac(512.0*j);
                j *= .125;
                r.x = frac(512.0*j);
                j *= .125;
                r.y = frac(512.0*j);
                return r;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float3 p = float3(i.uv, _Seed);
                float3 rgb = random3(p);
                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
}