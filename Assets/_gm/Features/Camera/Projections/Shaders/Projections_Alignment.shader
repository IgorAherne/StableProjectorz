//makes textures that show which point-of-view (POV) is most orthogonal to each polygon.
Shader "Unlit/Projections_Alignment"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off
        ZTest Off

        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //so that the 'SV_RenderTargetArrayIndex' works

            #pragma vertex vert
            #pragma fragment frag 

            #pragma multi_compile _ NUM_POV_2  NUM_POV_3  NUM_POV_4  NUM_POV_5  NUM_POV_6
            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24

            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/MultiProjectionVariables.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"

            
            struct VertexInput{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
			};


            struct PixelInput{
                uint renderIx : SV_RenderTargetArrayIndex;

                float4 rasterize_into_uv_SV : SV_POSITION; //to land the vertex into the screen space
                float2 uv : TEXCOORD0;

                float3 worldNormal : TEXCOORD1;

                float3 worldView0 : TEXCOORD2;
                
                #ifdef NUM_POV_2
                  float3 worldView1 : TEXCOORD3;
                #endif
                #ifdef NUM_POV_3
                  float3 worldView2 : TEXCOORD4;
                #endif
                #ifdef NUM_POV_4
                  float3 worldView3 : TEXCOORD5;
                #endif
                #ifdef NUM_POV_5
                  float3 worldView4 : TEXCOORD6;
                #endif
                #ifdef NUM_POV_6
                  float3 worldView5 : TEXCOORD7;
                #endif
            };

            
            // Vertex function. Very simple, all the transformations 
            // will be happening inside the geometry shader.
            PixelInput vert(VertexInput i){
                PixelInput pix; 
                
                pix.renderIx = max(0, uv_to_renderTargIX(i.uv));//done BEFORE looping the uvs.

                i.uv = loopUV(i.uv);//so that UDIMs can sample textures like usual, in [0,1] space.
                pix.uv = i.uv;

                // Invert the Y-coordinate of the UV maybe (for DirectX):
                //https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
                if(_ProjectionParams.x < 0){  i.uv.y = 1 - i.uv.y;  }
                
                //TODO maybe check UNITY_UV_STARTS_AT_TOP before flipping uvs?  https://docs.unity3d.com/Manual/SL-BuiltinMacros.html

                // our uvs are marked as 'SV_Position'. 
                // Therefore Unity will automatically apply ComputeScreenPos() onto it, before frag() begins.
                // AND will divide by W (but luckily w=1 for UVs).
                // Therefore, we need to cancel-out this transformation, by making our stuff larger than necessary:
                float2 clipSpaceUV =  i.uv*2 - 1;
                pix.rasterize_into_uv_SV =  float4(clipSpaceUV, 0, 1); // Set z to zero and w to one for correct depth
                // Again, notice that we DO NOT use ComputeScreenPos() 
                // because parameter is marked by SV_Position anyway.
                 
                float3 worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                pix.worldNormal = UnityObjectToWorldNormal(i.normal);

                  pix.worldView0 = _CameraWorldPos0.xyz - worldPos; //no need to normalize here, - will be done in frag anyway
                #ifdef NUM_POV_2
                  pix.worldView1 = _CameraWorldPos1.xyz - worldPos;
                #endif
                #ifdef NUM_POV_3
                  pix.worldView2 = _CameraWorldPos2.xyz - worldPos;
                #endif
                #ifdef NUM_POV_4
                  pix.worldView3 = _CameraWorldPos3.xyz - worldPos;
                #endif
                #ifdef NUM_POV_5
                  pix.worldView4 = _CameraWorldPos4.xyz - worldPos;
                #endif
                #ifdef NUM_POV_6
                  pix.worldView5 = _CameraWorldPos5.xyz - worldPos;
                #endif

                return pix;
            }


            //stores dot product and id of POV. Id is compressed into [0,1] range. 
            //For example if there are 3 ids, the first one will be 0.33333.
            // to get visibility we sampe texture that has R as "faded visibility, with edges softened etc", and G as "true visibility".
            //
            // NOTICE: we are sampling the visibility texture using UVs, not via 'rasterize_into_uv_SV'.
            #define CALC_DOT(ix)                                                                                     \
            {                                                                                                        \
                float3 viewDir   =  normalize(i.worldView##ix);                                                      \
                float visibility = _POV##ix##_ProjVisibility.Sample(sampler_linear_repeat, float3(i.uv,i.renderIx) ).g;\
                dots[ix].r =  saturate(dot(i.worldNormal, viewDir))*visibility;                                      \
                dots[ix].g =  (ix+1) / (float)_NumPOV;                                                               \
            }  


            void sortDotsArray_Descending(inout float2 dots[6]){
                float2 temp;
                if (dots[0].x < dots[1].x){ temp = dots[0]; dots[0] = dots[1]; dots[1] = temp; }
                if (dots[0].x < dots[2].x){ temp = dots[0]; dots[0] = dots[2]; dots[2] = temp; }
                if (dots[0].x < dots[3].x){ temp = dots[0]; dots[0] = dots[3]; dots[3] = temp; }
                if (dots[0].x < dots[4].x){ temp = dots[0]; dots[0] = dots[4]; dots[4] = temp; }
                if (dots[0].x < dots[5].x){ temp = dots[0]; dots[0] = dots[5]; dots[5] = temp; }

                if (dots[1].x < dots[2].x){ temp = dots[1]; dots[1] = dots[2]; dots[2] = temp; }
                if (dots[1].x < dots[3].x){ temp = dots[1]; dots[1] = dots[3]; dots[3] = temp; }
                if (dots[1].x < dots[4].x){ temp = dots[1]; dots[1] = dots[4]; dots[4] = temp; }
                if (dots[1].x < dots[5].x){ temp = dots[1]; dots[1] = dots[5]; dots[5] = temp; }

                if (dots[2].x < dots[3].x){ temp = dots[2]; dots[2] = dots[3]; dots[3] = temp; }
                if (dots[2].x < dots[4].x){ temp = dots[2]; dots[2] = dots[4]; dots[4] = temp; }
                if (dots[2].x < dots[5].x){ temp = dots[2]; dots[2] = dots[5]; dots[5] = temp; }

                if (dots[3].x < dots[4].x){ temp = dots[3]; dots[3] = dots[4]; dots[4] = temp; }
                if (dots[3].x < dots[5].x){ temp = dots[3]; dots[3] = dots[5]; dots[5] = temp; }

                if (dots[4].x < dots[5].x){ temp = dots[4]; dots[4] = dots[5]; dots[5] = temp; }
            }


            // We are outputting to the same slice of 2 different texture arrays. 
            // dots goes into the slice of the first.
            // povIds goes into the slice of the second.
            struct FragOutput {
                float4 dots : SV_Target0;
                // Indexes have to be packed into [0,1]  range.
                // If there are 5 cameras total, then  0 is Unknown,  0.2 is first,  0.4 is second, 0.6 is third, etc:
                float4 povIds : SV_Target1;
            };                              


            FragOutput frag(PixelInput i){
                 //notice, no need to divide the uv by its w component,
                 //because uvs were marked as SV_POSITION, so unity did it already.
                 i.worldNormal   = normalize(i.worldNormal);
                 
                 float2 dots[6];
                 for(int d=0; d<6; ++d){ dots[d]=0; }
                 
                   CALC_DOT(0)
                 
                 #ifdef NUM_POV_2
                   CALC_DOT(1)
                 #endif
                 #ifdef NUM_POV_3
                   CALC_DOT(2)
                 #endif
                 #ifdef NUM_POV_4
                   CALC_DOT(3)
                 #endif
                 #ifdef NUM_POV_5
                   CALC_DOT(4)
                 #endif
                 #ifdef NUM_POV_6
                   CALC_DOT(5)
                 #endif
                 
                 sortDotsArray_Descending(dots);
                 
                 //store first four entries, that have largest dot products:
                 FragOutput o;
                 o.dots   = float4(dots[0].x, dots[1].x, dots[2].x, dots[3].x);//dots from 1st component
                 
                 //ids from 2nd component.
                 //for each pixel we know the most-suitable camera, the 2nd-most-suitable, etc (up to 4).
                 o.povIds = float4(dots[0].y, dots[1].y, dots[2].y, dots[3].y);

                 return o;
            }
            ENDCG
        }
    }

    FallBack "Diffuse" //for depth
}
