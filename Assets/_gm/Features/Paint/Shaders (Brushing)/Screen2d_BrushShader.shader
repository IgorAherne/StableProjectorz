//for painting a screen-space mask
//we drag mouse on the screen and the screen-space texture gets adjusted
Shader "Custom/Screen2d_BrushShader"{
    Properties{
        _ScreenAspectRatio("Screen Aspect Ratio", Float) = 1

        [Header(Brush Properties)]
        _BrushStamp ("Brush Texture (circular gradient, etc)", 2D) = "black" {}
        _BrushStrength ("Brush Strength [-1,1] (x:prev, y:new, zw:0)", Vector) = (1,1,0,0)
        _PrevNewBrushScreenCoord ("Stroke Coordinates (prev, new)", Vector) = (0,0,0,0)
        _BrushSize_andFirstFrameFlag("BrushSize (prev, new, isFirstFrame, 0)", Vector) = (1,1,0,0)
    }
    SubShader{
        Tags { "RenderType"="Opaque" } 
        LOD 100 

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile  NUM_SLICES_UPTO_8
            
            #define USING_TEXTURE_ARRAY //<---define, not using multi_compile, because always using arrays.
            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"


            // Only contains the brush-dab from previous frame. Usually looks like a "worm" on a black texture.
            // Uses 8-bit texture, meaning its values only have 256 increments available, for precision.
            // We will paste these values over some specific mask ('_CurrentBrushMask') which we are currently painting, soon.
            DECLARE_TEXTURE_OR_ARRAY( _PrevBrushPathTex );

            sampler2D _BrushStamp;
            float4 _BrushStrength;
            float4 _PrevNewBrushScreenCoord;
            float4 _BrushSize_andFirstFrameFlag;

            float _ScreenAspectRatio;


            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v){
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); 
                o.uv = v.uv;
                return o; 
            }

            #include "Assets/_gm/_Core/Shader_Includes/BrushEffects.cginc"

            float frag (v2f i) : SV_Target{
                PaintInBrushStroke_Input pibs_input;
                pibs_input.screenAspectRatio  = _ScreenAspectRatio;
                pibs_input.fragScreenSpaceUV  = i.uv;
                pibs_input.PrevNewBrushScreenCoord = _PrevNewBrushScreenCoord;
                pibs_input.BrushSizes_andFirstFrameFlag = _BrushSize_andFirstFrameFlag;
                pibs_input.BrushStamp      = _BrushStamp;
                pibs_input.brushStampStronger    = 0;
                // Sample the obj-mask-texture with the texture space UVs:
                pibs_input.BrushStrength01 = abs(_BrushStrength.xy);
                pibs_input.currentBrushPath01 =  SAMPLE_TEXTURE_OR_ARRAY(_PrevBrushPathTex, float3(i.uv,0));
                pibs_input.normalDotView = 1.0;

                return PaintInBrushStroke(pibs_input); //[0,1]
            }

            ENDCG
        } 
    }
    FallBack "Diffuse"
}
