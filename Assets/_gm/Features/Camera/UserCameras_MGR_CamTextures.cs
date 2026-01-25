using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace spz {

	public enum CameraTexType{
	    Unknown, Nothing, ViewUserCamera, ContentUserCam, DepthUserCamera, NormalsUserCamera, VertexColorsUserCamera,
	}

	// A global container-class.
	// Holds render-target textures of cameras that the user is working with.
	// These textures represent what the camera is observing. They can be sent to StableDiffusion, etc.
	public class UserCameras_MGR_CamTextures : MonoBehaviour
	{
	        //UI script with texture. All view cameras will render into it.
	              public ViewRT_From_RectTransf_UI view_RT_fromRectTransf => _view_RT_fromRectTransf;
	    [SerializeField] ViewRT_From_RectTransf_UI _view_RT_fromRectTransf;
	    [SerializeField] RenderTex_from_SD_WidthHeight _depth_RT_from_WidthHeight;
	    [SerializeField] RenderTex_from_SD_WidthHeight _normals_RT_from_WidthHeight;
	    [SerializeField] RenderTex_from_SD_WidthHeight _content_RT_from_WidthHeight;
	    [SerializeField] RenderTex_from_SD_WidthHeight _vertexColors_RT_from_WidthHeight;
	    [Space(10)]
	    [SerializeField] Depth_Contrast_Helper _depthContrastHelper;
	    [Space(10)]
	    [SerializeField] Shader _BlitLatestDepth_sh;//helps to capture a most recent depth texture into RenderTexture.

	    Material _BlitLatestDepth_mat;


	    public RenderTexture _viewCam_RT_ref { get; private set; } = null;//doesn't belong to us.
	    public RenderTexture _viewCam_meshIDs_ref => _viewCam_meshIDs_RG8;//allows to understand what mesh we are clicking on/hovering. 
	    public RenderTexture _viewCam_meshIDs_RG8 = null;
	    public RenderTexture _viewCamDepthLast_linear_ref =>_viewCamDepthLast_linear_R32;//belongs to us. Whatever the size of ENTIRE viewport is. Black-white texture.
	           RenderTexture _viewCamDepthLast_linear_R32 = null; //must be highest precision, so that we can reconstruct world coords from it (for click-select object etc).
	                                                              //NOTICE: depth is between 0 and farClipPlane of a ViewCamera.

	    public RenderTexture _SD_depthCam_RT_R32_linear { get; private set; } = null;//For storing black and white depth texture, as Content Camera sees it (512x512, etc). single channel, 0 depthBits.
	    public RenderTexture _SD_depthCam_RT_R32_contrast { get; private set; } = null;//We'll do contrast on this image, to enhance it. We actually send THIS depth to StableDiffusion.
	                                                                                   //NON-LINEAR, because edge-detection algorithms will work better with it.

	    public RenderTexture _contentCam_RT_ref { get; private set; } = null;
	    public RenderTexture _normalsCam_RT_ref { get; private set; } = null;
	    public RenderTexture _vertexColorsCam_RT_ref { get; private set; } = null;

	    public Action<RenderTexture,CameraTexType> _Act_WillDestroy_RenderTex { get; set; } = null;
	    public Action<RenderTexture,CameraTexType> _Act_CreatedNewRenderTexture { get; set; } = null;

	    Action<RenderTexture> _manualRenderNow_contentCamera;//allows THIS class to trigger rendering of ContentCamera whenever we want.


	    //you can destroy the returned texture as you wish. Don't forget to destroy it, else will memory leak.
	    public Texture2D GetDisposable_DepthTexture() //forceAlpha1: SUPER IMPORTANT. (July 2024) Otherwise high contrast = StableDiffusion generates bad images.
	        => TextureTools_SPZ.R_to_RGBA_Texture2D( _SD_depthCam_RT_R32_contrast,  forceAlpha1:true, forceFullWhite:false );
    
	    public Texture2D GetDisposable_NormalsTexture()
	        => TextureTools_SPZ.RenderTextureToTexture2D(_normalsCam_RT_ref);

	    public Texture2D GetDisposable_VertexColorsTexture()
	        => TextureTools_SPZ.RenderTextureToTexture2D(_vertexColorsCam_RT_ref);

	    public Texture2D GetDisposable_ContentCamTexture(){
	        _manualRenderNow_contentCamera(_contentCam_RT_ref);
	        return TextureTools_SPZ.RenderTextureToTexture2D(_contentCam_RT_ref);
	    }

	    public RenderTexture GetDisposableRT_ContentCamTexture(){
	        _manualRenderNow_contentCamera(_contentCam_RT_ref);
	        var rt = new RenderTexture(_contentCam_RT_ref.descriptor);
	        Graphics.Blit(_contentCam_RT_ref, rt);
	        return rt;
	    }


	    //depth of the entire viewport, larger than content-depth (what would be sent to stable diffusion).
	    public void OnUpdated_ViewCameraDepth(Vector2 cam_NearFar_clipPlanes){
	        //shader is non-additive, no need ot clear texture, just blit:
	        //NOTICE: linear is important, for CameraOrbitPivot, etc.
	        _BlitLatestDepth_mat.SetFloat("_NearClip", cam_NearFar_clipPlanes.x);
	        _BlitLatestDepth_mat.SetFloat("_FarClip", cam_NearFar_clipPlanes.y);
	        TextureTools_SPZ.SetKeyword_Material(_BlitLatestDepth_mat, "ENSURE_LINEAR_01_DEPTH", true);
	        TextureTools_SPZ.Blit(null, _viewCamDepthLast_linear_R32, _BlitLatestDepth_mat );
	        //NOTICE: we are NOT applying contrast on that depth, to ensure scripts can access the true depth in [0,1] range.
	    }

    
	    //depth that might be sent to StableDiffusion, if we generate (512x512, etc)
	    public void OnUpdate_SD_ContentDepth_Started(){
	        //clear because depth will be additively blended:
	        TextureTools_SPZ.ClearRenderTexture(_SD_depthCam_RT_R32_linear, Color.black, true,true);
	        TextureTools_SPZ.ClearRenderTexture(_SD_depthCam_RT_R32_contrast, Color.black, true,true);
	    }

	    public void OnDepthUpdate_End(){
	        bool depth_needed = UserCameras_Permissions.depthCam_keepRendering.isLocked();
	        if (depth_needed==false){ return; }

	        var tr = LeftRibbon_UI.instance;
	        var contrastMode = Depth_Contrast_Helper.ContrastMode.ApplyExact;
	        var depthArg = new Depth_Contrast_Helper.DepthContrast_arg( _SD_depthCam_RT_R32_contrast, contrastMode, tr.depthContrast, tr.depthBrightness );
	        depthArg.blurStepSize01 = tr.depthBlur_StepSize;
	        depthArg.blurSkipSamples_R_differenceGrtr = 1 / Mathf.Pow(2, tr.depthSharpBlur);
	        depthArg.final_blurStepSize01 = tr.depthBlurFinal_StepSize;
	        depthArg.finalBlur_ignoreSamples_0rgb = tr.depthFinalBlur_Inside;
       
	        _depthContrastHelper.Improve_DepthContrast(depthArg);
	    }


	    public void Init(Action<RenderTexture> manualRenderNow_contentCamera){
	        _manualRenderNow_contentCamera = manualRenderNow_contentCamera;
	    }

	    void Start(){
	        _BlitLatestDepth_mat = new Material(_BlitLatestDepth_sh);
	        _view_RT_fromRectTransf.Subscribe(OnWillDestroy_View_RenderTex, OnCreatedNew_View_RenderTex);
	        _content_RT_from_WidthHeight.Subscribe(OnWillDestroy_Content_RenderTex, OnCreatedNew_Content_RenderTex);
	        _depth_RT_from_WidthHeight.Subscribe(OnWillDestroy_Depth_RenderTex, OnCreatedNew_Depth_RenderTex);
	        _normals_RT_from_WidthHeight.Subscribe(OnWillDestroy_Normals_RenderTex, OnCreatedNew_Normals_RenderTex);
	        _vertexColors_RT_from_WidthHeight.Subscribe(OnWillDestroy_VertexColors_RenderTex, OnCreatedNew_VertexColors_RenderTex);

	        UserCameras_Permissions.vertexColorsCam_keepRendering.onLockStatusChanged += (isLocked)=>OnTexture_StatusOfNeed(CameraTexType.VertexColorsUserCamera, isNeeded:isLocked);
	        UserCameras_Permissions.contentCam_keepRendering.onLockStatusChanged  += (isLocked)=>OnTexture_StatusOfNeed(CameraTexType.ContentUserCam, isNeeded:isLocked);
	        UserCameras_Permissions.normalsCam_keepRendering.onLockStatusChanged  += (isLocked)=>OnTexture_StatusOfNeed(CameraTexType.NormalsUserCamera, isNeeded:isLocked);
	        UserCameras_Permissions.depthCam_keepRendering.onLockStatusChanged    += (isLocked)=>OnTexture_StatusOfNeed(CameraTexType.DepthUserCamera, isNeeded:isLocked);
	    }


	    void OnDestroy(){
	        DestroyImmediate(_BlitLatestDepth_mat);
	        _view_RT_fromRectTransf.Unsubscribe(OnWillDestroy_View_RenderTex, OnCreatedNew_View_RenderTex);
	        _content_RT_from_WidthHeight.Unsubscribe(OnWillDestroy_Content_RenderTex, OnCreatedNew_Content_RenderTex);
	        _depth_RT_from_WidthHeight.Unsubscribe(OnWillDestroy_Depth_RenderTex, OnCreatedNew_Depth_RenderTex);
	        _normals_RT_from_WidthHeight.Unsubscribe(OnWillDestroy_Normals_RenderTex, OnCreatedNew_Normals_RenderTex);
	        _vertexColors_RT_from_WidthHeight.Unsubscribe(OnWillDestroy_VertexColors_RenderTex, OnCreatedNew_VertexColors_RenderTex);
	    }


	    void OnTexture_StatusOfNeed(CameraTexType type, bool isNeeded){
	        switch (type){
	            case CameraTexType.ViewUserCamera: break;
	            case CameraTexType.ContentUserCam:  _content_RT_from_WidthHeight.set_is_Want(isNeeded); break;
	            case CameraTexType.DepthUserCamera:  _depth_RT_from_WidthHeight.set_is_Want(isNeeded);  break;
	            case CameraTexType.NormalsUserCamera: _normals_RT_from_WidthHeight.set_is_Want(isNeeded); break;
	            case CameraTexType.VertexColorsUserCamera: _vertexColors_RT_from_WidthHeight.set_is_Want(isNeeded); break;
	            default: Debug.Log($"OnTexture_StatusOfNeed: unknown RenderTextType {type}"); break;
	        }
	    }


	    void OnWillDestroy_View_RenderTex(RenderTexture rt){
	        _Act_WillDestroy_RenderTex?.Invoke(rt, CameraTexType.ViewUserCamera);
	        _viewCam_RT_ref = null;
	        //we own the viewport-depth texture, so destroy it too:
	        TextureTools_SPZ.Dispose_RT(ref _viewCamDepthLast_linear_R32, false);
	        TextureTools_SPZ.Dispose_RT(ref _viewCam_meshIDs_RG8, false);
	    }
	    void OnCreatedNew_View_RenderTex(RenderTexture rt){
	        _viewCam_RT_ref = rt;
	        _Act_CreatedNewRenderTexture?.Invoke(rt, CameraTexType.ViewUserCamera);
	        Debug.Assert(_viewCamDepthLast_linear_R32 == null, "ViewCamDepthLast was expected to be null! Memory leak.");
	        _viewCamDepthLast_linear_R32 = new RenderTexture(_viewCam_RT_ref.width, _viewCam_RT_ref.height,
	                                                         //depth 0, unity renders into internal depth buffers anyway:
	                                                         depth: 0, GraphicsFormat.R32_SFloat, mipCount:0);

	        Debug.Assert(_viewCam_meshIDs_RG8 == null, "ViewCamMeshIdsRG8 was expected to be null! Memory leak.");
	        _viewCam_meshIDs_RG8 = new RenderTexture( _viewCam_RT_ref.width, _viewCam_RT_ref.height, 
	                                                  depth:0, GraphicsFormat.R8G8_UNorm, mipCount:0);
	    }
    
  
	    void OnWillDestroy_Depth_RenderTex(RenderTexture rt){
	        _Act_WillDestroy_RenderTex?.Invoke(rt, CameraTexType.DepthUserCamera);
	        _SD_depthCam_RT_R32_linear = null;//doesn't belong to us, just forget
	        DestroyImmediate(_SD_depthCam_RT_R32_contrast);
	    }
	    void OnCreatedNew_Depth_RenderTex(RenderTexture rt){
	        // A single-channel texture was created (high-precision)
	        // It will be used for showing depth as black-and-white, after it was rendered:
	        _SD_depthCam_RT_R32_linear = rt;
	        if(_SD_depthCam_RT_R32_contrast != null){  DestroyImmediate(_SD_depthCam_RT_R32_contrast);  }
	        _SD_depthCam_RT_R32_contrast = new RenderTexture(rt.descriptor);

	        _Act_CreatedNewRenderTexture?.Invoke(rt, CameraTexType.DepthUserCamera);
	    }


	    void OnWillDestroy_Content_RenderTex(RenderTexture rt){
	        _Act_WillDestroy_RenderTex?.Invoke(rt, CameraTexType.ContentUserCam);
	        _contentCam_RT_ref = null;
	    }
	    void OnCreatedNew_Content_RenderTex(RenderTexture rt){
	        _contentCam_RT_ref = rt;
	        _Act_CreatedNewRenderTexture?.Invoke(rt, CameraTexType.ContentUserCam);
	    }


	    void OnWillDestroy_Normals_RenderTex(RenderTexture rt){
	        _Act_WillDestroy_RenderTex?.Invoke(rt, CameraTexType.NormalsUserCamera);
	        _normalsCam_RT_ref = null;
	    }
	    void OnCreatedNew_Normals_RenderTex(RenderTexture rt){
	        _normalsCam_RT_ref = rt;
	        _Act_CreatedNewRenderTexture?.Invoke(rt, CameraTexType.NormalsUserCamera);
	    }


	    void OnWillDestroy_VertexColors_RenderTex(RenderTexture rt){
	        _Act_WillDestroy_RenderTex?.Invoke(rt, CameraTexType.VertexColorsUserCamera);
	        _vertexColorsCam_RT_ref = null;
	    }
	    void OnCreatedNew_VertexColors_RenderTex(RenderTexture rt){
	        _vertexColorsCam_RT_ref = rt;
	        _Act_CreatedNewRenderTexture?.Invoke(rt, CameraTexType.VertexColorsUserCamera);
	    }

	}
}//end namespace
