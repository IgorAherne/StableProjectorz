using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEngine.EventSystems;

namespace spz {

	public enum MultiView_StartEditMode_Args{
	    None = 0, 
	    DontFocusTheCamera = 1,
	}

	//this enum is flags. 0, 1,2,4,8 etc
	//We stop edit when number of cameras becomes more than 1.
	public enum MultiView_StopEdit_Args{ 
	    Nothing = 0,
	    RestoreToPriorPositions=1,
	    FocusTheCamera = 2,
	};
    

	public class MultiView_Ribbon_UI : MonoBehaviour {
	    public static MultiView_Ribbon_UI instance { get; private set; } = null;

	    [SerializeField] MultiView_SortButtons_UI _sortPins_Button;
	    [SerializeField] Button _BlendCams_button;
	    [SerializeField] GameObject _blendCams_space;
	    [SerializeField] Animation _BlendCams_button_anim;
	    [SerializeField] Toggle _showGrid_toggle;
	    [Space(10)]
	    [SerializeField] SliderUI_Snapping _numCameras_slider;
	    [SerializeField] TextMeshProUGUI _numCams_numberText;
	    [Space(10)]
	    [SerializeField] List<Toggle> _editPOV_toggles;//allow to enable editing (brushing, etc) specific POV of the projection.
	    [SerializeField] GameObject _turnMeON_ifEditingMode;

	    MultiView_StopEdit_Args _stopEdit_args;
	    int _prevSliderValue = 0;
	    int _wantedNumCams   = 0;//we'll ensure everything respects this number, during LateUpdate.

	    // Helps to figure out which brush-mask to tweak (what we will be painting)
	    // Even if we have 4 pov inside a generation, current camera might be #2,  but currentPovIx might be #3, etc
	    // NOTICE 5 - sibling index because they are arranged in reversed order in hieararchy.
	    public int currentPovIx =>  5 - _editPOV_toggles.First(t=>t.isOn).transform.GetSiblingIndex();
	    public int hoveredPovIx { get; private set; } = -1; //cursor might be hovering a pov-toggle, but not click it yet. Useful for previewing via checker-texture.
	    public bool _isEditingMode{ get; private set; } = false;
	    public bool _isShowGrid => _showGrid_toggle.isOn;

	    public static Action<MultiView_StartEditMode_Args> OnStartEditMode { get; set; } = null;
	    public static Action<MultiView_StopEdit_Args> OnStop1_EditMode { get; set; } = null;
	    public static Action OnStop2_EditMode { get; set; } = null;//invoked after 'OnStopEditMode'. Helps to order things.
    



	    void OnCameraPlacements_Restored(GenData2D genData)
	        =>  _wantedNumCams = genData.povInfos.numEnabled;


	    void OnSlider_NumCameras(float val){
	        int sliderNum = Mathf.RoundToInt(val);
	        _numCams_numberText.text = sliderNum.ToString();
	        _wantedNumCams = sliderNum;
	        //DO NOT update the _prevSliderValue, it's done in LateUpdate.
	    }


	    void OnViewCamera_Toggled(int camIx, bool isOn){
	        int numCams =  UserCameras_MGR.instance.numActiveViewCameras();
	        _wantedNumCams = numCams;
	        _numCams_numberText.text = numCams.ToString();                          
	    }


	    public int currIcon_numPovs(){
	        IconUI icon = Art2D_IconsUI_List.instance?._mainSelectedIcon;
	        if(icon == null){ return 1; }
	        if(icon._genData==null){ return 1; }
	        return icon._genData.povInfos.numEnabled;
	    }

	    void OnToggleButton_EditMode() => _wantedNumCams = 1;

	    void OnOrderPins_Button(){
	        int numCamsSlider = Mathf.RoundToInt(_numCameras_slider.value);
	        int numCamsIcon   = currIcon_numPovs();
	        int _wantedNumCams = Mathf.Max(numCamsSlider, numCamsIcon);
	        CamerasMGR_PinsZone_UI.instance.OnOrderPinsButton();
	        _stopEdit_args = MultiView_StopEdit_Args.RestoreToPriorPositions;
	    }

	    void OnWillDispose_GenerationData(GenData2D genData){
	        int numCamsSlider = Mathf.RoundToInt(_numCameras_slider.value);
	        int numCamsIcon = currIcon_numPovs();
	        _wantedNumCams = Mathf.Max(numCamsSlider, numCamsIcon);
	        _stopEdit_args = MultiView_StopEdit_Args.Nothing;
	    }


