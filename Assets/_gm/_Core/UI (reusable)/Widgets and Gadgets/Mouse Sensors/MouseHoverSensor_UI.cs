using UnityEngine;
using UnityEngine.EventSystems;

namespace spz {

	// Can "feel" when mouse cursor enters and exits it. 
	// Sends notification to whoever is subscribed to it.
	// Usually you need some kind of graphic nex to it, such as NonDrawingGraphic, or Image
	public class MouseHoverSensor_UI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler{
	    public System.Action<PointerEventData> onSurfaceExit { get; set; } = null;
	    public System.Action<PointerEventData> onSurfaceEnter { get; set; } = null;

	    public bool isHovering { get; private set; } = false;

	    public void OnPointerEnter(PointerEventData eventData){ 
	        isHovering=true; 
	        onSurfaceEnter?.Invoke(eventData); 
	    }

	    public void OnPointerExit(PointerEventData eventData){ 
	        isHovering=false; 
	        onSurfaceExit?.Invoke(eventData); 
	    }

	    private void OnApplicationFocus(bool focus){
	        if(Time.time == 0){
	            //just entered game, return else bugs appear in callbacks (elements haven't init yet)
	            return; 
	        }
	        //important! Feb 2024 user told that after opening File window they couldn't
	        //manipulate camera anymore, even after focusing the stable projectorz
	        var cursor = new PointerEventData(EventSystem.current);
	        cursor.position = new Vector2(0, 0);
	        OnPointerExit(cursor);
	    }
	}
}//end namespace
