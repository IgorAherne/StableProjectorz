using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;


namespace spz {

	public class IconUI : MonoBehaviour {

	    public static readonly string IconUI_tag = "IconUI";

	    [SerializeField] IconUI_Picture _picture;
	    [SerializeField] IconUI_SelectionFrame _selectionFrame;
	    [SerializeField] IconUI_HideSolo_Buttons _hideSolo_buttons;
	    [Space(10)]
	    [SerializeField] IconUI_Art2D_ContextMenu _art_contextMenu; //might be null.
	    [SerializeField] IconUI_AO_ContextMenu _AO_contextMenu; //might be null. For Ambient Occlusion.
	               public IconUI_ClickAndHover hovers => _hovers;
	    [SerializeField] IconUI_ClickAndHover _hovers;
	              public DraggableItem_UI myDraggableScript => _myDraggableScript;
	    [SerializeField] DraggableItem_UI _myDraggableScript;

	    bool _deinit = false;//did we perform the final cleanup (during destroy)

	    //Usually icon corresponds a single texture, from generation.
	    //But sometimes it owns many textures in generation.
	    //In that case, this icon would visually appear as a "stack". And would have several texture-guids.
	                       List<Guid> _texture_guids = new List<Guid>();
	        public IReadOnlyList<Guid> texture_guids => _texture_guids;
	    public void Set_Texture_guids( IReadOnlyList<Guid> newGuids)
	    {
	        IReadOnlyList<Guid> oldGuids = _texture_guids.ToList();//copy
	        _texture_guids = newGuids.ToList();
	        Act_OnSomeIcon_TextureGuidsChanged?.Invoke(this, oldGuids, newGuids);//invoke AFTER updating _texture_guids.
	    }

	    // this icon might "belong" to some generation. There could be several icons in the same generation.
	    // For example, 2 batches of 4 icons each, would have 8 total.
	    public Guid generation_guid => _genData.total_GUID;
	    public GenData2D _genData { get; private set; } = null; //from Generations_Dictionary. Assigned during Init

	    //icon, old guids, new guids.
	    public static Action<IconUI,IReadOnlyList<Guid>,IReadOnlyList<Guid>> Act_OnSomeIcon_TextureGuidsChanged { get; set; } = null;


	    public bool isChosen() =>  this == _myIconGroup.chosenIcon;
	    public bool isMainSelected => _myOwnerList._mainSelectedIcon == this;
	    public IconsUI_List _myOwnerList { get; private set; } = null;
	    public ArtIconsGroup _myIconGroup { get; private set; } = null;


	    public GenData_TextureRef texture0(){
	        if(_genData==null){  
	            return new GenData_TextureRef(guid:default, TexturePreference.Unknown);  
	        }
	        Guid firstGuid =  texture_guids.Count>0?  texture_guids[0] : default;
	        return _genData.GetTexture_ref(firstGuid);
	    }

	    public bool isShowing_contextMenu() => isShowing_artContextMenu() || isShowing_aoContextMenu();
	    public bool isShowing_artContextMenu() => _art_contextMenu.gameObject?.activeSelf ?? false;
	    public bool isShowing_aoContextMenu()  => _AO_contextMenu.gameObject?.activeSelf ?? false;


	    public ProjBlendingParams projBends()=>_art_contextMenu?.projBlends ?? new ProjBlendingParams();
	    public BackgroundBlendParams bgBlends() =>_art_contextMenu?.bgBlends?? new BackgroundBlendParams();
	    public HueSatValueContrast hsvc() => _art_contextMenu?.hsvc ?? new HueSatValueContrast();
	    public AmbientOcclusionInfo aoInfo() => _AO_contextMenu?.aoInfo ?? new AmbientOcclusionInfo();


	    public Action Act_OnSomeBlends_sliders{ get; set; } = null;
	    public Action Act_OnSomeBgBlends_sliders { get; set; } = null;
	    public Action<HueSatValueContrast> Act_OnHSVC_sliders { get; set; } = null;

