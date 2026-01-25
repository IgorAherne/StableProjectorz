using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;
using UnityEngine.Events;

namespace spz {

	[Serializable]
	public struct HueSatValueContrast{
	    public float hueShift;
	    public float saturation;
	    public float value;
	    public float contrast;
	    public HueSatValueContrast(float h, float s, float v, float c){
	        hueShift = h; saturation = s;  value = v; contrast = c;
	    }
	    public Vector4 toVector4() => new Vector4(hueShift,saturation,value,contrast);
	}



	// Collection of values that helps some Projector Camera.
	// Helps to smooth-out the edges of its projection which the camera casts onto surfaces.
	[Serializable]
	public struct ProjBlendingParams{
	    public float totalVisibility_01;//for the entire projection
	    public float edgeBlurStride_01;
	    public float edgeBlurPow_01; //also used to blend multiview projection.
	}


	public struct BackgroundBlendParams{
	    public float blur_01;
	}


	//opens little menu inside a 2D icon which sits in some Art ui list.
	//controls several sliders etc.
	public class IconUI_Art2D_ContextMenu : MonoBehaviour{

	    [SerializeField] IconUI _myIcon;
	    [Space(10)]
	    [SerializeField] CircleSlider_Snapping_UI _slider_visibility;//opacity of the projection
	    [SerializeField] CircleSlider_Snapping_UI _slider_projCam_BlurStride;
	    [SerializeField] CircleSlider_Snapping_UI _slider_projCam_BlurPow;
	    [SerializeField] CircleSlider_Snapping_UI _slider_multiProj_BlendStr;
	    [Space(10)]
	    [SerializeField] CircleSlider_Snapping_UI _slider_BG_blurStrength;
	    [Space(10)]
	    [SerializeField] CircleSlider_Snapping_UI _slider_hueOffset;
	    [SerializeField] CircleSlider_Snapping_UI _slider_saturation;
	    [SerializeField] CircleSlider_Snapping_UI _slider_value;
	    [SerializeField] CircleSlider_Snapping_UI _slider_contrast;
	    [Space(10)]
	    [SerializeField] DoubleClickButton_UI _delete_button;
	    [SerializeField] RectTransform _upscale_buttons_parent;
	    [SerializeField] DoubleClickButton_UI _upscale_x2_button;
	    [SerializeField] DoubleClickButton_UI _upscale_x4_button;
	    [Space(10)]
	    [SerializeField] Button _save_button;
	    [SerializeField] Button _load_button;//allows to load custom image into the projection.
	    [SerializeField] Button _button_clone;
	    [SerializeField] Button _button_restoreCamera;
	    [Space(10)]
	    [SerializeField] ButtonToggle_UI _showDepth_toggle;
	    [SerializeField] Button _copySeed_button;
	    [Space(10)]
	    [SerializeField] List<GameObject> _disableWhenDepth;
	    [Space(10)]
	    [SerializeField] DoubleClickButton_UI _delight_ShadowR_button;

	    bool _alreadyShown = false;//prevents Start() from turning off gameObject.
	    bool _StartInvoked = false;

	    public Action<float> Act_OnUpscaleButtonCheck { get; set; }//First click, user haven't confirmed yet.
	    public Action<float> Act_OnUpscaleButton { get; set; }//Confirmed, argument is the upscale factor (x2 etc)
	    public Action Act_OnUpscaleButtonsHovered { get; set; }

	    public Action Act_OnDeleteButton { get; set; }
	    public Action Act_OnSaveButton { get; set; }
	    public Action Act_OnLoadButton { get; set; }
	    public Action Act_OnCopySeed_button { get; set; }
	    public Action Act_OnCloneButton { get; set; }
	    public Action<bool> Act_OnDepthButton { get; set; } //arg is 'isOn'
	    public Action Act_OnProjBlendsSliders { get; set; } = null;//when one of Projection-cam related params changes.
	    public Action Act_OnBgBlendsSliders { get; set; } = null; //when one of Background-related parameters changes.
	    public Action Act_OnDelight_ShadowR_button { get; set; } = null;//when user wants to remove shadows via ShadowR algo.

	    public Action<HueSatValueContrast> Act_OnHSVC_sliders{ get; set; } = null;

