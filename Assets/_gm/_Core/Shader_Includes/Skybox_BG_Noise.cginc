#ifndef SKYBOX_BG_NOISE
#define SKYBOX_BG_NOISE

// used to animate noise as waves. Used for Backgrounds/skyboxes, to show where it's transparent.
float4 animated_color_noise(float2 texcoord, sampler2D perlinBlobsTex, sampler2D rgb_NoiseTexture, 
                            float4 noiseColor, float noiseSpeed, float aspectRatio){
    float2 noiseUV  = texcoord * float2(1.4,1);
                 
    //blo1 is BIG uvs (*0.5)
    float2 blob1_uv =  pow(noiseUV*0.35, 1.5)  +  float2(_Time.y*0.2, _Time.y*1)*noiseSpeed; 
    float2 blob2_uv =  pow(noiseUV*0.5, 1.4)  +  float2(_Time.y*1.35, _Time.y*1.2)*noiseSpeed;
    float2 blob3_uv =  pow(noiseUV*1.5, 1.2)  +  float2(_Time.y*1.3, _Time.y*1.3)*noiseSpeed*2;

    float4 blob1_tex   =  tex2D(perlinBlobsTex, blob1_uv).rrrr;
    float4 blobsNoise1 = blob1_tex;

    blob2_uv += float2(blobsNoise1.r*0.1, -blobsNoise1.g*0.07);
    float4 blob2_tex   =  tex2D(perlinBlobsTex, blob2_uv).rrrr;
    float4 blobsNoise2 = blob2_tex;

    blob3_uv += float2(blobsNoise2.r*0.1, -blobsNoise2.g*0.07);
    float4 blob3_tex   =  tex2D(perlinBlobsTex, blob3_uv).rrrr;
    float4 blobsNoise_small = blob3_tex;
                
    float4 noise = float4(tex2D(rgb_NoiseTexture, noiseUV * 0.2).rgb, 1);

    blobsNoise_small = 1-pow(blobsNoise_small,0.5);//smallest fractal
    blobsNoise1      = 1-pow(blobsNoise1,1.5*blobsNoise_small);
    blobsNoise2      = 1-pow(blobsNoise2,1.0*blobsNoise_small);//large fractal
    
    float4 shine     = 4*pow(saturate(abs(blobsNoise1 * blobsNoise2)), 0.7) + blobsNoise2*1;
               
    noise = lerp(noise.rrrr*0,  noise.rrrr*noiseColor.rgba,  shine);
    return noise;
}

#endif