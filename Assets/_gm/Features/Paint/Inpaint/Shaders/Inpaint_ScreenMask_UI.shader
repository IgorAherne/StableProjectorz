
//for previewing a renderTexture inside RawImage UI component
Shader "Unlit/Inpaint ScreenMask UI"
{
    Properties
    {
        //the screen-space mask:
        _MainTex ("Texture", 2D) = "black" {}
        
        _InfoTex("Texture With Info", 2D) = "black"{}
        _TextTintColor("Color", Color) = (1,1,1,1)
        _TintColor("Color", Color) = (1,1,1,1)
        _ScreenAspectRatio("Screen Aspect Ratio", Float) = 1
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

            sampler2D _InfoTex;
            float4 _TextTintColor;
            float4 _InfoTex_ST;

            fixed4 _TintColor;
            fixed4 _ColorInfoText;

            float _ScreenAspectRatio;

            v2f vert (appdata v){
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float invLerp(float a, float b, float t){
                return saturate((t-a)/(b-a));
            }

            fixed4 frag (v2f i) : SV_Target{
                
                fixed4 col   = _TintColor;
                       col.a = tex2D(_MainTex, i.uv).r;

                       col.a *= _TintColor.a;

                //apply text as well:
                float2 tiledUV    =  i.uv*_InfoTex_ST.xy  +  _InfoTex_ST.zw;
                       tiledUV.x *= _ScreenAspectRatio;
                
                float4 txtCol = tex2D(_InfoTex, tiledUV);

                //multiply text by the color of the mask AND boost brightness by opacity of mask (at least by 20%)
                       txtCol.rgb  *= (max(0.2,col.a)*1.4 + col.rgb );
                       txtCol.rgb *= _TextTintColor;

                col.rgb +=  txtCol;

                //make sure alpha of text is used, so text isn't transparent:
                float alphaFromText =  txtCol.r*invLerp(0,0.4, col.a);
                
                col.a =  max(col.a, alphaFromText);
                return col;
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
