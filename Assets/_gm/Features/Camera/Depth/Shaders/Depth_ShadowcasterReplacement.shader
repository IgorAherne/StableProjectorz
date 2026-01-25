// Use this shader to populate depth map of your camera.
//
// _camera.enabled=false; //keep always disabled, will be RenderWithShader() manually.
// _camera.depthTextureMode = DepthTextureMode.Depth;
// _camera.targetTexture = _myRenderTex_with32depthBits;  // 'new RenderTexture(512,512,32);'
// _camera.RenderWithShader(thisShader,"");
 
Shader "Unlit/Depth_ShadowcasterReplacement"
{
    SubShader
    {
        Pass {
            Name "ShadowCaster"
            Tags { "RenderType"="Opaque" "LightMode" = "ShadowCaster" }
            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask 0
 
            CGPROGRAM
  
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 
            struct v2f {
                float4 pos : SV_POSITION;
                float2 depth : TEXCOORD0;
            };
 
            v2f vert (appdata_base v){
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_DEPTH(o.depth);
                return o;
            }
 
            float frag(v2f i) : SV_Target {
                //This macro doesn't return anything. Depth will be computed automatically by the native.
                //https://forum.unity.com/threads/different-approaches-to-compute-depth-of-a-model.680101/#post-4553104
                UNITY_OUTPUT_DEPTH(i.depth); 
            }
            ENDCG
        }
    }//end SubShader
}
 