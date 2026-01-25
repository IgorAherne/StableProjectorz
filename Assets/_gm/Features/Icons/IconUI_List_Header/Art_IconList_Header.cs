using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace spz {

	public class Art_IconList_Header : MonoBehaviour{
	    [Space(10)]
	    [SerializeField] Button _import_button;//to import textures from user's directory. Also, hovering it activates slide-out panel.

	    [SerializeField] Button _loadFromFile_uv_button;//to import usual texture that wraps around model, from directory.
	    [SerializeField] Button _loadFromFile_normals_button; //to import Normals-map texture from directory.
	    [SerializeField] Button _loadFromFile_projection_button;//to import Projection-image from directory.

	    [SerializeField] Button _importBG_fromFile_button;
	    [SerializeField] Button _importBG_normals_fromFile_button;
	    [SerializeField] Button _importBG_from_currView_button;

	    [SerializeField] SlideOut_Widget_UI _customTex_slideOut;
	    [Space(10)]
	    [SerializeField] Button _BakeAO_button;
	    [SerializeField] SlideOut_Widget_UI _bakeAO_optionsSlideOut;
	    [SerializeField] ButtonToggle_UI _BakeAO_withBlur_button;//ambient occlusion (with smoothing)
	    [SerializeField] ButtonToggle_UI _BakeAO_darkerBelow_button;//ambient occlusion (more dark underneath the object).
	    [SerializeField] Button _StopBakeAO_button; //can be shown instead of bakeAO.
	    [Space(10)]
	    [SerializeField] Button _del_AllIcons_button;
	    [SerializeField] Button _del_HiddenIcons_button;
	    [SerializeField] Button _del_NotSelectedIcons_button;
	    [Space(10)]
	    [SerializeField] Button _merge_all_button; //for merging all layers into single png
	    [SerializeField] Animation _merge_all_anim;
	    [Space(10)]
	    [SerializeField] Button _headerSettings_button;
	    [SerializeField] SlideOut_Widget_UI _headerSettings_optionsSlideOut;
	    [SerializeField] NumIconsPerRow_Button _numIconsPerRow_button;

	    public Action<GenerationData_Kind> onImport_CustomImageButton { get; set; }
	    public Action onImport_BG_fromCurrView { get; set; }

	    public Action<int> onNumIconsPerRow_button { get; set; }
	    public Action onDel_AllIcons { get; set; }
	    public Action onDel_HiddenIcons{ get; set; }
	    public Action onDel_NonSelectedIcons { get; set; }
	    public Action onMerge_all_icons { get; set; } //to collapse into a single png. This frees up memory.

	    IconsUI_List _myList;
    
	    public void OnPulsate_MergeAll_Button(bool isPulsate){
	        if(_merge_all_anim.isPlaying==isPulsate){ return; }
	        _merge_all_anim.transform.localScale = Vector3.one;
	        if(isPulsate && StableDiffusion_Hub.instance._generating==false){ 
	            _merge_all_anim.Play();
	        }else {
	            _merge_all_anim.Stop();
	        }
	    } 

	    void OnImportTextureFromFile_Hover() => _customTex_slideOut?.Toggle_if_Different(true);
    
	    void OnBakeAmbientOcclusion_ButtonStartHover() => _bakeAO_optionsSlideOut?.Toggle_if_Different(true); 

	    void OnBakeAmbientOcclusion_Button(){
	        Toggle_BakeAO_buttons(false);
	        AmbientOcclusionBake_Args args = new AmbientOcclusionBake_Args{
	            withBlur    =  _BakeAO_withBlur_button.isPressed,
	            darkerBelow =  _BakeAO_darkerBelow_button.isPressed,
	        };
	        AmbientOcclusion_Baker.instance?.BakeAO( args,  (isSuccess)=>Toggle_BakeAO_buttons(true) );
	    }


	    void Toggle_BakeAO_buttons(bool isShow){
	        if(_bakeAO_optionsSlideOut.isShowing){ _bakeAO_optionsSlideOut.Toggle_if_Different(false); }
	        _StopBakeAO_button.gameObject.SetActive(!isShow);
	        _BakeAO_button.gameObject.SetActive(isShow);
	    }


	    void OnStopBakeAmbientOcclusion_Button() => AmbientOcclusion_Baker.instance?.InterruptBake();


	    void OnHeaderSettings_ButtonHover(bool isStoppedHover){
	        if(_headerSettings_optionsSlideOut == null){ return; }
	        if(isStoppedHover){ return; }
	        _headerSettings_optionsSlideOut.Toggle_if_Different(true); 
	    }


	    public void Save(IconsList_Header_SL here){
	        here.numIcons_inGrid = _numIconsPerRow_button._num;
	        here.bakeAO_withBlur = _BakeAO_withBlur_button.isPressed;
	        here.bakeAO_shineAbove = _BakeAO_darkerBelow_button.isPressed;
	        var sl_as_bg  = here as ArtBG_IconsList_Header_SL;
	        var sl_as_art = here as Art2D_IconsList_Header_SL;
	    }

	    public void Load(IconsList_Header_SL sl){
	        _numIconsPerRow_button.Press_Manually(sl.numIcons_inGrid);//updates its text and will invoke our callback.
	        _BakeAO_withBlur_button.ForceSameValueAs( sl.bakeAO_withBlur );
	        _BakeAO_darkerBelow_button.ForceSameValueAs( sl.bakeAO_shineAbove );
	    }

	    void Awake(){
	        Init_ImportButtons();
	        _BakeAO_button.GetComponent<MouseHoverSensor_UI>().onSurfaceEnter += (cursor)=> OnBakeAmbientOcclusion_ButtonStartHover();
	        _BakeAO_button.onClick.AddListener( OnBakeAmbientOcclusion_Button );
	        _StopBakeAO_button.onClick.AddListener( OnStopBakeAmbientOcclusion_Button );
        
	        _headerSettings_button.GetComponent<MouseHoverSensor_UI>().onSurfaceEnter += (cursor)=>OnHeaderSettings_ButtonHover(isStoppedHover:false);
	        _headerSettings_button.GetComponent<MouseHoverSensor_UI>().onSurfaceExit  += (cursor)=>OnHeaderSettings_ButtonHover(isStoppedHover:true);

	        _merge_all_button.onClick.AddListener( ()=>onMerge_all_icons() );

	        _del_AllIcons_button.onClick.AddListener( ()=>onDel_AllIcons() );
	        _del_HiddenIcons_button.onClick.AddListener( ()=>onDel_HiddenIcons() );
	        _del_NotSelectedIcons_button.onClick.AddListener( ()=>onDel_NonSelectedIcons() );
	        _numIconsPerRow_button.onNumPerRow_changed +=  (num)=>onNumIconsPerRow_button?.Invoke(num);
	    }

	    void Init_ImportButtons(){
	        _import_button.onClick.AddListener( ()=>onImport_CustomImageButton?.Invoke(GenerationData_Kind.UvTextures_FromFile) );
	        _import_button.GetComponent<MouseHoverSensor_UI>().onSurfaceEnter += (cursor)=> OnImportTextureFromFile_Hover();

	        //uv, projections:
	        _loadFromFile_uv_button.onClick.AddListener(  ()=>onImport_CustomImageButton?.Invoke(GenerationData_Kind.UvTextures_FromFile) );
	        _loadFromFile_normals_button.onClick.AddListener( ()=>onImport_CustomImageButton?.Invoke(GenerationData_Kind.UvNormals_FromFile) );
	        _loadFromFile_projection_button.onClick.AddListener( ()=> onImport_CustomImageButton?.Invoke(GenerationData_Kind.SD_ProjTextures) );

	        //bgs:
	        _importBG_fromFile_button.onClick.AddListener( ()=>onImport_CustomImageButton?.Invoke(GenerationData_Kind.SD_Backgrounds) );
	        _importBG_from_currView_button.onClick.AddListener( ()=> onImport_BG_fromCurrView?.Invoke() );
	        _importBG_normals_fromFile_button.onClick.AddListener( ()=> onImport_CustomImageButton?.Invoke(GenerationData_Kind.BgNormals_FromFile) );

	        //enable/disable:
	        _myList = GetComponentInParent<IconsUI_List>();
	        bool isForBG = (_myList as ArtBG_IconsUI_List) != null;

	        _importBG_fromFile_button.gameObject.SetActive(isForBG);
	        _importBG_from_currView_button.gameObject.SetActive(isForBG);
	        _importBG_normals_fromFile_button.gameObject.SetActive(isForBG);

	        _loadFromFile_uv_button.gameObject.SetActive( !isForBG );
	        _loadFromFile_normals_button.gameObject.SetActive( !isForBG );
	        _loadFromFile_projection_button.gameObject.SetActive( !isForBG );

	        int numButtons = isForBG ? 3 : 3;
	        var slideout_rtf = _customTex_slideOut.transform as RectTransform;
	        slideout_rtf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, numButtons*40);
	    }


	    void OnDestroy(){
	        _import_button.onClick.RemoveAllListeners();
	    }

	}
}//end namespace
