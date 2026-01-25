using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class IsShowTextureSizes_UI : ButtonCollection_UI_MGR{
	    public static IsShowTextureSizes_UI instance { get; private set; } = null;

	    protected override void OnTogglePressed(ButtonToggle_UI but, bool isOn){
	        StaticEvents.Invoke("IsShowTextureSizes_UI:OnTogglePressed", isOn);
	        base.OnTogglePressed(but, isOn);
	    }

	    protected override void Awake(){
	        if(instance != null){  DestroyImmediate(this); return; }
	        instance = this;
	        base.Awake();
	    }
	}


	// Controls collection of buttons that 'show info' of texture icons.
	// Synchronises all buttons to the same value.
	// This means it synchronizes buttons in headers header even if they are hidden/inactive.
	// We'll still ensure their buttons update to required value and trigger their events.
	public class ButtonCollection_UI_MGR : MonoBehaviour
	{
	    [Space(10)]
	    [SerializeField] bool _beginPressed = true;
	    [SerializeField] protected List<ButtonToggle_UI> _toggles; //could be empty
	    [SerializeField] protected List<Button> _buttons; //could be empty
	    [Space(10)]
	    [SerializeField] bool _writeIntoPlayerPrefs = true;
	    [SerializeField] string _playerPrefsSuffix = "";

	    bool _isBroadcasting = false;
    
	    public System.Action<bool> onTogglePressed { get; set; } = null;
	    public System.Action onButtonPressed { get; set; } = null;
	    public bool isToggleOn { get; private set; }
    
	    protected virtual void OnTogglePressed(ButtonToggle_UI but, bool isOn){
	        if (_isBroadcasting){ return; }
	        _isBroadcasting = true;//to avoid infinite recursions, as we force their values to be same.

	        isToggleOn = isOn;
	        foreach(ButtonToggle_UI b in _toggles){
	            if(b==but){ continue; }
	            b.ForceSameValueAs(isOn);
	        }
	        onTogglePressed?.Invoke(isOn);
	        _isBroadcasting = false;

	        if (_writeIntoPlayerPrefs){
	            string prefsName = this.GetType().ToString() + _playerPrefsSuffix;
	            PlayerPrefs.SetInt( prefsName, isOn?1:0);
	            PlayerPrefs.Save();
	        }
	    }

	    protected virtual void OnButtonPressed(Button but){
	        onButtonPressed?.Invoke();
	    }

	    protected virtual void Awake(){
	        string prefsName = this.GetType().ToString() + _playerPrefsSuffix;
	        int prefsVal = PlayerPrefs.GetInt(prefsName, -1); //default is -1 (if never existed)
	        if(prefsVal==0){  _beginPressed = false;  }
	        else if(prefsVal == 1){  _beginPressed = true;  }

	        isToggleOn = _beginPressed;
	        foreach (ButtonToggle_UI but in _toggles){ 
	            but.onClick += (isOn)=>OnTogglePressed(but,isOn);
	            but.ForceSameValueAs(isToggleOn);
	        }
	        _buttons.ForEach( b=>b.onClick.AddListener(()=>OnButtonPressed(b)) );
	    }

	}
}//end namespace
