using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace spz {

	//usually needs a graphic such as NonDrawingGraphic or Image on same gameObject.
	//Can feel the 2D-raycast, and sends event when we press it or let go.
	public class MouseGrab_Sensor_UI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler{
	    public bool isGrabbed { get; private set; } = false;
	    public System.Action<PointerEventData> _onPointerDown { get; set; } = null;
	    public System.Action<PointerEventData> _onPointerUp { get; set; } = null;

	    public void OnPointerDown(PointerEventData eventData){ 
	        isGrabbed=true;  
	        _onPointerDown?.Invoke(eventData);
	    }

	    public void OnPointerUp(PointerEventData eventData){ 
	        isGrabbed=false;  
	        _onPointerUp?.Invoke(eventData); 
	    }

	}
}//end namespace
