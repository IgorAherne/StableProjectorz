// used in Graphics.Blit(). 
// Draws a grid of UDIMs, for previewing our UVs.
Shader "Custom/UV_Preview_bg"
{
    Properties
    {
        //a square image with 1 pixel border. When repeated creates a grid for showing UDIM sectors.
        _GridTex ("Grid Texture", 2D) = "white" {}
        _Color ("Grid Color", Color) = (1,1,1,0.2)
        _ColorZerothSector ("Color Zeroth Sector", Color) = (0,1,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent"  "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _GridTex;
            float4 _GridTex_ST;
            float4 _Color;
            float4 _ColorZerothSector;
            float _Visibility;

            float4 _GLOBAL_InspectUV_Navigate; // XY: offset, ZW: scale
            float _GLOBAL_inv_cameraAspect01;

            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v){
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // Transform UV for grid:
                float2 screenUV = v.uv;
                screenUV.y = 1-screenUV.y;               // flip Y
    
                screenUV -= 0.5;                         // Center UVs (move to -0.5 to 0.5 range)
                screenUV *= _GLOBAL_InspectUV_Navigate.z;// Apply zoom from center
    
                screenUV -= _GLOBAL_InspectUV_Navigate.xy;// Apply pan
                screenUV.x /= _GLOBAL_inv_cameraAspect01;// Apply aspect ratio correction AFTER the zoom and pan operations
                screenUV += 0.5;                         // Move back to 0-1 range
    
                o.uv = screenUV;
                return o;
            }

            float4 frag(v2f i) : SV_Target{
                // Sample the texture with tiling
                float isZeroUDIM = (i.uv.x > -0.001? 1:0) * (i.uv.y>-0.001?1:0) * (i.uv.x < 1.001?1:0) * (i.uv.y <1.001? 1:0);

                float4 color = lerp(_Color, _ColorZerothSector, isZeroUDIM);

                float2 tiledUV = frac(i.uv);
                float a = tex2D(_GridTex, tiledUV).r * color.a;
                      a *= _Visibility;
                return float4(color.rgb, a);
            }
            ENDCG
        }
    }
}