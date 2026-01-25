using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	public class ScreenDepth_EdgesDetector : MonoBehaviour
	{
	    [SerializeField] ComputeShader _detectEdges_byDepth;

	    public class TexArgs{
	        public RenderTexture depthNonLinear_contrast_R16;//non-linear depth, which was improved by contrast.
	        public RenderTexture result_edges_R16;
	        public Texture screenBrushMask_R = null;//optional
	        public float edgesThresh01 = 0.5f; //greater value = more permissive (fewer edges detected)
	    }
     
	    public class BlurArgs{
	        public float edgeBlurStride_01;
	        public float edgeBlurPow_01;
	        // If NOT provided, we will use 'TexArgs.depthRT_R16' as a buffer, affecting it
	        // Remember to destroy it afterwards!
	        public RenderTexture bufferTex = null; 
	    }

	    public void DetectEdges_ByDepth( TexArgs a ){
	        if(a.edgesThresh01 == 1){
	            TextureTools_SPZ.ClearRenderTexture(a.result_edges_R16, Color.black);
	            return;
	        }

	        int kernel = _detectEdges_byDepth.FindKernel("EdgeDetect");

	        for(int i=1; i<=4; ++i){  _detectEdges_byDepth.DisableKeyword($"SEARCH_RANGE_{i}");  }
	        _detectEdges_byDepth.EnableKeyword("SEARCH_RANGE_1");
        
	        _detectEdges_byDepth.SetInt("_TexWidth", a.result_edges_R16.width);
	        _detectEdges_byDepth.SetInt("_TexHeight", a.result_edges_R16.height);

	        _detectEdges_byDepth.SetFloat("_DepthDifference_Thresh", Mathf.Lerp(0.005f, 0.1f, a.edgesThresh01));
	        _detectEdges_byDepth.SetTexture(kernel, "_DepthTexture", a.depthNonLinear_contrast_R16);//will read whatever is here (already-normalized depth)
	        _detectEdges_byDepth.SetTexture(kernel, "_OutputTexture", a.result_edges_R16);//will store result into here.

	        TextureTools_SPZ.SetKeyword_ComputeShader(_detectEdges_byDepth, "HAS_SCREEN_MASK", a.screenBrushMask_R != null);
	        if(a.screenBrushMask_R != null){ _detectEdges_byDepth.SetTexture(kernel, "_ScreenMaskTexture", a.screenBrushMask_R);  }

	        Vector3Int grps = ComputeShaders_MGR.calcNumGroups( a.depthNonLinear_contrast_R16 );
	        _detectEdges_byDepth.Dispatch(kernel, grps.x, grps.y, grps.z);
	    }


	    public void BlurEdges_ofDepth(TexArgs a, BlurArgs b){

	        bool bufferGiven =  b.bufferTex != null;
	        RenderTexture buffer =  bufferGiven?  b.bufferTex : a.depthNonLinear_contrast_R16;

	        int kernel = _detectEdges_byDepth.FindKernel("BoxBlur");

	        float blurStride  =  (b.edgeBlurStride_01);
	              blurStride *=  Mathf.Max(a.result_edges_R16.width, a.result_edges_R16.height) / 1024.0f;//makes it independent of res.

	        _detectEdges_byDepth.SetInt("_TexWidth", a.result_edges_R16.width);
	        _detectEdges_byDepth.SetInt("_TexHeight", a.result_edges_R16.height);

	        _detectEdges_byDepth.SetFloat("_Blur_Stride", blurStride);
	        _detectEdges_byDepth.SetFloat("_BlurPow", b.edgeBlurPow_01 );

	        //was only used during edge-detection, not used now:
	        TextureTools_SPZ.SetKeyword_ComputeShader(_detectEdges_byDepth, "HAS_SCREEN_MASK", false);

	        RenderTexture from = a.result_edges_R16;
	        RenderTexture into = buffer;
	        Vector3Int grps = ComputeShaders_MGR.calcNumGroups( buffer );

	        _detectEdges_byDepth.SetInt("_Total_BlurDispatchesExpected", 7);
	        for(int i=0; i<7; ++i){
	            _detectEdges_byDepth.SetFloat("_Curr_BlurDispatch", i);
	            _detectEdges_byDepth.SetTexture(kernel, "_DepthTexture", from);
	            _detectEdges_byDepth.SetTexture(kernel, "_OutputTexture", into);
	            _detectEdges_byDepth.Dispatch(kernel, grps.x, grps.y, grps.z);
	            //swap the textures around for the next iteration:
	            var temp = from;
	            from = into;
	            into = temp;
	        }
	        //ensure the final result is stored into our edgesRT_R16 texture:
	        if(from != a.result_edges_R16){  TextureTools_SPZ.Blit(buffer, a.result_edges_R16);  }
	    }
	}
}//end namespace
