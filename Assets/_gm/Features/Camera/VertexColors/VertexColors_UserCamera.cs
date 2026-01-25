using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	public class VertexColors_UserCamera : MonoBehaviour
	{
	    [SerializeField] Camera _camera;
	    [SerializeField] View_UserCamera _view_camera_inParent;

	    [Space(10)]
	    [SerializeField] Shader _vertColorsShader;


	    public void RenderVertexColors(RenderTexture here, CameraClearFlags flags){

	        if(UserCameras_Permissions.vertexColorsCam_keepRendering.isLocked() == false){ return; }

	        var prevParams = new ParamsBeforeRender(_camera);
	            _camera.SetReplacementShader(_vertColorsShader, "");
	            _camera.targetTexture = here;
	            _camera.clearFlags = flags;
	            _camera.allowMSAA = false;//else produces flickering of depth image.
            
	            _camera.Render();
	            _camera.ResetReplacementShader();
	        prevParams.RestoreCam(_camera);
	    }

	    void OnPreCull(){
	        Texture tex = _camera.targetTexture;
	        _camera.aspect = tex.width / (float)tex.height;
	        CameraTools.ShiftViewportCenter_ofProjMat( _camera,  _view_camera_inParent._projectionMat_center );
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


	    public void OnInit(){
	        _camera.enabled = false;//Keep disabled.  Render() still works + avoids automatic renders.
	        UserCameras_MGR.instance.camTextures._Act_CreatedNewRenderTexture -= OnCreatedNewRenderTexture;
	        UserCameras_MGR.instance.camTextures._Act_CreatedNewRenderTexture += OnCreatedNewRenderTexture;
	        UserCameras_MGR.instance.camTextures._Act_WillDestroy_RenderTex -= OnWillDestroyRenderTexture;
	        UserCameras_MGR.instance.camTextures._Act_WillDestroy_RenderTex += OnWillDestroyRenderTexture;
	    }

	    void OnDestroy(){
	        _camera.targetTexture = null;
	        if (UserCameras_MGR.instance != null){
	            UserCameras_MGR.instance.camTextures._Act_CreatedNewRenderTexture -= OnCreatedNewRenderTexture;
	            UserCameras_MGR.instance.camTextures._Act_WillDestroy_RenderTex -= OnWillDestroyRenderTexture;
	        }
	    }

	    void OnCreatedNewRenderTexture(RenderTexture rt,  CameraTexType texType ){
	        if(texType != CameraTexType.VertexColorsUserCamera){ return; }
	        _camera.aspect = rt.width / (float)rt.height;
	    }
    
	    void OnWillDestroyRenderTexture( RenderTexture rt,  CameraTexType texType ){
	        if(texType != CameraTexType.VertexColorsUserCamera){ return; }
	    }
	}
}//end namespace
