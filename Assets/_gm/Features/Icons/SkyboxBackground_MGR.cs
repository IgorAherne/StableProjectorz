using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

namespace spz {

	public class SkyboxBackground_MGR : MonoBehaviour{
	    public static SkyboxBackground_MGR instance { get; private set; } = null;

	    [SerializeField] Material _skyboxMaterial; 
	    [SerializeField] Material _skyboxMaterial_ui;

	    Material _skyboxMaterial_copy = null;
	    Material _skyboxMaterial_ui_copy = null;
	    Material _skyboxMat_copy_override = null;

	    IconUI _currentIcon; //which icon's texture we are displaying.

	    //we can perform blur on the original texture and store result here.
	    RenderTexture _currentBG_texture_clone;

	    //helps to keep rendering with alpha of 1, ignoring the mask:
	    LocksHashset_OBJ _fullAlpha_lock = new LocksHashset_OBJ();


	    //keep showing gradient of those colors, unless Background icon is clicked again
	    public bool isGradientColorClear => _currentTopColor==Color.clear;
	    Color _currentTopColor    = Color.clear;
	    Color _currentBottomColor = Color.clear;
	    public void SetTopOrBottomColor(bool isTop, Color col){
	        string msg = "";
	        if(isTop){ 
	            _currentTopColor = col;
	            msg = "BG Color:  always Image-to-Image.  Showing Colors until some background icon is selected.";
	        }
	        else{ 
	            _currentBottomColor = col;
	            msg = "BG Color:  always Image-to-Image.  Showing Colors until some background icon is selected.";
	        }
	        //regardless of Top or Bottom, check if it's a clear color:
	        if(col == Color.clear){
	            msg = "BG Clear Color:  'Text-to-Image' until some background icon is selected.";
	        }
	        Viewport_StatusText.instance.ShowStatusText(msg, false, 7, false);
	        ArtBG_IconsUI_List.instance.Set_IsPretendNoBackground(true);
	    }


	    public void FullAlpha_Lock(object requestor) =>_fullAlpha_lock.Lock(requestor);
	    public void FullAlpha_StopLock(object originalRequestor) =>_fullAlpha_lock.Unlock(originalRequestor);


	    //are we paying attention to the same texture, which the IconUI is interested in:
	    public bool isObserving_IconUI( IconUI which ){
	        return which != null && which == _currentIcon;
	    }

	    void ForgetCurrentIconUI_ifCan( IReadOnlyList<Guid> icon_guidsToUnsubscribe=null ){
	        if (_currentIcon==null){return;} 
	        if (_currentIcon._genData == null){ return; }
	        icon_guidsToUnsubscribe = icon_guidsToUnsubscribe?? _currentIcon.texture_guids;
	        _currentIcon._genData.Unsubscribe_from_textureUpdates( icon_guidsToUnsubscribe,  OnTextureUpdated );
	        _currentIcon.Act_OnSomeBgBlends_sliders -= OnSomeBgBlends_sliders;
	    }

	    public void Assign_Skybox_Background(IconUI icon, bool forceRefresh=false){
	        if(!forceRefresh && _currentIcon == icon) { return; } //avoids cloning the texture, cancelling the blur, etc.

	        ForgetCurrentIconUI_ifCan();

	        if(icon == null){
	            _currentIcon = null;
	            OnTextureUpdated(null);
	            return;
	        }
	        _currentIcon = icon;
	        GenData_TextureRef texRef = icon.texture0();
	        OnTextureUpdated(texRef);

	        icon._genData.Subscribe_for_TextureUpdates( icon.texture_guids,  OnTextureUpdated );
	        icon.Act_OnSomeBgBlends_sliders += OnSomeBgBlends_sliders;
	    }


	    // For example, when we need to render normals-map as a background, at some point during frame
	    // Don't forget to invoke EndMomentaryOverride() after you are done.
	    public void MomentaryOverride_Texture(Texture temporaryBGtex){
	        //if failed, you probably forgot to invoke EndMomentaryOverride() before:
	        Debug.Assert(RenderSettings.skybox != _skyboxMat_copy_override);
	        _skyboxMat_copy_override.SetTexture("_MainTex", temporaryBGtex);
	        var maskTex = getCurrentMaskTexture();
	        _skyboxMat_copy_override.SetTexture("_BgMaskTex", maskTex?? Texture2D.whiteTexture);
	        RenderSettings.skybox = _skyboxMat_copy_override;
	    }

