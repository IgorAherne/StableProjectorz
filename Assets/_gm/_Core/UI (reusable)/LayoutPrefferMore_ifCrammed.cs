using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Tells layout element to be more "aggressive", when there is little space.
	// This allows the element to "take domminance" when other sibling elements
	// still have some size-amount to spare (before their "min allowed" is reached).
	// Otherwise all siblings would be treated evenly in crammed conditions.
	public class LayoutPrefferMore_ifCrammed : MonoBehaviour{
	    [SerializeField] LayoutElement _layoutElem;
	    [SerializeField]  bool _isWidth = true;
	    [Space(10)]
	    [SerializeField] float _start_if_parentLessThan = 1000;
	    [SerializeField] float _max_if_parentLessThan  = 800;
	    [SerializeField] float _max_prefferedSize = 10000;

	    float _originalPreffered;

	    void Start(){
	        _originalPreffered = _isWidth ? _layoutElem.preferredWidth : _layoutElem.preferredHeight;
	        if(_start_if_parentLessThan < _max_if_parentLessThan){
	            Debug.LogError("_start_if_parentLessThan has to be greater than '_max_if_parentLessThan'"); 
	        }
	    }

	    void Update(){
	        Rect parentRect = (transform.parent as RectTransform).rect;
	        float curr = _isWidth? parentRect.width : parentRect.height;

	        float howMuch01 = Mathf.InverseLerp(_start_if_parentLessThan, _max_if_parentLessThan, curr);
	              howMuch01 = Mathf.Clamp01(howMuch01);

	        float newPreferred = Mathf.Lerp(_originalPreffered, _max_prefferedSize, howMuch01);

	        if (_isWidth){  
	            _layoutElem.preferredWidth = newPreferred;  
	        }else{  
	            _layoutElem.preferredHeight = newPreferred;  
	        }
	    }//end()
	}
}//end namespace
