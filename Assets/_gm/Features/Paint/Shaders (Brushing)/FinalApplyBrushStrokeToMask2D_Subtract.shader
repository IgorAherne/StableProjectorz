// Used in Graphics.Blit
// For erasing from a 2D mask (see BlendOp and Blend).
// There is a similar shader but for _Add
Shader "Custom/FinalApplyBrushStrokeToMask_Subtract"
{
    SubShader{
        Tags { "RenderType"="Transparent" }
        Blend One One
        BlendOp RevSub
        
        // We'll do a simple full-screen pass. 
        // The output is a single float in [0..1].
        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag 
            
            #pragma multi_compile  NUM_SLICES_UPTO_8
            
            #define USING_TEXTURE_ARRAY //<---define, not using multi_compile, because always using arrays.
            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"


            DECLARE_TEXTURE_OR_ARRAY(_CurrBrushStroke);
            float _MaxStrength;

            struct appdata{
                float4 vertex : POSITION; 
                float2 uv     : TEXCOORD0;
            };

            struct v2f{
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            v2f vert(appdata v){
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            // We'll produce a single channel (float) as the mask. 
            float frag(v2f i) : SV_Target{
                return SAMPLE_TEXTURE_OR_ARRAY(_CurrBrushStroke, float3(i.uv,0)).r * _MaxStrength;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
