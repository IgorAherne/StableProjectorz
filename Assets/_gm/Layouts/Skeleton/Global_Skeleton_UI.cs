using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	//different UI sections (located in other scenes) can copy our rect transform values, to position themselves.
	public class Global_Skeleton_UI : MonoBehaviour{
	    public static Global_Skeleton_UI instance { get; private set; } = null;

	    [SerializeField] RectTransform _leftColumn_rTransf;
	    [SerializeField] RectTransform _mainViewport_rTransf;
	    [SerializeField] RectTransform _rightColumn_rTransf;

	    public void Place_onto_LeftColumn(RectTransform place_me){
	        place_me.CopyValsFrom(_leftColumn_rTransf);
	    }
	    public void Place_onto_MainViewport(RectTransform place_me){
	        place_me.CopyValsFrom(_mainViewport_rTransf);
	    }

	    public void Place_onto_MainViewport_between_ribbons(RectTransform place_me){
	        if(MainViewport_UI.instance == null){ return; }

	        // Cache original state
	        Transform originalParent = place_me.parent;
	        int originalSiblingIndex = place_me.GetSiblingIndex();

	        // 1. Temporarily parent to the target to inherit its coordinate space
	        // false = reset local position/rotation/scale to match target immediately
	        place_me.SetParent(MainViewport_UI.instance.mainViewportRect, false);

	        // 2. Force stretch to corners (fill the target completely)
	        place_me.anchorMin = Vector2.zero;
	        place_me.anchorMax = Vector2.one;
	        place_me.offsetMin = Vector2.zero;
	        place_me.offsetMax = Vector2.zero;

	        // 3. Return to original parent
	        // true = Unity will recalculate anchors/offsets to maintain the visual position we just set
	        place_me.SetParent(originalParent, true);
	        place_me.SetSiblingIndex(originalSiblingIndex);
	    }

	    public void Place_onto_RightColumn(RectTransform place_me){
	        place_me.CopyValsFrom(_rightColumn_rTransf);
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	    }
	}
}//end namespace
