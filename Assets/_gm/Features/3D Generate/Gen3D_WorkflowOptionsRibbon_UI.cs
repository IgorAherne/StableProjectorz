using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace spz {

	public class Gen3D_WorkflowOptionsRibbon_UI : MonoBehaviour{
	    public static Gen3D_WorkflowOptionsRibbon_UI instance { get; private set; } = null;

	    [Space(10)]
	    [SerializeField] CanvasGroup _wholePanel_canvGrp;
	    [Space(10)]
	    [SerializeField] CircleSlider_Snapping_UI _rembg_backgroundThresh;
	    [SerializeField] TextMeshProUGUI _rembg_backgroundTxt;
	    [Space(10)]
	    [SerializeField] CircleSlider_Snapping_UI _rembg_foregroundThresh;
	    [SerializeField] TextMeshProUGUI _rembg_foregroundTxt;
	    [Space(10)]
	    [SerializeField] Toggle _showAlphaOnly_toggle;
	    [SerializeField] Toggle _makeScreenshots_toggle;
	    [SerializeField] Animation _takeScreenshots_toggleAnim;
	    [SerializeField] Button _rembg_button;
	    [SerializeField] Gen3D_BrushRibbon_UI_Direction _direction;
	    [Space(10)]
	    [SerializeField] Shader _rgba_to_a_shader;

	    Material _rgba_to_a_mat;
	    GenData2D _currentlyProcessed_genData = null;

	    public bool _brush_isPositive =>_direction.isPositive; //positive negative
	    public bool _is_can_adjust_BG => _makeScreenshots_toggle.isOn==false;
	    public bool _is_can_take_screenshots => _makeScreenshots_toggle.isOn;
	    public bool _isShowAlphaOnly_toggle => _showAlphaOnly_toggle.isOn;

	    public Action<bool> Act_AllowTakeScreenshots { get; set; } = null;


	    void OnButton_RemBG(){
	        var bgIcon = ArtBG_IconsUI_List.instance?._mainSelectedIcon;
	        if(bgIcon == null){
	            string msg = "Please import and select a background image, in the Art (BG) panel first.";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 5, false);
	            return; 
	        }
	        var genData = bgIcon._genData; 
	        if(genData == null){
	            string msg = "Please import and select a background image, in the Art (BG) panel first.";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 5, false);
	            return; 
	        }
	        _currentlyProcessed_genData = genData;

	        var rembg_arg = new Rembg_PythonRunner.Rembg_arg{
	            backgroundThresh_0_255 =  Mathf.RoundToInt(255 * _rembg_backgroundThresh.value/(float)_rembg_backgroundThresh.max),
	            foregroundThresh_0_255 =  Mathf.RoundToInt(255 * _rembg_foregroundThresh.value/(float)_rembg_foregroundThresh.max),
	            input =  new List<Texture2D>{ bgIcon.texture0().tex2D },
	            destroyInputTextures_whenDone = false,
	            onReady = OnBackgroundRemoved,
	        };
	        Rembg_PythonRunner.instance.RemoveBackground_Rembg(rembg_arg);
	    }

	    void OnBackgroundRemoved( List<Texture2D> texs ){
	        if(texs == null || texs.Count==0){ return; }

	        // extract the alpha channel from the returned textures (only one should have been returned).
	        // Use this alpha channel as the new mask of the BG image:
	        _rgba_to_a_mat.SetTexture("_MainTex", texs[0]);
	        RenderTexture dest_mask = _currentlyProcessed_genData._masking_utils._ObjectUV_brushedMaskR8[0].texArray;
	        TextureTools_SPZ.Blit( null, dest_mask, _rgba_to_a_mat);

	        texs.ForEach( t=>DestroyImmediate(t) );
	    }


	    void OnMakeScreenshotsToggle(bool isOn){
	        if(isOn){ _direction.Hide(); }
	        else{ _direction.Show(); }
	        Act_AllowTakeScreenshots?.Invoke(isOn);
	        if(isOn){ 
	            Viewport_StatusText.instance.ShowStatusText("Left-Drag will make Screenshots", false, 2, false);
	        }
	    }


	    void FadeWholePanel(){
	        float targAlpha = 0;
	        DimensionMode currMode = DimensionMode_MGR.instance._dimensionMode;
	        switch(currMode){
	            case DimensionMode.dim_gen_3d:
	                targAlpha = 1;
	                break;
	            default: 
	                targAlpha = 0;
	                break;
	        }
	        _wholePanel_canvGrp.alpha = Mathf.MoveTowards(_wholePanel_canvGrp.alpha, targAlpha, Time.deltaTime*7);
	        _wholePanel_canvGrp.gameObject.SetActive(_wholePanel_canvGrp.alpha!=0);
        
	        if(_showAlphaOnly_toggle.isOn && _wholePanel_canvGrp.gameObject.activeSelf == false){
	            _showAlphaOnly_toggle.isOn = false;
	        }
	    }


	    void Shortcuts_maybe(){
	        bool isDim3D =  DimensionMode_MGR.instance._dimensionMode == DimensionMode.dim_gen_3d;
	        if(!isDim3D){ return; }
	        if (KeyMousePenInput.isSomeInputFieldActive()) { return; }

	        if(Input.GetKeyDown(KeyCode.A) && KeyMousePenInput.isRMBpressed()==false){
	            _showAlphaOnly_toggle.isOn = !_showAlphaOnly_toggle.isOn;
	        }
	        if(Input.GetKeyDown(KeyCode.B) && KeyMousePenInput.isKey_CtrlOrCommand_pressed()){
	            OnButton_RemBG();
	        }
	    }


	    void Refresh_SliderTexts(){
	        int value     = Mathf.RoundToInt(_rembg_backgroundThresh.value);
	        string valStr = value < 100? value.ToString() : $"<size=90%>{value}</size>";
	        _rembg_backgroundTxt.text = valStr;

	        value  = Mathf.RoundToInt(_rembg_foregroundThresh.value);
	        valStr = value < 100? value.ToString() : $"<size=90%>{value}</size>";
	        _rembg_foregroundTxt.text = valStr;
	    }
    

	    void Refresh_ScreenshotToggle(){
	         //always force as off unless we are in 3D.  Helps to untoggle it when we are no longer in the 3d.
	        _makeScreenshots_toggle.isOn &=  DimensionMode_MGR.instance._dimensionMode == DimensionMode.dim_gen_3d; ;

	        // doing it here, without callback from the 'Act_OnPaintStrokeEnd' callback of the 'Background_Painter'.
	        // That's because must animate even if background doesn't exist (and we can't brush it):
	        bool wantScreenshot = _is_can_take_screenshots &&
	                                  KeyMousePenInput.isLMBreleasedThisFrame() &&
	                                  KeyMousePenInput.isKey_alt_pressed() == false &&
	                                  KeyMousePenInput.isKey_CtrlOrCommand_pressed() == false &&
	                                  MainViewport_UI.instance.isCursorHoveringMe();
	        if(wantScreenshot && Screenshot_MGR.instance.isPrefferCaptureSnippets()==false){
	            string msg = "Screenshots are possible only if a 3D generator is connected.";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 3, false);
	        }
	    }


	    void OnSomeScreenshotTaken(bool isBecauseMouseDragged)
	        => _takeScreenshots_toggleAnim.Play(); //little bouncing animation, so that user can remember that they are still painting.


	    void Update(){
	        FadeWholePanel();
	        Refresh_ScreenshotToggle();
	        Shortcuts_maybe();
	        Refresh_SliderTexts();
	    }

	    public void Save(StableProjectorz_SL spz){
	        spz.gen3D_WorkflowOptionsRibbon = new Gen3D_WorkflowOptionsRibbon_SL();
	        spz.gen3D_WorkflowOptionsRibbon.rembg_backgroundThresh = _rembg_backgroundThresh.value;
	        spz.gen3D_WorkflowOptionsRibbon.rembg_foregroundThresh = _rembg_foregroundThresh.value;
	    }

	    public void Load(StableProjectorz_SL spz){
	        _rembg_backgroundThresh.SetSliderValue(spz.gen3D_WorkflowOptionsRibbon.rembg_backgroundThresh, true);
	        _rembg_foregroundThresh.SetSliderValue(spz.gen3D_WorkflowOptionsRibbon.rembg_foregroundThresh, true);
	    }

	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;

	        _rembg_button.onClick.AddListener( OnButton_RemBG );
	        //Begin the toggle as false, but true->false, to trigger the callback:
	        _makeScreenshots_toggle.onValueChanged.AddListener( OnMakeScreenshotsToggle );
	        _makeScreenshots_toggle.isOn = true;
	        _makeScreenshots_toggle.isOn = false;
        
	        _rgba_to_a_mat = new Material(_rgba_to_a_shader);
	    }

	    void Start(){
	        Screenshot_MGR._Act_OnScreenshot += OnSomeScreenshotTaken;
	    }

	    void OnDestroy(){
	        Screenshot_MGR._Act_OnScreenshot -= OnSomeScreenshotTaken;
	        DestroyImmediate(_rgba_to_a_mat);
	    }

	}
}//end namespace
