Shader "Unlit/UI_Show_SkyboxImage"
{
   Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HSV_and_Contrast("HueShift, Saturation, Value, Contrast", Vector) = (0,1,1,1)

        _BG_Color_for_UV_inspection("Background color for inspecting-uvs", Color) = (0.3, 0.3, 0.3, 1)

        _NoiseColor ("Noise Color", Color) = (1,0.6,0,1)
        _PerlinBlobsTex("Perlin Blobs Tex", 2D) = "white" {}

        _CheckerTex("Transpar Checker Tex", 2D) = "white" {}

        //can be shown (tiled) on top of the texture, for example text "Inpaint" etc.
        _ScreenspaceText_Tex ("Screenspace Text texture", 2D) = "black" {}
        _TextColor ("Text Color", Color) = (1,0.6,0,1)
        _TextTranspar("Text Transparency", Float) = 0.15
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _RGB_NoiseTexture("RGB Noise Texture", 2D) = "white"{}

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha  OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile  NUM_SLICES_UPTO_8
            #define USING_TEXTURE_ARRAY //<---define, not using multi_compile, because always using arrays.

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"

            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 tintColor: COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float4 vertex   : SV_POSITION;
                fixed4 tintColor  : COLOR;
                half2 uv  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 gradientColor : TEXCOORD2;
            };
            
            sampler2D _MainTex;

            sampler2D _CheckerTex;
            float _isShowCheckerTex;//0 or 1
            
            sampler2D _PerlinBlobsTex;
            sampler2D _RGB_NoiseTexture;
            DECLARE_TEXTURE_OR_ARRAY(_BgMaskTex);//contains already accepted strokes from before
            DECLARE_TEXTURE_OR_ARRAY(_Mask_CurrBrushStrokeTex);//whatever user is currently painting with the current dab.
            float _Mask_CurrBrushStroke_Exists;//1 if painting 0 of not.
            float _Mask_CurrBrushStroke_Strength_m1_p1;//strength by which to scale the curr brush stroke [-1, 1].
            float _isForceMaskAlpha1; //0 or 1

            float _showAlphaOnly; //0 or 1.


            float4 _HSV_and_Contrast;
            fixed4 _Color;
            
            float4 _BotGradientColor;
            float4 _TopGradientColor;
            float _UseGradientColors;//either 0 or 1

            fixed4 _BG_Color_for_UV_inspection;
            float _InspectUVs_01; //if 1, we will lerp towards solid color, to have background when inspecting uv-chunks.


            float _NoiseSpeed;
            float4 _NoiseColor;

            sampler2D _ScreenspaceText_Tex;
            float4 _TextColor;
            float _TextTranspar;

            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _ViewportAspect;


            float2 RotateUV(float2 uv, float angle){
                float s = sin(angle);
                float c = cos(angle);
                float2 pivot = float2(0.5, 0.5); // Rotate around the center of the texture
                return float2(
                    c * (uv.x - pivot.x) + s * (uv.y - pivot.y) + pivot.x,
                    -s * (uv.x - pivot.x) + c * (uv.y - pivot.y) + pivot.y
                );
            }


            v2f vert(appdata_t IN){
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.uv = IN.uv;
                
                #ifdef UNITY_HALF_TEXEL_OFFSET
                OUT.vertex.xy += (_ScreenParams.zw-1.0) * float2(-1,1) * OUT.vertex.w;
                #endif
                
                OUT.tintColor = IN.tintColor * _Color;
                OUT.gradientColor = lerp(_BotGradientColor, _TopGradientColor, IN.uv.y);
                return OUT;
            }

			#include "Assets/_gm/_Core/Shader_Includes/ShaderEffects.cginc"



            float3 random3(float2 st) {
                return frac(sin(float3(
                    dot(st.xy, float2(127.1,311.7)),
                    dot(st.xy, float2(269.5,183.3)),
                    dot(st.xy, float2(419.2,371.9)) 
                )) * 43758.5453123);
            }


            float inv_lerp(float a, float b, float t){
                return saturate((t-a)/(b-a));
            }


            #include "Assets/_gm/_Core/Shader_Includes/Skybox_BG_Noise.cginc"


            fixed4 frag(v2f IN) : SV_Target{
                float4 color = tex2D(_MainTex, IN.uv) * IN.tintColor;

                float maskVal  = SAMPLE_TEXTURE_OR_ARRAY(_BgMaskTex, float3(IN.uv.xy,0)).r;//0 because bgs only have one slice.
                
                float stroke   = _Mask_CurrBrushStroke_Strength_m1_p1 * _Mask_CurrBrushStroke_Exists;
                      maskVal += stroke * SAMPLE_TEXTURE_OR_ARRAY(_Mask_CurrBrushStrokeTex, float3(IN.uv.xy,0));
                      maskVal *= color.a;
                      maskVal  = _isForceMaskAlpha1>0? 1 : maskVal;
                      maskVal  = saturate(maskVal);

                float4 noise = animated_color_noise(IN.uv, _PerlinBlobsTex, _RGB_NoiseTexture, 
                                                    _NoiseColor, _NoiseSpeed, _ViewportAspect);

                float4 checker =  tex2D(_CheckerTex, IN.uv);
                       checker.rgb = max(0.075, checker.rgb*0.1); //makes the checker brighter and not too dark

                color = EffectsPostProcess(color, _HSV_and_Contrast);
                color = lerp(color, float4(noise.rgb,1), 1.0-maskVal);
                color = lerp(color, IN.gradientColor,  _UseGradientColors);
                color = lerp(color, checker, _isShowCheckerTex);
                color = lerp(color, _BG_Color_for_UV_inspection, _InspectUVs_01);

                color = lerp(color, float4(maskVal,maskVal,maskVal,1), _showAlphaOnly);
                
                // text texture isn't used right now - too distracting. (jul 2024)
                //  float4 text  = tex2D(_ScreenspaceText_Tex, RotateUV(IN.uv*8, 1.55) );
                //  color = saturate(_TextColor*text*_TextTranspar + color);
                
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001); 
                #endif

                return color;
            }
        ENDCG
        }
    }
    Fallback "UI/Default"
}



