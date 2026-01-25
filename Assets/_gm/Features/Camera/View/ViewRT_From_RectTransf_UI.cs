using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// UI script - maintains a render texture, ensuring it has the size equal to the rect-transform.
	// Also, able to Bind itself, so others can locate this script even from other scenes after loading.
	public class ViewRT_From_RectTransf_UI : RenderTexture_from_RectTransform{

	    protected override RectTransform GetTargetRectTransform()
	    {
	        // find the component sitting on our "target ui element", whose size we are observing:
	        string id = nameof(ViewRT_Destination_UI);
	        ViewRT_Destination_UI destin = EventsBinder.FindComponent<ViewRT_Destination_UI>(id);
	        return destin?.transform as RectTransform;
	    }
	}
}//end namespace
