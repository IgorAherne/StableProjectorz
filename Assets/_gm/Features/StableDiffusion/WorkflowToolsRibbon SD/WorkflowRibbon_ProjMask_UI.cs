using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace spz {

	public interface IWorkflowModeToggle{
	    void EnableToggle(bool playAttentionAnim = false);
	}


	public class WorkflowRibbon_ProjMask_UI : MonoBehaviour, IWorkflowModeToggle{
    
	    [SerializeField] Toggle _toggle;
	    [SerializeField] Animation _anim;
	    public bool isOn => _toggle.isOn;

	    public Action<bool> onValueChanged;

	    bool _isDoingCallback = false;


	    // 1 because we start on this tooltip when the game is loaded.
	    // So, we want to prevent initiating a hint-delay during the start, because it's not the user who enabled us.
	    float _next_hintTime  = 1;
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
	        string msg = "Projection-Masking: Use Eraser or Brush  to remove/restore the projection." +
	                     "\nHold 'R' to see projection better.  1,2,3,4 etc for the Brush Strength.";
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
