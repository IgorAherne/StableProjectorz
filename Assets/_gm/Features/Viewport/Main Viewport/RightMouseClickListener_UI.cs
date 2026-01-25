using UnityEngine;
using UnityEngine.EventSystems; // Required for event handling
using System;

namespace spz {

	public class RightMouseClickListener_UI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler{
	    public Action OnRightClick { get; set; } = null;

	    bool _hasPointer = false;
	    public void OnPointerEnter(PointerEventData eventData) => _hasPointer = true;
	    public void OnPointerExit(PointerEventData eventData) => _hasPointer = false;

	    void Update(){
	        if(!_hasPointer){ return; }
	        bool rmb = KeyMousePenInput.isRMBpressedThisFrame();
	        if (rmb){  OnRightClick?.Invoke(); }
	    }
	}
}//end namespace
