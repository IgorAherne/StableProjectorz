
// Looks at the objects wrapped in uv-texture that contains all projections.
// Where the texture is not fully opaque it returns 1. Otherwise returns 0.
// This helps us find the transparent or semi-transparent regions, as seen by the camera.
Shader "Unlit/Inpaint_WhereEmpty_to_screenMask"{
    SubShader{
        Tags { "RenderType"="Opaque" }
        Cull Back  

        Pass{
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24
            

            #include "UnityCG.cginc"

            #define USING_TEXTURE_ARRAY
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"


            DECLARE_TEXTURE2D_OR_ARRAY(_AccumulatedArt_Tex); //all projections so far, in uv space.

            SamplerState  sampler_point_clamp;


            struct appdata{
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };


            struct v2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 objNormal : TEXCOORD2;
                float3 objViewDir : TEXCOORD3;
                uint slice : TEXCOORD5; //NOTICE: just a TEXCOORD, no need for 'SV_RenderTargetArrayIndex'
            };                          //because we are rendering to screen, and will only use the slice for sampling


            v2f vert (appdata v){
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                o.slice = max(0, uv_to_renderTargIX(v.uv));
                //AFTER slice is determined, loop the uv from UDIM into a [0,1] range. 
                //This will help us to sample from any slice.
                o.uv = loopUV(v.uv);

                o.objViewDir = ObjSpaceViewDir(v.vertex);
                o.objNormal = v.normal;

                return o;
            }
            

              
            float4 frag (v2f i) : SV_Target{
                float3 uv_withSliceIx =  float3(i.uv.xy, i.slice);
                 
                //IMPORTANT! sample via Point, not Bilinear! Otherwise dilation will have issues.
                float art_opacity = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_AccumulatedArt_Tex, uv_withSliceIx, _point_clamp).a;
                // Don't check as 'if < 0.01', instead check 'if < 0.95'.  
                // That's because we want to apply mask even if visibility is, say, 98%.
                // This is helpful around borders, where we might have fading.
                // NOTICE: 0.95 works more reliably than 0.99.
                // 0.99 picks up on minor brush imprecisions even though projection might look fully opaque.
                float mask =  art_opacity < 0.95 ?  1 : 0;
                return float4(mask,0,0,0);
            }
            ENDCG
        }
    }//end SubShader
}
