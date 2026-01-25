using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	public class ViewCamera_FOV : MonoBehaviour{

	    [SerializeField] View_UserCamera _viewCamera;

	    //used when we want to alter FOV but move the camera back to keep apparent size of object the same.
	    float _compensatedFOV_startDist;
	    float _compensatedFOV_initialFOV;
	    Vector3 _compensatedFOV_nearestPoint;
	    Vector3 _compensatedFOV_originalFwdDir;

	    //we will artificially adjust field of view sometimes,
	    //so this value helps us revert at the end of frame.
	    public float _trueCameraFov { get; private set; } = -1;
	    public void Remember_TrueFov(float fov) => _trueCameraFov = fov;


	    //used when we want to alter FOV but move the camera back to keep apparent size of object the same.
	    //Makes initial snapshots, which are then used as we increase or decrease the FOV via ui-slider.
	    public void Start_offsetCompensated_FOV(){
	        if(ModelsHandler_3D.instance == null){ return; }//Scenes are probably still loading

	        Bounds bounds   = ModelsHandler_3D.instance.GetTotalBounds_ofSelectedMeshes();
	        Vector3 nearest = bounds.ClosestPoint( transform.position );
	                nearest = Vector3.Lerp(nearest, bounds.center, 0.3f);//pulls it a tiny bit further into the bounds (looks better).
	        _compensatedFOV_startDist    = Vector3.Distance( transform.position,  nearest );
	        _compensatedFOV_nearestPoint = transform.position  +  transform.forward*_compensatedFOV_startDist;
	        _compensatedFOV_initialFOV   = _trueCameraFov;
	        _compensatedFOV_originalFwdDir = transform.forward;
	    }


	    public void SetFieldOfView(float wantedFOV, bool compensateByDistanceOffset=false){
	        _viewCamera.myCamera.fieldOfView  = _trueCameraFov  = wantedFOV;
        
	        if(compensateByDistanceOffset){
	            float offset = CameraTools.Calc_PosOffset_forFOVchange(_compensatedFOV_initialFOV, wantedFOV, _compensatedFOV_startDist);
	            transform.position = _compensatedFOV_nearestPoint - offset*_compensatedFOV_originalFwdDir;
	        }
	    }


	    public void Restore_FieldOfView(float fov){
	        Coroutines_MGR.instance.StartCoroutine( Restore_FOV_crtn(fov, _viewCamera.cameraFocus.restorationDur) );
	    }


	    IEnumerator Restore_FOV_crtn(float wantedFOV, float dur){
	        float fromTime = Time.unscaledTime;
	        float fromFOV = _trueCameraFov;
	        while (true){
	            float elapsed01 = (Time.unscaledTime - fromTime) / dur;
	            elapsed01 = Mathf.Clamp01(elapsed01);
	            float lerpFactor01 = Mathf.SmoothStep(0, 1, elapsed01);
	            float fov = Mathf.Lerp(fromFOV, wantedFOV, lerpFactor01);
	            SetFieldOfView(fov);
	            if (elapsed01 == 1) { break; }
	            yield return null;
	        }
	    }

	}
}//end namespace
