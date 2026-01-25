
    // for the  DECLARE_TEXTURE_OR_ARRAY:
    #define USING_TEXTURE_ARRAY//#define, not #multi_compile, because always used for projection

    #include "UnityCG.cginc"
    #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
    #include "Assets/_gm/_Core/Shader_Includes/UDIM_AutoDefines.cginc"


    DECLARE_TEXTURE_OR_ARRAY(_SrcTex);

    //if user wants to paint on 3d model to further mask the projected art:
    DECLARE_TEXTURE_OR_ARRAY(_uvMask);


    struct appdata {
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


    // geom func. Spawns quads for every slice of the destination textureArra.
    #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_GeomFunc_ForBlitSlices.cginc"


    fixed4 frag (g2f i) : SV_Target{
               
        float3 uv_withSliceIx =  float3(i.uv, i.slice);

        // sample the texture
        float4 col  = SAMPLE_TEXTURE_OR_ARRAY(_SrcTex, uv_withSliceIx);
                
        // [0,1] --> [0,2]. Anything above 1 would be for fighting the invisibility.
        // Custom uv textures don't have invisibility, they wrap around all object, unlike projections.
        float uvFinetuneMask =  SAMPLE_TEXTURE_OR_ARRAY(_uvMask, uv_withSliceIx).r * 2; 

        col.a *= uvFinetuneMask;
        return col;
    }