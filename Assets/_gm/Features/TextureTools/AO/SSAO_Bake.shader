
// We bake ambient occlusion by rendering the same object from different angles, 
// with this shader applied to it.
Shader "Custom/SSAO_Bake"{
	Properties{
        _NoiseTextureRGB("Noise Texture (RGB)", 2D) = "white"{}
         _ShaderAccumulStability("Shader Accumulation Stability", Range(0.7, 0.99999)) = 0.97
         [Space]
         _TotalStrength("Total Strength", Float) = 1
         _FullDepth01Difference("Full Effect when depth difference:", Range(0.05, 0.4)) = 0.7
         _SearchRadius("Search Radius", Range(0.03, 0.5)) = 0.1
	}
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

            v2f vert (appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_DEPTH(o.depth);
                return o;
            }

            float frag(v2f i) : SV_Target {
                UNITY_OUTPUT_DEPTH(i.depth);
            }
            ENDCG
        }

		Pass
		{
            ZTest Always
            Cull Off

			CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //for SV_RenderTargetArrayIndex

			#pragma vertex vert
            #pragma geometry geom
			#pragma fragment frag

            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24

            #define USING_TEXTURE_ARRAY //<---define, not using multi_compile, because always using arrays.
			
			#include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"


            DECLARE_TEXTURE_OR_ARRAY(_CurrentAO_uvTexture);

            sampler2D _NoiseTextureRGB;//screen space (never an array)
            sampler2D _LastCameraDepthTexture;//screen space (never an array)

            float _TotalStrength;
            float _FullDepth01Difference;
            float _SearchRadius; 
            float _ShaderAccumulStability;
            float4 _CameraWorldPos;


			struct VertexInput{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
			};

            struct GeometryInput{
				float2 uv : TEXCOORD0;
                float4 vertex : TEXCOORD1;
                float4 worldPos : TEXCOORD2;
            };

            struct PixelInput{
                float2 uv : TEXCOORD0;
                float4 rasterize_into_uv_SV : SV_POSITION;
                float3 viewSpaceNormal : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                uint renderIx : SV_RenderTargetArrayIndex;
            };


            bool isTriFacingCamera(float4 worldVert0, float4 worldVert1, float4 worldVert2, out float3 worldNormal_){
                // Calculate the normal of the triangle
                float3 edge1 = worldVert1 - worldVert0;
                float3 edge2 = worldVert2 - worldVert0;
                worldNormal_ = normalize(cross(edge1, edge2));

                float3 viewDir0 = (_CameraWorldPos.xyz - worldVert0);
                float3 viewDir1 = (_CameraWorldPos.xyz - worldVert1);
                float3 viewDir2 = (_CameraWorldPos.xyz - worldVert2);

                float3 viewDir = viewDir0*0.3333333f  + viewDir1*0.3333333f  + viewDir2*0.3333333f;
                       viewDir = normalize(viewDir);
                return dot(worldNormal_, viewDir) >= 0;
            }


            PixelInput setupPixelInput( inout GeometryInput g,  uint renderIx,  float3 worldNormal ){
                PixelInput px;

                px.renderIx = renderIx;
                g.uv = loopUV(g.uv);//so that UDIMs can sample textures like usual, in [0,1] space.
                px.uv = g.uv;

                // Transform world normal to view space:
                px.viewSpaceNormal = mul((float3x3)UNITY_MATRIX_V, normalize(worldNormal));

				float4 clipVert = UnityObjectToClipPos(g.vertex);
                px.screenPos = ComputeScreenPos(clipVert);

                float2 clipSpaceUV = g.uv*2 - 1; // Convert UVs from [0, 1] range to [-1, 1] (clip space)
                px.rasterize_into_uv_SV = float4(clipSpaceUV, 0, 1); // Set z to zero and w to one for correct depth     
                px.rasterize_into_uv_SV.y *=-1;
                // 'rasterize_into_uv_SV' is marked as 'SV_Position'.
                // Therefore Unity will automatically apply ComputeScreenPos() onto it, before frag() begins.
                // AND will divide by W.
                return px;
            }


			GeometryInput vert (VertexInput v){
				GeometryInput go;
                go.vertex = v.vertex;
                go.worldPos = mul(unity_ObjectToWorld, v.vertex);
				go.uv = v.uv;
				return go;
			}


            [maxvertexcount(3)]
            void geom( triangle GeometryInput g[3],  inout TriangleStream<PixelInput> triStream ){

                float3 worldNormal;
                bool isFacingCam = isTriFacingCamera(g[0].worldPos, g[1].worldPos, g[2].worldPos, worldNormal);
                if(!isFacingCam){ return; }//skips the triangle if it's facing away, else perform transformations and append it:

                uint renderIx = max(0, uv_to_renderTargIX(g[0].uv));

                for(int i=0; i<3; i++){
                    PixelInput px = setupPixelInput( g[i], renderIx, worldNormal );
                    triStream.Append( px );
                }
                triStream.RestartStrip();
            }


            float calc_AO(float depth, float3 screenUV, float3 normal){
                float3 sample_sphere[16] = {
                    float3( 0.5381, 0.1856,-0.4319), float3( 0.1379, 0.2486, 0.4430),
                    float3( 0.3371, 0.5679,-0.0057), float3(-0.6999,-0.0451,-0.0019),
                    float3( 0.0689,-0.1598,-0.8547), float3( 0.0560, 0.0069,-0.1843),
                    float3(-0.0146, 0.1402, 0.0762), float3( 0.0100,-0.1924,-0.0344),
                    float3(-0.3577,-0.5301,-0.4358), float3(-0.3169, 0.1063, 0.0158),
                    float3( 0.0103,-0.5869, 0.0046), float3(-0.0897,-0.4940, 0.3287),
                    float3( 0.7119,-0.0154,-0.0918), float3(-0.0533, 0.0596,-0.5411),
                    float3( 0.0352,-0.0631, 0.5460), float3(-0.4776, 0.2847,-0.0271)
                };
                float3 random = tex2D(_NoiseTextureRGB, screenUV.xy).rgb;
                       random = normalize(random*2-1); //from [0,1] to [-1,1]

                float radius_depth = _SearchRadius / depth;     //deeper = smaller
                float occlusion = 0.0; 
                for(int i=0; i<16; i++){
                    float3 ray      = radius_depth * reflect(sample_sphere[i], random);
                    float3 hemi_ray =  sign(dot(ray, normal))*ray; //flips the ray if it's pointing in an opposite direction to normal.
                    
                    float2 sampleUV    = saturate(screenUV.xy+hemi_ray.xy);
                    float sample_depth = LinearEyeDepth(tex2D(_LastCameraDepthTexture , sampleUV)).r;
                    
                    float difference   = depth - sample_depth;
                    float sample_contribution  =  smoothstep( 0,  _FullDepth01Difference,  difference);//smoothley lerps from 0 to 1 when difference moves from 0 to FullDiff.
                    occlusion += sample_contribution;                                                                        
                }

                float ao =  1.0 - _TotalStrength * occlusion * (1.0f/16);
                return ao;
            }



            fixed4 frag (PixelInput input) : SV_Target{
                
                float3 screenUV = input.screenPos.xyz/input.screenPos.w;
                
                // notice, '0.001f' was causing painting-through the belt of leather bag (in 3d brush shader)
                // It depends on Near & Far clip planes. [0.25, 1000] needs 0.00003 (three-hundred-thousandth), it's a good value.
                // I tested it on objects of different import-scale factors (0.0001 to 1000 ratio), 
                // that were then resized into a 3x3x3 bounding box.  It also works well with projection-shader.
                // NOTICE: brining far plane closer WILL WORSEN THE QUALITY. Depth is non-linearly distributed.
                // BY BRINING FAR-PLANE CLOSER, YOU ARE SHIFTING LOW PRECISION CLOSER TO THE OBJECT. Default far plane is 1000 and is good.
                float depthOffset = 0.00003;
                float myDepth  = LinearEyeDepth(screenUV.z) - depthOffset;
                float depthTex = LinearEyeDepth(tex2D(_LastCameraDepthTexture , screenUV.xy)).r;
                
                float3 uv_withSlice = float3(input.uv, input.renderIx);
                fixed prev_ao  = SAMPLE_TEXTURE_OR_ARRAY(_CurrentAO_uvTexture, uv_withSlice).r;

                if(myDepth > depthTex){ 
                    return fixed4(prev_ao.rrr,1);
                }

                float3 normal = normalize(input.viewSpaceNormal.xyz);
                fixed ao = calc_AO(depthTex, screenUV, normal);
                //pull to the current ao very slowly, to prevent chaotic obliterating updates.
                //Given that we render object at least a few hundred times, it will average-out to a nice result:
                fixed runningAvg_ao =  prev_ao*_ShaderAccumulStability  +  ao*(1-_ShaderAccumulStability);
                
                //Texture is RG, not RGBA, so output R and G.
                return fixed4(runningAvg_ao.r, 1, 1, 1);//Make G 1, so that dilation is able to identify UV islands later on.
            }
			ENDCG
		}
		
	}
    FallBack "Legacy Shaders/VertexLit"
}