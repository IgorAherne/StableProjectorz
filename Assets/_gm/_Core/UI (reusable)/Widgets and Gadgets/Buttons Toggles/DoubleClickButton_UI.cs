using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

namespace spz {

	public class DoubleClickButton_UI : MonoBehaviour
	{
	    [SerializeField] Button _button;
	    [SerializeField] Image _image_optional;
	    [FormerlySerializedAs("_text_optional")][SerializeField] TextMeshProUGUI _text;
	    [SerializeField] string _confirm_text;//what to show inside button when asking user to make a second click.
	    [SerializeField] float _secondClick_deadline = 1.2f;

	    string _startingText;
	    bool _awaitingConfirm = false;
	    float _prevClickTime = -999;

	    public System.Action onCheckClick { get; set; } = null;
	    //invoked only when user clicks a second time within the required time interval.
	    public System.Action onConfirmedClick { get; set; } = null;

	    void OnClickedButton(){
	        if (_awaitingConfirm){
	            _awaitingConfirm = false;
	            onConfirmedClick?.Invoke();
	            if(_image_optional!=null){ _image_optional.enabled = true; }
	            _text.text = _startingText;
	            return;
	        }
	        _awaitingConfirm = true;
	        _prevClickTime = Time.time;
	        _text.text = _confirm_text;
	        if(_image_optional!=null){ _image_optional.enabled = false; }
	        onCheckClick?.Invoke();
	    }

	    void Update(){
	        if (!_awaitingConfirm){ return; }
	        if (Time.time < _prevClickTime+_secondClick_deadline){ return; }
	        _awaitingConfirm = false;
	        _text.text = _startingText;
	        if(_image_optional!=null){ _image_optional.enabled = true; }
	    }


	    void Awake(){
	        _button.onClick.AddListener(OnClickedButton);
	        _startingText = _text.text;
	        if(_image_optional != null){ 
	            _text.text = "";
	            _startingText = "";
	        }
	    }

	    private void OnDestroy(){
	        if(_button != null){ _button.onClick.RemoveAllListeners(); }
	    }
	}
}//end namespace
