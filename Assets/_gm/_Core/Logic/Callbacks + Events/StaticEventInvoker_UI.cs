using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace spz {

	// A reusable component that listens to a local UI element
	// and invokes a global StaticEvent when its value changes.
	//
	// Automatically detects standard Unity UI components as well as custom ones like
	// SliderUI_Snapping, IntegerInputField, and FloatInputField.
	public class StaticEventInvoker_UI : MonoBehaviour
	{
	    [Tooltip("The exact string ID of the event to invoke. For example  Settings:OpenSettingsPanel")]
	    [SerializeField] private string _eventID;

	    void Start(){
	        if (TryGetComponent<SliderUI_Snapping>(out var customSlider)){
	            customSlider.onValueChanged.AddListener((val) => StaticEvents.Invoke<float>(_eventID, val));
	            return;
	        }

	        if (TryGetComponent<IntegerInputField>(out var intInput)){
	            intInput.onValidInput.AddListener((val) => StaticEvents.Invoke<int>(_eventID, val));
	            return;
	        }
        
	        if (TryGetComponent<FloatInputField>(out var floatInput)){
	            floatInput.onValidInput.AddListener((val) => StaticEvents.Invoke<float>(_eventID, val));
	            return;
	        }
        
	        if (TryGetComponent<Button>(out var button)){
	            button.onClick.AddListener(() => StaticEvents.Invoke(_eventID));
	            return;
	        }

	        if (TryGetComponent<Toggle>(out var toggle)){
	            toggle.onValueChanged.AddListener((val) => StaticEvents.Invoke<bool>(_eventID, val));
	            return;
	        }

	        if (TryGetComponent<Slider>(out var slider)){
	            slider.onValueChanged.AddListener((val) => StaticEvents.Invoke<float>(_eventID, val));
	            return;
	        }
        
	        if (TryGetComponent<TMP_InputField>(out var tmpInputField)){
	            tmpInputField.onValueChanged.AddListener((val) => StaticEvents.Invoke<string>(_eventID, val));
	            return;
	        }
	    }
	}
}//end namespace
