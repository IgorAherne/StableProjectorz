using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	public class Proj_AutoSoftEdges_UI_MGR : ButtonCollection_UI_MGR{
	    public static Proj_AutoSoftEdges_UI_MGR instance { get; private set; } = null;

	    protected override void Awake(){
	        if(instance != null){  DestroyImmediate(this.gameObject); return; }
	        instance = this;
	        base.Awake();
	    }
	}
}//end namespace
