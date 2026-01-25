
// applies a uv-texture that contains AO onto another UV texture.
// This darkens it with ambient oclusion shadows.
Shader "Unlit/AO_BlitApply"
{
    Properties{
        [Header(Ambient Occlusion)]
        _AO_Visibility("AO Overall Visibility", Float) = 1.0
        _AO_Pivot("Pivot", Float) = 0.5
        _AO_Darks("Darks", Float) = 1.0
        _AO_Midtones("Midtones", Float) = 1.0
        _AO_Highlights("Highlights", Float) = 1.0
    }
    SubShader{
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend Zero SrcColor //multiplicative, will darken
        ZWrite Off
        ZTest Off
        Cull Off

        Pass{
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //so that the 'SV_RenderTargetArrayIndex' works

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24

            #define USING_TEXTURE_ARRAY //<---define, not using multi_compile, because always using arrays

            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"


            DECLARE_TEXTURE_OR_ARRAY(_SrcTex); //Ambient occlusion tex, to be applied.
            float _AO_Visibility;
            float _AO_Pivot;
            float _AO_Darks;
            float _AO_Midtones;
            float _AO_Highlights;
            

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

            //geom func. Spawns quads for every slice of the destination textureArra.
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_GeomFunc_ForBlitSlices.cginc"



            fixed4 frag (g2f i) : SV_Target {
                float3 uv_withSlice = float3(i.uv, i.slice);
                float4 aoFromTex = SAMPLE_TEXTURE_OR_ARRAY(_SrcTex, uv_withSlice).rrrg; // Assuming AO texture is grayscale
                float ao = aoFromTex;

                float isDark  = smoothstep(_AO_Pivot, 0.0, ao);
                float isMid   = smoothstep(0.0, _AO_Pivot, ao)*smoothstep(1.0, _AO_Pivot, ao);
                float isLight = smoothstep(_AO_Pivot, 1.0, ao);

                ao =  isDark*(ao+_AO_Darks-1)  +  isMid*(ao+_AO_Midtones-1)  +  isLight*(ao+_AO_Highlights-1);
                
                ao = saturate(ao);
                ao = lerp(1, ao, _AO_Visibility);

                // We need to ensure alpha is 1 where UV chunks are. 
                // So, using aoFromTex.a
                // NOTICE: using alpha of 'aoFromTex.a' and not simply 1 because THIS shader is invoked for a simple screen quad, during Blit()
                return fixed4(ao, ao, ao, aoFromTex.a);
            }
            ENDCG

        }//end Pass
    }//end SubShader
}