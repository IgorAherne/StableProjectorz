using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace spz {

	public class RestartTheWebui_Trellis : RestartTheWebui{

	    [SerializeField] protected SlideOut_Widget_UI _precision_sliderOut;
	    [Space(10)]
	    [SerializeField] Toggle _fp16_toggle; //for setting precision (full, half)
	    [SerializeField] Toggle _fp32_toggle;

	    protected override string OnWillLaunchWebui_AdjustArgs(string path){
	        string precision = _fp16_toggle.isOn? "--precision half" : "--precision full";
	        return path + " " + precision;
	    }

    
	    protected virtual void OnButtonLaunch_Hover(PointerEventData p){
	        if(_precision_sliderOut == null) { return; }
	        _precision_sliderOut.Toggle_if_Different(true);
	    }


	    void OnToggleClicked(int mouseId){
	        base.OnStartWebuiButton();
	    }

	    protected override void Start(){
	        base.Start();

	        EventsBinder.Bind_Clickable_to_event("RestartWebui_TrellisInputPanelButton", base._launchButton);

	        base._launchButton.GetComponent<MouseHoverSensor_UI>().onSurfaceEnter += OnButtonLaunch_Hover;
	        _precision_sliderOut.Toggle_if_Different(false, slideDuration:0);
	        //user might not realze they can press the large button, so make sure toggles report click as well.
	        //We'll start server whent they are pressed:
	        _fp16_toggle.GetComponent<MouseClickSensor_UI>()._onMouseClick += OnToggleClicked;
	        _fp32_toggle.GetComponent<MouseClickSensor_UI>()._onMouseClick += OnToggleClicked;
	    }
	}
}//end namespace
