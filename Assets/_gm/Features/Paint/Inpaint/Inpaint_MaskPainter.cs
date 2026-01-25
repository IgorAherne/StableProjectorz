using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;

namespace spz {

	//allows us to drag mouse in viewport and "draw" a 2D screen-space mask.
	public class Inpaint_MaskPainter : MaskPainter{
	    public static Inpaint_MaskPainter instance { get; private set; } = null;

	    [Space(10)]
	    [SerializeField] Texture _colorlessMaskChecker_tex;
	    [Space(10)]
	    [SerializeField] Shader _brushShader_noCVTRaster; //without Conservative Raster. For older GPUs.
	    [SerializeField] ApplyBrushStroke_ToUvMask _applyBrushStroke_toUvMask;
	    [SerializeField] Shader _blitApplyEntireColorLayer_shader;
	    [Space(10)]
	    [SerializeField] Inpaint_ScreenMasker _inpaintScreenMasker;
	    [Space(10)]
	    [SerializeField] float _hoverFadeIn_speed = 7;//how soon brushed color becomes visible, when we hover mainViewport
	    [SerializeField] float _hoverFadeOut_speed = 0.5f;

	    float _prevStrength = 0;
	    RenderTexture _latestBrushStroke_ref;//doesn't belong to us, belongs to parent class

	    Material _blitApplyEntireColorLayer_mat;


	    public bool isPaintMaskEmpty { get; private set; } = true;
	    public RenderUdims _ObjectUV_brushedColorRGBA { get; private set; }
	    public static Action Act_OnPaintStrokeEnd { get; set; } = null;


	    public SoftInpaintingArgs GetArgs_for_SoftInpaint_GenRequest(){

	        if(!SD_WorkflowOptionsRibbon_UI.instance.isSoftInpaint){ return null; }

	        var entry = new SoftInpaintingArgsEntry{};//keep default values, they don't have much difference (Jul 2024)

	        var sft_args = new SoftInpaintingArgs(){
	            args = new SoftInpaintingArgsEntry[1]{ entry },
	        };
	        return sft_args;
	    }


	    // Takes all of the color that we've brushed, and applies into accumulation texture.
	    // This is typically done as the final step during projection-cams-renders (not during painting).
	    public void ApplyColorLayer_To_UV_Textures( RenderUdims ontoHere ){
        
	        bool isColorless  = WorkflowRibbon_UI.instance.currentMode() == WorkflowRibbon_CurrMode.Inpaint_NoColor;
	        bool willSendToSD = StableDiffusion_Hub.instance._finalPreparations_beforeGen;
	        if(isColorless && willSendToSD){ return; }//Don't blit, keep as is.
	        if(Save_MGR.instance._isSaving){ return; }//Never blit colors if saving. Or merging icons into one, because no ctrl+z.

	        RenderUdims.SetNumUdims( ontoHere, _blitApplyEntireColorLayer_mat );

	        // If we are still dragging mouse (painting), we must submit latest brush stroke texture as well.
	        // This is important, because color blending (lerp) can't be "baked in" at each frame.
	        // We have to keep applying it (without affecting original maks) until the mouse is released.
	        // We "bake it in" only when the mouse is released, once.
	        // Otherwise brush path becomes non-smooth, due to repetitive lerp() accross consecutive frames.
	        TextureTools_SPZ.SetKeyword_Material(_blitApplyEntireColorLayer_mat, "APPLY_LATEST_BRUSH_TOO", _isPainting);
	        _blitApplyEntireColorLayer_mat.SetTexture("_LatestBrushStroke", _latestBrushStroke_ref);
	        _blitApplyEntireColorLayer_mat.SetColor("_CurrBrushColor", SD_WorkflowOptionsRibbon_UI.instance.brushColor);
	        float sign = Mathf.Sign(_prevStrength);
	        _blitApplyEntireColorLayer_mat.SetFloat("_Sign", sign);
	        _blitApplyEntireColorLayer_mat.SetFloat("_MaxPossibleBrushStrength01", SD_WorkflowOptionsRibbon_UI.instance.maskBrushOpacity);

	        _blitApplyEntireColorLayer_mat.SetInteger("_isColorlessMask", isColorless?1:0);
	        if(isColorless){  _blitApplyEntireColorLayer_mat.SetTexture("_ColorlessCheckerTex", _colorlessMaskChecker_tex); }

	        _blitApplyEntireColorLayer_mat.SetFloat("_TotalOpacity01", 1);

	        TextureTools_SPZ.Blit( _ObjectUV_brushedColorRGBA.texArray,  ontoHere.texArray,  
	                              _blitApplyEntireColorLayer_mat );
	    }


	    // expensive! only use during manual baking/extraction of colors, not every frame
	    public List<Texture2D> ExtractColorLayer_as_UV_texture2D(out List<UDIM_Sector> udims_sectors_){
	        udims_sectors_ = _ObjectUV_brushedColorRGBA.udims_sectors.ToList();//ToList() makes a copy
	        List<Texture2D> textures = TextureTools_SPZ.TextureArray_to_Texture2DList(_ObjectUV_brushedColorRGBA.texArray );
	        return textures;
	    }


