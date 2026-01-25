// Has Blend One Zero equation. 
// If you need additive depth (from multiple cameras) - use another (additive) version of this shader.
//
// NOTICE: you must provide the _NearClip and _FarClip of the current camera.
// DO NOT rely on _ProjectionParams: Unity's Linear01Depth() provides inconsistent results in Editor vs Build.
// This is related to _ProjectionParams.y and _ProjectionParams.x which it uses internally.
//
// To be used by TextureTOosl_SPZ.Blit() immediately after you rendered some camera that supports depth.
// This shader will sample the depth and paste it into your texture.
//
// MAKE SURE YOUR TEXTURE HAS FORMAT  R32_SFloat, to capture tiny variations in depth.
// If you want to make it more prominent, normalize it via  Depth_Contrast_Helper.cs afterwards.
Shader "Unlit/BlitDepth_of_LatestCamera"
{
    SubShader{
    Tags { "RenderType"="Transparent" }
    LOD 100

    // NOTICE: completely overwrites previous contents. 
    // If you need additive depth (from multiple cameras) - use another (additive) version of this shader.
    Blend One Zero

    Pass{
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        // you can do _material.EnableKeyword("ENSURE_LINEAR_01_DEPTH") etc:
        #pragma multi_compile __ ENSURE_LINEAR_01_DEPTH  
        #include "UnityCG.cginc"


        float _CloseIsWhite;//either white means closer, or black means closer.
        sampler2D _LastCameraDepthTexture;
        float _NearClip;
        float _FarClip;


        struct appdata{
            float4 vertex : POSITION;
        };

        struct v2f{
            float4 vertex : SV_POSITION;
            float4 screenPos : TEXCOORD0;
        };


        v2f vert (appdata v){
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.screenPos = ComputeScreenPos(o.vertex);
            return o;
        }

        
        // NOTICE: Unity's Linear01Depth() provides inconsistent results in Editor vs Build.
        // This is related to _ProjectionParams.y and _ProjectionParams.x which it uses internally.
        // But manually passing near and far from c# takes care of that:
        float Linear01Depth_float(float InDepth, float NearClip, float FarClip){
            // Notice, don't worry about reversing the Z-buffer:
            // Unity provides the _LastCameraDepthTexture in a consistent format where 0.0 corresponds 
            // to the near plane and 1.0 corresponds to the far plane, 
            // regardless of whether the underlying Z buffer is reversed.
            float x  = (FarClip - NearClip) / NearClip;
            return 1.0 / (x * InDepth + 1.0);
        }


        float4 frag (v2f i) : SV_Target{
            i.screenPos /= i.screenPos.w;
            float depth = tex2D( _LastCameraDepthTexture,  i.screenPos ).r;
            
            #ifdef ENSURE_LINEAR_01_DEPTH
               depth = Linear01Depth_float(depth, _NearClip, _FarClip);
            #endif

            return float4(depth, depth, depth, 1);
        } 
        ENDCG
    }//end Pass
    }//end SubShader
}
