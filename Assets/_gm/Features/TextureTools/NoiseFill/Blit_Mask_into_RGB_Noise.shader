
//for Graphics.Blit
// Done in two passes:
// 1) put uniform RGB noise where the mask is white
// 2) sample randomly around uv coordinate if inside mask
//
// The second step "steals" nearby colors, that are hopefully around mask
Shader "Unlit/Blit_Mask_into_RGB_Noise"
{
    Properties
    {
        _NoiseRGB_Tex ("Noise (RGB)", 2D) = "white" {}
    }
    SubShader{
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ SAMPLE_RANDOMLY

            #include "UnityCG.cginc"

            sampler2D _SrcTex;
            sampler2D _MaskTex;
            Texture2D _NoiseRGB_Tex;

            SamplerState sampler_point_repeat;


            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };


            v2f vert (appdata v){
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            
            float4 frag (v2f i) : SV_Target{

                #ifndef SAMPLE_RANDOMLY // if NOT defined.
                    float4 srcCol   = tex2D(_SrcTex, i.uv);
                    float4 noiseCol = _NoiseRGB_Tex.Sample(sampler_point_repeat, i.uv);
                    float mask01    = tex2D(_MaskTex, i.uv).r;
                    return lerp(srcCol, noiseCol, mask01); 

                #else
                    //else SAMPLE_RANDOMLY (usually second pass, our _SrcTex already contains noise inside silhuette).

                    //get random offsets (the full and also the nearby of current uv).
                    float2 noise2    = _NoiseRGB_Tex.Sample(sampler_point_repeat, (i.uv + float2(0.01, 0.03)) ).rg;
                    float2 randOffset= noise2.xy*2 - 1;
                    float2 randomUV  = frac(i.uv + randOffset);
                    float2 randomUV_near = saturate( i.uv + randOffset*0.2);

                    //use nearby-random uv to see if we are still in the mask
                    float randMask = tex2D(_MaskTex, randomUV_near).r;

                    // Sample SrcTex nearby. But if nearby coordinate already took 
                    // us inside mask, sample completely randomly in attempt to get out of it and get overall image:
                    float4 rnd_srcCol =  tex2D(_SrcTex, lerp(randomUV_near, randomUV, randMask));

                    //only allow inside the mask, else just return SrcColor from prev pass 'as is':
                    float mask01    = tex2D(_MaskTex, i.uv).r;
                    float4 srcCol   = tex2D(_SrcTex, i.uv);
                    return lerp(srcCol, rnd_srcCol, mask01);
                #endif
            }
            ENDCG
        }
    }//end SubShader
}
