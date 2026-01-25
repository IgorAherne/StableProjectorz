
//for previewing a renderTexture inside RawImage UI component
Shader "Unlit/UI Show Cursor"
{
   Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _CursorA_Strength("_CursorA_Strength", Float) = 1
        _CursorB_Strength("_CursorB_Strength", Float) = 1
        _CursorC_Strength("_CursorC_Strength", Float) = 1
        
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
        Blend SrcAlpha OneMinusSrcAlpha
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
            
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            sampler2D _MainTex;
            float _CursorA_Strength;
            float _CursorB_Strength;
            float _CursorC_Strength;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = IN.texcoord;
                
                #ifdef UNITY_HALF_TEXEL_OFFSET
                OUT.vertex.xy += (_ScreenParams.zw-1.0) * float2(-1,1) * OUT.vertex.w;
                #endif
                
                OUT.color = IN.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                //we will sample the cursor texture 3 times, shrinking towards center each time.
                //When cursor is small, having 3 circles makes it thicker and more visible
                float2 zoomedUV_a =  IN.texcoord;

                float2 zoomedUV_b  =  0.5f+(IN.texcoord-0.5f)/0.98f;
                       zoomedUV_b  = clamp(zoomedUV_b, 0,1);

                float2 zoomedUV_c  =  0.5f+(IN.texcoord-0.5f)/0.9f;
                       zoomedUV_c  = clamp(zoomedUV_c, 0,1);

                half4 cursorCol  = (tex2D(_MainTex, zoomedUV_a) + _TextureSampleAdd);
                half4 cursorColZoomedA  = (tex2D(_MainTex, zoomedUV_b) + _TextureSampleAdd);
                half4 cursorColZoomedB  = (tex2D(_MainTex, zoomedUV_c) + _TextureSampleAdd);
                
                half4 color  = cursorCol * _CursorA_Strength;
                      color += cursorColZoomedA * _CursorB_Strength;
                      color += cursorColZoomedB * _CursorC_Strength;
                
                color.a = color.r;
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return fixed4(IN.color.rgb,  IN.color.a * color.a);
            }
        ENDCG
        }
    }
}



