#ifndef UDIM_AUTODEFS
#define UDIM_AUTODEFS

    //UDIM is the location of UV, that can also be outside of [0,1] range.
    //This allows artists to keep uvs of character in several textures.

    // these defines typically affect number
    // of vertices emitted during geom() shader. 
    // So they have to be mutually exclusive.
    #if defined(NUM_SLICES_UPTO_24)
        #if !defined(UVtoUdimArrSize)
            #define UVtoUdimArrSize 24
        #endif

    #elif defined(NUM_SLICES_UPTO_16)
        #if !defined(UVtoUdimArrSize)
            #define UVtoUdimArrSize 16
        #endif
         
    #else 
        #if !defined(NUM_SLICES_UPTO_8)
            #define NUM_SLICES_UPTO_8
        #endif
        #if !defined(UVtoUdimArrSize)
            #define UVtoUdimArrSize 8
        #endif 
    #endif


    float2 loopUV(float2 uv){
        // IMPORTANT. The usual frac() results in glitches when users have 
        // uv that touch the sector borders (0.0 or 1.0 or 3.0 coordinate, etc).
        // So use this function to fix this.
        return uv < 0 ? frac(min(uv+0.000001, 0)) 
                      : frac(max(0, uv-0.000001));
    }

    // each float contains (minXY, maxXY) entry of each UDIM.
    // Udims processed in this shader might be scattered in different locations,
    // So we'll use this array to find out which one we use.
    // NOTICE: Always using max size (24), because unity can't adjust count 
    // (between 8 and 24) on the fly. Even if we were to put it in the #ifdef.
    float4 _UV_toUdimIx[24];

    //returns -1 if not found in the array, else returns index [0,7]
    int uv_to_renderTargIX(float2 uv){
        int index = -1;
        float foundMatch = 0;
        for (int i=0; i<UVtoUdimArrSize; i++){
            float4 udimEntry = _UV_toUdimIx[i];
            float2 minXY = udimEntry.xy;
            float2 maxXY = udimEntry.zw;

            float2 mask = step(minXY, uv)*step(uv, maxXY);
            float isFound = float(mask.x*mask.y);
            foundMatch += isFound;
            index = max(index, i*isFound);
        }
        return foundMatch>0.01f ?  index : -1;
    }

    // the exact number of UDIMS currently used. Set by script. 
    // Helps to know number of slices in our TextureArrays.
    int _NumUDIM; 


    #undef UVtoUdimArrSize //only to be mentioned in this cginc.
#endif