// Used during Graphics.blit();
// Allows us to capture a rectangular portion of the Screen-screenshot, into a smaller render texture.
// Works similar to the Windows 10 Snipping-Tool.
Shader "Unlit/ScreenshotRegion_UI_Blit"
{
    Properties{
        _MainTex ("Original Texture (Screen Capture)", 2D) = "white" {}
        // The portion of the screen we want to copy
        _OffsetAndScale ("ScreenRT sub-rect (offset, scale)", Vector) = (0,0,1,1)

        // The BG rect in screen space: offset=(x,y), scale=(w,h)
        // We'll call it _BG_ScreenRect01 to indicate it's in "screen-UV" space
        _BG_ScreenRect01("BG ScreenRect offset+scale", Vector) = (0,0,1,1)
    }
    SubShader{
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // For multi-slice texture array usage
            #pragma multi_compile  NUM_SLICES_UPTO_8
            #pragma multi_compile __ USING_TEXTURE_ARRAY

            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float4 vertex  : SV_POSITION;
                float2 uv      : TEXCOORD0;   // "screen uv"
            };

            sampler2D _MainTex;
            float4    _OffsetAndScale;// for the screenshot region (offsetX, offsetY, scaleX, scaleY)

            float _hasBgTex;//either 0 or 1;
            sampler2D _bgTex;
            sampler2D _viewportTex;
            DECLARE_TEXTURE_OR_ARRAY(_bgMaskTex);
            float _isForceMaskAlpha1;

            float4 _BG_ScreenRect01;// Screen rectangle of BG in [0..1]: offsetX, offsetY, scaleX, scaleY
            float4 _View_ScreenRect01;


            v2f vert (appdata v){
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // We interpret v.uv in range [0..1] as "entire screen"
                // "OffsetAndScale" chooses a subregion from the screen capture
                float2 scaledUV = v.uv * _OffsetAndScale.zw;   // (scaleX, scaleY)
                o.uv = scaledUV + _OffsetAndScale.xy;          // + (offsetX, offsetY)
                return o;
            }

            float2 get_bg_screenUV(v2f i){
                 // i.uv is the "screen UV" in [0..1].
                // The BG is displayed from _BG_ScreenRect01 offset & scale.
                // So, to convert from "screen UV" => "BG mask UV [0..1]":
                //   screenUV -> remove offset, then divide by scale.
                // i.uv = [0..1] means the entire screen. 
                // If the BG is in a subrectangle:
                //     float2 relative = (i.uv - offset) / scale
                float2 offsetXY = _BG_ScreenRect01.xy;
                float2 scaleXY  = _BG_ScreenRect01.zw;
                float2 bg_ScreenUV = (i.uv - offsetXY) / scaleXY;
                return bg_ScreenUV;
            }

            float2 get_view_screenUV(v2f i){
                float2 offsetXY = _View_ScreenRect01.xy;
                float2 scaleXY  = _View_ScreenRect01.zw;
                float2 view_ScreenUV = (i.uv - offsetXY) / scaleXY;
                return view_ScreenUV;
            }

            
            fixed4 frag (v2f i) : SV_Target{
                //-------------------------------------
                // 1) Sample the screen-capture color
                //-------------------------------------
                float2 mainUVclamped = saturate(i.uv);

                #if UNITY_UV_STARTS_AT_TOP
                    mainUVclamped.y = 1.0 - mainUVclamped.y; 
                #endif

                fixed4 col = tex2D(_MainTex, mainUVclamped);

                //-------------------------------------
                // 2) If we have a background mask, 
                //    figure out which UV we sample from it.
                //-------------------------------------
                if (_isForceMaskAlpha1 > 0.5f){
                    return float4(col.rgb, 1);
                }
              
                float2 bg_screenUV  = get_bg_screenUV(i);
                float2 view_screenUV = get_view_screenUV(i);

                // We typically do NOT flip the BG again. 
                // If you see it upside-down, you can add:
                // #if UNITY_UV_STARTS_AT_TOP
                //     bg_screenUV.y = 1.0 - bg_screenUV.y;
                //     view_screenUV.y = 1.0 - view_screenUV.y;
                // #endif

                // Now sample from 0..1, but if out-of-bounds => alpha=0
                float2 bg_uvClamp = saturate(bg_screenUV);
                float2 view_uvClamp = saturate(view_screenUV);

                    float bgAlpha = tex2D(_bgTex, bg_uvClamp).a;//alpha of BG tex
                          bgAlpha *= SAMPLE_TEXTURE_OR_ARRAY(_bgMaskTex, float3(bg_uvClamp , 0)).r;//red of MASK
                          bgAlpha = _hasBgTex>0? bgAlpha : 0;

                    float viewAlpha = tex2D(_viewportTex, view_uvClamp).a;

                    float alpha =  saturate(bgAlpha  + viewAlpha);
          
                return float4(col.rgb, alpha);
            }//end frag()

            ENDCG
        }//end Pass
    }
}
