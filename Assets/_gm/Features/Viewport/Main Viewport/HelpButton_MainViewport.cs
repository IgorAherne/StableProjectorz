using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class HelpButton_MainViewport : MonoBehaviour
	{
	    [SerializeField] Button _button;

	    void Awake(){
	        EventsBinder.Bind_Clickable_to_event("HelpButton_MainViewport", this);
	    }

	}
}//end namespace
