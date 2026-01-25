using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;
using UnityEngine.UI;

namespace spz {

	// Helper-class of the control-net-unit. It contains the UI dropdowns of the unit.
	// These dropdowns are for control-neural-net (model), for the preprocessor, etc.
	public class ControlNetUnit_Dropdowns : MonoBehaviour{
	    [SerializeField] ControlNetUnit_UI _myUnit;
	    [Space(10)]
	    [SerializeField] TMP_Dropdown _preprocessor_dropdown;
	    [SerializeField] TMP_Dropdown _model_dropdown;
	    [SerializeField] TMP_Dropdown _controlType_dropdown;//For presets of "preprocessor + model". not used right now..
	    [Space(10)]
	    [SerializeField] CircleSlider_Snapping_UI _startingControl_step;
	    [SerializeField] ControlNetUnit_ThreshSliders _threshSliders;
	    [SerializeField] GameObject _contextMenu_gameObj;
	    [SerializeField] Toggle _imgToSend_none_toggle;

	    // if we loaded from a save-file, we migth want to select a model.
	    // If we are not connected, this model won't be in the dropdown.
	    // But we can try to find it as soon a we connect next time;
	    string _prefferedPreProcessor_viaLoad = "";
	    string _prefferedModel_viaLoad = "";

	    public void CopyFromAnother(ControlNetUnit_Dropdowns dropdowns){
	        _preprocessor_dropdown.options = dropdowns._preprocessor_dropdown.options;
	        _preprocessor_dropdown.value   = dropdowns._preprocessor_dropdown.value;

	        _model_dropdown.options = dropdowns._model_dropdown.options;
	        _model_dropdown.value = dropdowns._model_dropdown.value;

	        if (_controlType_dropdown != null){
	            _controlType_dropdown.options = dropdowns._controlType_dropdown.options;
	            _controlType_dropdown.value = dropdowns._controlType_dropdown.value;
	        }
	    }

	    public string currPreprocessorName(){
	        if(_preprocessor_dropdown.options.Count == 0){ return "None"; }
	        return _preprocessor_dropdown.options[_preprocessor_dropdown.value].text;
	    }
	    public string currModelName(){
	        if(_model_dropdown.options.Count == 0){ return "None"; }
	        string chosen = _model_dropdown.options[_model_dropdown.value].text;
	        if(chosen.ToLower().Contains("not connected")){  return "None"; }
	        return chosen.ToLower().Contains("none") ? "None" : chosen;//because sometimes None or none conflict or don't get recognized.
	    }

	    public bool is_currPreprocessor_none => currPreprocessorName().ToLower()=="none";
	    public bool is_currModel_none => currModelName().ToLower()=="none";
	    public bool isReferencePreprocessor() => currPreprocessorName().ToLower().Contains("reference");
	    public static bool hasAtLeastSomeModel { get; private set; } = false;


    
	    //'castToLowercase' is important, some users have models with _Depth_ instead of _depth_ in their name
	    public bool HasString(string substr, bool castToLowercase=true){
	        substr = castToLowercase? substr.ToLower() : substr;

	        if (_preprocessor_dropdown.options.Count > 0){
	            var preprocessor = _preprocessor_dropdown.options[_preprocessor_dropdown.value];
	            string prep_name =  castToLowercase?  preprocessor.text.ToLower() : preprocessor.text;
	            if(prep_name.Contains(substr)){ return true; }
	        }
	        if (_model_dropdown.options.Count > 0){
	            var model = _model_dropdown.options[_model_dropdown.value];
	            string model_name = castToLowercase ? model.text.ToLower() : model.text;
	            if(model_name.Contains(substr)){ return true; }
	        }
	        return false;
	    }


	    public void OnRefreshInfo_Complete( out bool isNeedDownloadMandatoryModel_ ){
	        bool pickDepth_ifWasNone = false;
	        UpdateDropdown( _preprocessor_dropdown,  SD_ControlNetsList_UI.instance._preprocessors_list.module_list,
	                        pickDepth_ifWasNone,  ref _prefferedPreProcessor_viaLoad );

	        //force dropdown to pick depth, if was none.
	        //But 'none' is actually allowed if preprocessor is 'reference_only'. People requested it Apr 2024:
	        pickDepth_ifWasNone =  true;
	        pickDepth_ifWasNone &= currPreprocessorName().ToLower().Contains("ref")==false;

	        UpdateDropdown( _model_dropdown,  SD_ControlNetsList_UI.instance._models.model_list,
	                        pickDepth_ifWasNone,  ref _prefferedModel_viaLoad);

	        //When StableProjectorz was launched for the first time, there are no models at all.
	        //In this case, show big button, prompting the user to install a first model for this ControlNetUnit:
	        hasAtLeastSomeModel = _model_dropdown.options.Count > 0;

	        bool mustDownload  =  _model_dropdown.options.Count == 0;
	             mustDownload |=  _model_dropdown.options.Count==1 && _model_dropdown.options[0].text.ToLower()=="none";
	             mustDownload |= ControlNetUnit_DownloadHelper.isSomeUnit_downloadingModels;

	        _model_dropdown.gameObject.SetActive(!mustDownload);
	        isNeedDownloadMandatoryModel_ = mustDownload;
	    }


