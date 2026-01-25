using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

namespace spz {

	public class ControlnetPreprocessor_UI : MonoBehaviour
	{
	    [SerializeField] ControlNetUnit_UI _unit;
	    [SerializeField] ControlNetUnit_Dropdowns _dropdowns;
	    [Space(10)]
	    [SerializeField] MouseHoverSensor_UI _preprocessorRes_hoverMe;
	    [SerializeField] SlideOut_Widget_UI _preprocessorRes_slideOut;
	    [SerializeField] Toggle _preprocessorRes_05;
	    [SerializeField] Toggle _preprocessorRes_1;
	    [SerializeField] Toggle _preprocessorRes_15; // x1.5 from the largest dimension in the Input panel
	    [SerializeField] Toggle _preprocessorRes_2;

	    bool _wasCreatedViaLoad = false;

	    public bool isReferencePreprocessor() => _dropdowns.isReferencePreprocessor();
	    public string currPreprocessorName() => _dropdowns.currPreprocessorName();
	    public bool is_currPreprocessor_none => _dropdowns.is_currPreprocessor_none;

	    public float get_processor_res(){
	        Vector2 widthHeight = SD_InputPanel_UI.instance.widthHeight();
	        int maxDim = Mathf.RoundToInt(  Mathf.Max(widthHeight.x, widthHeight.y)  );
	        if(_preprocessorRes_05.isOn){ return Mathf.RoundToInt(maxDim*0.5f); }
	        if(_preprocessorRes_1.isOn){  return Mathf.RoundToInt(maxDim*1); }
	        if(_preprocessorRes_15.isOn){ return Mathf.RoundToInt(maxDim*1.5f); }
	        if(_preprocessorRes_2.isOn){  return Mathf.RoundToInt(maxDim*2); }
	        return 512;
	    }


	    void OnPreprocessorResHover(bool isStoppedHover){ 
	        if(!isStoppedHover){ 
	            _preprocessorRes_slideOut.Toggle_if_Different(true); 
	            return; 
	        }
	    }

	    void OnPreprocessorToggle(Toggle tog, bool isOn){
	        if(!isOn){ return; }
	        string txt = tog.GetComponentInChildren<TextMeshProUGUI>().text;
	        _preprocessorRes_hoverMe.GetComponentInChildren<TextMeshProUGUI>().text =  "res <size=80%>x</size>" + txt;
	    }


	    void Awake(){
	        _preprocessorRes_hoverMe.onSurfaceEnter += (cursor)=>OnPreprocessorResHover(isStoppedHover:false);
	        _preprocessorRes_hoverMe.onSurfaceExit  += (cursor)=>OnPreprocessorResHover(isStoppedHover:true);
	        _preprocessorRes_05.onValueChanged.AddListener( (isOn)=>OnPreprocessorToggle(_preprocessorRes_05,isOn)  );
	        _preprocessorRes_1.onValueChanged.AddListener(  (isOn)=>OnPreprocessorToggle(_preprocessorRes_1, isOn)  );
	        _preprocessorRes_15.onValueChanged.AddListener( (isOn)=>OnPreprocessorToggle(_preprocessorRes_15,isOn)  );
	        _preprocessorRes_2.onValueChanged.AddListener(  (isOn)=>OnPreprocessorToggle(_preprocessorRes_2, isOn)  );
	        if(_wasCreatedViaLoad==false){//checks if wasn't spawned by a project-save file.
	            _preprocessorRes_1.isOn = true;
	        }
	    }


	    public void Save(ControlNetUnit_SL unit_sl){
	        Save_PreprocessorRes(unit_sl);
	    }

	    public void Load(ControlNetUnit_SL unit_sl){
	        _wasCreatedViaLoad = true;
	        Load_PreprocessorRes(unit_sl);
	    }

	    void Save_PreprocessorRes( ControlNetUnit_SL unit_sl ){
	        if( _preprocessorRes_05.isOn ){
	            unit_sl.preprocessorRes_factor = 0.5f;
	        }else if(_preprocessorRes_1.isOn){
	            unit_sl.preprocessorRes_factor = 1.0f;
	        }else if(_preprocessorRes_15.isOn){
	            unit_sl.preprocessorRes_factor = 1.5f;
	        }else{
	            unit_sl.preprocessorRes_factor = 2;
	        }
	    }

	    void Load_PreprocessorRes( ControlNetUnit_SL unit_sl ){
	        Toggle tog = null;
	        if(unit_sl.preprocessorRes_factor <= 0.6){
	            tog = _preprocessorRes_05;
	        }else if(unit_sl.preprocessorRes_factor <= 1.1){
	            tog = _preprocessorRes_1;
	        }else if(unit_sl.preprocessorRes_factor <= 1.6){
	            tog = _preprocessorRes_15;
	        }else{
	            tog = _preprocessorRes_2;
	        }//manually invoke the callback, if our Awake function wasn't invoked yet:
	        tog.isOn = true;
	        OnPreprocessorToggle(tog, true);
	    }

	    public void CopyFromAnother(ControlnetPreprocessor_UI other){
	        _preprocessorRes_05.isOn = other._preprocessorRes_05.isOn;
	        _preprocessorRes_1.isOn  = other._preprocessorRes_1.isOn;
	        _preprocessorRes_15.isOn = other._preprocessorRes_15.isOn;
	        _preprocessorRes_2.isOn  = other._preprocessorRes_2.isOn;
	    }
	}
}//end namespace
