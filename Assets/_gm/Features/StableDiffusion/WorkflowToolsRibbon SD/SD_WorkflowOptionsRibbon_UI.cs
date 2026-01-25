using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace spz {

	// Helper panel, belongs to SD_WorkflowRibbon_UI.
	// Contains additional controls, shows some of them
	// depending on the current mode.
	// 
	// Fetches settings from the webui, and assigns them as well.
	// For example, tileable, or the softInpant regimes.
	public class SD_WorkflowOptionsRibbon_UI : MonoBehaviour{
	    public static SD_WorkflowOptionsRibbon_UI instance { get; private set; } = null;

	    [Space(10)]
	    [SerializeField] CanvasGroup _wholePanel_canvGrp;
	    [Space(10)]
	    [SerializeField] WorkflowRibbon_UI _rib;
	    [Space(10)]
	    [SerializeField] BrushRibbon_UI_Size _brushSize_slider;
	    [Space(10)]
	    [SerializeField] TextMeshProUGUI _reThink_text;
	    [SerializeField] TextMeshProUGUI _reThink_text_mini;
	    [SerializeField] RectTransform _spaceAfter_reThinkSlider;
	    [SerializeField] CircleSlider_Snapping_UI _reThink_slider;
	    [SerializeField] CircleSlider_Snapping_UI _reThink_slider_mini;//next to GenArt button.
	    [SerializeField] CanvasGroup _reThink_slider_mini_canvGrp;
	    [Space(10)]
	    [SerializeField] RectTransform _del_Last_rectTransf;
	    [SerializeField] RectTransform _genArt_RectTransf;
	    [Space(10)]
	    [SerializeField] TextMeshProUGUI _mask_blur_text;
	    [SerializeField] CircleSlider_Snapping_UI _blur_slider;
	    [SerializeField] RectTransform _spaceInstead_of_blurSlider;

	    [Space(10)]
	    [SerializeField] CircleSlider_Snapping_UI _edgeThresh_slider;//z-distance
	    [SerializeField] CircleSlider_Snapping_UI _edgeThick_slider; //overall thickness of edge after it's detected.
	    [SerializeField] RectTransform _spaceAfter_img2ImgSliders;
	    [Space(10)]
	    [SerializeField] BrushRibbon_UI_Colors _brushColor;
	    [Space(10)] 
	    [SerializeField] BrushRibbon_UI_Opacity _brushOpacity;
	    [SerializeField] BrushRibbon_UI_Hardness _brushHardness;
	    [SerializeField] SD_BrushRibbon_UI_Direction _direction;
	    [Space(10)]
	    [SerializeField] BrushRibbon_UI_BucketFill _bucketFill;
	    [SerializeField] BrushRibbon_UI_DeleteButton _deleteMask;
	    [SerializeField] BrushRibbon_UI_InvertMask _InvertMask;
	    [SerializeField] BrushRibbon_UI_PressureMode _pressureTabletMode;
	    [Space(10)]
	    [SerializeField] Toggle _softInpaint;
	    [SerializeField] Toggle _tileableInpaint;
	    [SerializeField] Toggle _ignoreDepthOrNormals;

	    public Texture2D _brushHardnessTex => _brushHardness._brushHardnessTex;

	    public bool direction => _direction.isPositive;
	    public bool isPositive => _direction.isPositive; //are we adding or erasing color with the brush.
	    public Color brushColor => _brushColor._brushColor;
	    public float maskBrushOpacity => _brushOpacity._maskBrushOpacity;

	    public float brushSize01 => _brushSize_slider.brushSize01;
	    public void SetBrushSize(float s) => _brushSize_slider.SetBrushSize(s);

	    public TabletPressureMode tabletPressureMode => _pressureTabletMode._mode;

	    public bool IsEyeDropperMagnified => _brushColor.IsEyeDropperMagnified;


	    public float denoisingStrength => _reThink_slider.value;
	    public float maskBlur_StepLength01 => _blur_slider.value;
	    public bool isSoftInpaint => on_and_interactable(_softInpaint);
	    public bool isTileable => on_and_interactable(_tileableInpaint);
	    public bool ignoreDepthOrNormals => on_and_interactable(_ignoreDepthOrNormals);
	    bool on_and_interactable(Toggle tog) => tog.isOn && tog.IsInteractable();

	    public float edgeThresh => 1 - _edgeThresh_slider.value;//1-value because slider is caled EDG. smaller=fewer edges (higher thresh), more intuitive
	    public float edgeThick => _edgeThick_slider==null? 0 : _edgeThick_slider.value;
	    public float edgeBlur => 0.3f; //0.5 would extend from borders, 0.3 leaves a nice gap with a bit softness.


	    // we remember the values of sliders that user prefers, inside the TotalObject worfklow mode.
	    // We can restore them when they re-enable to TotalObject. This is more comfortable for them, 
	    float _recentBlur_for_TotalObject = 0.5f;
	    float _recentEdgeThresh_for_TotalObj = 0.5f;

	    float _recentBlur_for_Color = 0.5f;
	    float _recentEdgeThresh_for_Color = 0.5f;

	    //user shouldn't have blur (at least by default) when not using colors. 
	    //We will rely on the alpha of each stroke, and its softness, instead of blur.
	    //Blur doesn't work too well together with dilation on top of these semi-visible brush strokes.
	    float _recentBlur_for_NoColor = 0.0f;
	    float _recentEdgeThresh_for_NoColor = 0.5f;



	    public void SetIsTileable_from_script(bool isOn){
	        _tileableInpaint.isOn = isOn;
	        SD_Options_Fetcher.instance.SubmitOptions_Asap();
	    }

	    public void SetIsSoftInpaint_from_script(bool isOn){
	        _softInpaint.isOn = isOn;
	        SD_Options_Fetcher.instance.SubmitOptions_Asap();
	    }


	    void Update(){
	        FadeWholePanel();

	        if(DimensionMode_MGR.instance._dimensionMode == DimensionMode.dim_sd) { 
	            ToggleControls_based_on_mode();
	            UpdateSliderTexts();
	            Reposition_ReDoMini_slider();
	            PlayAttentionAnim_ReDo_slider();
	        }
	    }

    
	    void FadeWholePanel(){
	        float targAlpha = 0;
	        DimensionMode currMode = DimensionMode_MGR.instance._dimensionMode;
	        switch(currMode){
	            case DimensionMode.dim_sd:
	                targAlpha = 1;
	                break;
	            default: 
	                targAlpha = 0; 
	                break;
	        }
	        _wholePanel_canvGrp.alpha = Mathf.MoveTowards(_wholePanel_canvGrp.alpha, targAlpha, Time.deltaTime*7);
	        _wholePanel_canvGrp.gameObject.SetActive(_wholePanel_canvGrp.alpha!=0);
	        //also adjust the visibility of elements on the other ui side:
	        _reThink_slider_mini_canvGrp.alpha = _wholePanel_canvGrp.alpha;
	        if (_wholePanel_canvGrp.alpha == 0){
	            _reThink_slider_mini_canvGrp.gameObject.SetActive(false);
	        }
	    }
    

	    void UpdateSliderTexts(){
	        string redo_str    =  Mathf.RoundToInt(_reThink_slider.value * 100).ToString();
	        _reThink_text.text =  redo_str.Length>2? $"<size=90%>{redo_str}</size>" : redo_str;
	        _reThink_text_mini.text = _reThink_text.text;

	        _mask_blur_text.text  = _blur_slider.value.ToString("0.0");
	    }


	    void ToggleControls_based_on_mode(){
	        var toEnable = new List<Component>();
	        var toDisable = new List<Component>(){
	            _brushSize_slider,
	            _reThink_slider_mini, _reThink_slider, _spaceAfter_reThinkSlider, 
	            _blur_slider, _spaceInstead_of_blurSlider,  
	            _edgeThick_slider, _edgeThresh_slider, 
	            _spaceAfter_img2ImgSliders, _brushColor, _brushHardness, 
	            _bucketFill, _deleteMask, _InvertMask,
	            _softInpaint, _tileableInpaint, _ignoreDepthOrNormals,
	        };

	        void add_toEnable(params Component[] mbs){ 
	            for(int i=0; i<mbs.Length; i++){ 
	                toEnable.Add(mbs[i]); 
	                bool removed = toDisable.Remove(mbs[i]);
	                Debug.Assert(removed, "you forgot to include component into toDisable. All must be included initially.");
	            }
	        }

	        switch ( _rib.currentMode() ){
	            case WorkflowRibbon_CurrMode.ProjectionsMasking:
	                add_toEnable( _brushHardness, _brushSize_slider, _bucketFill, _InvertMask);
	                break;

	            case WorkflowRibbon_CurrMode.Inpaint_Color:
	                add_toEnable( _brushSize_slider,
	                              _reThink_slider_mini, _reThink_slider, _spaceAfter_reThinkSlider, _blur_slider,  _edgeThresh_slider, 
	                              _spaceAfter_img2ImgSliders, _brushColor, _brushHardness,
	                              _bucketFill, _deleteMask, _softInpaint, _tileableInpaint );
	                break;

	            case WorkflowRibbon_CurrMode.Inpaint_NoColor:
	                add_toEnable( _brushSize_slider, //NOTICE, no blur slider for 'NoColor', because dilation doesn't work on it as on 'Color'
	                              _reThink_slider_mini, _reThink_slider, _spaceAfter_reThinkSlider, /*_blur_slider,*/ _spaceInstead_of_blurSlider, 
	                              _edgeThresh_slider,
	                              _spaceAfter_img2ImgSliders,  _brushHardness, 
	                              _bucketFill, _deleteMask, _softInpaint, _tileableInpaint );
	                break;

	            case WorkflowRibbon_CurrMode.TotalObject:
	                add_toEnable( _reThink_slider_mini, _reThink_slider, _spaceAfter_reThinkSlider, _blur_slider, _edgeThresh_slider,
	                              _spaceAfter_img2ImgSliders, _softInpaint, _tileableInpaint );
	                break;

	            case WorkflowRibbon_CurrMode.WhereEmpty:
	                add_toEnable( _blur_slider, _edgeThresh_slider, 
	                              _spaceAfter_img2ImgSliders, _softInpaint, _tileableInpaint );
	                break;

	            default: break;
	        }
	        Toggle_the_GOs( true, toEnable );
	        Toggle_the_GOs( false, toDisable );

	        //always ensure the Tileable is enabled, so user can easilly disable it, regardless of the Workflow Mode.
	        _tileableInpaint.gameObject.SetActive( true );
	    }


	    void Toggle_the_GOs(bool isOn, List<Component> mbs){
	        for(int i=0; i<mbs.Count; i++){  mbs[i].gameObject.SetActive(isOn); }
	    }


	    void Reposition_ReDoMini_slider(){
	        if (StableDiffusion_Hub.instance==null){ return; }//scenes are probably loading

	        bool isGen = StableDiffusion_Hub.instance._generating ||
	                     StableDiffusion_Hub.instance._finalPreparations_beforeGen;
	        if(isGen){ //force to disable, regardless of what it was set as in ToggleControls_based_on_mode():
	            _reThink_slider_mini.gameObject.SetActive(false);
	            _reThink_slider_mini.transform.localScale = Vector3.one;//to ensure animation doesn't affec it.
	        }

	        if (_del_Last_rectTransf.gameObject.activeSelf){
	            _reThink_slider_mini.transform.parent.position = _del_Last_rectTransf.position 
	                                                            + Vector3.up*_del_Last_rectTransf.rect.height;
	        }else{
	            _reThink_slider_mini.transform.parent.position = _genArt_RectTransf.position 
	                                                            + Vector3.up*_genArt_RectTransf.rect.height*0.5f;
	        }
	    }


	    bool _genArt_hoveredBefore = false;
	    void PlayAttentionAnim_ReDo_slider(){
	        bool hovering  = GenerateButtons_Main_UI.instance?.isHovering_GenArtButton ?? false;
	             hovering |= GenerateButtons_Mini_UI.instance?.isHovering_GenArtButton ?? false;

	        if(hovering && _genArt_hoveredBefore==false){  
	            _reThink_slider.GetComponentInChildren<Animation>().Play();  
	            _reThink_slider_mini.GetComponentInChildren<Animation>().Play();  
	        }
	       _genArt_hoveredBefore = hovering;
	    }


    

	    void OnModeChanged(WorkflowRibbon_CurrMode mode){
	        if(mode == WorkflowRibbon_CurrMode.TotalObject){
	            _blur_slider.SetSliderValue(_recentBlur_for_TotalObject, invokeCallback: true);
	            _edgeThresh_slider.SetSliderValue(_recentEdgeThresh_for_TotalObj, invokeCallback: true);//higher than zero, to enable edge detect
	        }

	        if(mode == WorkflowRibbon_CurrMode.Inpaint_Color){
	            _blur_slider.SetSliderValue(_recentBlur_for_Color, invokeCallback: true);
	            _edgeThresh_slider.SetSliderValue(_recentEdgeThresh_for_Color, invokeCallback: true);
	            /*_reThink_slider.SetSliderValue(0.8f, invokeCallback:true);*/ //<--commneted out for now, to avoid annoyance Nov 2024.
	        }
        
	        if(mode == WorkflowRibbon_CurrMode.Inpaint_NoColor){
	            _blur_slider.SetSliderValue(_recentBlur_for_NoColor, invokeCallback: true);
	            _edgeThresh_slider.SetSliderValue(_recentEdgeThresh_for_NoColor, invokeCallback: true);
	            /*_reThink_slider.SetSliderValue(0.55f, invokeCallback:true);*/
	        }

	        if(mode == WorkflowRibbon_CurrMode.WhereEmpty){ 
	            // WhereEmpty only works reasonably with specific settings. Set them here if it got enabled:
	            _blur_slider.SetSliderValue(0.5f, true);
	            _edgeThresh_slider.SetSliderValue(0.0f, true);//ZERO, to disable edge-detection.
	            /*_reThink_slider.SetSliderValue(1.0f, true);*/
	        }
	    }
    
	    void OnBlurSlider(float newVal01){
	        //remember the value in case we switch back to TotalObject later on. For the comfort.
	        var currMode = _rib.currentMode();
	        if(currMode == WorkflowRibbon_CurrMode.TotalObject){ _recentBlur_for_TotalObject = newVal01; }
	        if(currMode == WorkflowRibbon_CurrMode.Inpaint_Color){  _recentBlur_for_Color = newVal01; }
	        if(currMode == WorkflowRibbon_CurrMode.Inpaint_NoColor){_recentBlur_for_NoColor = newVal01; }
	    }

	    void OnEdgeThreshSlider(float newVal01){
	        //remember the value in case we switch back to TotalObject later on. For the comfort:
	        var currMode = _rib.currentMode();
	        if(currMode == WorkflowRibbon_CurrMode.TotalObject){  _recentEdgeThresh_for_TotalObj = newVal01;}
	        if(currMode == WorkflowRibbon_CurrMode.Inpaint_Color){  _recentEdgeThresh_for_Color  = newVal01; }
	        if(currMode == WorkflowRibbon_CurrMode.Inpaint_NoColor){_recentEdgeThresh_for_NoColor = newVal01; }
	    }
    
	    void OnReThinkSliderMini(float val) //mini was adjusted, set the usual slider:
	        =>_reThink_slider.SetSliderValue(val, invokeCallback:false);

	    void OnReThinkSlider(float val) //usual slider was adjusted, set the mini:
	        => _reThink_slider_mini.SetSliderValue(val, invokeCallback:false);
    
	    //tiling can affect the quality, reducing it. So user needs to pay attention to this toggle.
	    void OnWillSendOptions_AmmendPlz(SD_OptionsPacket opt)  => opt.tiling = _tileableInpaint.isOn;
    
	    void OnTileableToggle(bool isOn)  => SD_Options_Fetcher.instance.SubmitOptions_Asap();

	    void On_img2img_requested(GenData2D data)
	        => WorkflowRibbon_UI.instance.Set_CurrentMode(WorkflowRibbon_CurrMode.ProjectionsMasking, playAttentionAnim:true);
       

	    public void Save(StableProjectorz_SL spz){
	        spz.sd_workflowRibbon = spz.sd_workflowRibbon??new SD_WorkflowRibbon_SL();

	        spz.sd_workflowRibbon.denoisingStrength = _reThink_slider.value;

	        spz.sd_workflowRibbon.isUseSoftInpaint = isSoftInpaint;
	        spz.sd_workflowRibbon.isTileable = isTileable;

	        spz.sd_workflowRibbon.ignoreDepthOrNormals = _ignoreDepthOrNormals.isOn;

	        spz.sd_workflowRibbon.maskBlur_stepLength = maskBlur_StepLength01;
	    }

	    public void Load(StableProjectorz_SL spz){
	        _reThink_slider.SetSliderValue( spz.sd_workflowRibbon.denoisingStrength, true);

	        _softInpaint.isOn = spz.sd_workflowRibbon.isUseSoftInpaint;
	        _tileableInpaint.isOn    = spz.sd_workflowRibbon.isTileable;

	        _ignoreDepthOrNormals.isOn   = spz.sd_workflowRibbon.ignoreDepthOrNormals;

	        _blur_slider.SetSliderValue( spz.sd_workflowRibbon.maskBlur_stepLength, true);
	    }


	    void Awake(){
	        if (instance != null){ DestroyImmediate(this); return; }
	        instance = this;

	        WorkflowRibbon_UI._Act_OnModeChanged += OnModeChanged;
        
	        SD_Options_Fetcher.Act_onWillSendOptions_AmmendPlz += OnWillSendOptions_AmmendPlz;

	        _tileableInpaint.onValueChanged.AddListener( OnTileableToggle );

	        StableDiffusion_Hub._Act_img2img_requested += On_img2img_requested;

	        _reThink_slider.onValueChanged.AddListener( OnReThinkSlider );
	        _reThink_slider_mini.onValueChanged.AddListener( OnReThinkSliderMini );

	        _blur_slider.onValueChanged.AddListener(OnBlurSlider);
	        _edgeThresh_slider.onValueChanged.AddListener(OnEdgeThreshSlider);
	    }

	}
}//end namespace