	    public static Action<IconUI, GenerationData_Kind> Act_OnSomeIconClicked { get; set; } = null;
	    public static Action<IconUI, GenerationData_Kind> Act_OnSomeIconSelfDestroy { get; set; } = null;
	    public static Action<IconUI, GenerationData_Kind> Act_OnSomeIconCloneSelf { get; set; } = null;


	    public void preventShowingFrame(object requestor) =>_selectionFrame.PreventShowing(requestor);
	    public void isAllowShowingFrame(object originalRequestor) => _selectionFrame.AllowShowing(originalRequestor);


	    public void Toggle_ContextMenu(bool isShow){
	        if(isShow  && _hideSolo_buttons.is_HideOrSoloCover_showing){ return; }

	        _picture.OnToggled_ContextMenu(isShow);
        
	        if(_genData.kind == GenerationData_Kind.AmbientOcclusion){
	            _AO_contextMenu?.ShowOrHide( isShow );
	        }else { 
	            _art_contextMenu?.ShowOrHide( isShow );
	        }
	    }

	    public void OnMyIconGroup_HiddenOrSolo(bool someGroupRemains_asSolo){
	        bool isRemainSolo   = _myIconGroup.showMyIcons_as_solo;
	        //true only if its not our group who is solo:
	        bool OTHER_GroupRemains_asSolo =  someGroupRemains_asSolo && !isRemainSolo; 

	        bool isRemainHidden =  _myIconGroup.hideMyIcons_please || OTHER_GroupRemains_asSolo;

	        _hideSolo_buttons.SetWithoutNotify(isRemainSolo, isRemainHidden);
	        //_selectionFrame.OnMyIcon_HideOrSolo(isRemainHidden, isRemainSolo, OTHER_GroupRemains_asSolo);
	        _hideSolo_buttons.OnMyIcon_HideOrSolo(isRemainHidden, isRemainSolo, OTHER_GroupRemains_asSolo);
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();
	    }


	    //invoked when either Hide or Solo button was pressed for this icon.
	    void OnToggle_Hide(bool isOn){
	        var visibil = isOn? ArtIconsGroup.GroupVisibility.Hide 
	                          : ArtIconsGroup.GroupVisibility.DontHide;
	        _myOwnerList.ChangeVisibility_of_IconGroup(this, visibil);
	        if(visibil==ArtIconsGroup.GroupVisibility.DontHide && isChosen()){  Toggle_ContextMenu(true);  }
	    }


	    void OnToggle_Solo(bool isOn){
	        var visibil = isOn? ArtIconsGroup.GroupVisibility.Solo
	                          : ArtIconsGroup.GroupVisibility.DontSolo;
	        _myOwnerList.ChangeVisibility_of_IconGroup(this, visibil);
	        if(isChosen()){  Toggle_ContextMenu(true);  }
	    }

    
	    void OnCopySeedButton()
	        => SD_InputPanel_UI.instance.PasteSeedValue( _genData.seed() );


	    void OnCloneButton()
	        => Act_OnSomeIconCloneSelf?.Invoke(this, _genData.kind);


	    void OnMightUpscale_Button(float possible_upscaleFactor) 
	        => SD_Upscalers.instance.PlayAttentionAnim();

	    void OnUpscaleButton(float upscaleFactor){
	        StableDiffusion_Hub.instance.ManuallyUpscale(upscaleFactor, texture0().guid, _genData);
	        _myOwnerList.ChangeVisibility_of_IconGroup(this, ArtIconsGroup.GroupVisibility.Hide);
	    }

	    void OnUpscaleButtons_Hovered(){
	        if(_genData == null){ return; }
	        Texture tex = texture0().tex_by_preference();//either textureArray-render-texture, 2d-RenderTexture or a Texture2D.
	        ImagePreview_in_Viewport_MGR.instance.ShowImage( tex );
	    }

	    void OnDelight_ShadowR_Button(){
	        Delight_MGR.instance.ReduceShadows_ShadowR( _genData );
	    }


