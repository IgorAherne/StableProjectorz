Shader "Unlit/IconUI_Borders"
{
   Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LTRB_borders("Left/Top/Right/Bot Borders", Vector) = (1,1,1,1) //1 means border visible, else hidden
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

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

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                half2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            fixed4 _LTRB_borders;
            fixed4 _Color;

            float4 _ClipRect;

            v2f vert(appdata_t IN){
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = IN.texcoord;
                
                #ifdef UNITY_HALF_TEXEL_OFFSET
                OUT.vertex.xy += (_ScreenParams.zw-1.0) * float2(-1,1) * OUT.vertex.w;
                #endif
                
                OUT.color = IN.color * _Color;
                return OUT;
            }


            fixed4 frag(v2f IN) : SV_Target
            {
                //ensure RectMask2D affects us:

                half4 border  = tex2D(_MainTex, IN.texcoord);
                      border.a *= 1.14f;//for some strange reason A is dim without this coeff.
                                        //even if no compression, etc.

                //turn off, if one of sides and is zero:
                //NOTICE: += and not *= otherwise the corners get exlcuded.
                float borderAlpha = 0;
                borderAlpha +=  border.r * (_LTRB_borders.x);
                borderAlpha +=  border.g * (_LTRB_borders.y);
                borderAlpha +=  border.b * (_LTRB_borders.z);
                borderAlpha +=  border.a * (_LTRB_borders.w);
                borderAlpha  = saturate(borderAlpha);
                
                borderAlpha *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                #ifdef UNITY_UI_ALPHACLIP
                clip (borderAlpha - 0.001);
                #endif
                IN.color.a *= borderAlpha;
                return IN.color;
            }
        ENDCG
        }
    }
    Fallback "UI/Default"
}