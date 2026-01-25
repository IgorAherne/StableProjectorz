// Detects changes in alpha, and highlights it as "white".
// This basically finds where there are borders of the UV-chunks on the texture.
Shader "Unlit/TextureDilation_DetectUVchunkBorders"
{
    SubShader
    {
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma require geometry
            #pragma require setrtarrayindexfromanyshader //for SV_RenderTargetArrayIndex
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile _ DETECT_VIA_R_CHANNEL //if on, looking at .r otherwise at .a of texture.
            #pragma multi_compile _ USING_TEXTURE_ARRAY
            #pragma multi_compile  NUM_SLICES_UPTO_8  NUM_SLICES_UPTO_16  NUM_SLICES_UPTO_24

            #include "UnityCG.cginc"
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_AutoDefines.cginc"
            

            DECLARE_TEXTURE_OR_ARRAY(_SrcTex)
            float4 _SrcTex_invSize;


            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct g2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                uint slice : SV_RenderTargetArrayIndex;
            };


            v2g vert(appdata v){
                v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; 
                return o;
            }

            //geom func. Spawns quads for every slice of the destination textureArra.
            #include "Assets/_gm/_Core/Shader_Includes/TextureArrays_GeomFunc_ForBlitSlices.cginc"
            

            float totalSurroundingAlpha(float2 uv, int sliceIx, fixed alphaThresh, fixed2 tex_invSize){
                float totalNeighborAlpha = 0;
                for(int x=-1; x<=1; x++){
                    for(int y=-1; y<=1; y++){
                        
                        if (x == 0 && y == 0) continue;

                        float2 offset =  float2(x, y) * tex_invSize;
                        float3 sampleCoord =  float3((uv+offset).xy, sliceIx);
                        
                        #ifdef DETECT_VIA_R_CHANNEL
                        if( SAMPLE_TEXTURE_OR_ARRAY(_SrcTex, sampleCoord).r  <  alphaThresh){ 
                            continue; }
                        #else
                        if( SAMPLE_TEXTURE_OR_ARRAY(_SrcTex, sampleCoord).a  <  alphaThresh){ 
                            continue; }
                        #endif

                        totalNeighborAlpha++;
                    }
                }
                return totalNeighborAlpha;
            }


            fixed frag(g2f i) : SV_Target {

                fixed alphaThresh = 0.1f;
                fixed2 tex_invSize = (_SrcTex_invSize.xy); 

                fixed4 col = SAMPLE_TEXTURE_OR_ARRAY(_SrcTex, float3(i.uv, i.slice) );
                float neighborAlphaCount = totalSurroundingAlpha(i.uv, i.slice, alphaThresh, tex_invSize);

                #ifdef DETECT_VIA_R_CHANNEL
                  bool onTheBorder =  neighborAlphaCount >= 1  &&  neighborAlphaCount < 8  &&  col.r>alphaThresh;
                  return onTheBorder? 1 : 0;
                #else 
                  bool onTheBorder =  neighborAlphaCount >= 1  &&  neighborAlphaCount < 8  &&  col.a>alphaThresh;
                  return onTheBorder? 1 : 0;
                #endif
            }
            ENDCG
        }//end Pass
    }
}
