using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace spz {

	// Helps to tint the slider of MultiCam ui ribbon.
	// Works depending on whether we are in Editing mode or not.
	public class MultiView_CamsSlider_Darkner_UI : MonoBehaviour
	{
	    [SerializeField] Image _sliderKnob;
	    [SerializeField] float _darkenToPcnt;

	    Vector3 _knobStartHSV;

	    void OnStartEditMode(MultiView_StartEditMode_Args args){
	        _sliderKnob.color = Color.HSVToRGB(_knobStartHSV.x, _knobStartHSV.y, _knobStartHSV.z*_darkenToPcnt);
	    }

	    void OnStopEditMode( MultiView_StopEdit_Args args ){
	        _sliderKnob.color = Color.HSVToRGB(_knobStartHSV.x, _knobStartHSV.y, _knobStartHSV.z);
	    }


	    void Awake(){
	        MultiView_Ribbon_UI.OnStartEditMode += OnStartEditMode;
	        MultiView_Ribbon_UI.OnStop1_EditMode += OnStopEditMode;

	        float h, s, v;
	        Color.RGBToHSV(_sliderKnob.color, out h, out s, out v);
	        _knobStartHSV = new Vector3(h, s, v);
	    }

	}
}//end namespace
