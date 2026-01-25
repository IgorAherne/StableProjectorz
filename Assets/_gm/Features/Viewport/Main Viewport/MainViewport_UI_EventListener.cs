using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace spz {

	//sits on top of the MainViewport and can detect if cursor hovers it.
	public class MainViewport_UI_EventListener : MonoBehaviour{
	    public static MainViewport_UI_EventListener instance { get; private set; } = null;

	    bool _prevRaycastToSelf_rslt = false;
	    float _prevRaycastToSelf_time = -9999;//helps us to avoid raycasting towards self several times a frame.
    
	    bool _prevCursorInXSpanrslt = false;
	    float _prevCursorInXSpantime = -9999;//helps us to avoid checking cursor position several times a frame.


    
	    // Returns true even if cursor hovers a header, or some panel.
	    // As long as cursor is inside my horizontal span.
	    public bool IsCursorIn_my_width(){
	        float currTime = Time.unscaledTime;
	        if(currTime == _prevCursorInXSpantime){
	            return _prevCursorInXSpanrslt;//already checked during this frame, return result.
	        }
	        _prevCursorInXSpantime = currTime;
	        _prevCursorInXSpanrslt = false;
        
	        RectTransform rectTransform = GetComponent<RectTransform>();
	        Vector2 localPoint;
	        Vector2 cursorScreenPos = KeyMousePenInput.cursorScreenPos();
	        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, cursorScreenPos, null, out localPoint)){
	            float halfWidth = rectTransform.rect.width / 2;
	            _prevCursorInXSpanrslt = localPoint.x >= -halfWidth && localPoint.x <= halfWidth;
	        }
	        return _prevCursorInXSpanrslt;
	    }


	    public bool TryRaycastTowardsSelf(){
	        float currTime = Time.unscaledTime;
	        if(currTime == _prevRaycastToSelf_time){
	            return _prevRaycastToSelf_rslt;//already raycasted during this frame, return result.
	        }
	        _prevRaycastToSelf_time = currTime;
	        _prevRaycastToSelf_rslt = false;

	        RaycastToSelf();
	        return _prevRaycastToSelf_rslt;
	    }

	    void RaycastToSelf(){
	        Vector2 cursorScreenPos = KeyMousePenInput.cursorScreenPos();
	        PointerEventData eventData = new PointerEventData( EventSystem.current );
	        eventData.position = cursorScreenPos;

	        // Previously we would raycast from 'list _raycastersUI'.
	        // But it's messy and bug-prone, we easily forget to assign new raycasters here.
	        // And I think iterating them is even more expensive than doing once RaycastAll()
	        List<RaycastResult> results = new List<RaycastResult>();
	        EventSystem.current.RaycastAll(eventData, results);

	        //see if there is ANY blocker visible, inside our raycast:
	        _prevRaycastToSelf_rslt = false;

	        foreach (var r in results){
	            if(r.gameObject.GetComponentInParent<MainViewport_RaycastBlocker>()){
	                _prevRaycastToSelf_rslt = false;
	                return; //blocked by this component.
	            }
	            MainViewport_UI_EventListener uiComponent = r.gameObject.GetComponentInParent<MainViewport_UI_EventListener>();
	            if (uiComponent != null){  _prevRaycastToSelf_rslt=true; }//don't stop yet, might find a raycastBlocker.
	        }
	    }

	    void Awake(){
	        if (instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;
	    }
	}

}//end namespace
