Shader "Custom/Depth_MultiPOV_ShadowcasterReplacement" {
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass{//with adjustments, taken from https://github.com/przemyslawzaworski/Unity3D-CG-programming/blob/master/shadowcaster.shader
			
        Name "ShadowCaster"
        Tags { "LightMode" = "ShadowCaster" }
        ZWrite On  
        ZTest LEqual
        ColorMask 0

		CGPROGRAM
			#pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #include "UnityCG.cginc"

            #pragma multi_compile _ NUM_POV_2  NUM_POV_3  NUM_POV_4  NUM_POV_5  NUM_POV_6
            #include "Assets/_gm/_Core/Shader_Includes/MultiProjectionVariables.cginc"


            struct VertexInput{
                float4 vertex : POSITION;
            };

            struct GeometryInput{
                float4 worldPos : POSITION;
            };

            struct PixelInput{
                float4 fragScreenSpacePos_SV : SV_POSITION;
            };


            bool isTriFacingCamera(float4 worldVert0, float4 worldVert1, float4 worldVert2, float4 cameraWorldPos){
                // Calculate the normal of the triangle
                float3 edge1 = worldVert1 - worldVert0;
                float3 edge2 = worldVert2 - worldVert0;
                float3 normal = cross(edge1, edge2); //normalize not needed.

                float3 viewDir0 = (cameraWorldPos.xyz - worldVert0);
                float3 viewDir1 = (cameraWorldPos.xyz - worldVert1);
                float3 viewDir2 = (cameraWorldPos.xyz - worldVert2);

                float3 viewDir = viewDir0*0.3333333f  + viewDir1*0.3333333f  + viewDir2*0.3333333f;
                return dot(normal, viewDir) >= 0;
            }

            void TransformVertex(in GeometryInput g,  out PixelInput px,  in float4x4 viewProjMatrix){

                px.fragScreenSpacePos_SV = mul(viewProjMatrix, g.worldPos); // Transform the vertex position to clip space
                // 'fragScreenSpacePos_SV' is marked as 'SV_Position'.
                // Therefore Unity will automatically apply ComputeScreenPos() onto it, before frag() begins.
                // AND will divide by W.
            }


            void ProcessTrianglePOV( triangle GeometryInput g[3],  inout TriangleStream<PixelInput> triStream, 
                                     inout PixelInput pix[3],  in float4x4 viewProjMatrix,  float4 cameraWorldPos ){

                bool isFacingCam = isTriFacingCamera(g[0].worldPos, g[1].worldPos, g[2].worldPos, cameraWorldPos);
                if(!isFacingCam){ return; }//skips the triangle if it's facing away, else perform transformations and append it:
                for(int i=0; i<3; i++){
                    TransformVertex(g[i], pix[i], viewProjMatrix); //notice, same matrix for all 3 verts (matrix for some pov).
                    triStream.Append(pix[i]);
                }
                triStream.RestartStrip();
            }

            //vertex function. Very simple, all the transformations will be happening inside the geometry shader.
            GeometryInput vert(VertexInput v){
                GeometryInput go;
                go.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return go;
            }

            // Takes the inputs and spawns several triangles (one per camera point-of-view).
            // This is because we can have up to 6 cameras looking at the object from different directions.
            // Becasue the inputs are still 'raw', we'll need to perform vertex transformations on them.
            //
            // Also, Helps to do a manual backface cull.
            // We want to prevent current (this) projection from affecting the reverse side, and putting black color there.
            // Very useful, if users have a clone of same 3D model but rotated 180 degrees, and the back side of clone is facing us.
            // NOTICE: we can't rely on vertex normals, because polygon smoothing groups might be used, even if model is low-poly.
            [maxvertexcount(3*6)]
            void geom(triangle GeometryInput g[3],  inout TriangleStream<PixelInput> triStream){
                PixelInput pix[3];
                 ProcessTrianglePOV(g, triStream, pix, _ViewProj_matrix0, _CameraWorldPos0);

               #ifdef NUM_POV_2
                 ProcessTrianglePOV(g, triStream, pix, _ViewProj_matrix1, _CameraWorldPos1);
               #endif
               #ifdef NUM_POV_3
                 ProcessTrianglePOV(g, triStream, pix, _ViewProj_matrix2, _CameraWorldPos2);
               #endif
               #ifdef NUM_POV_4
                 ProcessTrianglePOV(g, triStream, pix, _ViewProj_matrix3, _CameraWorldPos3);
               #endif
               #ifdef NUM_POV_5
                 ProcessTrianglePOV(g, triStream, pix, _ViewProj_matrix4, _CameraWorldPos4);
               #endif 
               #ifdef NUM_POV_6
                 ProcessTrianglePOV(g, triStream, pix, _ViewProj_matrix5, _CameraWorldPos5);
               #endif
            }

            //Notice, we NEED to provide fragment function, so that this ShadowCaster pass works.
            //It is the same in the Official unity's mobile VertexLit shader.
            //Return type float 4, semantic is SV_Target (not SV_Depth) and returning 0.
            //https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/Mobile/Mobile-VertexLit.shader
            float4 frag(PixelInput input):SV_Target{  return 0;  }
		ENDCG
		}
    }
}