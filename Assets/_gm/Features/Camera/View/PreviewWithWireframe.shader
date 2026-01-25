// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
// from https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/VR/Shaders/SpatialMappingWireframe.shader
// This is adjusted version, allowing to tweak color of Wireframe, make it barely visible (alpha of color).
// Allows for a texture.
Shader "Custom/PreviewWithWireframe (Unlit Texture)"
{
    Properties 
    {
        [Header(Wireframe)]
        _WireThickness ("Wire Thickness", RANGE(0, 800)) = 100
        _WireColor("Wireframe Color", Color) = (1,1,1,1)
    } 

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off

        Pass
        {
            // Wireframe shader based on the the following
            // http://developer.download.nvidia.com/SDK/10/direct3d/Source/SolidWireframe/Doc/SolidWireframe.pdf

            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //so that the 'SV_RenderTargetArrayIndex' works

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma multi_compile  _  SAMPLER_POINT //Point-filtering (for pixelated look), else Bilinear
            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24
          
            #define USING_TEXTURE_ARRAY  //for the  DECLARE_TEXTURE2D_OR_ARRAY.  define because Always using texture arrays

            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"


            DECLARE_TEXTURE2D_OR_ARRAY(_MainTex) //depending on  'multi_compile USING_TEXTURE_ARRAY'
            float _WireThickness;
            fixed4 _WireColor;

            #ifdef SAMPLER_POINT
              SamplerState sampler_PointClamp;
            #else
              SamplerState sampler_LinearClamp;
            #endif

            // allows us to change the from the final position into uv space-position.
            // This allows us to achieve a cool animation effect where the model's fragments 
            // morph from 3D into UV representation.
            // Set as a Shader.SetGlobalFloat()
            float _GLOBAL_WarpIntoUVSpace01;
            float _GLOBAL_inv_cameraAspect01; // height/width
            float4 _GLOBAL_InspectUV_Navigate; // XY: offset, ZW: scale (for zooming)

            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            }; 

            struct v2g{
                float4 projectionSpaceVertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            }; 

            struct g2f{
                float4 projectionSpaceVertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 dist : TEXCOORD1;
                uint renderIx : SV_RenderTargetArrayIndex;
            };


            //func
            v2g vert (appdata v){
                v2g o;
                o.projectionSpaceVertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }



            float4 get_uv_pos(float2 uv) {
                // First flip Y and do aspect ratio correction
                float2 flippedUV = float2(uv.x * _GLOBAL_inv_cameraAspect01, 1-uv.y);

                // Center on screen
                float offsetX = (1.0 - _GLOBAL_inv_cameraAspect01) * 0.5;
                flippedUV.x += offsetX;

                // Apply pan offset
                flippedUV += _GLOBAL_InspectUV_Navigate.xy;

                // Convert to clip space
                float2 clipUV = flippedUV * 2.0 - 1.0;
    
                // Apply zoom in clip space (where 0,0 is screen center)
                clipUV = clipUV / _GLOBAL_InspectUV_Navigate.z;

                return float4(clipUV.x, clipUV.y, 0.5, 1);
            }


            //func
            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triangleStream)
            {
                uint renderIx = max(0, uv_to_renderTargIX(IN[0].uv)); //determine UDIM sector, before doing loopUV(uv).

                float2 p[3];
                for(int i=0; i<3; ++i){
                    float2 loopedUV = loopUV(IN[i].uv);//so that UDIMs can sample textures like usual, in [0,1] space.
                    IN[i].uv =  _GLOBAL_WarpIntoUVSpace01>0? IN[i].uv : loopedUV;
                    p[i] = IN[i].projectionSpaceVertex.xy / IN[i].projectionSpaceVertex.w;
                }
                float2 edge[3] =  { p[2]-p[1],  p[2]-p[0],  p[1]-p[0] };

                // To find the distance to the opposite edge, we take the
                // formula for finding the area of a triangle Area = Base/2 * Height,
                // and solve for the Height = (Area * 2)/Base.
                // We can get the area of a triangle by taking its cross product
                // divided by 2.  However we can avoid dividing our area/base by 2
                // since our cross product will already be double our area.
                float area = abs(edge[1].x * edge[2].y - edge[1].y * edge[2].x);
                float wireThickness = 800 - _WireThickness;

                g2f o;
                //first vertex:
                float4 uv_pos = get_uv_pos(IN[0].uv);
                o.projectionSpaceVertex = lerp(IN[0].projectionSpaceVertex, uv_pos, _GLOBAL_WarpIntoUVSpace01);

                o.dist.xyz = float3( (area / length(edge[0])), 0.0, 0.0) * o.projectionSpaceVertex.w * wireThickness;
                o.dist.w = 1.0 / o.projectionSpaceVertex.w;
                o.uv = IN[0].uv;
                o.renderIx = renderIx;
                triangleStream.Append(o);

                //second vertex:
                uv_pos = get_uv_pos(IN[1].uv);
                o.projectionSpaceVertex = lerp(IN[1].projectionSpaceVertex, uv_pos, _GLOBAL_WarpIntoUVSpace01);

                o.dist.xyz = float3(0.0, (area / length(edge[1])), 0.0) * o.projectionSpaceVertex.w * wireThickness;
                o.dist.w = 1.0 / o.projectionSpaceVertex.w;
                o.uv = IN[1].uv;
                o.renderIx = renderIx;
                triangleStream.Append(o);

                //third vertex:
                uv_pos = get_uv_pos(IN[2].uv);
                o.projectionSpaceVertex = lerp(IN[2].projectionSpaceVertex, uv_pos, _GLOBAL_WarpIntoUVSpace01);

                o.dist.xyz = float3(0.0, 0.0, (area / length(edge[2]))) * o.projectionSpaceVertex.w * wireThickness;
                o.dist.w = 1.0 / o.projectionSpaceVertex.w;
                o.uv = IN[2].uv;
                o.renderIx = renderIx;
                triangleStream.Append(o);
                triangleStream.RestartStrip();
            }



            fixed4 frag (g2f i) : SV_Target{
                float minDistanceToEdge = min(i.dist[0], min(i.dist[1], i.dist[2])) * i.dist[3];

                #ifdef SAMPLER_POINT 
                  float4 texCol =   SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_MainTex, float3(i.uv.xy, i.renderIx), _PointClamp);
                #else
                  float4 texCol =   SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_MainTex, float3(i.uv.xy, i.renderIx), _LinearClamp);
                #endif

                // Smooth our line out 
                float t = exp2(-2 * minDistanceToEdge * minDistanceToEdge); 
                      t *= _WireColor.a;

                fixed4 finalColor = lerp(texCol, _WireColor, t);
                finalColor.a = 1; 

                return finalColor; 
            }
            ENDCG
        }
    }
} 