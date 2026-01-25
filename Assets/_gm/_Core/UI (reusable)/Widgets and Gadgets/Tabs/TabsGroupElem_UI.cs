using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	//belongs to TabsGroup_UI, sits inside it together with other tab-elements.
	public class TabsGroupElem_UI : MonoBehaviour{
	    [SerializeField] Button _button;
	    [SerializeField] GameObject _go_active;
	    [SerializeField] GameObject _go_inactive;
	    [SerializeField] GameObject _dividerLeft;// Dividers are a separation-line between inactive tabs.
	    [SerializeField] GameObject _dividerRight;

	               public string title => _title;
	    [SerializeField] string _title;

	    bool _isInvoking_onClicked = false;//prevents recursive stack overflow
	    public Action<TabsGroupElem_UI> onClicked { get; set; }



	    //only invoked by our group, not from here.
	    public void Toggle(bool isOn){
	        if(_isInvoking_onClicked){ return; }//our own callback, avoid recursion

	        if(_go_active!=null){  _go_active.SetActive(isOn); }
	        if(_go_inactive!=null){ _go_inactive.SetActive(!isOn); }
	        if(_dividerLeft!=null){ _dividerLeft.SetActive(!isOn); }
	        if(_dividerRight!=null){ _dividerRight.SetActive(!isOn); }

	        if (isOn) { 
	            _isInvoking_onClicked = true;
	            onClicked?.Invoke(this);
	            _isInvoking_onClicked = false;
	        }
	    }


	    public void DisableDivider(bool isLeft){
	        if(isLeft){
	            if(_dividerLeft!=null){ _dividerLeft?.SetActive(false); }
	        }else{
	            if(_dividerRight!=null){ _dividerRight?.SetActive(false); }
	        }
	    }//end()


	    void OnClicked() => Toggle(true);


	    void Start(){
	        _button.onClick.AddListener( OnClicked );
	    }
	}
}//end namespace
