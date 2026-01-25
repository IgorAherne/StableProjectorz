using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// References several UI buttons that enable/disable highlighting of icons when cursor is above them. 
	// Keeps those button values in-sync (so that all are pressed or unpressed together, etc)
	public class HighlightHoveredIcons_UI_MGR : ButtonCollection_UI_MGR{
	    public static HighlightHoveredIcons_UI_MGR instance { get; private set; } = null;

	    protected override void OnTogglePressed(ButtonToggle_UI tog, bool isOn){
	        StaticEvents.Invoke("HighlightHoveredIcons_UI_MGR:OnTogglePressed");
	        base.OnTogglePressed(tog, isOn);
	    }

	    protected override void Awake(){
	        if (instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;
	        base.Awake();
	    }
	}
}//end namespace