	    void OnDeleteButton(){
	        if(StableDiffusion_Hub.instance._generating){
	            string msg = "can't remove icon while stable diffusion still generating and reports progress";
	            Viewport_StatusText.instance.ShowStatusText(msg, textIsETA_number: false, textVisibleDur:1, progressVisibility:true); 
	            return; 
	        }
	        GenerationData_Kind kind =  _genData?.kind ?? GenerationData_Kind.Unknown;
	        DestroySelf();
	    }


	    void SyncSliders_inMyGroup( Action invokeAfter ){ //when our context menu slider changes value, 
	        _myIconGroup.DoForEachIcon(sync);             //make sure the sliders in our sibling icons copy it.
	        void sync(IconUI icon){
	            if(icon == this){ return; }
	            icon._art_contextMenu.Set_ProjBlends( _art_contextMenu.projBlends, doCallback:false );
	            icon._art_contextMenu.Set_BgBlends( _art_contextMenu.bgBlends, doCallback:false );
	            icon._art_contextMenu.Set_HSVC( _art_contextMenu.hsvc, doCallback:false );
	        }
	        invokeAfter?.Invoke();
	    }


	    public void OnAfterInstantiated(ArtIconsGroup myIconsGroup, List<Guid> textureGuids){

	        _myOwnerList = GetComponentInParent<IconsUI_List>(includeInactive:true);

	        this._myIconGroup  = myIconsGroup;
	        this._genData  = myIconsGroup.myGenData;
	        this._texture_guids = textureGuids;

	        _picture.OnAfterInstatiated(_art_contextMenu, _AO_contextMenu);
	        _hideSolo_buttons.OnAfterInstantiated();
	        _selectionFrame.OnAfterInstantiated();
	        _hovers.OnAfterInstantiated();

	        _hideSolo_buttons.onHide_click += OnToggle_Hide;
	        _hideSolo_buttons.onSolo_click += OnToggle_Solo;

	        if(_art_contextMenu != null){
	            // Enable go, gives layout chance to update. We might reparent some of its buttons later, so need
	            // to ensure layout has resized them properly. Its Start() will disable it automatically soon:
	            _art_contextMenu.gameObject.SetActive(true);

	            _art_contextMenu.Act_OnUpscaleButtonCheck += OnMightUpscale_Button;
	            _art_contextMenu.Act_OnUpscaleButton += OnUpscaleButton;
	            _art_contextMenu.Act_OnUpscaleButtonsHovered += OnUpscaleButtons_Hovered;
	            _art_contextMenu.Act_OnDeleteButton += OnDeleteButton;
	            _art_contextMenu.Act_OnCopySeed_button += OnCopySeedButton;
	            _art_contextMenu.Act_OnCloneButton += OnCloneButton;

	            _art_contextMenu.Act_OnDelight_ShadowR_button += OnDelight_ShadowR_Button;

	            _art_contextMenu.Act_OnProjBlendsSliders += ()=>SyncSliders_inMyGroup( Act_OnSomeBlends_sliders );
	            _art_contextMenu.Act_OnBgBlendsSliders += ()=>SyncSliders_inMyGroup( Act_OnSomeBgBlends_sliders );
	            _art_contextMenu.Act_OnHSVC_sliders  += (hsvc)=>SyncSliders_inMyGroup( ()=>Act_OnHSVC_sliders?.Invoke(hsvc) );
	        }
	        if(_AO_contextMenu != null){
	            // Enable go, gives layout chance to update. We might reparent some of its buttons later, so need
	            // to ensure layout has resized them properly. Its Start() will disable it automatically soon:
	            _AO_contextMenu.gameObject.SetActive(true);
	            _AO_contextMenu.OnDeleteButton += OnDeleteButton;
	        }
	    }


