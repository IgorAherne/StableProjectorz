Shader "Custom/PasteRChannelToSpecificChannel"
{
    Properties
    {
        _SrcTex ("Source Texture", 2D) = "white" {}
        _DestTex ("Destination Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PASTE_TO_R  PASTE_TO_G  PASTE_TO_B  PASTE_TO_A
            #pragma multi_compile _ USING_TEXTURE_ARRAY
            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"

            DECLARE_TEXTURE2D_OR_ARRAY(_SrcTex)
            DECLARE_TEXTURE2D_OR_ARRAY(_DestTexCopy)
            SamplerState sampler_point_clamp;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 uv_xyz = float3(i.uv.xy, 0);
                float srcR       = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_SrcTex,  uv_xyz,  _point_clamp).r;
                float4 destColor = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_DestTexCopy,  uv_xyz,  _point_clamp);

                #if defined(PASTE_TO_R)
                    destColor.r = srcR;
                #elif defined(PASTE_TO_G)
                    destColor.g = srcR;
                #elif defined(PASTE_TO_B)
                    destColor.b = srcR;
                #elif defined(PASTE_TO_A)
                    destColor.a = srcR;
                #else
                    destColor.r = srcR;
                #endif

                return destColor;
            }
            ENDCG
        }
    }
}