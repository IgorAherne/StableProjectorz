using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace spz {

	//has tab-buttons that allow us to flick between different panels and preview regimes.
	public class LeftRibbon_UI : MonoBehaviour {
	    public static LeftRibbon_UI instance { get; private set; } = null;

	    [SerializeField] ButtonToggle_UI _toggleWireframe;
	    [SerializeField] Toggle _toggleDepthMode_button;
	    [SerializeField] CircleSlider_Snapping_UI _depthContrast_slider;
	    [SerializeField] CircleSlider_Snapping_UI _depthBrightness_slider;
	    [SerializeField] CircleSlider_Snapping_UI _depthBlur_StepSize_slider;
	    [SerializeField] CircleSlider_Snapping_UI _depthSharpBlur_slider;
	    [SerializeField] CircleSlider_Snapping_UI _depthBlurFinal_StepSize_slider;
	    [SerializeField] Toggle _depthFinalBlur_Inside_toggle;
	    [SerializeField] TextMeshProUGUI _depthContrast_text;
	    [SerializeField] TextMeshProUGUI _depthBrightness_text;
	    [SerializeField] TextMeshProUGUI _depthBlur_stepSize_text;
	    [SerializeField] TextMeshProUGUI _depthBlurFinal_stepSize_text;
	    [SerializeField] TextMeshProUGUI _depthSmartBlur_text;
	    [Space(10)]
	    [SerializeField] SlideOut_Widget_UI _depth_slideOut_panel;
	    [SerializeField] GameObject _depthSlideOut_antiClick_surf;

	    public bool isShowWireframe_onSelected => _toggleWireframe.isPressed;
	    public float depthContrast => _depthContrast_slider.value;
	    public float depthBrightness => _depthBrightness_slider.value;
	    public float depthBlur_StepSize => _depthBlur_StepSize_slider.value;
	    public float depthSharpBlur => _depthSharpBlur_slider.value;
	    public float depthBlurFinal_StepSize => _depthBlurFinal_StepSize_slider.value;
	    public bool depthFinalBlur_Inside => _depthFinalBlur_Inside_toggle.isOn;

    
	    public void SetDepthContrast01_fromCode(float value01){
	        bool invokeCallback =  Mathf.Approximately(_depthContrast_slider.value, value01) == false;
	        _depthContrast_slider.SetSliderValue(value01, invokeCallback);
	    }

	    public void SetDepthBrightness01_fromCode(float value01){
	        bool invokeCallback =  Mathf.Approximately(_depthBrightness_slider.value, value01) == false;
	        _depthBrightness_slider.SetSliderValue(value01, invokeCallback);
	    }


	    void Update(){
	        UdpateDepthSliderText();

	        // COMMENTED OUT, KEPT FOR PRECAUTION. Allow user to do it from anywhere, without hovering the viewport:
	        //    if(MainViewport_UI.instance.isCursorHoveringMe() == false){ return; }
	        if (KeyMousePenInput.isSomeInputFieldActive()){ return; }//maybe typing text, etc

	        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W)){
	            _toggleWireframe.ForceSameValueAs(!_toggleWireframe.isPressed);
	        }
	    }


	    void UdpateDepthSliderText(){
	        _depthContrast_text.text       = Mathf.RoundToInt(_depthContrast_slider.value*100).ToString();
	        _depthBrightness_text.text     = Mathf.RoundToInt(_depthBrightness_slider.value*100).ToString();
	        _depthBlur_stepSize_text.text  =  _depthBlur_StepSize_slider.value.ToString("0.0");
	        _depthBlurFinal_stepSize_text.text  = _depthBlurFinal_StepSize_slider.value.ToString("0.0");
        
	        _depthSmartBlur_text.text = _depthSharpBlur_slider.value.ToString("0.0");
	    }


	    int _numWarningsSoFar = 1;
	    float _nextWarnTime = -9999;
	    void OnDepthStepSlider(float val){
	        //produce performance warning but only for high resolutions:
	        if(SD_InputPanel_UI.instance.widthHeight().x <= 1024){ return; }
	        if(SD_InputPanel_UI.instance.widthHeight().y <= 1024){ return; }
	        if (Time.time < _nextWarnTime){ return; }
	        Viewport_StatusText.instance.ShowStatusText("Keeping blur as 0 might save performance.", false, 5, false);
	        _nextWarnTime = Time.time + 20*_numWarningsSoFar;
	        _numWarningsSoFar++;
	    }

	    // User toggled a setting to change the arrangement of the UI panels.
	    // We need to adjust the slide-outs, so that they still make sense:
	    void OnSettings_ToolRibbonSwapped(bool isSwapped){
	        if(_depth_slideOut_panel.isFlipped() == isSwapped){ return; }
	        _depth_slideOut_panel.Flip_if_possible();
	    }


	    void Awake(){
	        if (instance != null){ DestroyImmediate(this); return; }
	        instance = this;

	        EarlyUpdate_callbacks_MGR.instance.onEarlyUpdate3 += EarlyUpdate;
	    }

	    void Start(){
	        _toggleDepthMode_button.onValueChanged.AddListener( OnToggleDepthMode_button );
	        _depthBlur_StepSize_slider.onValueChanged.AddListener( OnDepthStepSlider );
	        Settings_MGR._Act_verticalRibbonsSwapped += OnSettings_ToolRibbonSwapped;
	        OnSettings_ToolRibbonSwapped( Settings_MGR.instance.get_viewport_isSwapVerticalRibbons() );
	    }


	    void OnDestroy(){
	        if (EarlyUpdate_callbacks_MGR.instance != null){
	            EarlyUpdate_callbacks_MGR.instance.onEarlyUpdate3 -= EarlyUpdate;
	        }
	        Settings_MGR._Act_verticalRibbonsSwapped -= OnSettings_ToolRibbonSwapped;
	    }


	    void EarlyUpdate(){
	        Vector2 cursorPos = KeyMousePenInput.cursorScreenPos();
	        RectTransform depthRect = _toggleDepthMode_button.transform as RectTransform;

	        //keep showing the slide out panel if we are viewing the depth:
	        bool isShowingDepth = MainViewport_UI.instance.showing == MainViewport_UI.Showing.Depth;
	        _depth_slideOut_panel._dontAutoHide = isShowingDepth;
	        _depthSlideOut_antiClick_surf.SetActive(!isShowingDepth); //else overlaps controlnet/Art panel.

	        bool contains =  RectTransformUtility.RectangleContainsScreenPoint(depthRect, cursorPos);
	             contains |= isShowingDepth;

	        if (contains  && _depth_slideOut_panel.isShowing == false){ 
	            _depth_slideOut_panel.Toggle_if_Different(true); 
	        }
	    }


	    void OnToggleDepthMode_button(bool isOn){
	        MainViewport_UI.instance.ToggleShowDepth(isOn);
	    }

    
	    public void Save( StableProjectorz_SL spz ){
	        var trSL = new MainViewWindow_ToolsRibbon_SL();
	         spz.mainViewWindow_ToolsRibbon = trSL;
	        trSL.isShowWireframe  = isShowWireframe_onSelected;
	        trSL.depthContrast = _depthContrast_slider.value;
	        trSL.depthBrightness = _depthBrightness_slider.value;
	        trSL.depthBlur_stepSize = depthBlur_StepSize;
	        trSL.depthBlurFinal_stepSize = depthBlurFinal_StepSize;
	        trSL.depth_sharpBlur  = depthSharpBlur;
	        trSL.depth_finalBlur_inside = depthFinalBlur_Inside;
	    }

	    public void Load( StableProjectorz_SL spz ){
	        MainViewWindow_ToolsRibbon_SL trSL = spz.mainViewWindow_ToolsRibbon;
	        _toggleWireframe.ForceSameValueAs( trSL.isShowWireframe );
        
	        _depthContrast_slider.SetSliderValue( trSL.depthContrast, true);
	        _depthBrightness_slider.SetSliderValue( trSL.depthBrightness, true);

	        _depthBlur_StepSize_slider.SetSliderValue(trSL.depthBlur_stepSize, true);
	        _depthSharpBlur_slider.SetSliderValue( trSL.depth_sharpBlur, true );

	        _depthBlurFinal_StepSize_slider.SetSliderValue(trSL.depthBlurFinal_stepSize, true);
	        _depthFinalBlur_Inside_toggle.isOn = trSL.depth_finalBlur_inside;
	    }

	}
}//end namespace
