using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// Shown inside the context menu, that user can show
	// by right-clicking inside the MainView window.
	public class GenerateButtons_Mini_UI : GenerateButtons_UI{
	    public static GenerateButtons_Mini_UI instance { get; private set; } = null;

	    protected void OnStartedGenerate_cb(){
	        _cancelGeneration_button.gameObject.SetActive(true);
	        _cancelGeneration_button.interactable = true;

	        if(_deleteLast_button!=null){  _deleteLast_button.gameObject.SetActive(false); }
	    }

	    protected void OnFinishedGenerate_cb(bool canceled){
	        if(_cancelGeneration_button.gameObject.activeSelf == false){ 
	            return; //important! helps avoid double invoke (with cancel:true and cancel:false)
	        }
	        _cancelGeneration_button.gameObject.SetActive(false);
	        _cancelGeneration_button.interactable = false;
	        if(_deleteLast_button!=null){  _deleteLast_button.gameObject.SetActive(!canceled); }
	    }

	    protected override void Awake(){
	        if (instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;
	        base.Awake();
	        //subscribe to our base class's action:
	        GenerateButtons_UI._Act_OnGenerate_started += OnStartedGenerate_cb;
	        GenerateButtons_UI._Act_OnGenerate_finished += OnFinishedGenerate_cb;
	        OnConfirmed_FinishedGenerate(canceled: true);//makes sure some buttons are hidden.
	    }
	}

}//end namespace
