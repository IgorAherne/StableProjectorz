//Helps to display a flat 2D image that always remains on screen,
//regardless of where the camera is looking.
//from https://gist.github.com/aras-p/3d8218ef5d96d5984019
// Maintains aspect of the image (outter-envelops the viewport)
 
Shader "Skybox/Background Texture Aspect (Advanced)"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HSV_and_Contrast("HueShift, Saturation, Value, Contrast", Vector) = (0,1,1,1)

        _BG_Color_for_UV_inspection("Background color for inspecting-uvs", Color) = (0.3, 0.3, 0.3, 1)

        _CheckerTex("Checker Texture", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
 
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile  NUM_SLICES_UPTO_8
            #define USING_TEXTURE_ARRAY //<---define, not using multi_compile, because always using arrays.

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"
 
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            
            sampler2D _CheckerTex;
            float _isShowCheckerTex;//0 or 1

            float4 _HSV_and_Contrast;

            float4 _BotGradientColor;
            float4 _TopGradientColor;
            float _UseGradientColors;//either 0 or 1

            float4 _BG_Color_for_UV_inspection;
            float _InspectUVs_01; //if 1, we will lerp towards solid color, to have background when inspecting uv-chunks.

            DECLARE_TEXTURE_OR_ARRAY(_BgMaskTex);
            DECLARE_TEXTURE_OR_ARRAY(_Mask_CurrBrushStrokeTex)//whatever user is currently painting with the current dab.
            float _Mask_CurrBrushStroke_Exists;//1 if painting 0 of not.
            float _Mask_CurrBrushStroke_Strength_m1_p1;//strength by which to scale the curr brush stroke [-1, 1].
            float _isForceMaskAlpha1; //0 or 1

 
            void vert (float4 pos : POSITION, out float4 outUV : TEXCOORD0, out float4 outPos : SV_POSITION){    
                outPos = UnityObjectToClipPos(pos);
                outUV = ComputeScreenPos(outPos);
            }

			#include "Assets/_gm/_Core/Shader_Includes/ShaderEffects.cginc"
			
            fixed4 frag (float4 uv : TEXCOORD0) : SV_Target{
                // Compute aspect ratio of the texture and the screen
                float textureAspect = _MainTex_TexelSize.z / _MainTex_TexelSize.w; // Use z and w for texture width and height
                float screenAspect = _ScreenParams.x / _ScreenParams.y;
 
                uv /= uv.w;
                float2 originalUV   = uv;
                       originalUV.x *= screenAspect;
 
                // Calculate scale and offset for UVs to keep the image centered
                float scale, offset;
 
                if (screenAspect < textureAspect){
                    // Screen is narrower than texture - adjust UV.x
                    scale = screenAspect / textureAspect;
                    offset = (1.0f - scale) * 0.5f;
                    uv.x = uv.x * scale + offset;
                } else {// Screen is less tall than texture - adjust UV.y
                    scale = textureAspect / screenAspect;
                    offset = (1.0f - scale) * 0.5f;
                    uv.y = uv.y * scale + offset;
                }
                
                fixed4 col = tex2D(_MainTex, uv);

                float4 checker  = tex2D(_CheckerTex, originalUV);
                      checker.rgb = checker.rgb*0.06;
                
                float maskVal  = SAMPLE_TEXTURE_OR_ARRAY(_BgMaskTex, float3(uv.xy,0) ).r;//0 because bgs only have one slice.

                float stroke   = _Mask_CurrBrushStroke_Strength_m1_p1 * _Mask_CurrBrushStroke_Exists;
                      maskVal +=  stroke * SAMPLE_TEXTURE_OR_ARRAY(_Mask_CurrBrushStrokeTex, float3(uv.xy,0));
                      maskVal *= col.a;
                      maskVal  = _isForceMaskAlpha1>0? 1 : maskVal;
                      maskVal  = saturate(maskVal);

                float4 gradientColor = lerp(_BotGradientColor, _TopGradientColor, uv.y);

					   col = EffectsPostProcess(col, _HSV_and_Contrast);
                       col = lerp(col, gradientColor, _UseGradientColors);
                       col.a = maskVal;
                       col = lerp(col, _BG_Color_for_UV_inspection, _InspectUVs_01);
                return col;
            }
 
            ENDCG
        }
    }
}