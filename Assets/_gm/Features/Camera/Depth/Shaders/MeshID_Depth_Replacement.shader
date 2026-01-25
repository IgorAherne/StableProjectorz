// outputs RG color that contains an id of the object. 
// And also, unity will render into the internall depthmap, 
Shader "Unlit/MeshIds_Depth_Replacement"{

    SubShader{
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            Name "ShadowCaster"
            //ShadowCaster, so that unity renders into depth map:
            Tags { "LightMode" = "ShadowCaster" }
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

        Pass{
            Name "Mesh IDs"
            Cull Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            //ushort that's encoded into color. Gets assigned by the MaterialPropertyBlock of a renderer.
            float4 _UniqueMeshID; 

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

            fixed4 frag (v2f i) : SV_Target{
                return _UniqueMeshID;
            }
            ENDCG
        }
    }
}
