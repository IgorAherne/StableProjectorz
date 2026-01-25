
// Looks at the objects wrapped in uv-texture (texture contains brushed mask, for inpaint).
// Returns 1 everywhere where the uv-texture is non-zero, or 0 otherwise.
// This helps us find the silhuette of the mask, as seen by the camera.
//
// The second channel (G) contains dot(view,normal), 
// showing us how much surface is facing towards the camera.
Shader "Unlit/Inpaint_UV_Mask_to_screenMask"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Back  

        Pass{
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma require setrtarrayindexfromanyshader //so that the 'SV_RenderTargetArrayIndex' works


            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24
            
            // We can discard the mask unless the normals point towards camera:
            #pragma multi_compile  __  ONLY_WHERE_NORMALS_OK  

            #include "UnityCG.cginc"

            #define USING_TEXTURE_ARRAY
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"


            DECLARE_TEXTURE2D_OR_ARRAY(_ObjectUV_MaskTex); //brushed/painted colors, in uv space

            SamplerState  sampler_point_clamp;

            float _IsFullyWhite;
            
            //0: produce "either 0 or 1" opacity, 
            //1: produce smooth value between [0,1] based on alpha.
            float _isColorlessMask;


            struct appdata{
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };


            struct v2g{
                float2 uv : TEXCOORD0;
                float3 objVertex : TEXCOORD2;
                float3 objViewDir : TEXCOORD3;
                uint slice : TEXCOORD5; //NOTICE: just a TEXCOORD, no need for 'SV_RenderTargetArrayIndex'
            };                          //because we are rendering to screen, and will only use the slice for sampling

            
            struct g2f{
                float4 vertex : SV_POSITION;
                float3 objNormal : TEXCOORD0;
                float3 objViewDir : TEXCOORD1;
                float2 uv : TEXCOORD2;
                uint slice : TEXCOORD3;
            };


            v2g vert (appdata v){
                v2g o;
                o.objVertex = v.vertex;
                
                o.slice = max(0, uv_to_renderTargIX(v.uv));
                //AFTER slice is determined, loop the uv from UDIM into a [0,1] range. 
                //This will help us to sample from any slice.
                o.uv = loopUV(v.uv);

                o.objViewDir = ObjSpaceViewDir(v.vertex);
                return o; 
            }


            g2f Init_pixelInput(in v2g g, float3 objNormal, float3 objViewDir){
                g2f pix;
                
                pix.uv = g.uv;
                pix.slice  = g.slice;
                pix.vertex = UnityObjectToClipPos(g.objVertex);
                pix.objNormal = objNormal;
                pix.objViewDir = objViewDir;

                return pix;
            }

            [maxvertexcount(3)]
            void geom( triangle v2g vertices[3],  inout TriangleStream<g2f> triStream ){
                // Calculate the normal of the triangle
                float3 edge1 = vertices[1].objVertex - vertices[0].objVertex;
                float3 edge2 = vertices[2].objVertex - vertices[0].objVertex;
                float3 objNormal = cross(edge1, edge2);//normalize not needed.

                float3 objViewDir = vertices[0].objViewDir*0.3333333f 
                                  + vertices[1].objViewDir*0.3333333f 
                                  + vertices[2].objViewDir*0.3333333f;
                // Discard the triangle if it's facing away
                bool isFacing =  dot(objNormal, objViewDir) >= 0;
                if( !isFacing ){ return; }

                for (int i=0; i<3; i++){
                    g2f pix = Init_pixelInput(vertices[i], objNormal, objViewDir);
                    triStream.Append(pix);
                }
                triStream.RestartStrip();
            }
            
            
            float invLerp(float a, float b, float t){
                return saturate( (t-a)/(b-a) );
            }


            float4 frag (g2f i) : SV_Target{
                float3 uv_withSliceIx =  float3(i.uv.xy, i.slice);
                 
                //IMPORTANT! sample via Point, not Bilinear! Otherwise dilation will have issues.
                float objectUvMask_a  = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_ObjectUV_MaskTex, uv_withSliceIx, _point_clamp).a;
                
                float strict = objectUvMask_a > 0 ?   float4(1,1,1,1) : float4(0,0,0,0);
                float smooth = objectUvMask_a;

                float mask = lerp(strict, smooth, _isColorlessMask);
                //sometimes we need to output complete white for 
                //entire object silhouette, regardless of texture:
                mask = _IsFullyWhite>0? 1 : mask; 

                // #ifdef ONLY_WHERE_NORMALS_OK
                //     float viewDotNorm = dot(normalize(i.objNormal), normalize(i.objViewDir));
                //           viewDotNorm = invLerp(0.6, 0.65, viewDotNorm);
                //     mask *= viewDotNorm;
                // #endif

                return float4(mask,0,0,0); 
            }
            ENDCG
        }
    }//end SubShader
}
