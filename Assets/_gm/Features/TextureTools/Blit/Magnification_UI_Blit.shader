Shader "Custom/Magnification_UI_Blit"
{
    Properties
    {
        _MainTex ("Original Texture", 2D) = "white" {}
        _MagnifiedTex ("Magnified Texture", 2D) = "white" {}
        _MagnificationRect ("Magnification Rectangle", Vector) = (0,0,1,1)
        _MagnificationScale ("Magnification Scale", Float) = 1
        _ScreenAspectRatio ("Screen Aspect Ratio", Float) = 1
        _BorderColor ("Border Color", Color) = (0.83, 0.61, 0.4, 0.5)
        _BorderWidth ("Border Width", Range(0.001, 0.1)) = 0.002
    }
    SubShader
    {
        Tags {"Queue"="Overlay" "RenderType"="Transparent"}
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
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

            sampler2D _MainTex;
            sampler2D _MagnifiedTex;
            float4 _MagnificationRect; // (x, y, width, height) in screen space [0,1]
            float _MagnificationScale;
            float _ScreenAspectRatio;
            float4 _BorderColor;
            float _BorderWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
    
                // UV coordinate handling for _MainTex, to handle differences between DirectX and OpenGL.
                float2 mainUV = uv;
                #if UNITY_UV_STARTS_AT_TOP
                    mainUV.y = 1 - mainUV.y;
                #endif
    
                // Set up magnification area
                float2 rect_min = _MagnificationRect.xy;
                float2 rect_max = _MagnificationRect.xy + _MagnificationRect.zw * _MagnificationScale;
                float2 rect_center = (rect_min + rect_max) * 0.5;
                
                // Use the height of the magnification rectangle to determine the circle radius
                float rect_radius = _MagnificationRect.w * _MagnificationScale * 0.5;
    
                // Calculate distance for circular mask, accounting for aspect ratio
                float2 delta = (uv - rect_center) * float2(_ScreenAspectRatio, 1.0);
                float dist = length(delta);
    
                // Calculate UV for magnified texture
                float2 magUV = (uv - rect_min) / (_MagnificationRect.zw * _MagnificationScale);
                #if UNITY_UV_STARTS_AT_TOP 
                    magUV.y = 1 - magUV.y;
                #endif
    
                // Sample textures
                fixed4 originalColor = tex2D(_MainTex, mainUV);
                fixed4 magnifiedColor = tex2D(_MagnifiedTex, magUV);

                // Determine final color using ternary operators
                fixed4 finalColor = dist > (rect_radius + _BorderWidth) ? originalColor : 
                                    (dist > rect_radius ? _BorderColor : magnifiedColor);

                return finalColor;
            }
            ENDCG
        }
    }
}