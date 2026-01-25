using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace spz {

	//if we are opened, control net is considered "active"
	public class ControlNetUnit_UI : MonoBehaviour{

	    // Is it to be merely shown as a popup next to the controlnet thumbnails under the text prompts.
	    // If so, it's just meant to mirror some specific controlnet panel.
	    [SerializeField] bool _isForPreviewOnly = false;

	    [Space(10)]
	    [SerializeField] RectTransform _headerTransf;
	    [SerializeField] TextMeshProUGUI _mainHeader;
	    [SerializeField] Image _mainHeaderImage;
	    [SerializeField] Button _headerRibbon_button;
	    [SerializeField] LayoutElement _myLayoutElement;//for this entire unit box (contains header and contents)
	    [SerializeField] GameObject _contents;//hidden when we are collapsed.
	    [Space(10)]
	    [SerializeField] CollapsableSection_UI _collapsableSection;
	    [Space(10)]
	    [SerializeField] Toggle _lowVRAM_toggle;

	    public ControlNetUnit_Dropdowns dropdowns => _dropdowns;
	    [SerializeField] ControlNetUnit_Dropdowns _dropdowns;
	    [SerializeField] ControlnetPreprocessor_UI _preprocessor;
	    [Space(10)]
	    [SerializeField] ControlNetUnit_DownloadHelper _downloadHelper;
	    [Space(10)]
	    [SerializeField] ControlNetUnit_ImagesDisplay _imgsDisplay;
	    [Space(10)]
	    [SerializeField] CircleSlider_Snapping_UI _controlWeight_slider;
	    [SerializeField] CircleSlider_Snapping_UI _startingControl_step;
	    [SerializeField] CircleSlider_Snapping_UI _endingControl_step;
	    [Space(10)]
	    [SerializeField] GameObject _headerMenu_go;//contains some buttons. Can hide when we are collapsed.
	    [SerializeField] Toggle _balanced_toggle;
	    [SerializeField] Toggle _promptImportant_toggle;
	    [SerializeField] Toggle _ctrlNetImportant_toggle;
	    [Space(10)]
	    [SerializeField] ControlNetUnit_ThreshSliders _threshSliders;
    
	    bool _wasCreatedViaLoad = false;


	    public bool isActivated => _collapsableSection._isExpanded;

	    //When StableProjectorz was launched for the first time, there are no models at all (user needs to download them)
	    //Other scripts will query this static value, and can refuse to do things while it's false.
	    public static bool hasAtLeastSomeModel => ControlNetUnit_Dropdowns.hasAtLeastSomeModel;

	    public bool isReferencePreprocessor() => _preprocessor.isReferencePreprocessor();
	    public string currPreprocessorName() => _preprocessor.currPreprocessorName();
	    public bool is_currPreprocessor_none => _preprocessor.is_currPreprocessor_none;



	    //for "Control Mode": https://github.com/Mikubill/sd-webui-controlnet/wiki/API#controlnetunitrequest-json-object
	    public enum ControlMode { Balanced=0, MyPromptMoreImportant=1, CtrlNetMoreImportant=2, }
	    ControlMode _controlMode =  ControlMode.Balanced;
	    public static string ControlMode_tostr(ControlMode mode){
	        switch (mode){//Automatic1111 needs exact string, with spaces (no longer accepts integers, from May2024).
	            case ControlMode.Balanced: return "Balanced";            //so, using this method to convert to str.
	            case ControlMode.MyPromptMoreImportant: return "My prompt is more important";
	            case ControlMode.CtrlNetMoreImportant: return "ControlNet is more important";//inside 'sd-webui-controlnet/scripts/enums.py'
	            default: return "Balanced";
	        }
	    }

	    public bool isForInpaint() => _dropdowns.HasString("inpaint");//does this unit help to reproduce image in masked areas.
	    public bool isForDepth() => _dropdowns.HasString("depth") || _imgsDisplay._whatImageToSend ==WhatImageToSend_CTRLNET.Depth;
	    public bool isForNormals() => _dropdowns.HasString("norm")|| _imgsDisplay._whatImageToSend==WhatImageToSend_CTRLNET.Normals;
	    public bool isForColors() => _dropdowns.HasString("vert");
	    public string currModelName() =>_dropdowns.currModelName();
	    public bool is_currModel_none => _dropdowns.is_currModel_none;


	    // For example to also show this texture in some preview-thumbnail elsewhere.
	    // Beware, the texture might be destroyed, so your thumb might show black.
	    // DO NOT USE THIS FOR SD-GENERATION ARGUMENTS, instead use GetArgs_forGenerationRequest().
	    public Texture visibleTexture_ref => _imgsDisplay.getTexture_ref_ownedBySomeone(returnPlaceholder_ifNone:true);

	    // DO NOT USE THIS FOR SD-GENERATION ARGUMENTS, instead use GetArgs_forGenerationRequest().
	    public WhatImageToSend_CTRLNET _whatImageToSend  => _imgsDisplay._whatImageToSend;


	    // Usually for copying values between this unit-ui and a Thumbnail-ui
	    // (next to the text prompts, inside input panel).
	    // Allows us to be affected when the user adjusts sliders inside thumbnail/its preview panel.
	    public void CopyFromAnother( ControlNetUnit_UI other ){
	        //DO NOT copy _isForPreviewOnly. Usually we invoke this methond between a preview and a non-preview.
	        _lowVRAM_toggle.isOn = other._lowVRAM_toggle.isOn;
	        _dropdowns.CopyFromAnother(other._dropdowns);
	        _preprocessor.CopyFromAnother(other._preprocessor);
	        _imgsDisplay.CopyFromAnother(other._imgsDisplay); 

	        void setSlider_ifDifferent(CircleSlider_Snapping_UI slider, CircleSlider_Snapping_UI otherSlider){
	            if(slider.value == otherSlider.value){ return; }//to avoid duplicate callbacks
	            slider.SetSliderValue(otherSlider.value, invokeCallback:true);
	        }
	        setSlider_ifDifferent(_controlWeight_slider, other._controlWeight_slider);
	        setSlider_ifDifferent(_startingControl_step, other._startingControl_step);
	        setSlider_ifDifferent(_endingControl_step,  other._endingControl_step);

	        _balanced_toggle.isOn = other._balanced_toggle.isOn;
	        _promptImportant_toggle.isOn = other._promptImportant_toggle.isOn;
	        _ctrlNetImportant_toggle.isOn = other._ctrlNetImportant_toggle.isOn;

	        _threshSliders.CopyFromAnother(other._threshSliders);
	        _controlMode = other._controlMode;
	    }



	    //provides a summary of the current settings,
	    //so that we can send a Generate request to stable diffusion.
	    // We can use what's already in 'intermediates' arg, or actually add stuff to it.
	    // WILL RETURN NULL IF THIS UNIT DOESN'T WANT TO PARTICIPATE FOR SOME REASON.
	    public ControlNetUnit_NetworkArgs GetArgs_forGenerationRequest( SD_GenRequestArgs_byproducts intermediates ){
	        if (!isActivated){ return null; }//show that we are not participating

	        bool ignoreDepthOrNorms  = SD_WorkflowOptionsRibbon_UI.instance.ignoreDepthOrNormals;
	        if(isForDepth() && ignoreDepthOrNorms){ return null; }
	        if(isForNormals() && ignoreDepthOrNorms){ return null; }

	        Texture2D imageToSend = getDisposableTexture_toSend(intermediates);

	        if (isForInpaint()){
	            var trib = WorkflowRibbon_UI.instance;
	            bool hasMask = trib.has_brushed_mask() || trib.has_auto_mask();
	            bool isImg2Img = trib.isMode_using_img2img();

	            if(!isImg2Img || !trib.has_brushed_mask() ){ return null; }//indicate that we won't participate.

	            if(intermediates.screenSpaceMask_NE_disposableTex==null){
	                Texture2D skipAntiEdge_;
	                Texture2D withAntiEdge_;
	                Inpaint_MaskPainter.instance.GetDisposable_ScreenMask( forceFullWhite:false, out skipAntiEdge_, out withAntiEdge_ );
	                intermediates.screenSpaceMask_NE_disposableTex = skipAntiEdge_;
	                intermediates.screenSpaceMask_WE_disposableTex = withAntiEdge_;
	            }
	        }

	        string inputImgStr = TextureTools_SPZ.TextureToBase64(imageToSend);
	        HowToResizeImg_CTRLNET resizeMode =  _imgsDisplay._whatImageToSend==WhatImageToSend_CTRLNET.CustomFile?
	                                                                            _imgsDisplay._customImg_howResize 
	                                                                          : HowToResizeImg_CTRLNET.ScaleToFit_InnerFit;
	        return new ControlNetUnit_NetworkArgs {
	            image = inputImgStr,
	            resize_mode = ControlNetUnit_ImagesDisplay.HowToResizeImg_tostr(resizeMode),
	            low_vram = _lowVRAM_toggle.isOn,
	            processor_res = _preprocessor.get_processor_res(),
	            threshold_a = _threshSliders.threshold_A,
	            threshold_b = _threshSliders.threshold_B,
	            model = currModelName(),
	            module = _preprocessor.currPreprocessorName(),
	            weight = _controlWeight_slider.value,
	            guidance_start = _startingControl_step.value,
	            guidance_end = _endingControl_step.value,
	            control_mode = ControlMode_tostr(_controlMode),
	        };
	    }


	    Texture2D getDisposableTexture_toSend(SD_GenRequestArgs_byproducts intermediates){
	        switch (_imgsDisplay._whatImageToSend){
	            case WhatImageToSend_CTRLNET.None:
	                return null;

	            case WhatImageToSend_CTRLNET.Depth:
	                //if depth was already generated while collecting these args,
	                //then just use it instead of creating one more:
	                Texture2D depth =  intermediates.depth_disposableTex;
	                if(depth == null){
	                    depth = UserCameras_MGR.instance.camTextures.GetDisposable_DepthTexture();
	                    intermediates.depth_disposableTex = depth;
	                }
	                return depth;

	            case WhatImageToSend_CTRLNET.Normals:
	                //if view-normals was already generated while collecting these args,
	                //then just use it instead of creating one more:
	                Texture2D normals =  intermediates.viewNormals_disposableTex;
	                if(normals == null){
	                    normals = UserCameras_MGR.instance.camTextures.GetDisposable_NormalsTexture();
	                    intermediates.viewNormals_disposableTex = normals;
	                }
	                return normals;

	            case WhatImageToSend_CTRLNET.VertexColors:
	                //if texture was already generated while collecting these args,
	                //then just use it instead of creating one more:
	                Texture2D tex =  intermediates.vertexColors_disposableTex;
	                if(tex == null){
	                    tex = UserCameras_MGR.instance.camTextures.GetDisposable_VertexColorsTexture();
	                    intermediates.vertexColors_disposableTex = tex;
	                }
	                return tex;

	            case WhatImageToSend_CTRLNET.ContentCam:
	                //if texture was already generated while collecting these args,
	                //then just use it instead of creating one more:
	                Texture2D viewTex =  intermediates.usualView_disposableTexture;
	                if(viewTex == null){
	                    viewTex = UserCameras_MGR.instance.camTextures.GetDisposable_ContentCamTexture();
	                    intermediates.usualView_disposableTexture = viewTex;
	                }
	                return viewTex;

	            case WhatImageToSend_CTRLNET.CustomFile:
	                //NOTICE: not storing to intermediates, because each CTRLNet might have its own custom img.
	                return _imgsDisplay.GetCustomImg_sysFile_disposableCpy();

	            default:
	                Debug.LogError("unknown WhatImageToSend type");
	                break;
	        }
	        return null;
	    }


	    void OnControlModeToggle(bool isOn, ControlMode mode){
	        if(!isOn){ return; }
	        _controlMode = mode;
	    }


	    void OnOpenCloseSelf(bool isOpen){
	        _headerMenu_go.SetActive(isOpen);
	    }
    
	    void OnHeaderButton(){
	        if(_isForPreviewOnly){ return; }//ignore collapsing, always keep expanded.
	        _collapsableSection.OpenCloseSelf();
	    }
     

	    void OnDestroy(){
	    }


	    void Awake(){
	        if(!_wasCreatedViaLoad){ InitCollapsableSection(isFromAwake:true); }

	        _balanced_toggle.onValueChanged.AddListener( (isOn)=>OnControlModeToggle(isOn,ControlMode.Balanced) );
	        _promptImportant_toggle.onValueChanged.AddListener( (isOn)=>OnControlModeToggle(isOn,ControlMode.MyPromptMoreImportant) );
	        _ctrlNetImportant_toggle.onValueChanged.AddListener( (isOn)=>OnControlModeToggle(isOn,ControlMode.CtrlNetMoreImportant) );
	    }


	    //either via Awake() or Load(), or both. (order might change depending if gameObj is active or not)
	    void InitCollapsableSection(bool isFromAwake, bool isPreferOpen=false){ 
        
	        _headerRibbon_button.onClick.RemoveAllListeners();
	        _headerRibbon_button.onClick.AddListener( OnHeaderButton );

	        _collapsableSection.onOpenOrClose -= OnOpenCloseSelf;
	        _collapsableSection.onOpenOrClose += OnOpenCloseSelf;

	        _mainHeader.text = "ControlNet " + transform.GetSiblingIndex();
	        //check if wasn't spawned by loading a project-save file:
	        if(isFromAwake){
	            //only first unit is opened. Rest start collapsed.
	            //Set self as closed if we are zero, so that OnOpenCloseSelf flips it to opened:
	            bool isFirst =  transform.GetSiblingIndex()==0;
	            _collapsableSection.OpenOrCloseSelf( isFirst, dur:0 );
	        }else{
	            _collapsableSection.OpenOrCloseSelf( isPreferOpen, dur:0 );
	        }
	    }


	    public void OnRefresh_WebuiInfo_Complete(){
	        bool isNeedDownloadMandatoryModel;
	        _dropdowns.OnRefreshInfo_Complete( out isNeedDownloadMandatoryModel );
	        _downloadHelper.OnRefreshInfoComplete( isNeedDownloadMandatoryModel );
	    }

   
	    public void Save(int ix, ControlNetUnit_SL unit_sl, string dataDir){

	        unit_sl.isEnabled = isActivated;

	        _preprocessor.Save(unit_sl);

	        unit_sl.isLowVram = _lowVRAM_toggle.isOn;
	        //for "Control Mode": https://github.com/Mikubill/sd-webui-controlnet/wiki/API#controlnetunitrequest-json-object
	        unit_sl.controlMode =  _controlMode.ToString();
        
	        unit_sl.controlWeight = _controlWeight_slider.value;
	        unit_sl.startingControl_step = _startingControl_step.value;
	        unit_sl.endingControl_step = _endingControl_step.value;

	        _dropdowns.Save(unit_sl);
	        _imgsDisplay.Save(ix, unit_sl, dataDir);
	    }

    
	    public void Load(ControlNetUnit_SL unit_sl, string dataDir){
	        _wasCreatedViaLoad = true;

	        InitCollapsableSection(isFromAwake:false, isPreferOpen:unit_sl.isEnabled);

	        _preprocessor.Load(unit_sl);
	        Load_ControlMode(unit_sl);

	        _lowVRAM_toggle.isOn =  unit_sl.isLowVram;
	        _controlWeight_slider.SetSliderValue( unit_sl.controlWeight, true);
	        _startingControl_step.SetSliderValue( unit_sl.startingControl_step, true);
	        _endingControl_step.SetSliderValue( unit_sl.endingControl_step, true);

	        _dropdowns.Load(unit_sl);
	        _imgsDisplay.Load(unit_sl, dataDir);
	    }


	    void Load_ControlMode( ControlNetUnit_SL unit_sl ){
	        object parsedMode;
	        bool parsed = Enum.TryParse( typeof(ControlMode), unit_sl.controlMode, out parsedMode);
	        ControlMode mode = parsed? (ControlMode)parsedMode : ControlMode.Balanced;
	        switch (mode){
	            case ControlMode.Balanced: _balanced_toggle.isOn = true; break;
	            case ControlMode.MyPromptMoreImportant: _promptImportant_toggle.isOn = true; break;
	            case ControlMode.CtrlNetMoreImportant: _ctrlNetImportant_toggle.isOn = true; break;
	            default: Debug.Log("unknown ControlMode type"); break;
	        }
	    }

	}
}//end namespace
