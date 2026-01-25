using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// Used only for debugging.
	// Helps to understand who is keeping the Camera render-permisssion locks.
	// (our cameras will keep rendering if someone keep holding their lock).
	public class CameraTexture_PermisionsVisualizer : MonoBehaviour{

	#if UNITY_EDITOR
	    public List<Component> _contentCam_lockOwners = new List<Component>();
	    public List<Component> _vertColorsCam_lockOwners = new List<Component>();
	    public List<Component> _normalsCam_lockOwners = new List<Component>();
	    public List<Component> _depthCam_lockOwners = new List<Component>();

	    // Update is called once per frame
	    void LateUpdate(){
	        VisualizeLock(UserCameras_Permissions.contentCam_keepRendering, _contentCam_lockOwners);
	        VisualizeLock(UserCameras_Permissions.vertexColorsCam_keepRendering, _vertColorsCam_lockOwners);
	        VisualizeLock(UserCameras_Permissions.normalsCam_keepRendering, _normalsCam_lockOwners);
	        VisualizeLock(UserCameras_Permissions.depthCam_keepRendering, _depthCam_lockOwners);
	    }

	    void VisualizeLock(LocksHashset_OBJ lockHashset, List<Component> owners){
	        owners.Clear();
	        HashSet<object> lockers =  lockHashset.lockers_editorOnly;
	        foreach(object obj in lockers){
	            owners.Add(obj as Component);
	        }
	    }
	#endif
	}
}//end namespace
