using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Sits on the same gameObject as 'Dropdown_NoGlobalAntiClick_UI'.
	// Helps it know which gameObjects to allow clicking on, while dropdown is expanded.
	// Otherwise, dropdowns prevent clicking anywhere while expanded.
	public class Dropdown_ClickThroughAllowThose_UI : MonoBehaviour{
	    public List<RectTransform> allowThose;
	}
}//end namespace
