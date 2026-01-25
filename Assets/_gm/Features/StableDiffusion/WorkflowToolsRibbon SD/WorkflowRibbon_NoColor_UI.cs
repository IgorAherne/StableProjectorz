using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class WorkflowRibbon_NoColor_UI : MonoBehaviour, IWorkflowModeToggle{
	    [SerializeField] Toggle _toggle;
	    [SerializeField] Animation _anim;
	    public bool isOn => _toggle.isOn;

	    public Action<bool> onValueChanged;

	    bool _isDoingCallback = false;

	    float _next_hintTime  = 0;
	    int _num_hintsShown = 0;
	    int _hints_spacing = 15;


	    static int _latestHintShown_frame = 0;
	    public static bool didShowHint_thisFrame(){ return _latestHintShown_frame==Time.frameCount;}


	    public void EnableToggle(bool playAttentionAnim=false){
	        _toggle.isOn = true;
	        if(playAttentionAnim){ _anim.Play(); }
	        ShowHint_maybe();
	    }

	    void ShowHint_maybe(){
	        if(Time.time < _next_hintTime){ return; }
	        if(_num_hintsShown > 3){ return; }
	        string msg = "No-Color-Inpaint:  GenArt will re-think the existing art,  according to the Re-do slider." +
	                     "\nIt is a mask, but without a color.  Helps to fix the seams by 're-thinking' them";
	        Viewport_StatusText.instance.ShowStatusText(msg, false, 6, false);
	        _num_hintsShown++;
	        _next_hintTime = Time.time + _hints_spacing*_num_hintsShown;
	        _latestHintShown_frame = Time.frameCount;
	    }

	    void OnValueChanged(bool isOn){
	        if(_isDoingCallback){ return; }//avoid recursion
	        _isDoingCallback = true;
	        onValueChanged?.Invoke(isOn);
	        _isDoingCallback = false;
	    }

	    void Awake(){
	        _toggle.onValueChanged.AddListener( OnValueChanged );
	    }
	}
}//end namespace
