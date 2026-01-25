using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace spz {

	public class Normals_UserCamera : MonoBehaviour
	{
	    [SerializeField] Camera _camera;
	    [SerializeField] View_UserCamera _view_camera_inParent;    


	    void LateUpdate(){//Late update because AFTER all the translate/rotate was done.
	         Camera vcam = _view_camera_inParent.myCamera;
	        _camera.fieldOfView = vcam.fieldOfView;
	        _camera.orthographic = vcam.orthographic;
	        _camera.orthographicSize = vcam.orthographicSize;
	    }


	    public void RenderNormals( RenderTexture here, bool ignore_nonSelected_meshes, CameraClearFlags flags ){
        
	        if(UserCameras_Permissions.depthCam_keepRendering.isLocked() == false){ return; }

	        Debug.Assert(here.dimension == TextureDimension.Tex2D,
	                     "expecting destination RenderTexture to be a 2D, not an array of images, etc");

	        var prevParams = new ParamsBeforeRender(_camera);
	            _camera.targetTexture = here;
	            _camera.clearFlags = flags;

	            int maskAll = LayerMask.GetMask("Geometry", "Default", "Geometry Hidden");
	            _camera.cullingMask = ignore_nonSelected_meshes ? LayerMask.GetMask("Geometry") : maskAll;
	            _camera.allowMSAA = false;//else produces flickering of depth image.
            
	            _camera.Render();
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
	        if(texType != CameraTexType.NormalsUserCamera){ return; }
	        _camera.aspect = rt.width / (float)rt.height;
	    }
    
	    void OnWillDestroyRenderTexture( RenderTexture rt,  CameraTexType texType ){
	        if(texType != CameraTexType.NormalsUserCamera){ return; }
	    }
	}
}//end namespace
