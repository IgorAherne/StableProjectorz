#ifndef TEXTURE_ARRAY_AUTODEFS
#define TEXTURE_ARRAY_AUTODEFS

    // Slice is a plane inside a texture array (which is like a stack of 2D textures).

    // These defines typically affect number
    // of vertices emitted during geom() shader.
    #if !defined(NUM_SLICES_UPTO_24) && !defined(NUM_SLICES_UPTO_16)  && !defined(NUM_SLICES_UPTO_8)
        #define NUM_SLICES_UPTO_8
    #endif


    // the exact number of texture-array planes currently used. Set by script. 
    int _NumSlices; 


    // Either textureArray or sampler2D, depending on if 'USING_TEXTURE_ARRAY' is defined
    #ifdef USING_TEXTURE_ARRAY
        #define DECLARE_TEXTURE_OR_ARRAY(TextureName)  \
            UNITY_DECLARE_TEX2DARRAY(TextureName);     
    #else                           
        #define DECLARE_TEXTURE_OR_ARRAY(TextureName)  \
            sampler2D TextureName;
    #endif


    #ifdef USING_TEXTURE_ARRAY
        #define DECLARE_TEXTURE2D_OR_ARRAY(TextureName)  \
            UNITY_DECLARE_TEX2DARRAY(TextureName);     
    #else                           
        #define DECLARE_TEXTURE2D_OR_ARRAY(TextureName)  \
            Texture2D TextureName;
    #endif

    // Depending on if 'USING_TEXTURE_ARRAY' is defined:
    //  float 3 for sampling if an array (z is the slice ix).
    //  But xy will be used if not an array.
    #ifdef USING_TEXTURE_ARRAY                                  
        #define SAMPLE_TEXTURE_OR_ARRAY(TextureName,uv_float3)  \
            UNITY_SAMPLE_TEX2DARRAY(TextureName, uv_float3)
    #else                                                       
        #define SAMPLE_TEXTURE_OR_ARRAY(TextureName,uv_float3)  \
            tex2D(TextureName, uv_float3.xy)                    
    #endif


    #ifdef USING_TEXTURE_ARRAY                                  
        #define SAMPLE_TEXTURE2D_OR_ARRAY(TextureName,uv_float3)  \
            UNITY_SAMPLE_TEX2DARRAY(TextureName, uv_float3)
    #else                                                       
        #define SAMPLE_TEXTURE2D_OR_ARRAY(TextureName,uv_float3)  \
            TextureName.Sample(uv_float3.xy);
    #endif



    // Depending on if 'USING_TEXTURE_ARRAY' is defined:
    //  float 3 for sampling if an array (z is the slice ix).
    //  But xy will be used if not an array.
    #ifdef USING_TEXTURE_ARRAY                                   
        #define SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(TextureName,uv_float3,texSampler) UNITY_SAMPLE_TEX2DARRAY_SAMPLER(TextureName, texSampler, uv_float3)
    #else                                                       
        #define SAMPLE_TEXTURE2D_OR_ARRAY_SAMPLER(TextureName,uv_float3,texSampler) TextureName.Sample(sampler##texSampler, uv_float3.xy)
    #endif


#endif