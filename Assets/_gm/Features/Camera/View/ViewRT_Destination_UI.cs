using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// Sits on a UI element that supplies RectTransform dimensions, and that has RawImage component.
	// Other script will look at us, and understand the size of RenderTexture to be made.
	public class ViewRT_Destination_UI : MonoBehaviour{
	    void Start(){
	        EventsBinder.Bind_Clickable_to_event(nameof(ViewRT_Destination_UI), this);
	    }
	}
}//end namespace
