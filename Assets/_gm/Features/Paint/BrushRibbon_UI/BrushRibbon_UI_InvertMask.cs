using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	//helps the 'BrushRibbon_UI' component.
	// Only deals with and represents the small button
	// that can flip the current mask.
	public class BrushRibbon_UI_InvertMask : MonoBehaviour{
	    [SerializeField] Button _button;

	    public static Action onClicked { get; set; }

	    void OnButtonPressed() =>  onClicked?.Invoke();

	    void Awake(){
	        _button.onClick.AddListener(OnButtonPressed);
	    }
	}
}//end namespace
