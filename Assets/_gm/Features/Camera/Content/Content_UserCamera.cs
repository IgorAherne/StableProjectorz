using System.Linq;
using UnityEngine;

namespace spz {

	// Render the "Content Camera" is similar to view_camera, shows colors.
	// But at Stable-diffusion resolution, for example, 512x512.
	public class Content_UserCamera : MonoBehaviour{
	    public Camera myCamera => _camera;
	    [SerializeField] Camera _camera;
	    [SerializeField] View_UserCamera _view_camera_inParent;

	    public float cameraAspect => _camera.aspect;

	    // (0,0) is left corner of viewport, (1,1) top right.
	    public Vector2 _projectionMat_center{get; private set;}  = new Vector2(0.5f, 0.5f);
	    public void Set_ProjMat_center(Vector2 viewportCoord01) => _projectionMat_center=viewportCoord01;


	    public void RenderWhatsVisible(RenderTexture renderIntoHere,  
	                                   CameraClearFlags flags=CameraClearFlags.Nothing,
	                                   bool allowMSSA = false){//mssa smoothese edges, but will cause dilation to mess up.

	        if(UserCameras_Permissions.contentCam_keepRendering.isLocked() == false){ return; }

	        var prevParams = new ParamsBeforeRender(_camera);
	        //_camera.enabled = true;  COMMENTED OUT, KEPT FOR PRECAUTION. Keep disabled. Render() still works + avoids automatic renders
	        _camera.targetTexture = renderIntoHere;
	        _camera.clearFlags = flags;
	        _camera.allowMSAA = allowMSSA;
	        _camera.Render();
	        prevParams.RestoreCam(_camera);
	    }


	    public void RenderWhatsVisible(RenderTexture[] renderIntoHere,  
	                                   CameraClearFlags flags=CameraClearFlags.Nothing,
	                                   bool allowMSSA = false){//mssa smoothese edges, but will cause dilation to mess up.

	        if(UserCameras_Permissions.contentCam_keepRendering.isLocked() == false){ return; }

	        var prevParams = new ParamsBeforeRender(_camera);
	        //_camera.enabled = true;  COMMENTED OUT, KEPT FOR PRECAUTION. Keep disabled. Render() still works + avoids automatic renders

	        // Depth buffer doesn't matter because unity will use its own internal depth buffer automatically. So set 0th one:
	        _camera.SetTargetBuffers(renderIntoHere.Select(rt=>rt.colorBuffer).ToArray(), renderIntoHere[0].depthBuffer);

	        _camera.clearFlags = flags;
	        _camera.allowMSAA = allowMSSA;
	        _camera.Render();
	        prevParams.RestoreCam(_camera);
	    }


	    void OnPreRender(){
	        CameraTools.ShiftViewportCenter_ofProjMat( _camera, _projectionMat_center );
	    }

	    void OnPostRender(){
	        _camera.ResetCullingMatrix();
	        _camera.ResetProjectionMatrix();
	    }
    

	    public void OnUpdateParams(){//copying parameters, BUT IN UPDATE, not in the OnPreRender(). Latter would reset fov after render is done.
	        Camera vcam = _view_camera_inParent.myCamera;
        
	        _camera.fieldOfView = vcam.fieldOfView;
	        _camera.orthographic = vcam.orthographic;
	        _camera.orthographicSize = vcam.orthographicSize;
	    }


	#region init
	    public void OnInit() {
	        int ix = UserCameras_MGR.instance.ix_specificViewCam(_view_camera_inParent);
	        EventsBinder.Bind_Clickable_to_event(nameof(Content_UserCamera)+"_" +ix, this);

	        UserCameras_MGR.instance.camTextures._Act_CreatedNewRenderTexture -= OnCreatedNewRenderTexture;
	        UserCameras_MGR.instance.camTextures._Act_CreatedNewRenderTexture += OnCreatedNewRenderTexture;
	        UserCameras_MGR.instance.camTextures._Act_WillDestroy_RenderTex -= OnWillDestroyRenderTexture;
	        UserCameras_MGR.instance.camTextures._Act_WillDestroy_RenderTex += OnWillDestroyRenderTexture;
	        _camera.enabled = false; //Keep disabled.  Render() still works + avoids automatic renders.
	        _camera.depthTextureMode = DepthTextureMode.None;
	    }

	    void OnDestroy(){ 
        
	        if (UserCameras_MGR.instance != null){
	            UserCameras_MGR.instance.camTextures._Act_CreatedNewRenderTexture -= OnCreatedNewRenderTexture;
	            UserCameras_MGR.instance.camTextures._Act_WillDestroy_RenderTex -= OnWillDestroyRenderTexture;
	        }
	    }

	    void OnCreatedNewRenderTexture(RenderTexture rt,  CameraTexType texType ){
	        if(texType != CameraTexType.ContentUserCam){ return; }
	        _camera.aspect = rt.width / (float)rt.height;
	    }

	    void OnWillDestroyRenderTexture( RenderTexture rt,  CameraTexType texType ){
	        if(texType != CameraTexType.ContentUserCam){ return; }
	    }
	#endregion

	}
}//end namespace
