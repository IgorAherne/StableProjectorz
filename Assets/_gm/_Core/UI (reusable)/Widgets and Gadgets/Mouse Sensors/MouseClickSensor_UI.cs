using UnityEngine.EventSystems;
using UnityEngine;

namespace spz {

	//needs a graphic on the same gameObject, or a non-drawing-graphic.
	//This will allow it to "feel" the raycasts from cursor.
	public class MouseClickSensor_UI : MonoBehaviour, IPointerClickHandler{
	    float _disableAfter = -1;

	    public System.Action<int> _onMouseClick { get; set; } = null;
	    public void OnPointerClick(PointerEventData eventData){
	        _onMouseClick?.Invoke( (int)eventData.button );
	    }
	    public void ActivateFor(float disableAfter){
	        gameObject.SetActive(true);
	        _disableAfter = Time.time + disableAfter;
	    }

	    void Update(){
	        if(_disableAfter<0){ return; }
	        if(Time.time < _disableAfter){ return; }
	        gameObject.SetActive(false);
	    }
	}
}//end namespace
