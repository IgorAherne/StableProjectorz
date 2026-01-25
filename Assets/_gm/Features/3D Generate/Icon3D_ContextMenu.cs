using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace spz {

	//belongs to 'Icon3D_UI' element.

	public class Icon3D_ContextMenu : MonoBehaviour{

	    [SerializeField] GameObject _contextMenu_go; //holds all controls for the context menu.
	    [Space(10)]
	    [SerializeField] Button _exportMeshButton;
	    [SerializeField] Button _generateButton;
	    [SerializeField] TextMeshProUGUI _text;

	    float _confirmByTime;

	    public Action onGenerateButton;
	    public bool isShowing => _contextMenu_go.activeSelf;
    

	    public void Toggle(bool isOn){
	        _contextMenu_go.SetActive(isOn);
	    }

	    void OnExportMeshButton(){
	        ModelsHandler_3D.instance.ExportModel();
	    }

	    void OnGenerateButton(){
	        if (Time.time > _confirmByTime){
	            _text.text = "ok?";
	            _confirmByTime = Time.time + 1.0f;
	        }else{
	            _text.text = "GEN";
	             onGenerateButton?.Invoke();
	        }
	    } 

	    void Update(){
	        if(Time.time > _confirmByTime){ _text.text = "GEN"; }
	    }

	    void Start(){
	        _exportMeshButton.onClick.AddListener(OnExportMeshButton);
	        _generateButton.onClick.AddListener(OnGenerateButton);
	    }
	}
}//end namespace
