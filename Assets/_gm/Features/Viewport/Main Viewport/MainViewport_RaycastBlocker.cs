using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// Copmponent that prevents raycast from reaching the main viewport. (we manually notice it)
	// For example, can be on an invisible panel, which is on top of main viewport.
	// This rpevents the main viewport from "being hovered".
	public class MainViewport_RaycastBlocker : MonoBehaviour{
    
	}
}//end namespace
