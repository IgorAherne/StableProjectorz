using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	[RequireComponent(typeof(Camera))]
	public class Camera_KeepBoundingBoxVisible : MonoBehaviour
	{
	    [SerializeField] Camera _camera;
	    [SerializeField] Transform _cameraTransf;
	    [SerializeField] int _numCalibration_iters = 12;

    
	    //pivot is our parent, and _cameraTransf is its child. Think of it like a selfie-stick.
	    public void Ensure_BoundsVisible(ref Bounds bounds, Transform myPivot_willMove){
	        int numIter = _numCalibration_iters;
	        myPivot_willMove.transform.position = bounds.center;

	        float prevMagnitude = bounds.size.magnitude*4;
	        float currmagnitude = prevMagnitude*0.5f;
	        float tooClose = 0;

	        while (numIter>0){
	            _cameraTransf.localPosition = Vector3.back*currmagnitude;
	            bool isInside = isInsideView(ref bounds);
	            if(isInside){
	                prevMagnitude = currmagnitude;
	                currmagnitude = (currmagnitude+tooClose)*0.5f;
	            }else { 
	                tooClose = currmagnitude;
	                currmagnitude = (prevMagnitude+currmagnitude)*0.5f;
	            }
	            numIter--;
	        }
	    }


	    bool isInsideView(ref Bounds bounds){
	        for (int i = 0; i < 8; i++){
	            Vector3 worldCorner = bounds.center + new Vector3(
	                (i & 1) == 0 ? -bounds.extents.x : bounds.extents.x,
	                (i & 2) == 0 ? -bounds.extents.y : bounds.extents.y,
	                (i & 4) == 0 ? -bounds.extents.z : bounds.extents.z);

	            Vector3 localCorner = _camera.WorldToViewportPoint(worldCorner);
	            if(localCorner.z < 0){ return false; }
	            if(localCorner.x < 0.1f || localCorner.x > 0.9f){ return false; }
	            if(localCorner.y < 0.1f || localCorner.y > 0.9f){ return false; }
	        }
	        return true;
	    }


	}
}//end namespace
