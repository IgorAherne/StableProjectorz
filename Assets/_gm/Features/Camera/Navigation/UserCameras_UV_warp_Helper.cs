using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// Helper of the 'UserCameras_MGR'.
	// Updates the 'warp_into_uv01' property so the uv-warping effect
	// looks gradual independent of camera's fov.
	// Without this, the low-fov cameras would have no visual effect
	// and then an abrupt snap into UV representation at the end.
	public class UserCameras_UV_warp_Helper : MonoBehaviour{
	    public static UserCameras_UV_warp_Helper instance { get; private set; }


	    [SerializeField] AnimationCurve _highFovSpeedCurve;
	    [SerializeField] float _warpSpeed = 1;

	    float _warp_into_uv01;
	    public float warp_into_uv01 => _warp_into_uv01;


	    void Update(){
	        float wanted_warp = _warp_into_uv01;

	        float fov         = UserCameras_MGR.instance._curr_viewCamera.myCamera.fieldOfView;
	        float isHighFov01 = Mathf.InverseLerp(1,90,fov);

	        float speed       = Mathf.Lerp( _warpSpeed,  _warpSpeed*_highFovSpeedCurve.Evaluate(_warp_into_uv01),  isHighFov01);
	              speed      *= Settings_MGR.instance.get_uvWarpSpeed();

	        float dt = Time.deltaTime*speed;

	        switch (DimensionMode_MGR.instance._dimensionMode){
	            case DimensionMode.dim_uv:
	                float t = Mathf.Clamp(_warp_into_uv01, 0.0001f, 0.9999f);
	                // For 0 --> 1: slow down as we approach 1
	                float dt_adjusted = dt * (1 - t);
	                wanted_warp += dt_adjusted;
	                break;

	            case DimensionMode.dim_sd:
	            case DimensionMode.dim_gen_3d:
	            default:
	                // Scale by an extra speed, IF we are High fov (90) and if we are going (0 <-- 1)
	                // Especially if we are close to 1. Using Pow to give it a sharp spike close at 1.
	                float extraSpeed = Mathf.Lerp(1, 1.55f, Mathf.Pow(_warp_into_uv01*isHighFov01,4));
	                dt *= extraSpeed;
	                t = Mathf.Clamp(_warp_into_uv01, 0.0001f, 0.9999f);
	                // For 1 --> 0: slow down as we approach 0
	                dt_adjusted = dt * (1 - t); 
	                wanted_warp -= dt_adjusted;
	                break;
	        }
	        _warp_into_uv01 = Mathf.Clamp01(wanted_warp);
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this; 
	    }
	}
}//end namespace
