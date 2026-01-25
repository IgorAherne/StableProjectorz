
// To be sued by Graphics.Blit() 
Shader "Unlit/BlitDepth_of_LatestCamera"
{
    Properties{
        _MainTex("Main Texture (Depth)", 2D) = "black" {}
    }

    SubShader{
    Tags { "RenderType"="Opaque" }
    LOD 100

    Pass{
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        #include "UnityCG.cginc"


        sampler2D _MainTex;


        struct appdata{
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f{
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        v2f vert (appdata v){
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        float4 frag (v2f i) : SV_Target{
            float depth = tex2D( _MainTex,  i.uv ).r;
                  depth = Linear01Depth(depth);//Linear01Depth always takes care of reversing the Z-range, makes far white.
            return float4(depth, depth, depth, 1);
        }
        ENDCG
    }//end Pass
    }//end SubShader
}