	    public void EndMomentaryOverride(){
	        RenderSettings.skybox = _skyboxMaterial_copy;
	    }


	    void OnTextureUpdated( GenData_TextureRef texRef ){

	        var bg_rect = EventsBinder.FindObj<SkyboxBackground_Rect_UI>( nameof(SkyboxBackground_Rect_UI) );
	        if (bg_rect == null){ return; }

	        Texture tex = texRef?.tex_by_preference();
	        if(tex == null){
	            while (bg_rect.uiImage_withAspect.RemoveLatestTexture_ifExists()){ }
	            return;  
	        }
	        Debug.Assert(tex.dimension == TextureDimension.Tex2D);

	        //copy to our buffer, in case we want to blur it later:
	        CloneTexture( tex );

	        //showing the clone, not the original texture. Because we can blur the clone etc:
	        _skyboxMaterial_copy?.SetTexture("_MainTex", _currentBG_texture_clone);

	        bg_rect.uiImage_withAspect.ShowTexture_dontOwn(_currentBG_texture_clone,  0,  isGenerated:false,  CameraTexType.Nothing, 
	                                                        GenerationData_Kind.SD_Backgrounds,  forceAspect:-1);
	        //don't update the _skyboxMat_copy_forTempReplace.
	    }

  
	    void CloneTexture(Texture tex){
	        bool remake =  _currentBG_texture_clone == null  ||
	                       _currentBG_texture_clone.width != tex.width  ||
	                       _currentBG_texture_clone.height != tex.height  ||
	                       _currentBG_texture_clone.graphicsFormat != tex.graphicsFormat;
	        if(remake){ 
	            if(_currentBG_texture_clone != null){  
	                if(RenderTexture.active == _currentBG_texture_clone){ RenderTexture.active = null; }
	                DestroyImmediate(_currentBG_texture_clone); 
	            }
	            _currentBG_texture_clone = new RenderTexture(tex.width, tex.height, 0, GraphicsFormat.R8G8B8A8_UNorm, mipCount:0);
	        }
	        TextureTools_SPZ.Blit(tex, _currentBG_texture_clone);
	    }


	    // Return the R8 mask from the currently selected BGs GenData2D:
	    Texture getCurrentMaskTexture(){
	        if(_currentIcon == null || _currentIcon._genData == null){ return null; }
	        var masks = _currentIcon._genData._masking_utils;
	        if(masks == null || masks._ObjectUV_brushedMaskR8.Count == 0){ return null; }

	        var maskUdims = masks._ObjectUV_brushedMaskR8[0];
	        if(maskUdims == null){ return null; }
	        return maskUdims.texArray;// Usually maskUdims.texArray is the actual R8. If its only 1 slice, thats fine.
	    }


	    void OnSomeIconUI_selected(IconUI someIcon, GenerationData_Kind kind){
	        if(kind != GenerationData_Kind.SD_Backgrounds){ return;}
	        Assign_Skybox_Background( someIcon );
	        string msg = "Background Picture:  always Image-to-Image. Showing background until color is selected.";
	        Viewport_StatusText.instance.ShowStatusText(msg, false, 8, false);
	    }


	    void OnSomeBgBlends_sliders(){
	        GenData_TextureRef texRef = _currentIcon.texture0();
	        TextureTools_SPZ.Blit(texRef.tex_by_preference(), _currentBG_texture_clone); //so that we don't blur already blurred.

	        //Use  3 iters,  half size 8,  amplification at or less than 0.35
	        //For simpler values the wide aspects (768 x 512) will show artifacts at high blur values
	        BackgroundBlendParams bgBlends = _currentIcon.bgBlends();
	        for(int i=0; i<3; ++i){ 
	            var arg = new BlurTextures_MGR.BlurTextureArg( _currentBG_texture_clone, null, blurBoxHalfSize_1_to_12:8,
	                                                           bgBlends.blur_01 );
	            arg.farSteps_amplification01 = 0.35f;
	            BlurTextures_MGR.instance.Blur_texture(arg);
	        }
	    }