	    void OnWorkflowMode_Toggled(){
	        //Nov 2024 Commented out, annoying:
	        // _wantedNumCams = 1;
	        _BlendCams_button_anim.Play(); //to catch user's attention.
	    }


	    void EarlyUpdate(){
	        Vector2 cursorPos = KeyMousePenInput.cursorScreenPos();
	        bool contains = RectTransformUtility.RectangleContainsScreenPoint(transform as RectTransform, cursorPos);

	        _turnMeON_ifEditingMode.SetActive(_isEditingMode);

	        if (_isEditingMode){
	            bool isToggleShown(int ix) => _editPOV_toggles[ix].gameObject.activeSelf;

	            if(Input.GetKeyDown(KeyCode.F1) && isToggleShown(0)){ _editPOV_toggles[0].isOn = true; }
	            if(Input.GetKeyDown(KeyCode.F2) && isToggleShown(1)){ _editPOV_toggles[1].isOn = true; }
	            if(Input.GetKeyDown(KeyCode.F3) && isToggleShown(2)){ _editPOV_toggles[2].isOn = true; }
	            if(Input.GetKeyDown(KeyCode.F4) && isToggleShown(3)){ _editPOV_toggles[3].isOn = true; }
	            if(Input.GetKeyDown(KeyCode.F5) && isToggleShown(4)){ _editPOV_toggles[4].isOn = true; }
	            if(Input.GetKeyDown(KeyCode.F6) && isToggleShown(5)){ _editPOV_toggles[5].isOn = true; }
            
	            _BlendCams_button.gameObject.SetActive(false);
	            _blendCams_space.gameObject.SetActive(false);
	        }
	        else{//not editing mode:
	            bool isOn =  currIcon_numPovs() > 1;
	            _BlendCams_button.gameObject.SetActive(isOn);
	            _blendCams_space.gameObject.SetActive(isOn);
	        }
	    }


	    void LateUpdate(){
	        if (UserCameras_MGR.instance == null) { return; } //scenes are probably still loading.
	        if (ModelsHandler_3D.instance == null) { return; }

	        LateUpdate_PrntHints_maybe();
	        LateUpdate_EnsureEditPOV_Toggles();

	        if(_prevSliderValue == _wantedNumCams){
	            LateUpdate_Done();
	            return; //because slider still sends callback even if not incremented to next integer.
	        }

	        _numCameras_slider.SetSliderValue( _wantedNumCams, invokeCallback:false);
	        UserCameras_MGR.instance?.EnableExactly_N_ViewCameras( _wantedNumCams );
        
	        if(_wantedNumCams > 1){
	            StopEditMode(_stopEdit_args, keepEdit_if_currIcon_has_1_POV:false);
	        }else{
	            StartEditMode(MultiView_StartEditMode_Args.DontFocusTheCamera);
	        }
	        LateUpdate_Done();
	    }

