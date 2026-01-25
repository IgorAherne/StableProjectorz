using System;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	//helps the 'BrushRibbon_UI' component.
	// Only deals with and represents the small button
	// that can flood the current mask (or color mask) to a solid value.
	public class BrushRibbon_UI_BucketFill : MonoBehaviour{

	    [SerializeField] BrushRibbon_UI _rib;
	    [Space(10)]
	    [SerializeField] Button _button;
	    [SerializeField] GameObject _icon_go;
	    [SerializeField] GameObject _confirmText_go;

	    float _confirmBy_time;
	    public static Action _Act_onClicked { get; set; }

	    public void OnFillFromCode(){
	        _Act_onClicked?.Invoke();
	        _confirmBy_time = 0;
	    }


	    void OnButtonPressed(){
	        if(Time.time < _confirmBy_time){ 
	            Viewport_StatusText.instance.ShowStatusText("Press Ctrl+F to Bucket-Fill easier :)", false, 4, false);
	            _Act_onClicked?.Invoke();
	            _confirmBy_time = 0;
	        }else{ 
	            _confirmBy_time = Time.time+1;
	        }
	    }

	    void Update(){ 
	        if(_button.gameObject.activeSelf == false){ return; }

	        _confirmText_go.SetActive( Time.time < _confirmBy_time );
	        _icon_go.SetActive( Time.time >= _confirmBy_time );

	        bool cmd_or_shift =   KeyMousePenInput.isKey_CtrlOrCommand_pressed() || KeyMousePenInput.isKey_Shift_pressed();
	        if(cmd_or_shift &&  Input.GetKeyDown(KeyCode.F) ){
	            OnFillFromCode();
	        }
	    }

	    void Awake(){
	        _button.onClick.AddListener(OnButtonPressed);
	    }
	}
}//end namespace
