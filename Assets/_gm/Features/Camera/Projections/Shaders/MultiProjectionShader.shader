
//Can grab the texture from screen and land it in correct uv location of an object.
//Works with several Point-of-view (POV), allowing to cast up to 6 projections from different directions. 
Shader "Custom/MultiProjectionShader"
{
    Properties{
        _ScreenArtTexture("ScreenSpace-Art Texture", 2D) = "white"{} 
        _TintColorCurrProjection("ScreenSpace-Art TintColor and Opacity", Color) = (1.0, 1.0, 1.0, 1.0)
        _HSV_and_Contrast("HueShift, Saturation, Value, Contrast", Vector) = (0,1,1,1)

        _ScreenMaskTexture("ScreenSpace Mask", 2D) = "white"{} //if user was using mask for inpaint.
    }

    SubShader{
        
        Pass{//with adjustments, taken from https://github.com/przemyslawzaworski/Unity3D-CG-programming/blob/master/shadowcaster.shader
			
            Name "ShadowCaster"
            Tags { "RenderType"="Opaque" "LightMode" = "ShadowCaster" }
            Cull Off
            ZWrite On  
            ZTest LEqual
            ColorMask 0

			CGPROGRAM
            #pragma multi_compile _ NUM_POV_2  NUM_POV_3  NUM_POV_4  NUM_POV_5  NUM_POV_6

			#pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"
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
            float4 frag(PixelInput input):SV_Target{
                return 0;
            }
			ENDCG
		}


        Pass{
            Tags { "RenderType"="Transparent" }
            Cull Off
            ZWrite Off
            
            // Setup blending, for blending new stuff on top of other already existing projections.
            // I need an alpha which is to be added to the already existing alpha.
            // Yet, the current alpha also needs to "lerp" its RGB.
            BlendOp Add //<--the source and destination values will be added together.
            Blend SrcAlpha OneMinusSrcAlpha, One One//<--two separate equations, one for RGB, one for Alpha.

            CGPROGRAM 
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //so that the 'SV_RenderTargetArrayIndex' works

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma multi_compile  _ NUM_POV_2  NUM_POV_3  NUM_POV_4  NUM_POV_5  NUM_POV_6
            #pragma multi_compile  _ CURSOR_COLOR_WHITE
            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24

            // for the  DECLARE_TEXTURE_OR_ARRAY:
            #define USING_TEXTURE_ARRAY//#define, not #multi_compile, because always used for projection

            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/MultiProjectionVariables.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"

            sampler2D _LastCameraDepthTexture;
             
            sampler2D _ScreenArtTexture;//screen-space art that will be projected
            fixed4 _TintColorCurrProjection;// ScreenSpace-Art TintColor and Opacity
            float4 _HSV_and_Contrast; //hue saturation value contrast.

            sampler2D _ScreenMaskTexture;//screen-space mask, if user was using mask for inpaint.

            //for previewing the projection only inside the cursor.
            float _ScreenAspectRatio;

            sampler2D _BrushStamp;//texture of the cursor, usually circular. Shader will move it to correct screen location.
            float4 _PrevNewBrushScreenCoord;
            float4 _BrushSize_andFirstFrameFlag;
            int _Cursor_for_POV_ix; //should we preview some POV inside cursor. for example 0,1,2,3,4, or 5.

            sampler2D _MultiProj_WrongSide_Tex;


            struct VertexInput{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;    
            };


            struct GeometryInput{
                float2 uv : TEXCOORD0;
                float4 worldPos : POSITION;
            };


            struct PixelInput{
                uint renderIx : SV_RenderTargetArrayIndex;
                float2 uv : TEXCOORD0;
                float4 rasterize_into_uv_SV : SV_POSITION; //to land the vertex into the screen space

                  float4 fragScreenSpacePos_cursor : TEXCOORD1;

                  float4 fragScreenSpacePos0 : TEXCOORD2;

                #ifdef NUM_POV_2
                  float4 fragScreenSpacePos1 : TEXCOORD3;
                #endif
                #ifdef NUM_POV_3
                  float4 fragScreenSpacePos2 : TEXCOORD4;
                #endif
                #ifdef NUM_POV_4
                  float4 fragScreenSpacePos3 : TEXCOORD5;
                #endif
                #ifdef NUM_POV_5
                  float4 fragScreenSpacePos4 : TEXCOORD6;
                #endif
                #ifdef NUM_POV_6
                  float4 fragScreenSpacePos5 : TEXCOORD7;
                #endif
            };


            // Define a function to initialize PixelInput with default values
            PixelInput make_PixelInput(){
                PixelInput pi;
                pi.uv = float2(0,0);
                pi.rasterize_into_uv_SV = float4(0,0,0,0);

                  pi.fragScreenSpacePos_cursor = float4(0,0,0,0);
                
                  pi.fragScreenSpacePos0 = float4(0,0,0,0);

                #ifdef NUM_POV_2
                  pi.fragScreenSpacePos1 = float4(0,0,0,0);
                #endif
                #ifdef NUM_POV_3
                  pi.fragScreenSpacePos2 = float4(0,0,0,0);
                #endif
                #ifdef NUM_POV_4
                  pi.fragScreenSpacePos3 = float4(0,0,0,0);
                #endif
                #ifdef NUM_POV_5
                  pi.fragScreenSpacePos4 = float4(0,0,0,0);
                #endif
                #ifdef NUM_POV_6
                  pi.fragScreenSpacePos5 = float4(0,0,0,0);
                #endif
                return pi;
            }

            
            //TODO maybe check UNITY_UV_STARTS_AT_TOP before flipping uvs?  https://docs.unity3d.com/Manual/SL-BuiltinMacros.html
            //ALso see if there is a macros-check I should do instead of always doing   pix2_fragScreenSpacePos.y *= -1;
            
            //prepares some variables inside the PixelInput struct.
            void finalize_pix(in GeometryInput g, inout PixelInput pix, uint renderIx){
                
                pix.renderIx = renderIx;
                pix.uv =  g.uv;

                pix.fragScreenSpacePos_cursor =  ComputeScreenPos( mul(_CurrViewport_VP_matrix, g.worldPos) );

                // Invert the Y-coordinate of the UV maybe (for DirectX):
                //https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
                if(_ProjectionParams.x < 0){  g.uv.y = 1 - g.uv.y;  }
                
                // our uvs are marked as 'SV_Position'. 
                // Therefore Unity will automatically apply ComputeScreenPos() onto it, before frag() begins.
                // AND will divide by W (but luckily w=1 for UVs).
                // So, we need to cancel-out this transformation, by making our stuff larger than necessary:
                float2 clipSpaceUV =  g.uv*2 - 1;
                pix.rasterize_into_uv_SV =  float4(clipSpaceUV, 0, 1); // Set z to zero and w to one for correct depth
                // Again, notice that we DO NOT use ComputeScreenPos() 
                // because parameter is marked by SV_Position anyway.
            } 



            float get_Norm_dot_View(float3 worldVert0, float3 worldVert1, float3 worldVert2, float3 cameraWorldPos){
                // Calculate the normal of the triangle
                float3 edge1  = worldVert1 - worldVert0;
                float3 edge2  = worldVert2 - worldVert0;
                float3 normal = normalize(cross(edge1, edge2));

                float3 viewDir0 = (cameraWorldPos - worldVert0);
                float3 viewDir1 = (cameraWorldPos - worldVert1);
                float3 viewDir2 = (cameraWorldPos - worldVert2);

                float3 viewDir =  viewDir0*0.3333333f  +  viewDir1*0.3333333f  +  viewDir2*0.3333333f;
                       viewDir = normalize(viewDir);
                return  dot(normal, viewDir);
            }

            
            void ProcessTrianglePOV( triangle GeometryInput g[3], 
                                     inout float4 pix0_fragScreenSpacePos, 
                                     inout float4 pix1_fragScreenSpacePos,
                                     inout float4 pix2_fragScreenSpacePos,
                                     in float4x4 viewProjMatrix,  float4 cameraWorldPos,
                                     inout float largest_normDotView){

                float normDotView = get_Norm_dot_View( g[0].worldPos, g[1].worldPos, g[2].worldPos, cameraWorldPos );
                largest_normDotView = max(normDotView, largest_normDotView); 

                //notice, same matrix for all 3 verts (matrix for some specific pov):
                pix0_fragScreenSpacePos = mul(viewProjMatrix, g[0].worldPos); // Transform the vertex position to clip space
                pix0_fragScreenSpacePos = ComputeScreenPos(pix0_fragScreenSpacePos);
                // NOTICE 'pix0_fragScreenSpacePos' is not marked by SV_Position, 
                // therefore we had to do ComputeScreenPos() ourselves for it. 
                // ComputeScreenPos doesn't divide by W, but we'll do it in fragment shader.

                pix1_fragScreenSpacePos = mul(viewProjMatrix, g[1].worldPos); // Transform the vertex position to clip space
                pix1_fragScreenSpacePos = ComputeScreenPos(pix1_fragScreenSpacePos);

                pix2_fragScreenSpacePos = mul(viewProjMatrix, g[2].worldPos); // Transform the vertex position to clip space
                pix2_fragScreenSpacePos = ComputeScreenPos(pix2_fragScreenSpacePos);
            }


            
            // Vertex function. Very simple, all the transformations 
            // will be happening inside the geometry shader.
            GeometryInput vert(VertexInput i){
                GeometryInput go;
                go.worldPos = mul(unity_ObjectToWorld, i.vertex);
                go.uv = i.uv;
                return go;
            }

            // produces a triangle if one of the POV cameras is facing this triangle.  Otherwise, doesn't emit anything.
            // If a triangle is emited, we just emit one. But will do vertex transformations on it.
            // Becasue the inputs are still 'raw', we'll need to perform vertex transformations on them.
            //
            // If there are N pov-cameras, we don't emit N triangles, but just a single tri.
            // Otherwise, multiple triangles would fight over their destination fragment, and we wouldn't achieve a sequential blend in frag().
            // However, our triangle contains 6 screen transformations. It will use them inside fragment shader.
            //
            // Also, Helps to do a manual backface cull.
            // We want to prevent current (this) projection from affecting the reverse side, and putting black color there.
            // Very useful, if users have a clone of same 3D model but rotated 180 degrees, and the back side of clone is facing us.
            // NOTICE: we can't rely on vertex normals, because polygon smoothing groups might be used, even if model is low-poly.
            [maxvertexcount(3)]
            void geom(triangle GeometryInput g[3],  inout TriangleStream<PixelInput> triStream){
                
                uint renderIx =  max(0, uv_to_renderTargIX(g[0].uv));
                
                for(int i=0; i<3; ++i){  g[i].uv = loopUV(g[i].uv); }//so that UDIMs can sample textures like usual, in [0,1] space.

                PixelInput pix[3];//will be reused and its copies pasted into triangle stream.
                pix[0] = make_PixelInput();
                pix[1] = make_PixelInput();
                pix[2] = make_PixelInput();

                float largest_normDotView = 0;
                
                    ProcessTrianglePOV( g, pix[0].fragScreenSpacePos0,  pix[1].fragScreenSpacePos0,  pix[2].fragScreenSpacePos0, 
                                        _ViewProj_matrix0,  _CameraWorldPos0,  largest_normDotView );

                #ifdef NUM_POV_2
                    ProcessTrianglePOV( g, pix[0].fragScreenSpacePos1,  pix[1].fragScreenSpacePos1,  pix[2].fragScreenSpacePos1, 
                                        _ViewProj_matrix1,  _CameraWorldPos1,  largest_normDotView );
                #endif
                #ifdef NUM_POV_3
                    ProcessTrianglePOV( g, pix[0].fragScreenSpacePos2,  pix[1].fragScreenSpacePos2,  pix[2].fragScreenSpacePos2, 
                                        _ViewProj_matrix2,  _CameraWorldPos2,  largest_normDotView );
                #endif
                #ifdef NUM_POV_4
                    ProcessTrianglePOV( g, pix[0].fragScreenSpacePos3,  pix[1].fragScreenSpacePos3,  pix[2].fragScreenSpacePos3,
                                        _ViewProj_matrix3,  _CameraWorldPos3,  largest_normDotView );
                #endif
                #ifdef NUM_POV_5
                    ProcessTrianglePOV( g,  pix[0].fragScreenSpacePos4,  pix[1].fragScreenSpacePos4,  pix[2].fragScreenSpacePos4, 
                                        _ViewProj_matrix4,  _CameraWorldPos4,  largest_normDotView );
                #endif
                #ifdef NUM_POV_6
                    ProcessTrianglePOV( g,  pix[0].fragScreenSpacePos5,  pix[1].fragScreenSpacePos5,  pix[2].fragScreenSpacePos5, 
                                        _ViewProj_matrix5,  _CameraWorldPos5,  largest_normDotView );
                #endif
                // NOTICE: we never cull, EVEN IF TRIANGLE FACES AWAY.  
                // We might need to display "wrong side" text later on, on reverse side
                finalize_pix(g[0], pix[0], renderIx);
                finalize_pix(g[1], pix[1], renderIx);
                finalize_pix(g[2], pix[2], renderIx);
                triStream.Append(pix[0]);   triStream.Append(pix[1]);    triStream.Append(pix[2]);
            }//end geom()



            #include "Assets/_gm/_Core/Shader_Includes/ShaderEffects.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/BrushEffects.cginc"

            struct Frag_processPOV_arg{
                float4 fragScreenspacePos;
                float4 fragScreenSpacePos_cursor;
                float3 objUV;
                Texture2DArray uvMask;
                Texture2DArray projectionVisibility;
                bool preview_inCursor;
            }; 


            struct Frag_processPOV_out{
                fixed4 colorSoFar;
                float totalMaskWeight;
                float total_Mask_vs_Invisibility;
                float4 special_color;
                float isEnd_with_special_color;
                // float debugA;  //commented out. Keep here in case you need to debug something.
                // float debugB;
            };

            //find out where the cursor-circle is. 
            //We can show a preview of some pov, inside it.
            float MaskByCursor( float2 fragScreenSpacePos ){
                PaintInBrushStroke_Input pibs;
                pibs.screenAspectRatio  = _ScreenAspectRatio;
                pibs.fragScreenSpaceUV  = fragScreenSpacePos.xy;
                pibs.PrevNewBrushScreenCoord = _PrevNewBrushScreenCoord;
                pibs.BrushSizes_andFirstFrameFlag = _BrushSize_andFirstFrameFlag;
                pibs.BrushStamp    = _BrushStamp;
                pibs.brushStampStronger = 0;
                pibs.BrushStrength01    = 0; //doesn't matter
                pibs.currentBrushPath01 = 0; //doesn't matter.
                float mask01 = Mask_by_CurrBrushCursor(pibs);
                return mask01;
            }


            void FragColor_POV( in Frag_processPOV_arg a,  inout Frag_processPOV_out o ){

                //Art that we want to "shine". Sample the screen texture with the screen-space uvs:
                float4 screenArtColor = tex2D(_ScreenArtTexture, a.fragScreenspacePos.xy);
                       screenArtColor = EffectsPostProcess(screenArtColor, _HSV_and_Contrast);
                       screenArtColor.rgb *= _TintColorCurrProjection.rgb;

                float2 wrongSide_uv = a.fragScreenSpacePos_cursor.xy*9;
                wrongSide_uv.x *= _ScreenAspectRatio;
                float4 wrongSideText = tex2D(_MultiProj_WrongSide_Tex, wrongSide_uv);

                float artOpacity =  _TintColorCurrProjection.a;

                float screenMask =  tex2D(_ScreenMaskTexture, a.fragScreenspacePos.xy).r;
                 
                float2 visibility     = UNITY_SAMPLE_TEX2DARRAY_SAMPLER(a.projectionVisibility,  _linear_repeat,  a.objUV).rg;
                float visibilityFaded = visibility.r;//Fades to black closer to edges of 3d surfaces. Doesn't know about front or reverse side.
                float visibilityReal  = visibility.g;//front-facing side of geometry will have 1, reverse side will have 0.
                float invisibility    = 1.0 - visibilityFaded; //[0,1], greater when visibilityFaded is darker.

                float uvBrushMask_02 =  UNITY_SAMPLE_TEX2DARRAY_SAMPLER(a.uvMask,  _linear_repeat,  a.objUV).r * 2; //[0,1] --> [0,2]. Anything above 1 is for fighting the invisibility.
                float uvMask_vs_invisibility =  saturate(uvBrushMask_02-invisibility) * visibilityReal;

                float cursorMask = MaskByCursor(a.fragScreenSpacePos_cursor.xy);

                // using two summands gives the wrongSideText "a second chance to appear".
                // Otherwise, multiplying all three would cause text to disappear.
                float wrongTextFactor =  (1-screenMask)  +  cursorMask*(1-visibilityReal);
                      wrongTextFactor =  saturate(wrongTextFactor);

                #ifdef CURSOR_COLOR_WHITE
                    float ownCursorFactor01 =  a.preview_inCursor*cursorMask;
                #else //black:
                    float ownCursorFactor01 =  a.preview_inCursor*cursorMask*wrongTextFactor;
                #endif

                float4 myOwn_specialColor = screenArtColor*artOpacity;
                       myOwn_specialColor = lerp(myOwn_specialColor, wrongSideText, wrongTextFactor);

                o.special_color =  ownCursorFactor01 > 0?  myOwn_specialColor  :  o.special_color;

                o.isEnd_with_special_color =  max(o.isEnd_with_special_color,  ownCursorFactor01 );
                
                //all additiveMasks will add up to a maximum of 1. 
                //So we can simply add to the current color:
                // NOTICE: not using  (artOpacity * screenMask) for color, only for weight.  
                // Otherwise, color becomes black around borders of projection, which looks ugly.
                o.colorSoFar      +=  uvMask_vs_invisibility * screenArtColor;
                o.totalMaskWeight +=  artOpacity * screenMask * uvMask_vs_invisibility;
                o.total_Mask_vs_Invisibility +=  uvMask_vs_invisibility;

                // o.debugA = visibilityFaded;
                // o.debugB = visibilityReal; 
            }  


            // bool is_fragPOV_visible( in Frag_processPOV_arg a ){
            //     // check if the coordinate is outside the screen (if user zoomed up on a detail of meshes).
            //     // This ensures nothing will be tiled beyond the screen:
            //     if( isOutsideScreen_or_behind(a.fragScreenspacePos) ){ return false;}
                
            //     //now, we will be sampling the screen art using 'fragScreenSpacePos' coordinate.

            //     // notice, '0.001f' was causing painting-through the belt of leather bag.
            //     // It depends on Near & Far clip planes. [0.25, 1000] needs 0.00003 (three-hundred-thousandth), it's a good value.
            //     // I tested it on objects of different import-scale factors (0.0001 to 1000 ratio), 
            //     // that were then resized into a 3x3x3 bounding box.  It also works well with projection-shader.
            //     // NOTICE: brining far plane closer WILL WORSEN THE QUALITY. Depth is non-linearly distributed.
            //     // BY BRINING FAR-PLANE CLOSER, YOU ARE SHIFTING LOW PRECISION CLOSER TO THE OBJECT. Default far plane is 1000 and is good.
            //     //
            //     // NOTICE: it's also affected by directional light. 
            //     // It must exist in the scene (WITH SHADOWS ENABLED), else camera depth will be severely degraded by unity.
            //     float depthOffset = 0.00003;
            //     float depth   = Linear01Depth(tex2D(_LastCameraDepthTexture, a.fragScreenspacePos.xy).r);// Sample the depth texture.
            //     float myDepth = Linear01Depth(a.fragScreenspacePos.z) - depthOffset;
            //     if(myDepth > depth){ return false; }
            //     return true;
            // }


           
            #define LAUNCH_FRAG_COLOR_POV(ix, previewInCursor)               \
            {/*opens scope, to prevent redifinitions*/                       \
                Frag_processPOV_arg arg;                                     \
                arg.fragScreenspacePos = i.fragScreenSpacePos##ix;           \
                arg.fragScreenSpacePos_cursor = i.fragScreenSpacePos_cursor; \
                arg.objUV  =  float3(i.uv, i.renderIx);                      \
                arg.uvMask = _POV##ix##_additive_uvMask;                     \
                arg.projectionVisibility   = _POV##ix##_ProjVisibility;      \
                arg.preview_inCursor   = previewInCursor;                    \
                FragColor_POV( arg, o);                                      \
            }


            float4 frag(PixelInput i) : SV_Target{    
                
                i.fragScreenSpacePos_cursor.xyzw /= i.fragScreenSpacePos_cursor.w;

                Frag_processPOV_out o;
                o.colorSoFar = fixed4(0,0,0,0);
                o.totalMaskWeight   = 0;
                o.total_Mask_vs_Invisibility = 0;
                o.special_color = fixed4(0,0,0,0);
                o.isEnd_with_special_color = 0;
                // o.debugA = 0;
                // o.debugB = 0; //commented out. Keep here in case you need to debug something.

                // float debugAA=0; float debugBB=0; 

				{
                    bool previewInCursor =  _Cursor_for_POV_ix == 0;
                    i.fragScreenSpacePos0.xyzw /= i.fragScreenSpacePos0.w; 
                    LAUNCH_FRAG_COLOR_POV(0, previewInCursor)
                  #ifdef NUM_POV_2                                         
                    previewInCursor =  _Cursor_for_POV_ix == 1;
                    i.fragScreenSpacePos1.xyzw /= i.fragScreenSpacePos1.w; 
                    LAUNCH_FRAG_COLOR_POV(1, previewInCursor)
                    // debugAA = o.debugA;
                    // debugBB = o.debugB;
                  #endif                                                   
                  #ifdef NUM_POV_3                            
                    previewInCursor =  _Cursor_for_POV_ix == 2;
                    i.fragScreenSpacePos2.xyzw /= i.fragScreenSpacePos2.w; 
                    LAUNCH_FRAG_COLOR_POV(2, previewInCursor)
                  #endif                                                   
                  #ifdef NUM_POV_4                          
                    previewInCursor =  _Cursor_for_POV_ix == 3;
                    i.fragScreenSpacePos3.xyzw /= i.fragScreenSpacePos3.w; 
                    LAUNCH_FRAG_COLOR_POV(3, previewInCursor)
                  #endif                                                   
                  #ifdef NUM_POV_5                         
                    previewInCursor =  _Cursor_for_POV_ix == 4;
                    i.fragScreenSpacePos4.xyzw /= i.fragScreenSpacePos4.w; 
                    LAUNCH_FRAG_COLOR_POV(4, previewInCursor)
                  #endif                                                   
                  #ifdef NUM_POV_6                         
                    previewInCursor =  _Cursor_for_POV_ix == 5;
                    i.fragScreenSpacePos5.xyzw /= i.fragScreenSpacePos5.w; 
                    LAUNCH_FRAG_COLOR_POV(5, previewInCursor)
                  #endif                                                   
                }
                //NOTICE: we NEVER DISCARD BY DEPTH. Might have to show "Wrong Side" text on the reverse side.
                
                // Slap our new projection on top of everything rendered before.
                // Control the amount by the totalMaskWeight. 
                // Remember blending will be Transparent, 'SrcAlpha OneMinusSrcAlpha'.

                // Ensure the 'color so far' isn't too black, if we were masking-away some layers.
                // (in that case total sum is less than 1, which would cause black borders around brush strokes)
                o.colorSoFar.rgb /= max(o.total_Mask_vs_Invisibility, 0.000001);
                o.colorSoFar.a    = o.totalMaskWeight;

                o.special_color.a = 1; //<--to show inside cursor fully

                return lerp( o.colorSoFar,  o.special_color,  o.isEnd_with_special_color );
            }//end frag()
            ENDCG
        } 
    }
    //don't have any fallback, because Diffuse would have its  own shadowcaster pass.
    //It wouldn't work for us because we have up to 6 povs, which need to be included in pass.
} 