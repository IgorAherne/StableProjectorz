#ifndef SP_MULTI_PROJECTION_VARS
#define SP_MULTI_PROJECTION_VARS

#include "Assets/_gm/_Core/Shader_Includes/MultiProjection_AutoDefines.cginc"

//POV (point of view) is position + rotation + fov of projector camera.
//Camera can have several POV, which means it teleports a few times during a frame.
//This can "shine" several screen-space art textures
    
    // viewProjection matrix. Doesn't belong to any projector camera.
    // Instead, helps to map a vertex into the view of MainViewport camera.
    float4x4 _CurrViewport_VP_matrix; 


    //uv-space texture, for hiding away screen-art that projector camera wants to offer at POV0:
    UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(_POV0_additive_uvMask);
    //uv-space texture, describing texels as "seen" or "not seen" by projector camera while it's in POV0.
    //has several channels.
    UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(_POV0_ProjVisibility);

    // There is a limit of samplers, so we will be re-using the samplers for sampling different textures.
    // https://docs.unity3d.com/Manual/SL-SamplerStates.html
    SamplerState sampler_linear_repeat;


    //position of the projector camera while it's in POV0:
    float4 _CameraWorldPos0;
    //Transform the vertex from World position to clip space, how projector camera sees it (at POV0).
    float4x4 _ViewProj_matrix0;


#ifdef NUM_POV_2
    UNITY_DECLARE_TEX2DARRAY_NOSAMPLER( _POV1_additive_uvMask );
    UNITY_DECLARE_TEX2DARRAY_NOSAMPLER( _POV1_ProjVisibility );
    float4 _CameraWorldPos1;
    float4x4 _ViewProj_matrix1;
#endif
#ifdef NUM_POV_3
    UNITY_DECLARE_TEX2DARRAY_NOSAMPLER( _POV2_additive_uvMask );
    UNITY_DECLARE_TEX2DARRAY_NOSAMPLER( _POV2_ProjVisibility );
    float4 _CameraWorldPos2;
    float4x4 _ViewProj_matrix2; 
#endif
#ifdef NUM_POV_4
    UNITY_DECLARE_TEX2DARRAY_NOSAMPLER( _POV3_additive_uvMask );
    UNITY_DECLARE_TEX2DARRAY_NOSAMPLER( _POV3_ProjVisibility );
    float4 _CameraWorldPos3;
    float4x4 _ViewProj_matrix3;
#endif
#ifdef NUM_POV_5
    UNITY_DECLARE_TEX2DARRAY_NOSAMPLER( _POV4_additive_uvMask );
    UNITY_DECLARE_TEX2DARRAY_NOSAMPLER( _POV4_ProjVisibility );
    float4 _CameraWorldPos4;
    float4x4 _ViewProj_matrix4;
#endif
#ifdef NUM_POV_6
    UNITY_DECLARE_TEX2DARRAY_NOSAMPLER( _POV5_additive_uvMask );
    UNITY_DECLARE_TEX2DARRAY_NOSAMPLER( _POV5_ProjVisibility );
    float4 _CameraWorldPos5;
    float4x4 _ViewProj_matrix5;
#endif

#endif