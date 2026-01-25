Shader "Unlit/EntireColorLayer_BlitApply"
{
    Properties{
        _Sign("Sign (-1 for erase or 1 for add)", Float) = 1.0 //-1 for erase, 1 for add
        _MaxPossibleBrushStrength01("MaxPossibleBrushStrength (0,1)", Float) = 1.0
    }
     
    SubShader
    {
        Tags { "RenderType"="Transparent"  "Queue"="Transparent" }
        
        LOD 100
        ZWrite Off
        ZTest Off
        Cull Off

        // Setup blending, for blending new stuff on top of other already existing projections.
        // I need an alpha which is to be added to the already existing alpha.
        // Yet, the current alpha also needs to "lerp" its RGB.
        BlendOp Add //<--the source and destination values will be added together.
        Blend One OneMinusSrcAlpha//<--two separate equations, one for RGB, one for Alpha.

        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //for SV_RenderTargetArrayIndex

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            
            #pragma multi_compile  __  APPLY_LATEST_BRUSH_TOO
            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24

            #define USING_TEXTURE_ARRAY //<---define, not using multi_compile, because always using arrays.
           
            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"


            DECLARE_TEXTURE_OR_ARRAY(_SrcTex); //Color layer texture, to be applied.    
            DECLARE_TEXTURE_OR_ARRAY(_LatestBrushStroke); //recent brush stroke to be applied as well.
            float4 _CurrBrushColor;
            float _Sign;
            float _MaxPossibleBrushStrength01;

            uint _isColorlessMask;
            sampler2D _ColorlessCheckerTex;

            float _TotalOpacity01; //allows to fade out the entire effect, if we aren't hovering MainViewport, etc.


            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g{ 
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            }; 

            struct g2f{ 
                float4 vertex : SV_POSITION; 
                float2 uv : TEXCOORD0;
                uint slice : SV_RenderTargetArrayIndex;
            };

            v2g vert (appdata v) {
                v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }


            // geom func. Spawns quads for every slice of the destination textureArra.
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_GeomFunc_ForBlitSlices.cginc"


            #include "Assets/_gm/_Core/Shader_Includes/Brush_ApplyFinalStroke_ToMask.cginc"


            fixed4 frag (g2f i) : SV_Target {
                float3 uv_withSlice = float3(i.uv, i.slice);
                
                float4 finalColor  = SAMPLE_TEXTURE_OR_ARRAY(_SrcTex, uv_withSlice);

                // if we are using colorless mask, don't let any color to show through. (could appear on borders of brush dabs).
                // For this, make sure rgb is black, but don't touch alpha.'
                   finalColor.rgb *= saturate(1-_isColorlessMask);
               _CurrBrushColor.rgb*= saturate(1-_isColorlessMask);
                
                #ifdef APPLY_LATEST_BRUSH_TOO
                    // Convert brush stroke sample to [-1, 1] range for brush/erase logic
                    float diff_m1_p1 = SAMPLE_TEXTURE_OR_ARRAY(_LatestBrushStroke, uv_withSlice).r * _Sign; 
                    finalColor = apply_brush_stroke_rgba( finalColor, _CurrBrushColor, diff_m1_p1, _MaxPossibleBrushStrength01 );
                #endif
                
                /// Branch-free colorless checker application
                // This replaces the color with a checker pattern in non-transparent areas when _isColorlessMask is enabled
        
                // Sample the tiled checker texture
                float2 tiledUV = i.uv * 100;
                float4 checkerColor = tex2D(_ColorlessCheckerTex, tiledUV);
        
                // Determine if we should apply the checker
                // This is 1.0 if _isColorlessMask > 0 and finalColor.a > 0, and 0.0 otherwise
                float applyChecker = _isColorlessMask * step(0, finalColor.a);
        
                // Blend between original color and checker color
                // The blend factor is applyChecker * finalColor.a, which:
                //  - Is 0 if _isColorlessMask is 0 or the pixel is fully transparent
                //  - Equals finalColor.a if _isColorlessMask > 0 and the pixel has some opacity
                finalColor.rgb = lerp(finalColor.rgb, checkerColor.rgb, applyChecker * finalColor.a);
                
                return finalColor * _TotalOpacity01; //affec ALL 4 components (due to our blend mode), not only alpha
            }
            ENDCG 
        }
    }
}