	    public void Link_ViewportContextMenu_toSelf(ViewportContextMenu_UI menuInViewport){
	            var asArt = menuInViewport as ViewportContextMenu_Art_UI;
	          var asArtAO = menuInViewport as ViewportContextMenu_AO_UI;
	        if (asArt){
	            asArt.onHueSlider += _art_contextMenu.ForceChange_slider_hueOffset;
	            asArt.onSaturationSlider += _art_contextMenu.ForceChange_slider_saturation;
	            asArt.onValueSlider += _art_contextMenu.ForceChange_slider_value;
	            asArt.onContrastSlider += _art_contextMenu.ForceChange_slider_contrast;
	            asArt.onRestoreCameraButton += _art_contextMenu.ForceClick_RestoreCamButton;
	            asArt.onSeedButton += _art_contextMenu.ForceClick_SeedButton;
	            asArt.onLoadButton += _art_contextMenu.ForceClick_LoadButton;
	            asArt.onSaveButton += _art_contextMenu.ForceClick_SaveButton;
	            asArt.onClickButton += _art_contextMenu.ForceClick_CloneButton;
	            return;
	        }
	        if(asArtAO){
	            asArtAO.onVisibilitySlider += _AO_contextMenu.ForceChange_slider_visibility;
	            asArtAO.onHalfSlider += _AO_contextMenu.ForceChange_slider_half;
	            asArtAO.onDarkSlider += _AO_contextMenu.ForceChange_slider_dark;
	            asArtAO.onMidSlider  += _AO_contextMenu.ForceChange_slider_mid;
	            asArtAO.onHighSlider += _AO_contextMenu.ForceChange_slider_high;
	            asArtAO.onSaveButton += _AO_contextMenu.ForceClick_SaveButton;
	            asArtAO.onLoadButton += _AO_contextMenu.ForceClick_LoadButton;
	            return;
	        }
	    }


	    public IconUI_SL Save(){
	        IconUI_SL icon_sl = new IconUI_SL();
	        icon_sl.texture_guids = texture_guids.Select(g=>g.ToString()).ToList();
	        icon_sl.did_deinit = _deinit;

	        if(_art_contextMenu != null){
	            icon_sl.art2DcontextMenu = new IconUI_Art2DContextMenu_SL();
	            icon_sl.art2DcontextMenu.hsvc = _art_contextMenu.hsvc;
	            icon_sl.art2DcontextMenu.projBlends = _art_contextMenu.projBlends;
	        }
	        if(_AO_contextMenu != null){
	            icon_sl.aoContextMenu = new IconUI_AOContextMenu_SL();
	            icon_sl.aoContextMenu.aoInfo = _AO_contextMenu.aoInfo;
	        }
	        return icon_sl;
	    }

	    public void Load_AfterSpawned( IconUI_SL icon_sl,  bool isChosen_in_group ){
	        _texture_guids = icon_sl.texture_guids.Select(s=>new Guid(s)).ToList();
	        _deinit =  icon_sl.did_deinit;
        
	        _art_contextMenu?.Set_ProjBlends( icon_sl.art2DcontextMenu.projBlends );
	        _art_contextMenu?.Set_HSVC( icon_sl.art2DcontextMenu.hsvc );
	        _art_contextMenu?.Set_BgBlends( icon_sl.art2DcontextMenu.bgBlends );

	        _AO_contextMenu?.Set_AOInfo( icon_sl.aoContextMenu.aoInfo );

	        _picture.OnLoad_AfterSpawned();
	        _hovers.OnLoadAfterSpawned(isChosen_in_group);
	    }


	    public void DestroySelf(){
	        if (_deinit){ return;}
	        Cleanup();
	        DestroyImmediate(this.gameObject);
	        // Cleanup manually, because OnDestroy might not run yet,
	        // if we never were enabled (different tab etc)
	    }

	    void OnDestroy(){
	        if(!_deinit){ Cleanup(); }
	    }

	    void Cleanup(){
	        //checking genData for null - can be null if icon is editor-only placeholder (removed when game begins)
	        Act_OnSomeIconSelfDestroy?.Invoke(this, _genData?.kind??GenerationData_Kind.Unknown);
	        _deinit = true;
	        _selectionFrame.OnWillBeDestroyed();
	        _picture.OnCleanup();
	        _hovers.OnCleanup();

	        if(SkyboxBackground_MGR.instance?.isObserving_IconUI(this)?? false){
	            SkyboxBackground_MGR.instance.Assign_Skybox_Background(null);//reset the skybox
	        }
	        _genData?.DisposeTextures(texture_guids);
	        _genData = null;//does't belong to us, just forget.
	    }
	}
}//end namespace