	    public RenderTexture ScreenMask_ContentRT_ref(bool withAntiEdge)
	        => _inpaintScreenMasker.ScreenMask_ContentRT_ref(withAntiEdge);


	    // Expensive, invoked when we need a texture to send to StableDiffusion.
	    // Returns two textures, without anti-edge (sent to stableDiffusion, to avoid showing it colors through the gaps).
	    // and with anti-edge (used internally in stableProjectorz during projections)
	    public void GetDisposable_ScreenMask( bool forceFullWhite, out Texture2D skipAntiEdge_, out Texture2D withAntiEdge_ ){
	        //force render the mask again, just in case:
	        _inpaintScreenMasker.RenderScreenMask_maybe(_ObjectUV_brushedColorRGBA, mustRender:true);

	        RenderTexture skipAntiEdgeRT = ScreenMask_ContentRT_ref(withAntiEdge: false);
	        RenderTexture antiEdgeRT     = ScreenMask_ContentRT_ref(withAntiEdge: true);
	        skipAntiEdge_ =  TextureTools_SPZ.R_to_RGBA_Texture2D( skipAntiEdgeRT,  forceAlpha1:true,  forceFullWhite:forceFullWhite );
	        withAntiEdge_ =  TextureTools_SPZ.R_to_RGBA_Texture2D( antiEdgeRT,  forceAlpha1:true,  forceFullWhite:forceFullWhite );
	    }


	    public override void ResetPaintMask(){
	        base.ResetPaintMask();
	        _ObjectUV_brushedColorRGBA?.ClearTheTextures(Color.clear);
	        isPaintMaskEmpty=true;
	    }


	    protected override void OnBucketFill_button(){
	        if(MainViewport_UI.instance.showing != MainViewport_UI.Showing.UsualView){ return; }
	        if(WorkflowRibbon_UI.instance.isMode_using_img2img() == false){ return; }
	        Color col = SD_WorkflowOptionsRibbon_UI.instance.brushColor;
	        OnBucketFill_orDelete_button( col, _ObjectUV_brushedColorRGBA.texArray,  visibilTex:null );
	        isPaintMaskEmpty = false;
	    }
	    protected override void OnDelete_button(){//different to ResetPaintMask(), might be only for some isolated mesh.
	        if(MainViewport_UI.instance.showing != MainViewport_UI.Showing.UsualView){ return; }
	        OnBucketFill_orDelete_button( Color.clear, _ObjectUV_brushedColorRGBA.texArray,  visibilTex:null );
	    }

	    protected override bool isAllowedToShow_BrushCursorNow()
	        => MainViewport_UI.instance.showing == MainViewport_UI.Showing.UsualView
	           && WorkflowRibbon_UI.instance.isMode_using_img2img();

	    protected override bool isAllowedToPaintNow( bool also_check_viewportHovered ){
	        bool isAllowed =  MainViewport_UI.instance?.showing == MainViewport_UI.Showing.UsualView;
	             isAllowed &= DimensionMode_MGR.instance?._dimensionMode == DimensionMode.dim_sd;
	             isAllowed &= WorkflowRibbon_UI.instance?.isMode_using_img2img() ?? false;
	             isAllowed &= MultiView_Ribbon_UI.instance?._isEditingMode ?? false;
	             isAllowed &= !SD_WorkflowOptionsRibbon_UI.instance?.IsEyeDropperMagnified ?? false;
	             isAllowed &= !ClickSelect_Meshes_MGR.instance?._isSelectMode?? false;
	             isAllowed &= !GlobalClickBlocker.isLocked();
	        if (also_check_viewportHovered){
	            isAllowed &= MainViewport_UI.instance?.isCursorHoveringMe()?? false;
	        }
	        return isAllowed;
	    }

	    public override Vector2 getViewportSize()
	        => MainViewport_UI.instance.mainViewportRect.rect.size;

	    public override Vector2 getViewportCursorPos01(bool forceMainViewport=false)
	        =>MainViewport_UI.instance.cursorMainViewportPos01;

	    protected override Vector3Int maskResolution(){
	        IReadOnlyList<UDIM_Sector> allUdims = ModelsHandler_3D.instance._allKnownUdims;
	        int numSlices = allUdims.Count;
	        return new Vector3Int( GenData_Masks.COLOR_BRUSH_RESOLUTION,  GenData_Masks.COLOR_BRUSH_RESOLUTION,  numSlices);
	    }

	    protected override float getBrushStrength(){//strength [0,1] --> [-1,1]}
	        var orib = SD_WorkflowOptionsRibbon_UI.instance;
	        return orib.maskBrushOpacity * (orib.isPositive?1:-1);
	    }