	    void UpdateDropdown( TMP_Dropdown dropdown,  string[] choices,  bool pickDepth_ifWasNone,  ref string prefferedVal_via_Load_ ){
	        string prevChoice =  dropdown.options.Count==0 ? "" : dropdown.options[dropdown.value].text;
	        int prevIx = dropdown.value;
        
	        //ensure 'none' option exists. Users might need it (for "ReferenceOnly"), and some webui don't return it explicitly.
	        //NOTICE: A1111 uses lowercase 'none' ('None' isn't working).
	        var newOptions = choices.Select(c=>new TMP_Dropdown.OptionData(c)).ToList();
	        if(!newOptions.Exists(opt => opt.text.ToLower()=="none")){ newOptions.Insert(0, new TMP_Dropdown.OptionData("none")); }

	        dropdown.ClearOptions();
	        dropdown.AddOptions(newOptions);

	        //reset index if old index no logner leads to the same option.
	        if(newOptions.Count > 0){
	            bool changed =  (prevIx>=dropdown.options.Count) ||  (prevChoice != dropdown.options[prevIx].text);
	            if(changed){ dropdown.value = 0; }
	            else{ dropdown.SetValueWithoutNotify(prevIx); }

	            pickDepth_ifWasNone &=  (prevChoice == "" || prevChoice.ToLower()=="none");
	            if(pickDepth_ifWasNone){//if we didn't have a value, ensure the dropdown defaults to 'Depth', rather than to 'None'.
	                int ix = Array.FindIndex(choices, c=>c.ToLower().Contains("depth"));
	                dropdown.value = ix>=0? ix : dropdown.value;
	            }
	        }

	        dropdown_LoadSavedVal_maybe(dropdown, ref prefferedVal_via_Load_);
	        dropdown.RefreshShownValue();
	    }



	    void dropdown_LoadSavedVal_maybe(TMP_Dropdown dropdown, ref string prefferedVal_via_Load_){
	        // Check if there is a value we'd prefer to select,
	        // if we Loaded a saved project-file recently:
	        bool wantLoaded = string.IsNullOrEmpty(prefferedVal_via_Load_)==false;
	        if (!wantLoaded){ return; }

	        string wantedVal = prefferedVal_via_Load_;
	        int ix = dropdown.options.FindIndex(opt => opt.text==wantedVal);
	        if(ix>=0){
	            dropdown.value = ix;
	            prefferedVal_via_Load_ = "";//found, no longer need to search for it.
	        }
	    }


	    void OnModelDropdown_ValueChanged(int ix){
	        _threshSliders.OnUnitAltered();

	        string modelText = _model_dropdown.options[ix].text;
	        if (modelText.ToLower().Contains("xl_depth")){
	            string msg = "SDXL depth can make Low-Poly-Wireframe renders.  If so, fix it by blurring the Depth:" +
	                        "\nhover the Depth Button (next to the wireframe button) and use its sliders.";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 8, false);
	        }
	        // If dropdown changed, check if we are doing inpaint.
	        // For inpaint 99% of time we DON'T want to send an image.
	        // This one is very easy to forget, so always do so here automatically, whenever dropdown changes.
	        //   Illyasviel: "You do not need to add image to ControlNet."
	        //   https://github.com/Mikubill/sd-webui-controlnet/discussions/1143#discussion-5155255
	        if(_myUnit.isForInpaint()==false){ return; }
	        bool contextWasActive = _contextMenu_gameObj.activeSelf;
	        _contextMenu_gameObj.SetActive(true);//enableds ToggleGroup while we flick one of the toggles.
	        _imgToSend_none_toggle.isOn = true;
	        _contextMenu_gameObj.SetActive(contextWasActive);
	        Viewport_StatusText.instance.ShowStatusText("For Inpant controlnet we DON'T want to send an image. "
	                                                   +"\nIllyasviel said: 'You do not need to add image to ControlNet.'", false, 6, false);
	    }


	    void OnPreprocessorDropdown_ValChanged(int optionIx){
	        Adjust_others_if_me_referenceOnly();
	        _threshSliders.OnUnitAltered();
	    }//end()


