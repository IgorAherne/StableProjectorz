Shader "Custom/BlurTheTexture"
{
    Properties
    {
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
            #pragma multi_compile  ALPHA_R  ALPHA_G  ALPHA_B  ALPHA_A

            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"


            DECLARE_TEXTURE2D_OR_ARRAY(_SrcTex);
            SamplerState  sampler_point_clamp;  //IMPORTANT! otherwise bilinear would cause darker colors around outter edges.

            float4 _Tex_invSize;

            //Iterations on the sides of the box will make larger and larger steps. Allows for large cheap blur
            //From 0 to 1.  Zero will disable this effect entirely.
            float _FarSteps_amplification01;

            //Either 0 or 1.  if 1, we will skip black samples, regardless of alpha. Useful when bluring depth.
            float _SkipSample_if_zero_RGB = 0; 

            //For example 0.05  If we are bluring depth, we might want to ignore sample if its much darker than us (far along z):
            float _SkipSample_if_differenceGrtr = 1;


            
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


            //geom func. Spawns quads for every slice of the destination textureArray.
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_GeomFunc_ForBlitSlices.cginc"


            float GetRelevantChannel(float4 color)
            {
                #if defined(ALPHA_R)
                    return color.r;
                #elif defined(ALPHA_G)
                    return color.g;
                #elif defined(ALPHA_B)
                    return color.b;
                #else
                    return color.a;
                #endif
            }

            float4 applyBlur(float3 uv_withSlice, float3 stepLength){ 
                 #if BLUR_HALF_SIZE_0
                #define boxHalfSize 0 //watch out for division by zero.
                 return SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_SrcTex, uv_withSlice, _point_clamp);//will return texture, so no division by zero.

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
                 #define boxHalfSize 9

                #elif BLUR_HALF_SIZE_10 
                 #define boxHalfSize 10

                #elif BLUR_HALF_SIZE_11
                 #define boxHalfSize 11
                #else
                 #define boxHalfSize 12//NOTICE, don't Include ';'
                #endif


                float4 blurredColor = 0;//notice, 0 alpha as well! helps to maintain "emptiness" around UV chunks.
                float numSamples = 0;   //Such emptiness is useful during texture-dilation, so alpha is important.

                fixed4 mySample = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_SrcTex, uv_withSlice, _point_clamp);//will return texture, so no division by zero.
                float myRelevantChannel = GetRelevantChannel(mySample);

                if(_SkipSample_if_zero_RGB){
                    if(mySample.x + mySample.y + mySample.z == 0){ discard;}
                } 

                // NOTICE: exaggerating the effect when boxHalfSize gets larger.
                // (making wider steps). Not accurate, but allows for good looking cheap blur:
                float exaggerate = lerp(1, boxHalfSize, _FarSteps_amplification01);

                for (int x = -boxHalfSize; x<=boxHalfSize; x++){
                    for (int y = -boxHalfSize; y<=boxHalfSize; y++){

                        // Maybe exaggerate, and then add a tiny value.
                        // It prevents a glitch (down-left 1 pixel shift when step is exactly 0.25 or 0.5 or 0.75).
                        float3 offset  = float3(x*exaggerate, y*exaggerate, 0);
                               offset *= stepLength + float3(0.000001,0.000001, 0); 

                        fixed4 sample = SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(_SrcTex, uv_withSlice + offset, _point_clamp);
                        float sampleRelevantChannel = GetRelevantChannel(sample);

                        //NOTICE: preventing blackness from around uv chunks from creeping into the uv chunks.
                        float include =   1;
                              include *=  1  -  ((sample.r+sample.g + sample.b) <= 0) * _SkipSample_if_zero_RGB;

                              //if we are bluring depth, we might want to ignore sample if its much darker than us (far along z):
                              include *=  abs(sampleRelevantChannel - myRelevantChannel) <= _SkipSample_if_differenceGrtr;
                              
                        blurredColor += sample.rgba*include;
                        numSamples   += include;
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