	    void Update(){
	        // If no icon, making sure to use default HSVC.
	        // This prevents bug where HSVC isn't reset after an icon was deleted. (july 2024)
	        HueSatValueContrast hsvc = _currentIcon?.hsvc() ?? new HueSatValueContrast(0,1,1,1);

	        _skyboxMaterial_copy.SetVector("_HSV_and_Contrast", hsvc.toVector4());
	        _skyboxMaterial_ui_copy.SetVector("_HSV_and_Contrast", hsvc.toVector4());
	        _skyboxMat_copy_override.SetVector("_HSV_and_Contrast", hsvc.toVector4());

	        var ucw = UserCameras_UV_warp_Helper.instance;
	        float inspect_uvs_bg01 = ucw!=null? ucw.warp_into_uv01 : 0;
	        inspect_uvs_bg01 = Mathf.Pow(inspect_uvs_bg01, 5);
	        _skyboxMaterial_ui_copy.SetFloat("_InspectUVs_01", inspect_uvs_bg01);
	        _skyboxMaterial_copy.SetFloat("_InspectUVs_01", inspect_uvs_bg01);
	        _skyboxMat_copy_override.SetFloat("_InspectUVs_01", inspect_uvs_bg01);

	        var bgp = Background_Painter.instance;
	        RenderTexture maskTex  =  bgp!=null? bgp.current_BG_MaskRenderUdim()?.texArray : null;

	        var dims =  DimensionMode_MGR.instance;
	        bool isNot3D =  dims != null && dims._dimensionMode != DimensionMode.dim_gen_3d;
	        bool isForceMaskAlpha1 =  isNot3D || maskTex == null || _fullAlpha_lock.isLocked();

	        Color noiseColor = Settings_MGR.instance.get_noiseColor();
	        float noiseSpeed = Settings_MGR.instance.get_noiseSpeed() * 0.05f;
	              noiseSpeed = Application.isFocused?noiseSpeed : 0;//to avoid distracting user who Alt+Tabbed

	        Rect viewRect = MainViewport_UI.instance?.innerViewportRect.rect ?? new Rect(0,0,1024,1024);
	        float viewAspect = viewRect.width / (float)viewRect.height;

	        bool hasBg = ArtBG_IconsUI_List.instance?.hasBackground(considerGradientColors:false) ??  false;
	        bool useGradientColors  = _currentBottomColor != Color.clear;
	             useGradientColors |= _currentTopColor != Color.clear;
	             useGradientColors &= !hasBg;

	        bool isShowChecker  = !useGradientColors && !hasBg;

	        bool isOnlyShow_MaskAlpha = Gen3D_WorkflowOptionsRibbon_UI.instance?._isShowAlphaOnly_toggle ?? false;

	        _skyboxMaterial_copy?.SetTexture("_BgMaskTex", maskTex);
	        _skyboxMaterial_copy?.SetFloat("_has_bgMaskTex", maskTex!=null?1:0);
	        _skyboxMaterial_copy?.SetFloat("_isForceMaskAlpha1", isForceMaskAlpha1?1:0);
	        _skyboxMaterial_copy?.SetColor("_NoiseColor", noiseColor);
	        _skyboxMaterial_copy?.SetFloat("_NoiseSpeed", noiseSpeed);
	        _skyboxMaterial_copy?.SetFloat("_ViewportAspect", viewAspect);
	        _skyboxMaterial_copy?.SetFloat("_UseGradientColors", useGradientColors? 1 : 0);
	        _skyboxMaterial_copy?.SetColor("_BotGradientColor", _currentBottomColor);
	        _skyboxMaterial_copy?.SetColor("_TopGradientColor", _currentTopColor);
	        _skyboxMaterial_copy?.SetFloat("_isShowCheckerTex", isShowChecker?1:0);
	        _skyboxMaterial_copy?.SetFloat("_showAlphaOnly", isOnlyShow_MaskAlpha?1:0);

	        _skyboxMaterial_ui_copy?.SetTexture("_BgMaskTex", maskTex);
	        _skyboxMaterial_ui_copy?.SetFloat("_has_bgMaskTex", maskTex!=null?1:0);
	        _skyboxMaterial_ui_copy?.SetFloat("_isForceMaskAlpha1", isForceMaskAlpha1?1:0);
	        _skyboxMaterial_ui_copy?.SetColor("_NoiseColor", noiseColor);
	        _skyboxMaterial_ui_copy?.SetFloat("_NoiseSpeed", noiseSpeed);
	        _skyboxMaterial_ui_copy?.SetFloat("_ViewportAspect", viewAspect);
	        _skyboxMaterial_ui_copy?.SetFloat("_UseGradientColors", useGradientColors? 1 : 0);
	        _skyboxMaterial_ui_copy?.SetColor("_BotGradientColor", _currentBottomColor);
	        _skyboxMaterial_ui_copy?.SetColor("_TopGradientColor", _currentTopColor);
	        _skyboxMaterial_ui_copy?.SetFloat("_isShowCheckerTex", isShowChecker?1:0);
	        _skyboxMaterial_ui_copy?.SetFloat("_showAlphaOnly", isOnlyShow_MaskAlpha?1:0);

	        _skyboxMat_copy_override?.SetTexture("_BgMaskTex", maskTex);
	        _skyboxMat_copy_override?.SetFloat("_has_bgMaskTex", maskTex!=null?1:0);
	        _skyboxMat_copy_override?.SetFloat("_isForceMaskAlpha1", isForceMaskAlpha1?1:0);
	        _skyboxMat_copy_override?.SetColor("_NoiseColor", noiseColor);
	        _skyboxMat_copy_override?.SetFloat("_NoiseSpeed", noiseSpeed);
	        _skyboxMat_copy_override?.SetFloat("_ViewportAspect", viewAspect);
	        _skyboxMat_copy_override?.SetFloat("_UseGradientColors", useGradientColors? 1 : 0);
	        _skyboxMat_copy_override?.SetColor("_BotGradientColor", _currentBottomColor);
	        _skyboxMat_copy_override?.SetColor("_TopGradientColor", _currentTopColor);
	        _skyboxMat_copy_override?.SetFloat("_isShowCheckerTex", isShowChecker?1:0);
	        _skyboxMat_copy_override?.SetFloat("_showAlphaOnly", isOnlyShow_MaskAlpha?1:0);

	        Background_Painter.instance?.ApplyCurrBrushStroke(_skyboxMaterial_copy);
	        Background_Painter.instance?.ApplyCurrBrushStroke(_skyboxMaterial_ui_copy);
	        Background_Painter.instance?.ApplyCurrBrushStroke(_skyboxMat_copy_override);
	    }


