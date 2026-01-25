using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Helps the MultiView ui ribbon.
	// Will hide or show the Sort-viewports ui button, 
	// depending on whether we are in Editing mode or not.
	public class MultiView_SortButtons_UI : MonoBehaviour
	{
	    [SerializeField] Button _button;
	    [SerializeField] GameObject _space;

	    public Action onClick { get; set; }

	    void OnStartEditMode( MultiView_StartEditMode_Args args){
	        gameObject.SetActive(false);
	        _space.SetActive(false);
	    }

	    void OnStopEditMode(MultiView_StopEdit_Args args){
	        gameObject.SetActive(true);
	        _space.SetActive(true);
	    }

	    void Awake(){
	        MultiView_Ribbon_UI.OnStartEditMode += OnStartEditMode;
	        MultiView_Ribbon_UI.OnStop1_EditMode += OnStopEditMode;
	    }

	    void Start(){
	        _button.onClick.AddListener( ()=>onClick?.Invoke() );
	    }

	    void OnDestroy(){
	        _button?.onClick?.RemoveAllListeners();
	    }
	}
}//end namespace
