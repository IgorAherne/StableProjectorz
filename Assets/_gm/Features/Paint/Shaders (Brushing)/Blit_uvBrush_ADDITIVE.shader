Shader "Unlit/Blit_uvBrush_ADDITIVE"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
        Cull Off

        // Setup blending, for blending new stuff on top of other already existing projections.
        // I need an alpha which is to be added to the already existing alpha.
        // Yet, the current alpha also needs to "lerp" its RGB.
        BlendOp Add //<--the source and destination values will be added together.
        Blend One OneMinusSrcAlpha, One One//<--two separate equations, one for RGB, one for Alpha.

        Pass{
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //for SV_RenderTargetArrayIndex

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24

            #include "Assets/_gm/_Core/Shader_Includes/Blit_uvTex_with_uvMask_SHADER_PASS.cginc"
            ENDCG
        }
    }
}
