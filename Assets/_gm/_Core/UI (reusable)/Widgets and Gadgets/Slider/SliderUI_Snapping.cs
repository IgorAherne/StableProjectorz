using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace spz {

	//we need non-drawing-graphic for a wider "background" surface, to capture RMB clicks easlier.
	[RequireComponent(typeof(RightMouseClickListener_UI), typeof(NonDrawingGraphic))]
	public class SliderUI_Snapping : MonoBehaviour{
	    [SerializeField] Slider _slider = null;
	    [SerializeField] IntegerInputField _int_input;
	    [SerializeField] FloatInputField _float_input;
	    [Space(10)]
	    [SerializeField] bool _adjustText = true;
	    [SerializeField] TextMeshProUGUI _text = null;
	    [SerializeField] string _prefix = "";
	    [SerializeField] bool _showOnlyPrefix = false;
	    [Space(10)]
	    [SerializeField] float _min = 1; // Define minimum slider value
	    [SerializeField] float _max = 100; // Define maximum slider value
	    [SerializeField] float _default = 512; // Define maximum slider value
	    [SerializeField] float _increment = 0.5f; // Define the increment value as a float
	    [SerializeField] int _decimalPlaces = 1; // Number of decimal places to display

	    bool _valueNeverChangedYet = true;
	    public UnityEvent<float> onValueChanged { get; } = new UnityEvent<float>();

	    bool _intInputFieldActive = false;
	    bool _floatInputFieldActive = false;
	    public float value{ get =>_slider.value; }

	    public void AdjustMinMax(Vector2 newMinMax, float newValueAndDefault, bool invokeCallback){
	        _slider.minValue = _min = newMinMax.x;
	        _slider.maxValue = _max = newMinMax.y;
	        _slider.SetValueWithoutNotify(newValueAndDefault);
	        _default = newValueAndDefault;
	        if(invokeCallback){ _slider.onValueChanged?.Invoke(_slider.value); }
	    }

	    void Start(){
	        // Set the slider's min and max values
	        _slider.minValue = _min;
	        _slider.maxValue = _max;
	        _slider.wholeNumbers = false; // Allow for decimal increments
	        _slider.onValueChanged.AddListener(OnSliderValChanged);
	        _int_input?.onValidInput.AddListener(OnInputFieldEndEdit); // Add listener for when user finishes editing
	        _float_input?.onValidInput.AddListener(OnFloatFieldEndEdit);
	        // Initialize text and slider value
	        if(_valueNeverChangedYet){
	            SetSliderValue(_default, false);
	        }
	        GetComponent<RightMouseClickListener_UI>().OnRightClick +=  ()=>{ _slider.value = _default; };
	    }

	    void OnSliderValChanged(float val){
	        if (!_intInputFieldActive){// Snap to increment only if the change is not from the input field
	            val = SnapToIncrement(val, _increment);
	        }
	        val = Mathf.Clamp(val, _min, _max);
	        _slider.SetValueWithoutNotify(val);//important.

	        UpdateText(val);
	        onValueChanged?.Invoke(val);
	        _valueNeverChangedYet = false;
	    }

	    void UpdateText(float val){
	        if(_text == null){ return; }
	        string format = "F" + _decimalPlaces;
	        //invariant to ensure that 8 will indeed be 8.0 if "F1" and not merely "8":
	        string inputText = val.ToString(format, System.Globalization.CultureInfo.InvariantCulture);

	        if(_adjustText){ 
	            _text.text =  _showOnlyPrefix?  _prefix  :  (_prefix + " " + inputText);
	        }

	        if (!_intInputFieldActive){
	            _int_input?.SetValueWithoutNotify(inputText); // Update the input field text only if the change is not from the input field
	        }
	        if (!_floatInputFieldActive) { 
	            _float_input?.SetValueWithoutNotify(inputText, format);
	        }
	    }

	    public void SetSliderValue(float value, bool invokeCallback){
	        value = Mathf.Clamp(value, _min, _max);

	        if (invokeCallback){
	            _slider.value = value;
	        }else{
	            _slider.SetValueWithoutNotify(value);
	        }
	        UpdateText(value);
	        _valueNeverChangedYet = false;
	    }

	    float SnapToIncrement(float value, float increment){
	        return Mathf.Round(value / increment) * increment;
	    }

	    void OnInputFieldEndEdit(int inputVal){
	        inputVal = (int)Mathf.Clamp((float)inputVal, _min, _max);
	        _intInputFieldActive = true; // Indicate that the change is coming from the input field
	        SetSliderValue((float)inputVal, true);
	        _intInputFieldActive = false; // Reset the flag after the value is set
	        _valueNeverChangedYet = false;
	    }

	    void OnFloatFieldEndEdit(float inputVal){
	        inputVal = Mathf.Clamp(inputVal, _min, _max);
	        _floatInputFieldActive = true; // Indicate that the change is coming from the input field
	        SetSliderValue((float)inputVal, true);
	        _floatInputFieldActive = false; // Reset the flag after the value is set
	        _valueNeverChangedYet = false;
	    }
	}
}//end namespace