	    public void ShowOrHide(bool isShow){
	        if (_StartInvoked || isShow){  
	            _alreadyShown = true;
	            gameObject.SetActive(isShow);  
	        }

	        if (!isShow){
	            //Unity doesn't allow to do this in OnEnable() nor OnDisable(), so doing here.
	            //return the delete button back under this context menu, so it's hidden with it:
	            _delete_button.transform.SetParent(transform, worldPositionStays: true);
	            _delete_button.transform.SetAsLastSibling();
	            return;
	        }else{
	            //return the delete button back under this context menu, so it's hidden with it:
	            _delete_button.transform.SetParent(transform.parent, worldPositionStays: true);
	            _delete_button.transform.SetAsLastSibling();
	        }
	        bool isDepthOn = _showDepth_toggle.isPressed;
	        _disableWhenDepth.ForEach(go => go.SetActive(!isDepthOn));
	    }

	    public ProjBlendingParams projBlends
	        => new ProjBlendingParams { totalVisibility_01 = _slider_visibility.value,
	                                    edgeBlurStride_01 = _slider_projCam_BlurStride.value,
	                                    edgeBlurPow_01 = _slider_projCam_BlurPow.value };

	    public BackgroundBlendParams bgBlends
	        => new BackgroundBlendParams { blur_01=_slider_BG_blurStrength.value, };

	    public HueSatValueContrast hsvc
	        => new HueSatValueContrast{ hueShift = _slider_hueOffset.value,
	                                    saturation = _slider_saturation.value, 
	                                    value    = _slider_value.value, 
	                                    contrast = _slider_contrast.value };

	    public void Set_ProjBlends( ProjBlendingParams val, bool doCallback=true ){
	        _slider_visibility.SetSliderValue(val.totalVisibility_01, doCallback);
	        _slider_projCam_BlurStride.SetSliderValue(val.edgeBlurStride_01, doCallback);
	        _slider_projCam_BlurPow.SetSliderValue(val.edgeBlurPow_01, doCallback);
	    }

	    public void Set_BgBlends( BackgroundBlendParams val, bool doCallback=true ){
	        _slider_BG_blurStrength.SetSliderValue(val.blur_01, doCallback);
	    }

	    public void Set_HSVC( HueSatValueContrast val, bool doCallback=true ){
	        _slider_hueOffset.SetSliderValue(val.hueShift, doCallback);
	        _slider_saturation.SetSliderValue(val.saturation, doCallback);
	        _slider_value.SetSliderValue(val.value, doCallback);
	        _slider_contrast.SetSliderValue(val.contrast, doCallback);
	    }


	    public void ForceChange_slider_hueOffset(float val) =>_slider_hueOffset.SetSliderValue(val,true);
	    public void ForceChange_slider_saturation(float val)=>_slider_saturation.SetSliderValue(val,true);
	    public void ForceChange_slider_value(float val)  =>   _slider_value.SetSliderValue(val,true);
	    public void ForceChange_slider_contrast(float val) => _slider_contrast.SetSliderValue(val,true);
	    public void ForceClick_SeedButton() =>_copySeed_button.onClick.Invoke();
	    public void ForceClick_LoadButton() =>_load_button.onClick.Invoke();
	    public void ForceClick_SaveButton() =>_save_button.onClick.Invoke();
	    public void ForceClick_CloneButton() => _button_clone.onClick.Invoke();
	    public void ForceClick_RestoreCamButton() => _button_restoreCamera.onClick.Invoke();


	    void OnRestoreCameraButton(){
	        UserCameras_MGR.instance.Restore_CamerasPlacements(_myIcon._genData);
	    }

	    void OnCloneButton(){
	        Act_OnCloneButton?.Invoke();
	    }

	    void OnDepthButton(bool isOn){
	        Act_OnDepthButton?.Invoke(isOn);
	        //so that we can see the depth in the thumbnail, without obstructing by sliders:
	        _disableWhenDepth.ForEach(go => go.SetActive(!isOn));
	    }


	    void Update(){
	        bool contains = RectTransformUtility.RectangleContainsScreenPoint( _upscale_buttons_parent, 
	                                                                           KeyMousePenInput.cursorScreenPos());
	        if (contains){
	            Act_OnUpscaleButtonsHovered?.Invoke();
	        }
	    }


