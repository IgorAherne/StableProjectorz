Shader "Custom/BlurTheUVchunks"
{
    Properties{
        _Tex_invSize ("Texture Inverse Size", Vector) = (0.1, 0.1, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Overlay" "Queue"="Overlay" }
        
        LOD 100
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //so that the 'SV_RenderTargetArrayIndex' works

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            //notice _ is needed to support anything greater than 12:
            #pragma multi_compile _ BLUR_HALF_SIZE_0  BLUR_HALF_SIZE_1  BLUR_HALF_SIZE_2  BLUR_HALF_SIZE_3  BLUR_HALF_SIZE_4  BLUR_HALF_SIZE_5  BLUR_HALF_SIZE_6  BLUR_HALF_SIZE_7  BLUR_HALF_SIZE_8  BLUR_HALF_SIZE_9  BLUR_HALF_SIZE_10  BLUR_HALF_SIZE_11

            #pragma multi_compile _ USING_TEXTURE_ARRAY
            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24
            //where is the alpha stored:
            #pragma multi_compile  ALPHA_R  ALPHA_G  ALPHA_B  ALPHA_A 

            #include "UnityCG.cginc" 
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"


            DECLARE_TEXTURE_OR_ARRAY(_SrcTex);
            float4 _Tex_invSize;

            //Iterations on the sides of the box will make larger and larger steps. Allows for large cheap blur
            //From 0 to 1.  Zero will disable this effect entirely.
            float _FarSteps_amplification01;

            //Either 0 or 1.  if 1, we will skip samples that have 0 alpha.
            float _SkipSamples_that_zero_A = 0; 



            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g{
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            struct g2f{
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                uint slice : SV_RenderTargetArrayIndex;
            };


            v2g vert (appdata v){
                v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }


            //geom func. Spawns quads for every slice of the destination textureArra.
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_GeomFunc_ForBlitSlices.cginc"


            float get_myAlpha(float4 myCol){
                #ifdef ALPHA_R
                  return myCol.r;
                #elif ALPHA_G
                  return myCol.g;
                #elif ALPHA_B
                  return myCol.b;
                #elif ALPHA_A
                  return myCol.a;
                #endif
                return myCol.a;
            }


            void blur( float3 uv_withSlice,  int x,  int y,  int boxHalfSize,  float3 stepLength, float exaggerate,
                       inout float4 blurredColor,  inout int numSamples,
                       inout bool yNotZero){

                float3 offset  = float3(x*exaggerate,  y*exaggerate,  0);
                       offset *= stepLength;

                fixed4 sample  = SAMPLE_TEXTURE_OR_ARRAY(_SrcTex, uv_withSlice + offset);

                //ensure we are within chunk. If not, make any remaining subsequent y samples stay zero:
                yNotZero =  yNotZero & (get_myAlpha(sample)>0.001f);

                blurredColor += sample*yNotZero;
                numSamples   += 1*yNotZero;
            }

 

            void blur_y_downwards( float3 uv_withSlice,  int x,  int boxHalfSize,  float3 stepLength, float exaggerate,
                                   inout float4 blurredColor,  inout int numSamples,
                                   inout bool xNotZero){
                bool yNotZero = xNotZero;//initialize with whatever x has.

                for (int y=0; y>=-boxHalfSize; y--){//first loop of y starts from zero
                    blur(uv_withSlice, x, y, boxHalfSize, stepLength, exaggerate, blurredColor, numSamples, yNotZero);
                }
            }

            
            void blur_y_upwards( float3 uv_withSlice,  int x,  int boxHalfSize,  float3 stepLength, float exaggerate,
                                 inout float4 blurredColor,  inout int numSamples,
                                 inout bool xNotZero ){
                bool yNotZero = xNotZero;

                for (int y=1; y<=boxHalfSize; y++){//second loop of y starts from 1
                    blur(uv_withSlice, x, y, boxHalfSize, stepLength, exaggerate, blurredColor, numSamples, yNotZero);
                }
            }


            float4 applyBlur(float3 uv_withSlice, float3 stepLength){
                #define boxHalfSize 0

                #ifdef BLUR_HALF_SIZE_0//watch out for division by zero:
                 return SAMPLE_TEXTURE_OR_ARRAY(_SrcTex, uv_withSlice);//will return texture, so no division by zero.
                #elif BLUR_HALF_SIZE_1
                 #define boxHalfSize 1

                #elif BLUR_HALF_SIZE_2
                 #define boxHalfSize 2

                #elif BLUR_HALF_SIZE_3
                 #define boxHalfSize 3

                #elif BLUR_HALF_SIZE_4
                 #define boxHalfSize 4

                #elif BLUR_HALF_SIZE_5
                 #define boxHalfSize 5

                #elif BLUR_HALF_SIZE_6
                 #define boxHalfSize 6

                #elif BLUR_HALF_SIZE_7
                 #define boxHalfSize 7

                #elif BLUR_HALF_SIZE_8 
                 #define boxHalfSize 8

                #elif BLUR_HALF_SIZE_9
                 #define boxHalfSize 9 //NOTICE, don't Include ';'

                #elif BLUR_HALF_SIZE_10
                 #define boxHalfSize 10 //NOTICE, don't Include ';'

                #elif BLUR_HALF_SIZE_11
                 #define boxHalfSize 11 //NOTICE, don't Include ';'
                #else
                  #define boxHalfSize 12
                #endif
                 
                float4 blurredColor = 0;//notice, 0 alpha as well! helps to maintain "emptiness" around UV chunks.
                float numSamples = 0;   //Such emptiness is useful during texture-dilation, so alpha is important.

                fixed4 mySample = SAMPLE_TEXTURE_OR_ARRAY(_SrcTex, uv_withSlice);//will return texture, so no division by zero.
                if(_SkipSamples_that_zero_A  &&  get_myAlpha(mySample)==0){
                    discard;
                }
                // NOTICE: exaggerating the effect when boxHalfSize gets larger.
                // (making wider steps). Not accurate, but allows for good looking cheap blur:
                float exaggerate = lerp(1, boxHalfSize, _FarSteps_amplification01);

                { //USING {} scope because shader warns that same 'x' is used in several places.
                    bool xNotZero = true; 
                    for (int x=0; x>=-boxHalfSize; x--){
                        blur_y_downwards(uv_withSlice, x, boxHalfSize, stepLength, exaggerate, blurredColor, numSamples, xNotZero);
                        blur_y_upwards(uv_withSlice, x, boxHalfSize, stepLength, exaggerate, blurredColor, numSamples, xNotZero);
                    }
                }

                {
                    bool xNotZero = true;
                    for (int x=1; x<=boxHalfSize; x++){//second loop of x starts from 1
                        blur_y_downwards(uv_withSlice, x, boxHalfSize, stepLength, exaggerate, blurredColor, numSamples, xNotZero);
                        blur_y_upwards(uv_withSlice, x, boxHalfSize, stepLength, exaggerate, blurredColor, numSamples, xNotZero);
                    }
                }

                blurredColor /= max(1,numSamples);
                return blurredColor;
            }


            fixed4 frag (g2f i) : SV_Target {
                float3 uv_withSlice = float3(i.uv, i.slice);
                float3 stepLength  = float3(_Tex_invSize.xy, 0);

                float4 blurred = applyBlur(uv_withSlice, stepLength);
                return blurred;
            }
            ENDCG
        }
    }
}
 