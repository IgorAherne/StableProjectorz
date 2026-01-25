using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace spz {

	public class PreventBlur_if_DistanceFromMask : MonoBehaviour
	{
	    [SerializeField] ComputeShader _downsample_depthOfMasked_sh;
	    [SerializeField] ComputeShader _preventMaskBlur_byMaskDepth_sh;

	    public RenderTexture _screenDepthMaskDownsampled = null;

	    const string _downsample_Kernel_name = "DownsampleKernel_x4";
	    const int _downsampleFactor = 4;

	    const string _preventMaskBlurKernel_name = "PreventBlur_kernel";
	    const string _blurBordersKernel_name = "BlurBorders_kernel";


	    void CreateDownsampledTex_maybe(int screenMaskWidth, int screenMaskHeight){
	        // Create downsampled textures
	        int downWidth  = (screenMaskWidth + 1) / _downsampleFactor;
	        int downHeight = (screenMaskHeight + 1) / _downsampleFactor;

	        bool isNull = _screenDepthMaskDownsampled == null;
	        bool isWidthDifferent = !isNull && _screenDepthMaskDownsampled.width != downWidth;
	        bool isHeightDifferent = !isNull && _screenDepthMaskDownsampled.height != downHeight;

	        if (!isNull && !isWidthDifferent && !isHeightDifferent){ return; }// Already have appropriate texture.
	        if (!isNull){
	            DestroyImmediate(_screenDepthMaskDownsampled);
	        }
	        _screenDepthMaskDownsampled = new RenderTexture(downWidth, downHeight, 0, GraphicsFormat.R32_SFloat);
	        _screenDepthMaskDownsampled.enableRandomWrite = true;
	    }


	    // Create a downsampled version of depth, which is alowed only where the mask is non-zero.
	    // And if depth difference isn't too drastic. Otherwise depth is made black at such texels.
	    // We will use that downsampled version later, during neighbor-searches.
	    public void CalcDownscaledDepths_of_Mask(RenderTexture screenMask, RenderTexture screenDepthLinear01){
	        int kernelHandle = _downsample_depthOfMasked_sh.FindKernel(_downsample_Kernel_name);

	        CreateDownsampledTex_maybe(screenMask.width, screenMask.height);
	        int down_width = _screenDepthMaskDownsampled.width;
	        int down_height = _screenDepthMaskDownsampled.height;

	        // Set textures and variables
	        _downsample_depthOfMasked_sh.SetTexture(kernelHandle, "_ScreenMask", screenMask);
	        _downsample_depthOfMasked_sh.SetTexture(kernelHandle, "_ScreenDepthLinear01", screenDepthLinear01);
	        _downsample_depthOfMasked_sh.SetTexture(kernelHandle, "_ScreenDepthMaskDownsampled", _screenDepthMaskDownsampled);

	        _downsample_depthOfMasked_sh.SetInt("_Mask_Width", screenMask.width);
	        _downsample_depthOfMasked_sh.SetInt("_Mask_Height", screenMask.height);
	        _downsample_depthOfMasked_sh.SetInt("_Depth_Width", screenDepthLinear01.width);
	        _downsample_depthOfMasked_sh.SetInt("_Depth_Height", screenDepthLinear01.height);
	        _downsample_depthOfMasked_sh.SetInt("_Downsampled_Width", down_width);
	        _downsample_depthOfMasked_sh.SetInt("_Downsampled_Height", down_height);

	        _downsample_depthOfMasked_sh.SetVector("_InvMask_WidthHeight", new Vector4( 1.0f / screenMask.width,
	                                                                                    1.0f / screenMask.height, 0, 0));
	        //dispatch for the texels of the small (for the downsampling) depth 1:
	        Vector3Int numGroups = ComputeShaders_MGR.calcNumGroups(down_width, down_height);
	        _downsample_depthOfMasked_sh.Dispatch(kernelHandle, numGroups.x, numGroups.y, numGroups.z);
	    }


	    // For every texel, we'll see if there is a non-zero depth somewhere next to us.
	    // We preserve our blurred mask at such texels. If can't find, then we force our mask to zero.
	    // This prevents blurs (forces the blurred mask to zero) where the texel isn't next to the current texel. 
	    // Or even if found, prevents blur if the depth difference is too drastic.
	    // Also, prevents blur if there is an edge between such texels.
	    //
	    // featherEdges: smooth out edges, that might be too sharp even on blurred mask
	    // (because of us preventing some of its blur). We will feather the edges inwards
	    // (won't blur where texels are already zero)
	    public void PreventBlurredMask_whereFar( RenderTexture screenMask_original,//without dilation without blur etc.
	                                             RenderTexture screenMask_edges,
	                                             RenderTexture screenMask_blurred,
	                                             RenderTexture screenDepth, bool featherEdges=true){
	        bool hasEdgesTex = screenMask_edges != null;
	        Debug.Assert(screenMask_original.width == screenMask_blurred.width, "mask original vs blurred: Different widths!");
	        Debug.Assert(screenMask_original.height== screenMask_blurred.height,"mask original vs blurred: Different heights!");
	        if (hasEdgesTex){
	            Debug.Assert(screenMask_edges.width == screenMask_edges.width, "mask original vs edges: Different widths!");
	            Debug.Assert(screenMask_edges.height== screenMask_edges.height,"mask original vs edges: Different heights!");
	        }

	        int kernelHandle  = _preventMaskBlur_byMaskDepth_sh.FindKernel( _preventMaskBlurKernel_name );

	        _preventMaskBlur_byMaskDepth_sh.SetTexture(kernelHandle, "_ScreenMask_original", screenMask_original);
	        _preventMaskBlur_byMaskDepth_sh.SetTexture(kernelHandle, "_ScreenMask_blurred", screenMask_blurred);
	        _preventMaskBlur_byMaskDepth_sh.SetVector("_InvMask_WidthHeight", new Vector4( 1.0f/screenMask_original.width, 
	                                                                                       1.0f/screenMask_original.height, 0,0) );
	        _preventMaskBlur_byMaskDepth_sh.SetTexture(kernelHandle, "_ScreenDepth", screenDepth);
	        _preventMaskBlur_byMaskDepth_sh.SetInt("_ScreenDepth_Width", screenDepth.width);
	        _preventMaskBlur_byMaskDepth_sh.SetInt("_ScreenDepth_Height",screenDepth.height);

	        _preventMaskBlur_byMaskDepth_sh.SetTexture(kernelHandle, "_MaskDepth_downsampled", _screenDepthMaskDownsampled);
	        _preventMaskBlur_byMaskDepth_sh.SetInt("_MaskDepth_Width", _screenDepthMaskDownsampled.width);
	        _preventMaskBlur_byMaskDepth_sh.SetInt("_MaskDepth_Height", _screenDepthMaskDownsampled.height);

	        if(!hasEdgesTex){//we must provide a texture (can't be null). So if null, use black a texture with no edges:
	            screenMask_edges = RenderTexture.GetTemporary(32,32, depthBuffer:0, GraphicsFormat.R8_UNorm);
	            TextureTools_SPZ.ClearRenderTexture(screenMask_edges, Color.black);
	        }
	        _preventMaskBlur_byMaskDepth_sh.SetTexture(kernelHandle, "_MaskEdges", screenMask_edges);
	        _preventMaskBlur_byMaskDepth_sh.SetInt("_MaskEdges_Width", screenMask_edges.width);
	        _preventMaskBlur_byMaskDepth_sh.SetInt("_MaskEdges_Height", screenMask_edges.height);
        
	        Vector4 inv_downDown_size =  new Vector4( 1.0f/_screenDepthMaskDownsampled.width,  1.0f/_screenDepthMaskDownsampled.height, 0, 0);
	        _preventMaskBlur_byMaskDepth_sh.SetVector("_MaskDepthDownsampled_invWH", inv_downDown_size);

	        float invFarPlane =  1.0f / UserCameras_MGR.instance._curr_viewCamera.myCamera.farClipPlane;
	        _preventMaskBlur_byMaskDepth_sh.SetFloat("_zThreshUnits", 0.15f);//blur won't leak between surfaces offsetted by this much.
	                                                             //It's ok, because all imported meshes are scale-normalized into 3x3 units box.
	        _preventMaskBlur_byMaskDepth_sh.SetFloat( "_inv_CameraFarClipPlane", invFarPlane);

	        //dispatch for the texels of the screen MASK, not the texels of a downsampled image!
	        Vector3Int numGroups = ComputeShaders_MGR.calcNumGroups(screenMask_original.width, 
	                                                                screenMask_original.height);
	        _preventMaskBlur_byMaskDepth_sh.Dispatch(kernelHandle, numGroups.x, numGroups.y, numGroups.z);

	        if(!hasEdgesTex){ RenderTexture.ReleaseTemporary(screenMask_edges);  }

	        if(featherEdges){ 
	            FeatherBlurredMask(screenMask_original, screenMask_blurred);
	        }
	    }

	    // smooth out edges, that might be too sharp even on blurred mask (because of us preventing some of its blur).
	    // We will feather the edges inwards (won't blur where texels are already zero)
	    void FeatherBlurredMask( RenderTexture screenMask_original,//without dilation without blur etc.
	                             RenderTexture screenMask_blurred ){
	        RenderTexture screenMask_blurred_cpy =RenderTexture.GetTemporary(screenMask_blurred.descriptor);
	        TextureTools_SPZ.Blit(screenMask_blurred, screenMask_blurred_cpy);

	        int kernelHandle  = _preventMaskBlur_byMaskDepth_sh.FindKernel(_blurBordersKernel_name);
	        _preventMaskBlur_byMaskDepth_sh.SetTexture(kernelHandle, "_ScreenMask_original", screenMask_original);
	        _preventMaskBlur_byMaskDepth_sh.SetTexture(kernelHandle, "_ScreenMask_blurred", screenMask_blurred);
	        _preventMaskBlur_byMaskDepth_sh.SetTexture(kernelHandle, "_ScreenMask_blurred_cpy", screenMask_blurred_cpy);

	        _preventMaskBlur_byMaskDepth_sh.SetInt("_ScreenMask_Width", screenMask_original.width);
	        _preventMaskBlur_byMaskDepth_sh.SetInt("_ScreenMask_Height", screenMask_original.height);

	        Vector3Int numGroups = ComputeShaders_MGR.calcNumGroups(screenMask_blurred.width, screenMask_blurred.height);
	        _preventMaskBlur_byMaskDepth_sh.Dispatch(kernelHandle, numGroups.x, numGroups.y, numGroups.z);

	        RenderTexture.ReleaseTemporary(screenMask_blurred_cpy);
	    }
	}
}//end namespace
