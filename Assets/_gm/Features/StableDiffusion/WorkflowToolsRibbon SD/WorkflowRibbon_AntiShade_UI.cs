using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class WorkflowRibbon_AntiShade_UI : MonoBehaviour, IWorkflowModeToggle
	{
	    [SerializeField] Toggle _toggle;
	    [SerializeField] Animation _anim;
	    public bool isOn => _toggle.isOn;

	    public Action<bool> onValueChanged;

	    bool _isDoingCallback = false;


	    public void EnableToggle(bool playAttentionAnim = false){
	        _toggle.isOn = true;
	        if(playAttentionAnim){ _anim.Play(); }
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
