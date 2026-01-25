using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace spz {

	//renders whatever the camera sees using a depth replacement shader.
	public class Depth_UserCamera : MonoBehaviour{

	    [SerializeField] Camera _camera;
	    [SerializeField] View_UserCamera _view_camera_inParent;

	    [Space(10)]
	    [SerializeField] Shader _depthShader;
	    [SerializeField] Shader _blitDepthLatestCamera_add_shader; //additively blitting the colors to existing.

	    Material _blitDepthLatestCamera_add_mat; //additively blitting color to already existing texture
	    float _wantedAspect;

	    public float _nearPlane_forLinearDepth{get; private set;}//assigned once, during start
	    public float _farPlane_forLinearDepth {get; private set;}


	    public void RenderDepth_of_Objects( RenderTexture depthCam_RT_R32_linear,  RenderTexture depthCam_RT_R32_forContrast,  
	                                        CameraClearFlags clearFlags ){

	        if(UserCameras_Permissions.depthCam_keepRendering.isLocked() == false){ return; }

	        var prevParams = new ParamsBeforeRender(_camera);
	            _camera.aspect = depthCam_RT_R32_linear.width / (float)depthCam_RT_R32_linear.height;
	            _camera.clearFlags = clearFlags;
	            _camera.allowMSAA = false;//else produces flickering of depth image.

	            _camera.targetTexture = null;//don't render into tex, we'll paste rendered depth in separate blit.

	            View_UserCamera vcam = _view_camera_inParent;

	            //For linear depth, all the cameras must have the same near and far planes. Use those during the start:
	            renderCam( depthCam_RT_R32_linear, _nearPlane_forLinearDepth, _farPlane_forLinearDepth,  ensureLinearDepth:true );
            
	            // For contrast-depth, all the cameras must "hug" the meshes,
	            // so that intensity remains the same even if camera moves away.
	            // This will ensure their intensity is the same, regardless of distance.
	            // Later, it will allow us to do 1 contrast-compute invocation, instead of for 6 separate depths:
	            // Notice, it should be NON-LINEAR, edge-detection algorithms will work better with it.
	            //
	            // ONLY DOING IT FOR NEAR PLANE. Keep far plane at 1000, to keep all bad float imprecision away:
	            renderCam(depthCam_RT_R32_forContrast, vcam.tightNearPlane, 1000, ensureLinearDepth:false);

	        prevParams.RestoreCam(_camera);
	    }

	    void renderCam(RenderTexture rt, float nearPlane, float farPlane,  bool ensureLinearDepth){
	        _camera.nearClipPlane = nearPlane;
	        _camera.farClipPlane  = farPlane;
	        CameraTools.Set_POVs_properties_into_mat(null, _camera,  UserCameras_MGR.instance.get_viewCams_PovInfos() );
	        _camera.RenderWithShader(_depthShader,"");//populates the internal depth buffer.

	        _blitDepthLatestCamera_add_mat.SetFloat("_CloseIsWhite", 1);
	        _blitDepthLatestCamera_add_mat.SetFloat("_NearClip", _camera.nearClipPlane);
	        _blitDepthLatestCamera_add_mat.SetFloat("_FarClip", _camera.farClipPlane);

	        TextureTools_SPZ.SetKeyword_Material(_blitDepthLatestCamera_add_mat, "ENSURE_LINEAR_01_DEPTH", ensureLinearDepth);
	        Graphics.Blit(null, rt, _blitDepthLatestCamera_add_mat);
	    }


	    void OnPreCull(){
	        // NOTICE: very important to avoid culling. Otherwise can result in a sneaky visual bug:
	        //    1) importing alchemist-table.obj, isolating mesh 'candle', then enabling 2 multi-view cameras.
	        //    2) Switching to depth-preview and isolating to other meshes.
	        //       Some of meshes will be culled even after we focus on them.
	        _camera.cullingMatrix = Matrix4x4.Ortho(-100_000, 100_000, -100_000, 100_000, -100_000, 100_000);

	        CameraTools.ShiftViewportCenter_ofProjMat(_camera, _view_camera_inParent._projectionMat_center);
	    }


	    void OnPostRender(){
	        _camera.ResetCullingMatrix();
	        _camera.ResetProjectionMatrix();
	    }

	    public void OnUpdateParams(){
	        Camera vcam = _view_camera_inParent.myCamera;
	        _camera.fieldOfView = vcam.fieldOfView;
	        _camera.orthographic = vcam.orthographic;
	        _camera.orthographicSize = vcam.orthographicSize;
	    }


	    public void OnInit(){
	        _nearPlane_forLinearDepth = _camera.nearClipPlane;
	        _farPlane_forLinearDepth  = _camera.farClipPlane;
	        _camera.enabled = false;//Keep disabled.  Render() still works + avoids automatic renders.
	        _camera.depthTextureMode = DepthTextureMode.Depth;
	        UserCameras_MGR.instance.camTextures._Act_CreatedNewRenderTexture -= OnCreatedNewRenderTexture;
	        UserCameras_MGR.instance.camTextures._Act_CreatedNewRenderTexture += OnCreatedNewRenderTexture;
	        UserCameras_MGR.instance.camTextures._Act_WillDestroy_RenderTex -= OnWillDestroyRenderTexture;
	        UserCameras_MGR.instance.camTextures._Act_WillDestroy_RenderTex += OnWillDestroyRenderTexture;

	        _blitDepthLatestCamera_add_mat = new Material(_blitDepthLatestCamera_add_shader);
	    }


	    void OnDestroy(){
	        _camera.targetTexture = null;
	        if (UserCameras_MGR.instance != null){
	            UserCameras_MGR.instance.camTextures._Act_CreatedNewRenderTexture -= OnCreatedNewRenderTexture;
	            UserCameras_MGR.instance.camTextures._Act_WillDestroy_RenderTex -= OnWillDestroyRenderTexture;
	        }
	        if(_blitDepthLatestCamera_add_mat != null){ DestroyImmediate(_blitDepthLatestCamera_add_mat);  }
	    }

	    void OnCreatedNewRenderTexture(RenderTexture rt,  CameraTexType texType ){
	        if(texType != CameraTexType.DepthUserCamera){ return; }
	        _camera.aspect = rt.width / (float)rt.height;
	    }
    
	    void OnWillDestroyRenderTexture( RenderTexture rt,  CameraTexType texType ){
	        if(texType != CameraTexType.DepthUserCamera){ return; }
	    }


	}
}//end namespace