	    string _latestPreprocessorName = "";
	    void Adjust_others_if_me_referenceOnly(){
	        bool isForReference = currPreprocessorName().ToLower().Contains("reference");
	        bool wasForReference = _latestPreprocessorName.ToLower().Contains("reference");
	        _latestPreprocessorName = currPreprocessorName();
        
	        if(wasForReference == isForReference){ return; }//remains the same.

	        bool recoverOriginalVals = !isForReference;
	        if(recoverOriginalVals && SD_ControlNetsList_UI.instance.Num_Active_Reference_CTRLUnit()>0){//NOTICE >0
	            return;//there are other controlnets that have reference.
	        }

	        if(!recoverOriginalVals && SD_ControlNetsList_UI.instance.Num_Active_Reference_CTRLUnit()>1){//NOTICE >1
	            return;//there are other controlnets that have reference, we already adjusted values via them.
	        }

	        // Change the StartingStep values of any Depth or Normal controlnet units.
	        // (either set to greater value, or restore back to zero).
        
	        bool didAdjustVals = false;
	        SD_ControlNetsList_UI.instance.DoForEvery_CtrlUnit( Adjust_StartingStep );

	        void Adjust_StartingStep( ControlNetUnit_UI u,  int u_ix ){
	            if(u == _myUnit){ return; }
	            if(!u.isForDepth() && !u.isForNormals()){ return; }

	            float val =  u.dropdowns._startingControl_step.value;
	            if(!recoverOriginalVals && val == 0.0f){ 
	                val =  0.28f;
	                didAdjustVals = true;
	            }
	            if(recoverOriginalVals){
	                val =  0.0f;
	                didAdjustVals = true;
	            }
	            u.dropdowns._startingControl_step.SetSliderValue( val, invokeCallback:false );
	        }//end act()

	        if(!didAdjustVals){  return; }
        
	        string msg = recoverOriginalVals ? $"You removed <b>Reference</b> preprocessor ...<b>StartStep</b> of <b>Depth</b> and <b>Normal</b> CTRL Nets restored to 0 :)" 
	                                         : 
	                                           $"Picked <b>Reference</b> preprocessor ...<b>StartStep</b> of <b>Depth</b> and <b>Normal</b> CTRL Nets was changed to {0.28} :)" +
	                                           $"\nOtherwise, earlier Depth-controlling usually ruins the reference contribution.";
	        Viewport_StatusText.instance.ShowStatusText(msg, textIsETA_number:false, 7, progressVisibility:false);
	    }


	    void OnSomeUnit_StartDownloadModel(ControlNetUnit_DownloadHelper who) => _model_dropdown.gameObject.SetActive(false);//keep dropdown hidden
	    void OnSomeUnit_StopDownloadModel(ControlNetUnit_DownloadHelper who) => _model_dropdown.gameObject.SetActive(true);


	    public void Save( ControlNetUnit_SL unit_sl ){
	        unit_sl.hasAtLeastSomeModel = hasAtLeastSomeModel;
	        unit_sl.neural_model  = _model_dropdown.options.Count>0? _model_dropdown.options[_model_dropdown.value].text : "";
	        unit_sl.preProcessor  = _preprocessor_dropdown.options.Count>0? _preprocessor_dropdown.options[_preprocessor_dropdown.value].text : "";
	    }

	    public void Load( ControlNetUnit_SL unit_sl ){
	        Load_Dropdown_ifCan(_preprocessor_dropdown, unit_sl.preProcessor, ref _prefferedPreProcessor_viaLoad);
	        Load_Dropdown_ifCan(_model_dropdown, unit_sl.neural_model, ref _prefferedModel_viaLoad);
	    }

	    void Load_Dropdown_ifCan(TMP_Dropdown dropdown, string wantedVal, ref string preferredVal_viaLoad_){
	         int ixInDropdown = dropdown.options.FindIndex( opt => opt.text== wantedVal);

	        if(ixInDropdown >= 0){
	            dropdown.value = ixInDropdown;
	            preferredVal_viaLoad_ = "";
	        }else {//dropdown doesn't contain such a value, remember it for later, when dropdown will be refreshed:
	            preferredVal_viaLoad_ = wantedVal;
	            // But also, if the list is empty, set the model anyway.
	            // That's because we don't need to send JSON to SD, when changing control net model.
	            // We can pretend we have it, so that the list doesn't remain 'none':
	            if(dropdown.options.Count==0){
	                var options = new List<TMP_Dropdown.OptionData>(){ new TMP_Dropdown.OptionData() };
	                options[0].text = preferredVal_viaLoad_;
	                dropdown.AddOptions(options);
	                dropdown.value = 0;
	            }
	        }
	        dropdown.RefreshShownValue();
	    }

	    void Awake(){
	        ControlNetUnit_DownloadHelper._onSomeUnit_startedDownloadModel += OnSomeUnit_StartDownloadModel;
	        ControlNetUnit_DownloadHelper._onSomeUnit_stoppedDownloadModel += OnSomeUnit_StopDownloadModel;
	        _model_dropdown.onValueChanged.AddListener( OnModelDropdown_ValueChanged );
	        _preprocessor_dropdown.onValueChanged.AddListener( OnPreprocessorDropdown_ValChanged );
	        /* _controlType_dropdown.onValueChanged.AddListener( (int ix)=>OnModelDropdown_ValueChanged(_controlType_dropdown) ); */
	    }

	    void OnDestroy(){
	        ControlNetUnit_DownloadHelper._onSomeUnit_startedDownloadModel -= OnSomeUnit_StartDownloadModel;
	        ControlNetUnit_DownloadHelper._onSomeUnit_stoppedDownloadModel -= OnSomeUnit_StopDownloadModel;
	    }

	}
}//end namespace
