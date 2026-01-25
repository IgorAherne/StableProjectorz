using UnityEngine;

namespace spz {

	//ensures the meshes are always visible and rendered to the screen.
	// This is imprtant because MainView_Camera is doing FOV adjustments at the very last moment
	public class DisableFrustrumCulling : MonoBehaviour{
	    [SerializeField] Camera _cam;

	    void Start(){
	        _cam = this.GetComponent<Camera>();
	    }

	    void OnPreCull(){
	        _cam.cullingMatrix = Matrix4x4.Ortho(-99999, 99999, -99999, 99999, 0.001f, 99999) *
	                            Matrix4x4.Translate(Vector3.forward * -99999 / 2f) *
	                            _cam.worldToCameraMatrix;
	    }

	    void OnDisable(){
	        _cam.ResetCullingMatrix();
	    }
	}
}//end namespace
