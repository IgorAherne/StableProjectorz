using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace spz {

	//uses the string-id, to asosciate a component with events in our global-event-registry.
	public class BindEvents_UI : MonoBehaviour
	{
	    [SerializeField] Component _to_bind;
	    [SerializeField] string _eventId;

	    void Awake(){
	        StaticEvents.Bind_Clickable_to_event(_eventId, _to_bind);
	    }

	}
}//end namespace
