using UnityEngine;
using TMPro;
using System.Globalization;
using UnityEngine.Events;


namespace spz {

	[System.Serializable]
	public class FloatEvent : UnityEvent<float> { }


	//accepts text only if it's a decimal number.
	public class FloatInputField : MonoBehaviour
	{
	    [SerializeField] private TMP_InputField _inputField;
	    [SerializeField] float _default = 0;
	    bool _valueNeverChangedYet = true;
	    float _recentVal = 0;
	    public float recentVal => _recentVal;

	    public FloatEvent onValidInput = new FloatEvent();


	    private void Awake(){
	        _inputField?.onEndEdit.AddListener(ValidateAndInvoke);
	        if (_valueNeverChangedYet){
	            SetValueWithoutNotify( _default.ToString() );
	        }
	    }

	    private void ValidateAndInvoke(string input){
	        _valueNeverChangedYet = false;

	        //invariantCulture, because some users had empty text inside the input field, despite working on my pc.
	        //I suspect it might be to different locales/region where , is used instead of . etc.   August 2024
	        if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)){
	            _inputField.text = result.ToString();
	            _recentVal = result;
	            onValidInput.Invoke(result);
	        }else{
	            _inputField.text = ""; // Clear the input field if the value is not an float
	        }
	    }


	    public void SetValueWithoutNotify(string text, string floatDecimalsFormat = "F3")
	    {
	        _valueNeverChangedYet = false;
	        if (TryParseFloat(text, out float result)){
	            string strNumber = result.ToString(floatDecimalsFormat, CultureInfo.InvariantCulture);
	            _inputField.SetTextWithoutNotify(strNumber);
	            _recentVal = result;
	        }else{
	            _inputField.text = "";
	        }
	    }

	    private bool TryParseFloat(string input, out float result)
	    {
	        // Try parsing with invariant culture first
	        if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out result)){
	            return true;
	        }
	        // If that fails, try with the current culture
	        if (float.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out result)){
	            return true;
	        }
	        // If both fail, return false
	        result = 0f;
	        return false;
	    }
	}
}//end namespace
