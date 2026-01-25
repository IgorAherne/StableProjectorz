using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace spz {

	public class ConfirmPopup_UI : MonoBehaviour{
	    public static ConfirmPopup_UI instance { get; private set; }

	    [SerializeField] Button _background_button;
	    [SerializeField] TextMeshProUGUI _header;
	    [SerializeField] Button _yes;
	    [SerializeField] Button _no;
	    [SerializeField] TextMeshProUGUI _yesText;
	    [SerializeField] TextMeshProUGUI _noText;
	    Action _act_onYes;
	    Action _act_onNo;
	    bool _alreadyShownOrHidden = false;

	    private void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	    }

	    void Start(){
	        _yes.onClick.AddListener(OnYesClicked);
	        _no.onClick.AddListener(OnNoClicked);
	        _background_button.onClick.AddListener(OnBackgroundClicked);
	        if(!_alreadyShownOrHidden){ _background_button.gameObject.SetActive(false); }
	    }

	    void Update(){
	        if(Keyboard.current.escapeKey.wasPressedThisFrame){  OnNoClicked(); }
	    }

	    public void Show( string text,  Action onYes,  Action onNo, string yesText="Yes", string noText="No" ){
	        _background_button.gameObject.SetActive(true);
	        _header.text = text;
	        _act_onYes = onYes;
	        _act_onNo = onNo;
	        _yesText.text = yesText;
	        _noText.text = noText;
	        _alreadyShownOrHidden = true;
	    }

	    void OnYesClicked(){
	        Action act = _act_onYes;
	        _act_onYes = null;
	        _act_onNo = null;
	        act?.Invoke();
	        _background_button.gameObject.SetActive(false);
	    }

	    void OnNoClicked(){
	        Action act = _act_onNo;
	        _act_onYes = null;
	        _act_onNo = null;
	        act?.Invoke();
	        _background_button.gameObject.SetActive(false);
	    }

	    void OnBackgroundClicked(){
	        OnNoClicked();
	    }
	}
}//end namespace
