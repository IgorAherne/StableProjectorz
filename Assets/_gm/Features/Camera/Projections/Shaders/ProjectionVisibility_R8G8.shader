
// Can render straight into the R8G8 texture, telling us whether 
// a texel is visible (1) to the projection camera or not (0)
//
// R: With fade-effect applied to edges of model.
// G: without any fade effect. True (real visibility) of texel to the projector camera. 
//    Helps to identify front-facing reverse side of 3d models.
//
// Also, perform a usual shadowcaster pass, to write into depth buffer of camera. 
Shader "Custom/ProjectionVisibility_R8G8"
{
    Properties{
        _ScreenMaskTexture("ScreenSpace Mask", 2D) = "white"{} //if user was using mask for inpaint.
    }

    SubShader{
        Tags { "RenderType"="Opaque" }       

        Pass{//with adjustments, taken from https://github.com/przemyslawzaworski/Unity3D-CG-programming/blob/master/shadowcaster.shader
			
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0
            Cull Back
            ZWrite On  
            ZTest LEqual

			CGPROGRAM
			#pragma vertex vert 
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct VertexInput{
                float4 vertex : POSITION;
            };

            struct PixelInput{
                float4 vertex : SV_POSITION; //to land the vertex into the screen space
            };

            PixelInput vert(VertexInput i){
                PixelInput pix;
                pix.vertex = UnityObjectToClipPos(i.vertex);
                return pix;
            }

            //Notice, we NEED to provide fragment function, so that this ShadowCaster pass works.
            //It is the same in the Official unity's mobile VertexLit shader.
            //Return type float 4, semantic is SV_Target (not SV_Depth) and returning 0.
            //https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/Mobile/Mobile-VertexLit.shader
            float4 frag(PixelInput input):SV_Target{
                return 0;
            }
			ENDCG
        }//end Shadowcaster Pass

        Pass{
            ZWrite Off 
            ZTest Always//I'm rendering into texture space, so think Always is correct. (will be sampling depth manually here)
            Cull Off

            CGPROGRAM 
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //so that the 'SV_RenderTargetArrayIndex' works

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24

            #define USING_TEXTURE_ARRAY //<---define, not using multi_compile, because always using arrays.
                                        //for the  DECLARE_TEXTURE_OR_ARRAY
            
            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"


            sampler2D _LastCameraDepthTexture;
            sampler2D _ScreenMaskTexture;//screen-space mask, if user was using mask for inpaint.
            sampler2D _BlurredDepthEdges;
            float4 _CameraWorldPos;
            float _Dot_EdgesFade; //for example 0.3  Any dot(view,normal) that is 0.3 or less - will render as 0 visibility.

            struct VertexInput{
                float2 uv : TEXCOORD0;
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct GeometryInput{
                float2 uv : TEXCOORD0;
                float4 worldPos : POSITION;
                float3 worldNormal : TEXCOORD1;
            };

            struct PixelInput{
                float4 rasterize_into_uv_SV : SV_POSITION; //to land the vertex into the screen space
                float4 fragScreenspacePos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldView : TEXCOORD2;
                uint renderIx : SV_RenderTargetArrayIndex;
            };


            void isTriFacingCamera( float3 worldVert0, float3 worldVert1, float3 worldVert2, float3 cameraWorldPos, 
                                    out bool isLessThanZero,  out float3 viewDir0, out float3 viewDir1, out float3 viewDir2){
                // Calculate the normal of the triangle.
                // Not using the normals of the model: we want a flat normal across entire tri. 
                // Also, the 3d model might not have its own normals, so calculating manually:
                float3 edge1  = worldVert1 - worldVert0;
                float3 edge2  = worldVert2 - worldVert0;
                float3 triNorm = cross(edge1, edge2);

                viewDir0 = (cameraWorldPos - worldVert0); 
                viewDir1 = (cameraWorldPos - worldVert1);
                viewDir2 = (cameraWorldPos - worldVert2);

                float3 viewDir =  viewDir0*0.3333333f  +  viewDir1*0.3333333f  +  viewDir2*0.3333333f;
                
                float dot_nv = dot(triNorm, viewDir);
                isLessThanZero = dot_nv < 0;// Discard the triangle if it's facing away.
            }
             

            //TODO maybe check UNITY_UV_STARTS_AT_TOP before flipping uvs?  https://docs.unity3d.com/Manual/SL-BuiltinMacros.html
            
            //prepares some variables inside the PixelInput struct.
            void setup_pix(in GeometryInput g, inout PixelInput pix, float3 worldNormal, float3 worldView, uint renderIx ){

                pix.renderIx = renderIx;

                g.uv = loopUV(g.uv);//important when rendering geometry with UDIMs, that are outside the usual [0,1] range.
                
                pix.worldNormal = worldNormal;
                pix.worldView = worldView;

                // NOTICE 'fragScreenspacePos' is not marked by SV_Position, 
                // therefore we had to do ComputeScreenPos() ourselves for it. 
                // ComputeScreenPos doesn't divide by W, but we'll do it in fragment shader.
                pix.fragScreenspacePos = mul(UNITY_MATRIX_VP, g.worldPos); // Transform the vertex position to clip space
                pix.fragScreenspacePos = ComputeScreenPos(pix.fragScreenspacePos);

                // Invert the Y-coordinate of the UV maybe (for DirectX):
                //https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
                if(_ProjectionParams.x < 0){  g.uv.y = 1 - g.uv.y;  }
                
                // our uvs are marked as 'SV_Position'. 
                // Therefore Unity will automatically apply ComputeScreenPos() onto it, before frag() begins.
                // AND will divide by W (but luckily w=1 for UVs).
                // Therefore, we need to cancel-out this transformation, by making our stuff larger than necessary:
                float2 clipSpaceUV =  g.uv*2 - 1;
                pix.rasterize_into_uv_SV =  float4(clipSpaceUV, 0, 1); // Set z to zero and w to one for correct depth
                // Again, notice that we DO NOT use ComputeScreenPos() 
                // because parameter is marked by SV_Position anyway.
            }

            
            // Vertex function. Very simple, all the transformations 
            // will be happening inside the geometry shader.
            GeometryInput vert(VertexInput i){
                GeometryInput go;
                go.worldPos = mul(unity_ObjectToWorld, i.vertex);
                go.uv = i.uv;
                go.worldNormal = UnityObjectToWorldNormal(i.normal);
                return go;
            }

            // produces a triangle if one of the POV cameras is facing this triangle.  Otherwise, doesn't emit anything.
            // If a triangle is emited, we just emit one. But will do vertex transformations on it.
            // Becasue the inputs are still 'raw', we'll need to perform vertex transformations on them.
            // If there are N pov-cameras, we don't emit N triangles, but just a single tri.
            // Otherwise, multiple triangles would fight over their destination fragment, and we wouldn't achieve a sequential blend in frag().
            //
            // Also, Helps to do a manual backface cull.
            // We want to prevent current (this) projection from affecting the reverse side, and putting black color there.
            // Very useful, if users have a clone of same 3D model but rotated 180 degrees, and the back side of clone is facing us.
            // NOTICE: we can't rely on vertex normals, because polygon smoothing groups might be used, even if model is low-poly.
            [maxvertexcount(3)]
            void geom(triangle GeometryInput g[3],  inout TriangleStream<PixelInput> triStream){

                float3 worldView[3];
                bool isLessThanZero;

                isTriFacingCamera( g[0].worldPos,  g[1].worldPos,  g[2].worldPos,  _CameraWorldPos, 
                                   isLessThanZero, 
                                   worldView[0],  worldView[1],  worldView[2] );

                if(isLessThanZero){ return; }

                uint renderIx =  max(0, uv_to_renderTargIX(g[0].uv));
                
                PixelInput pix[3];
                setup_pix( g[0], pix[0], g[0].worldNormal, worldView[0], renderIx );
                setup_pix( g[1], pix[1], g[1].worldNormal, worldView[1], renderIx );
                setup_pix( g[2], pix[2], g[2].worldNormal, worldView[2], renderIx );
                triStream.Append(pix[0]);  
                triStream.Append(pix[1]);    
                triStream.Append(pix[2]);
                triStream.RestartStrip();
            }//end geom()


            float inverseLerp(float a, float b, float val){
                return (val - a) / (b - a);
            }


            float2 frag(PixelInput i) : SV_Target{
                
                i.fragScreenspacePos.xyz /= i.fragScreenspacePos.w;

                // check if the coordinate is outside the screen (if user zoomed up on a detail of meshes).
                // This ensures nothing will be tiled beyond the screen:
                float2 posFromCenter_abs = abs(i.fragScreenspacePos.xy-0.5);
                if(max(posFromCenter_abs.x, posFromCenter_abs.y)>0.5){  return 0; }

                //now, we will be sampling the screen mask using 'fragScreenspacePos' coordinate.

                // notice, '0.001f' was causing painting-through the belt of leather bag.
                // It depends on Near & Far clip planes. [0.25, 1000] needs 0.00003 (three-hundred-thousandth), it's a good value.
                // I tested it on objects of different import-scale factors (0.0001 to 1000 ratio), 
                // that were then resized into a 3x3x3 bounding box.  It also works well with projection-shader.
                // NOTICE: brining far plane closer WILL WORSEN THE QUALITY. Depth is non-linearly distributed.
                // BY BRINING FAR-PLANE CLOSER, YOU ARE SHIFTING LOW PRECISION CLOSER TO THE OBJECT. Default far plane is 1000 and is good.
                //
                // NOTICE: it's also affected by directional light. 
                // It must exist in the scene (WITH SHADOWS ENABLED), else camera depth will be severely degraded by unity.
                float depthOffset = 0.00003;
                float depth   = Linear01Depth(tex2D(_LastCameraDepthTexture, i.fragScreenspacePos.xy).r);// Sample the depth texture.
                float myDepth = Linear01Depth(i.fragScreenspacePos.z) - depthOffset;
                if(myDepth > depth){ return float2(0,0); }

                float screenMask01 =  tex2D(_ScreenMaskTexture, i.fragScreenspacePos.xy).r;
                if(screenMask01==0){ return float2(0,0); }
                
                //texel is visible to the current projection camera. Just ensure to smooth the edges:
                float smoothEdges  = tex2D(_BlurredDepthEdges, i.fragScreenspacePos.xy).r;
                float darkEdges    = saturate(1-smoothEdges);

                i.worldNormal = normalize(i.worldNormal);
                i.worldView   = normalize(i.worldView);
                float norm_dot_view = dot(i.worldNormal, i.worldView);
                      norm_dot_view = saturate( norm_dot_view );

                float invLerp_dotNV = 1- saturate(inverseLerp(_Dot_EdgesFade, -0.000002, norm_dot_view)*2);
                float visibilFaded  =  darkEdges*invLerp_dotNV;
                return float2(visibilFaded, 1); //G is 1 because texel is observable by camera (even if R is zero due to edge-fading).
            }
            ENDCG
        }
    }
    //don't have any fallback, because Diffuse would have its  own shadowcaster pass.
    //It wouldn't work for us because we have up to 6 povs, which need to be included in pass.
} 