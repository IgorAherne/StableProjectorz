// Looks at the 3D object from camera's view.
// Samples fragments around the current fragment, and stores their average into current fragment.
//
// Stores into the UV-space texture. Can be used after each SSAO iteration, to ensure we blur 
// accross uv-chunks, while in screen space.

Shader "Unlit/ScreenSpaceAverage_but_StoreIntoUV"{
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _SampleCount ("Sample Count", Int) = 16
        _SampleRadius ("Sample Radius", Float) = 0.01
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : TEXCOORD1;
                float4 screenPos : SV_POSITION;
            };

            sampler2D _MainTex;
            int _SampleCount;
            float _SampleRadius;
            sampler2D _CameraDepthTexture;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                
                // Apply the _SV trick to render directly into texture space
                float2 clipUV = o.uv * 2.0 - 1.0;
                o.screenPos = float4(clipUV, 0.0, 1.0);
                o.screenPos.y *= -1;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                
                fixed4 col = tex2D(_MainTex, i.uv);
                float3 avgColor = col.rgb;
                int sampleCount = 0;

                int halfCount =  (int)(_SampleCount/(uint)2);//dividing by uint is much faster than int.

                for (int x=-halfCount; x<=halfCount; x++){
                    for (int y=-halfCount; y<=halfCount; y++){
                        float2 offset = float2(x, y) * _SampleRadius;
                        float2 sampleUV = screenUV + offset;
                        float sampleDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampleUV);

                        avgColor += sampleDepth>0? tex2D(_MainTex, sampleUV).rgb : 0;
                        sampleCount += sampleDepth>0;
                    }
                }

                avgColor /= max(1, sampleCount);

                return fixed4(avgColor, 1);
            }
            ENDCG
        }
    }
}