	    void LateUpdate_PrntHints_maybe(){
	        if(_prevSliderValue == 1  && _wantedNumCams == 2){//incremented from 1 to 2, so, show tooltip.
	            string msg = "Arrange numbers to cover more pixels.   Zoom, drag, rotate to capture different sides.  Avoid overlap."
	                    +"\nPut 'variants, strong shadows' into Negative Prompt.   Increase resolution.   Press 'Blend Cams' to adjust.";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 13, false);
	        }else if(_wantedNumCams > 2){
	            string msg = "Orbit/Zoom/Pan while hovering numbers.  Or press BlendCams to enter Editing/Painting mode.";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 4, false);
	        }
	    }

	    void LateUpdate_EnsureEditPOV_Toggles(){
	        int iconPovs = currIcon_numPovs();
	        for (int i=0; i<_editPOV_toggles.Count; i++){
	            bool need_gameObj_on  = i < iconPovs;
	            bool toggleWasEnabled = _editPOV_toggles[i].isOn;

	            _editPOV_toggles[i].gameObject.SetActive(need_gameObj_on);
	            // Only activate entire gameObject of 0th toggle if more than 1 camera.
	            // Otherwise the single toggle looks weird:
	            _editPOV_toggles[0].gameObject.SetActive(iconPovs>1); 

	            if(toggleWasEnabled && _editPOV_toggles[i].gameObject.activeSelf==false){
	                _editPOV_toggles[0].isOn = true;//if num cams decreased, ensure 0th toggle is on (even if all of their gameObjs are off)
	            }
	        }
	    }

	    void LateUpdate_Done(){ 
	        _prevSliderValue = _wantedNumCams;
	        _stopEdit_args = MultiView_StopEdit_Args.Nothing;
	    }
    

	    void StartEditMode( MultiView_StartEditMode_Args arg = MultiView_StartEditMode_Args.None ){
	        if(_isEditingMode){ return; }

	        if(currIcon_numPovs() > 1){ 
	            string msg = "Blending the Cameras:  help each camera know better where to project."+
	                         "\nUse the white brush.  Change between cameras via F1, F2, etc.";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 10, false);
	        }
	        _isEditingMode = true;
	        OnStartEditMode?.Invoke(arg);
	    }


	    void StopEditMode(MultiView_StopEdit_Args howToStop, bool keepEdit_if_currIcon_has_1_POV=true){
	        if(!_isEditingMode){ return; }
	        if(keepEdit_if_currIcon_has_1_POV  &&  currIcon_numPovs() <= 1){ return;} //remain in the editing mode

	        _isEditingMode = false;
	        OnStop1_EditMode?.Invoke( howToStop );
	        OnStop2_EditMode?.Invoke();
	    }


	    void On_SD_willGenerateArt(GenData2D newGenData){
	        if(newGenData.povInfos.numEnabled <= 1){ return; }
	        _BlendCams_button_anim.Play();//else, is multiview, so play attention-animation.
	    }


	#region init / deinit
	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	        UserCameras_MGR._Act_OnRestoreCameraPlacements += OnCameraPlacements_Restored;
	        UserCameras_MGR._Act_OnTogledViewCamera += OnViewCamera_Toggled;

	        EarlyUpdate_callbacks_MGR.instance.onEarlyUpdate3 += EarlyUpdate;
	        Init_NumCams_Slider();

	        if (_sortPins_Button != null){ 
	            _sortPins_Button.onClick += OnOrderPins_Button;
	        }
	        _numCameras_slider.onValueChanged.AddListener(OnSlider_NumCameras );
	        _BlendCams_button.onClick.AddListener( OnToggleButton_EditMode );

	        GenData2D_Archive.OnWillDispose_GenerationData += OnWillDispose_GenerationData;

	        _turnMeON_ifEditingMode.SetActive(false);
	    }
    
	    void Start(){
	        WorkflowRibbon_UI._Act_OnModeChanged += (isOn)=>OnWorkflowMode_Toggled();

	        _prevSliderValue = 0;
	        _isEditingMode = false;
	        OnToggleButton_EditMode();

	        _editPOV_toggles.ForEach(t=>t.SetIsOnWithoutNotify(false));
	        _editPOV_toggles[0].isOn = true;//to invoke the OnEditPov_Toggle() callback.

	        GenData2D_Archive.OnWillGenerate += On_SD_willGenerateArt;
        
	        Settings_MGR._Act_verticalRibbonsSwapped += OnSettings_ToolRibbonSwapped;
	        OnSettings_ToolRibbonSwapped( Settings_MGR.instance.get_viewport_isSwapVerticalRibbons() );
	    }

	    void OnDestroy(){
	        UserCameras_MGR._Act_OnRestoreCameraPlacements -= OnCameraPlacements_Restored;
	        UserCameras_MGR._Act_OnTogledViewCamera -= OnViewCamera_Toggled;
	        Settings_MGR._Act_verticalRibbonsSwapped -= OnSettings_ToolRibbonSwapped;

	        if (EarlyUpdate_callbacks_MGR.instance != null){
	            EarlyUpdate_callbacks_MGR.instance.onEarlyUpdate3 -= EarlyUpdate;
	        }
	    }


	    void Init_NumCams_Slider(){
	        _numCameras_slider.onValueChanged.AddListener( OnSlider_NumCameras );
	    }

    
	    // User toggled a setting to change the arrangement of the UI panels.
	    // We need to adjust the slide-outs, so that they still make sense:
	    void OnSettings_ToolRibbonSwapped(bool isSwapped){
       
	    }
	#endregion

	}
}//end namespace
