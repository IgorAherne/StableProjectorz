using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// references a toggle on the ui-ribbon, for entering "Object Selection" mode.
	// Invokes event when OnToggled(), via StaticEvents.
	public class ClickSelectMeshes_Toggle_UI : MonoBehaviour{

	    [SerializeField] Toggle _selectMode_toggle;
	    [SerializeField] Animation _selectMode_toggleAnim;

	    public void PlayAnim(){
	        _selectMode_toggleAnim.Play();
	    }

	    public void SetIsOnWithoutNotify(bool isToggleOn){ 
	        // FIX: Use SetIsOnWithoutNotify to prevent triggering the 'onValueChanged' event loop
	        _selectMode_toggle.SetIsOnWithoutNotify(isToggleOn);
	    }

	    void OnToggled(bool isOn){
	        StaticEvents.Invoke(nameof(ClickSelectMeshes_Toggle_UI)+"_toggle", isOn);
	    }

	    void Awake(){
	        EventsBinder.Bind_Clickable_to_event(nameof(ClickSelectMeshes_Toggle_UI), this);
	        _selectMode_toggle.onValueChanged.AddListener(OnToggled);
	    }

	    void OnDestroy(){
	        _selectMode_toggle?.onValueChanged.RemoveListener(OnToggled);
	    }
	}
}//end namespace
