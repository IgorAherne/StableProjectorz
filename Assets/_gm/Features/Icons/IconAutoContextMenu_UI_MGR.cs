using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// References several UI buttons that enable/disable auto-context-menu feature.
	// Keeps those button values in-sync (so that all are pressed or unpressed together, etc)
	// The auto-context-menu helps to show the context menu of a chosen icon, when hovering it in the list.
	public class IconAutoContextMenu_UI_MGR : ButtonCollection_UI_MGR{
	    public static IconAutoContextMenu_UI_MGR instance { get; private set; } = null;

	    protected override void Awake(){
	        if(instance != null){  DestroyImmediate(this.gameObject); return; }
	        instance = this;
	        base.Awake();
	    }
	}
}//end namespace
