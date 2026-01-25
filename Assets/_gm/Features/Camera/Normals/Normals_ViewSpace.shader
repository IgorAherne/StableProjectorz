Shader "Custom/ViewSpaceNormals" {
    SubShader {
        Pass {
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //so that the 'SV_RenderTargetArrayIndex' works

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile  __  USING_TEXTURE_ARRAY  //for the  DECLARE_TEXTURE_OR_ARRAY
            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24
            #pragma multi_compile  __ NORMALMAP_IS_EMPTY

            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"

            DECLARE_TEXTURE_OR_ARRAY(_NormalsTex);

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 tspace0 : TEXCOORD1;
                float3 tspace1 : TEXCOORD2;
                float3 tspace2 : TEXCOORD3;
                uint renderIx : SV_RenderTargetArrayIndex;
            };
            
            
            v2f vert(appdata v) {
                v2f o;
                
                o.renderIx = max(0, uv_to_renderTargIX(v.uv)); //determine UDIM sector, before doing loopUV(uv).
                v.uv = loopUV(v.uv);//so that UDIMs can sample textures like usual, in [0,1] space.
                o.uv = v.uv;

                o.pos = UnityObjectToClipPos(v.vertex);
                
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                float3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;
                
                o.tspace0 = float3(worldTangent.x, worldBinormal.x, worldNormal.x);
                o.tspace1 = float3(worldTangent.y, worldBinormal.y, worldNormal.y);
                o.tspace2 = float3(worldTangent.z, worldBinormal.z, worldNormal.z);
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target {
                #ifdef NORMALMAP_IS_EMPTY
                    float3 tnormal =  float3(0,0,1);
                #else
                    float4 packedNormal = SAMPLE_TEXTURE_OR_ARRAY(_NormalsTex, float3(i.uv, i.renderIx) );
                    float3 tnormal =  UnpackNormal(packedNormal);
                #endif
                
                float3 worldNormal;
                worldNormal.x = dot(i.tspace0, tnormal);
                worldNormal.y = dot(i.tspace1, tnormal);
                worldNormal.z = dot(i.tspace2, tnormal);
                
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal);
                
                fixed3 col = (viewNormal.xyz + fixed3(1,1,1)) * 0.5;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    Fallback "VertexLit"
}