	    protected override void InitTextures( int width,  int height,  int numSlices, 
	                                          out RenderTexture prevBrushPath_,  out RenderTexture currBrushPath_){
        
	        prevBrushPath_ = TextureTools_SPZ.CreateTextureArray( new Vector2Int(width,height), GraphicsFormat.R8_UNorm, 
	                                                             FilterMode.Bilinear, numSlices, depthBits:0);

	        currBrushPath_ = TextureTools_SPZ.CreateTextureArray( new Vector2Int(width,height), GraphicsFormat.R8_UNorm, 
	                                                             FilterMode.Bilinear, numSlices, depthBits:0);
	        //MODIF ..maybe .Release() these when lowFPS is toggled? and prevent paiting until untoggled.
	        TextureTools_SPZ.ClearRenderTexture(prevBrushPath_, Color.black);
	        TextureTools_SPZ.ClearRenderTexture(currBrushPath_, Color.black);
	        _ObjectUV_brushedColorRGBA?.Dispose();
	        _ObjectUV_brushedColorRGBA =  new RenderUdims( UDIMs_Helper._allKnownUdims, new Vector2Int(width,height),
	                                                       GenData_Masks.colorBrushFormat,  GenData_Masks.masksFilter,
	                                                       Color.clear,  depthBits:0 );
	    }


	    protected override void OnRenderIntoCurrTex_please( RenderTexture prevBrushStroke_R8, RenderTexture currBrushStroke_R8,
	                                                        bool isFirstFrameOfStroke, float suggested_brushStrength ){
	        isPaintMaskEmpty = false;
	        //very important when painting with mouse instead of tablet (starts large)
	        if(isFirstFrameOfStroke){ _prevStrength = suggested_brushStrength; }
        
	        RenderUdims.SetNumUdims(_ObjectUV_brushedColorRGBA, _brushMaterial);

	        _brushMaterial.SetFloat("_ExtraVisibility", 1); //we don't have texture, so ensure it's full visibility.
	        _brushMaterial.SetTexture("_PrevBrushPathTex", prevBrushStroke_R8);
	        _brushMaterial.SetTexture("_BrushStamp", SD_WorkflowOptionsRibbon_UI.instance._brushHardnessTex); 
	        _brushMaterial.SetVector("_BrushStrength", new Vector4(_prevStrength,suggested_brushStrength,0,0));

	        //Apply material on the 3d meshes, and render, to alter the mask, painting it:
	        var selectedMeshes = ModelsHandler_3D.instance.selectedMeshes;
	        Objects_Renderer_MGR.instance.EquipMaterial_on_Specific( selectedMeshes, _brushMaterial );

	        UserCameras_MGR.instance._curr_viewCamera.RenderImmediate_Arr( renderIntoHere:currBrushStroke_R8,  ignore_nonSelected_meshes:true,
	                                                                       _brushMaterial,  useClearingColor:false,  Color.clear, dontFrustumCull:true);
	        _prevStrength = suggested_brushStrength;
	        _latestBrushStroke_ref = currBrushStroke_R8;
	    }


	    protected override void OnFinal_ApplyIncomingVals_intoMask( RenderTexture prevBrushStroke_R8, 
	                                                                RenderTexture currBrushStroke_R8 ){
	        // Only Apply (bake) the brush stroke into our RGB texture at the end.
	        // This is important, because color blending (lerp) can't be "baked in" at each frame.
	        // We have to keep applying it (without affecting original maks) until the mouse is released.
	        // We "bake it in" only when the mouse is released, once.
	        // Otherwise brush path becomes non-smooth, due to repetitive lerp() accross consecutive frames.
	        float sign =  Mathf.Sign(_prevStrength);
	        float maxStrength = SD_WorkflowOptionsRibbon_UI.instance.maskBrushOpacity;

	        _applyBrushStroke_toUvMask.Apply_into_ColorBrushTex( prevBrushStroke_R8, currBrushStroke_R8, sign,  maxStrength,  
	                                                              _ObjectUV_brushedColorRGBA );
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();
	        Act_OnPaintStrokeEnd?.Invoke();
	    }

    
	    protected override void Awake(){
	      #if UNITY_EDITOR
	        if(UnityEditor.EditorApplication.isPlaying==false){ return; }
	      #endif
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;

	        if (Pen.current != null  &&  Pen.current.deviceId != Pen.InvalidDeviceId){
	            Viewport_StatusText.instance.ShowStatusText($"Drawing Tablet '{Pen.current.displayName}' detected, "
	                                                        +$"will use pressure when brushing.", false, 5, progressVisibility:false );
	        }
	        _blitApplyEntireColorLayer_mat = new Material(_blitApplyEntireColorLayer_shader);

	        base.Awake();

	        if (SystemInfo.supportsConservativeRaster == false){
	            DestroyImmediate(base._brushMaterial); //secretly swap the parent's material with a more suitable one
	            base._brushMaterial = new Material(_brushShader_noCVTRaster);
	        }
	    }


	    protected override void OnDestroy(){
	        DestroyImmediate(_blitApplyEntireColorLayer_mat);
	        base.OnDestroy();
	    }

	}
}//end namespace