	    void Awake(){
	        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
	        // If user doesn't want to show fade-rim around new projections,
	        // ensure slider has value 0 instead of its default value.
	        //  NOTICE: doing this in Awake() and NOT invoking callback. 
	        //         Even if this icon was Loaded from disk, its value can be altered fine, after this Awake().
	        bool isAutoSoft = Proj_AutoSoftEdges_UI_MGR.instance?.isToggleOn?? false;
	        if(!isAutoSoft){  _slider_projCam_BlurStride.SetSliderValue( 0, invokeCallback:false );  }
	        if(!isAutoSoft){  _slider_projCam_BlurPow.SetSliderValue(0.5f, invokeCallback:false);  }
	    }


	    void Start(){
	        InitShowSliders();

	        _save_button.onClick.AddListener( ()=>Act_OnSaveButton?.Invoke() );
	        _load_button.onClick.AddListener( ()=>Act_OnLoadButton?.Invoke() );
	        _delete_button.onConfirmedClick +=  ()=>Act_OnDeleteButton?.Invoke();
	        _upscale_x2_button.onCheckClick += ()=>Act_OnUpscaleButtonCheck?.Invoke(2);
	        _upscale_x4_button.onCheckClick += ()=>Act_OnUpscaleButtonCheck?.Invoke(4);
	        _upscale_x2_button.onConfirmedClick += ()=>Act_OnUpscaleButton?.Invoke(2);
	        _upscale_x4_button.onConfirmedClick += ()=>Act_OnUpscaleButton?.Invoke(4);
	        _copySeed_button.onClick.AddListener( ()=>Act_OnCopySeed_button?.Invoke() );

	        _delight_ShadowR_button.onConfirmedClick += ()=> Act_OnDelight_ShadowR_button?.Invoke();

	        _showDepth_toggle.onClick += OnDepthButton;
	        _button_restoreCamera.onClick.AddListener(OnRestoreCameraButton);
	        _button_clone.onClick.AddListener(OnCloneButton);

	        if(!_alreadyShown){ gameObject.SetActive(false); }
	        _StartInvoked = true;
	    }


	    void InitShowSliders(){
	        if(_myIcon?._genData==null){ return; }//likely a "dummy" icon, that's used druing editing. Will be destroyed soon anyway.

	        bool isBG   =  _myIcon._genData.kind == GenerationData_Kind.SD_Backgrounds;
	        bool isProj =  _myIcon._genData.kind == GenerationData_Kind.SD_ProjTextures;
	        _slider_BG_blurStrength.gameObject.SetActive(isBG);
	        _slider_visibility.gameObject.SetActive(isProj);
	        _slider_projCam_BlurStride.gameObject.SetActive(isProj);
	        _slider_projCam_BlurPow.gameObject.SetActive(isProj);
	        //_slider_multiProj_BlendStr.gameObject.SetActive(isProj); //not used right now

	        _slider_visibility.onValueChanged.AddListener( v=>Objects_Renderer_MGR.instance.ReRenderAll_soon() );

	        SubscribeActions(_slider_projCam_BlurStride, v=>Act_OnProjBlendsSliders?.Invoke());
	        SubscribeActions(_slider_projCam_BlurPow,    v=>Act_OnProjBlendsSliders?.Invoke());
	        SubscribeActions(_slider_multiProj_BlendStr, v=>Act_OnProjBlendsSliders?.Invoke());

	        SubscribeActions(_slider_BG_blurStrength,  v=>Act_OnBgBlendsSliders?.Invoke());

	        SubscribeActions(_slider_hueOffset,  v=>Act_OnHSVC_sliders?.Invoke(hsvc) );
	        SubscribeActions(_slider_saturation, v=>Act_OnHSVC_sliders?.Invoke(hsvc));
	        SubscribeActions(_slider_value,    v=>Act_OnHSVC_sliders?.Invoke(hsvc));
	        SubscribeActions(_slider_contrast, v=>Act_OnHSVC_sliders?.Invoke(hsvc));
	    }
    

	    void SubscribeActions( CircleSlider_Snapping_UI slider,  UnityAction<float> subscribeThis ){
	        slider.onValueChanged.AddListener( (f)=>subscribeThis(f) );
	        slider.onValueChanged.AddListener( v=>Objects_Renderer_MGR.instance.ReRenderAll_soon() );
	    }

	}
}//end namespace