	    void OnSomeIcon_TextureGuidsChanged(IconUI icon, IReadOnlyList<Guid> oldGuids, IReadOnlyList<Guid> newGuids) {
	        if(isObserving_IconUI(icon)==false){ return; }

	        ForgetCurrentIconUI_ifCan(oldGuids);

	        _currentIcon._genData.Unsubscribe_from_textureUpdates(oldGuids, OnTextureUpdated);
	        _currentIcon.Act_OnSomeBgBlends_sliders -= OnSomeBgBlends_sliders;
	        _currentIcon = null;

	        Assign_Skybox_Background(icon, forceRefresh: true);
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;

	        InitMaterials();

	        IconUI.Act_OnSomeIconClicked += OnSomeIconUI_selected;
	        IconUI.Act_OnSomeIcon_TextureGuidsChanged += OnSomeIcon_TextureGuidsChanged;
	    }


	    void Start(){
	        OnTextureUpdated(null); //doing in start, else built exe shows gray color (Sept 2024)
	    }


	    void InitMaterials(){
	        // Apply the updated material to the skybox
	        _skyboxMaterial_copy  =  new Material(_skyboxMaterial);
	        RenderSettings.skybox = _skyboxMaterial_copy;

	        _skyboxMat_copy_override = new Material(_skyboxMaterial);
	        _skyboxMaterial_ui_copy =  new Material( _skyboxMaterial_ui );

	        var bgr = EventsBinder.FindComponent<SkyboxBackground_Rect_UI>( nameof(SkyboxBackground_Rect_UI) );
	        if(bgr != null){ InitOther(bgr); }
	    }

	    public void InitOther(SkyboxBackground_Rect_UI init_me){
	        init_me.uiImage_withAspect.GetComponent<RawImage>().material = _skyboxMaterial_ui_copy;
	    }


	    void OnDestroy(){
	        RenderSettings.skybox = _skyboxMaterial;

	        if(_skyboxMaterial_copy != null){  DestroyImmediate(_skyboxMaterial_copy);  }
	        if(_skyboxMaterial_ui_copy !=null){  DestroyImmediate(_skyboxMaterial_ui_copy); }
	        if(_skyboxMat_copy_override!=null){  DestroyImmediate(_skyboxMat_copy_override); }

	        IconUI i = _currentIcon;//for readability.
	        if (i!=null && i._genData != null){
	            i._genData.Unsubscribe_from_textureUpdates( i.texture_guids, OnTextureUpdated );
	        }

	        IconUI.Act_OnSomeIconClicked -= OnSomeIconUI_selected;
	    }
	}
}//end namespace
