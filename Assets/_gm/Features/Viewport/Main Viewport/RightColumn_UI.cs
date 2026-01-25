using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// Represents a large portion of UI on the right of the Viewport.
	// Parent of several other tabs, such as art icons, meshes and controlnets.
	public class RightColumn_UI : MonoBehaviour
	{
	    void Update(){
	        // ensure our recttransform has the same placement as defined by the ui-skeleton:
	        Global_Skeleton_UI.instance.Place_onto_RightColumn(transform as RectTransform);
	    }
	}
}//end namespace
