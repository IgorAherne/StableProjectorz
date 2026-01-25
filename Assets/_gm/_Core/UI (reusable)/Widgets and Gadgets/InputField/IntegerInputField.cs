using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System.Globalization;


namespace spz {

	[System.Serializable]
	public class IntEvent : UnityEvent<int> { }

	//accepts text only if it's a whole number.
	public class IntegerInputField : MonoBehaviour{
	    [SerializeField] private TMP_InputField _inputField;
	    [SerializeField] int _default = 0;
	    [SerializeField] int _min_val = int.MinValue;
	    [SerializeField] int _max_val = int.MaxValue;

	    bool _valueNeverChangedYet = true;

	    int _recentVal = 0;
	    public int recentVal => _recentVal;
	    public void SetMin(int min){ _min_val=min; if(_recentVal>_max_val){SetValueWithoutNotify($"{_max_val}"); }}
	    public void SetMax(int max){ _max_val=max; if(_recentVal<_min_val){SetValueWithoutNotify($"{_min_val}"); }}

	    public IntEvent onValidInput = new IntEvent();


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
	        if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)){
	            result = Mathf.Clamp(result, _min_val, _max_val);
	            _inputField.text = result.ToString();
	            _recentVal = result;
	            onValidInput?.Invoke(result);
	        }else{
	            _inputField.text = ""; // Clear the input field if the value is not an integer
	        }
	    }


	    public void SetValue(string text){
	        _valueNeverChangedYet = false;
	        bool success = SetValueWithoutNotify(text);
	        if (success){ onValidInput?.Invoke(_recentVal);  }
	    }


	    public bool SetValueWithoutNotify(string text){
	        _valueNeverChangedYet = false;

	        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)){
	            result = Mathf.Clamp(result, _min_val, _max_val);
	            _inputField.SetTextWithoutNotify(result.ToString());
	            _recentVal = result;
	            return true;
	        }
	        _inputField.text = "";
	        return false;
	    }
	}
}//end namespace
