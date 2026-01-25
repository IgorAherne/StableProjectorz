// Helps us to render a 2D image 'flipped' horizontally.
// Useful during blits, for example mirroring icons of screenshots, for 3D generations.
Shader "Custom/MirrorShader"
{
    Properties{
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader{
        // This SubShader is set to work in a typical UI/Overlay context
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata{
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f{
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;

            v2f vert (appdata v){
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Flip horizontally by taking "1 - u" for the x component
                float2 uv = v.uv;
                uv.x = 1.0 - uv.x;
                
                o.uv = uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target{
                return tex2D(_MainTex, i.uv).rgba;
            }
            ENDCG
        }
    }
    FallBack "UI/Unlit/Transparent"
}
