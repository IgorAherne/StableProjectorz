using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace spz {

	public class MultiView_CamerasFOV : MonoBehaviour
	{
	    [SerializeField] TextMeshProUGUI _cam_FOV_numberText;
	    [SerializeField] SliderUI_Snapping _camera_FOV_slider;

	    bool _fov_isSendingCallback = false;//helps avoid recursion.
	    bool _fovSliderBeingPressed = false;
	    void OnFovSliderPressed(){
	        if(_fovSliderBeingPressed){ return; }
	        _fovSliderBeingPressed = true;
	        UserCameras_MGR.instance.StartFOV_compensatedAdjustment();
	    }

	    void OnFovSliderReleased(){
	        _fovSliderBeingPressed = false;
	    }

	    void OnFOV_slider(float value){
	        _cam_FOV_numberText.text = Mathf.RoundToInt(value).ToString();
	        // If value is set directly (from code), ensure Pressed/Released callbacks
	        // are still invoked! It's important, to initiate the fov-compensated-adjustment, etc.
	        bool wasntPressed = !_fovSliderBeingPressed;
	        if(wasntPressed){ OnFovSliderPressed(); }
	        _fov_isSendingCallback = true;
	        UserCameras_MGR.instance.SetFieldOfView_allCameras(value);
	        _fov_isSendingCallback = false;
	        if(wasntPressed){ OnFovSliderReleased(); }
	    }

	    //if was changed through code, not because of our own fov slider:
	    void OnCameraMGR_FovChanged(float fov){
	        if(_fov_isSendingCallback){ return; }//skip, it's due to our own callback.
	        _camera_FOV_slider.SetSliderValue(fov, invokeCallback:false);
	    }

	    void OnCameraPlacements_Restored(GenData2D genData){
	        if (genData.povInfos.numEnabled==0){ return; }
	        float fov =  genData.povInfos.get_Nth_active_pov(0).camera_fov;
	        _camera_FOV_slider.SetSliderValue(fov, invokeCallback:false);
	        _cam_FOV_numberText.text = Mathf.RoundToInt(fov).ToString();
	    }

	    void Awake(){
	        UserCameras_MGR._Act_OnRestoreCameraPlacements += OnCameraPlacements_Restored;
	        UserCameras_MGR._Act_OnFovChanged += OnCameraMGR_FovChanged;

	        _camera_FOV_slider.onValueChanged.AddListener( OnFOV_slider );
	        EventTrigger.Entry entryDown = new EventTrigger.Entry();
	        entryDown.eventID = EventTriggerType.PointerDown;
	        entryDown.callback.AddListener( (data)=>OnFovSliderPressed() );

	        EventTrigger.Entry entryUp = new EventTrigger.Entry();
	        entryUp.eventID = EventTriggerType.PointerUp;
	        entryUp.callback.AddListener((data) => OnFovSliderReleased());

	        _camera_FOV_slider.GetComponent<EventTrigger>().triggers.Add(entryDown);
	        _camera_FOV_slider.GetComponent<EventTrigger>().triggers.Add(entryUp);
	    }

	    void OnDestroy(){
	        UserCameras_MGR._Act_OnRestoreCameraPlacements -= OnCameraPlacements_Restored;
	        UserCameras_MGR._Act_OnFovChanged -= OnCameraMGR_FovChanged;
	    }

	}
}//end namespace
