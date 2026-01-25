
//able to read R channel and output it as (RRR,1)
//For that, make sure to EnableKeyword(SHOW_R_CHANNEL_ONLY) in your material
Shader "Unlit/UI_Tintable_R_or_RGBA"
{
   Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Color_if_depth("Color if Depth", Color) = (1,1,1,1)
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

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

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma target 3.5
            #pragma require setrtarrayindexfromanyshader //for SV_RenderTargetArrayIndex

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ UNITY_UI_ALPHACLIP
            #pragma multi_compile _ SHOW_R_CHANNEL_ONLY //useful for displaying depth. uses RRRR instead of RGBA
            #pragma multi_compile _ USING_TEXTURE_ARRAY

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            

            struct appdata_t{
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };


            struct v2f{
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                half2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;

                #ifdef USING_TEXTURE_ARRAY
                   uint slice : SV_RenderTargetArrayIndex;
                #endif
            };

            
            DECLARE_TEXTURE_OR_ARRAY(_MainTex)

            int _TextureArraySlice; //only used if defined USING_TEXTURE_ARRAY

            fixed4 _Color;
            fixed4 _Color_if_depth;

            fixed4 _TextureSampleAdd;
            float4 _ClipRect;


            v2f vert(appdata_t IN){
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = IN.texcoord;
                
                #ifdef UNITY_HALF_TEXEL_OFFSET
                OUT.vertex.xy += (_ScreenParams.zw-1.0) * float2(-1,1) * OUT.vertex.w;
                #endif
                
                #ifdef SHOW_R_CHANNEL_ONLY
                    OUT.color = IN.color * _Color_if_depth;
                #else 
                    OUT.color = IN.color * _Color; 
                #endif

                #ifdef USING_TEXTURE_ARRAY
                    OUT.slice = (uint)(_TextureArraySlice<0?0:_TextureArraySlice);
                #endif

                return OUT;
            }



            float4 sampleMainTex(v2f IN){
                int s = 0;

                #ifdef USING_TEXTURE_ARRAY
                    s = (uint)IN.slice;
                #endif

                float4 color = SAMPLE_TEXTURE_OR_ARRAY(_MainTex, float3(IN.texcoord.xy, s));

                #ifdef SHOW_R_CHANNEL_ONLY
                  color   = color.rrrr;//R channel 4 times, to make black and white
                  color.a = 1;
                #endif

                return color;
            }

            
            fixed4 frag(v2f IN) : SV_Target
            {
                float4 color =  sampleMainTex(IN) * IN.color;
                
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