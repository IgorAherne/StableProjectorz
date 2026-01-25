
//for previewing a renderTexture inside RawImage UI component
Shader "Unlit/UI Show PaintMask BlackWhite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha // Enable alpha blending
        ZWrite Off // Disable depth write for transparency
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;

            v2f vert (appdata v){
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target{
                fixed4 col = tex2D(_MainTex, i.uv);
                float grayScale = (col.r + col.g + col.b) / 3.0; // Convert to grayscale for comparison
                col = float4(1,1,1,grayScale);
                return col;
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
