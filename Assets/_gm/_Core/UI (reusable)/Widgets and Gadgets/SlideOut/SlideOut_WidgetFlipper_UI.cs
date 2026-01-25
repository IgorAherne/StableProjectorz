using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace spz {

	// Helps the SlideOutWidget to mirror itself.
	// For example to mirror the panel leftwards if it was sticking out rightwards.
	// This is useful when adjusting placement of UI.
	// Takes care of undoing the flipping the special children, such as sliders and text.
	[RequireComponent(typeof(SlideOut_Widget_UI))]
	public class SlideOut_WidgetFlipper_UI : MonoBehaviour
	{
	    [SerializeField] SlideOut_Widget_UI _myWidget;
	    [SerializeField] Transform _maskTransf;
	    [SerializeField] Transform _submaskTransf;

	    public bool isFlipped { get; private set; } = false;

	    /// Flips the panel along the specified axis, adjusting pivots, anchors, and positions.
	    public void Flip(){
	        isFlipped = !isFlipped;
	        var rTransf =  transform as RectTransform;
	        Vector2 anchoredPosOld = rTransf.anchoredPosition;
        
	        if(_myWidget.isHoriz){
	            rTransf.anchorMax = new Vector2(1-rTransf.anchorMax.x,  rTransf.anchorMax.y);
	            rTransf.anchorMin = new Vector2(1-rTransf.anchorMin.x,  rTransf.anchorMin.y);
	            rTransf.anchoredPosition = new Vector2(-1*anchoredPosOld.x, anchoredPosOld.y);
	        }else{
	            rTransf.anchorMax = new Vector2(rTransf.anchorMax.x,  1-rTransf.anchorMax.y);
	            rTransf.anchorMin = new Vector2(rTransf.anchorMin.x,  1-rTransf.anchorMin.y);
	            rTransf.anchoredPosition = new Vector2(anchoredPosOld.x, -1*anchoredPosOld.y);
	        }

	        Vector3 flipVec = _myWidget.isHoriz? new Vector3(-1,1,1) : new Vector3(1,-1,1);

	        transform.localScale = transform.localScale.Multiply(flipVec);
	        _maskTransf.localScale = _maskTransf.localScale.Multiply(flipVec);
	        _submaskTransf.localScale = _submaskTransf.localScale.Multiply(flipVec);

	        Unflip_SpecialChildren(_submaskTransf, flipVec, typeof(SliderUI_Snapping), 
	                                typeof(CircleSlider_Snapping_UI), typeof(TextMeshProUGUI) );
	    }

	    void Unflip_SpecialChildren(Transform parent, Vector3 flipVec, params System.Type[] specials){
	        int childCount = parent.childCount;
	        for(int i=0; i<childCount; i++){
	            Transform child = parent.GetChild(i);
            
	            if(hasSpecialComponent(child)){
	                child.localScale = child.localScale.Multiply(flipVec);
	                //also, flip the rotation, - useful for vertical ribbons that had -90 rotation:
	                Vector3 angles   = child.localEulerAngles;
	                angles.z *= -1;
	                child.localEulerAngles = angles;
	                continue;//don't descend its children, we already "restored" this child.
	            }
	            Unflip_SpecialChildren(child, flipVec, specials);
	        }//end for

	        bool hasSpecialComponent(Transform tr){
	            int cnt = specials.Length;
	            for(int i=0; i<cnt; ++i){  
	                if(tr.GetComponent(specials[i])!=null){
	                    return true;
	                }  
	            }
	            return false;
	        }
	    }//end()

	}
}//end namespace
