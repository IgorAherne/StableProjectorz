using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace spz {

	// Allows us to drag mouse in the 2D background-viewport and "draw" a screen-space mask
	// that belongs to the currently selected background's GenData2D mask container.
	public class Background_Painter : MaskPainter{
	    public static Background_Painter instance { get; private set; } = null;

	    [SerializeField] Shader _applyFinalBrushStroke_add_shader;
	    [SerializeField] Shader _applyFinalBrushStroke_rmv_shader;
	    [SerializeField] RectTransform _bgRectTransf;
	    float _prevStrength = 0;
    
	    Material _applyFinalBrushStroke_add_mat;
	    Material _applyFinalBrushStroke_rmv_mat;
	    RenderTexture _latestCurrBrushStroke_R8 = null; //kept as null while not painting.

	    public static Action Act_OnPaintStrokeEnd { get; set; } = null;

	    public RectTransform getBackgroundRect() => _bgRectTransf;


	    // Helper to get the single UDIM where the background mask is stored
	    public RenderUdims current_BG_MaskRenderUdim(){
	        var bgIcon = ArtBG_IconsUI_List.instance?._mainSelectedIcon;
	        if(bgIcon == null){ return null; }

	        var genData = bgIcon._genData; 
	        if(genData == null){ return null; }

	        GenData_Masks masks = genData._masking_utils;
	        if(masks == null){ return null; }
	        // backgrounds typically only have one pov, so [0]:
	        if(masks._ObjectUV_brushedMaskR8.Count == 0){ return null; }
	        return masks._ObjectUV_brushedMaskR8[0]; 
	    }


	    public void ApplyCurrBrushStroke(Material mat){
	        mat.SetTexture("_Mask_CurrBrushStrokeTex", _latestCurrBrushStroke_R8);
	        mat.SetFloat("_Mask_CurrBrushStroke_Exists", _latestCurrBrushStroke_R8!=null?1:0);
	        mat.SetFloat("_Mask_CurrBrushStroke_Strength_m1_p1", _prevStrength);
	    }
    

	    public override Vector2 getViewportSize(){
	        return MainViewport_UI.instance.mainViewportRect.rect.size;
	    }

	    public override Vector2 getViewportCursorPos01(bool forceMainViewport = false){
	        // 1) Get actual pixel dimensions (or rect) of the viewport UI
	        Rect viewRect = MainViewport_UI.instance.innerViewportRect.rect;
	        //    For example:   viewRect.width, viewRect.height

	        // 2) Get the mask's resolution (or something that tells you the mask's width/height)
	        Vector3Int maskRes = maskResolution();
	        //    For example:   maskRes.x = maskWidth, maskRes.y = maskHeight

	        // 3) Calculate aspect ratios
	        float viewAspect = viewRect.width / (float)viewRect.height;     // e.g. 16/9 = ~1.7777
	        float maskAspect = maskRes.x / (float)maskRes.y;                // e.g. 1 (square) or 2 or 0.5, etc.

	        // 4) Current 01 position inside the viewport
	        //    i.e., pos = (0,0) means bottom-left corner of *viewport*, not the mask
	        Vector2 pos = MainViewport_UI.instance.cursorInnerViewportPos01;

	        // 5) We'll define a scale and offset so we can map the viewport's (0..1) coordinates 
	        //    into the mask's (0..1) coordinates, accounting for letterbox/pillarbox.

	        float offsetX = 0f;
	        float offsetY = 0f;
	        float scaleX = 1f;
	        float scaleY = 1f;

	        // If the aspect ratios match (within some small epsilon), no letterboxing/pillarboxing.
	        if (Mathf.Approximately(viewAspect, maskAspect)){
	            // scaleX = 1, scaleY = 1, offsetX = offsetY = 0
	        }
	        else if (viewAspect > maskAspect){
	            // The viewport is relatively wider than the mask.
	            // We'll match full widths => scaleX = 1, scaleY < 1
	            scaleX = 1f;
	            scaleY = maskAspect / viewAspect;
	            // This means we have empty space on top/bottom in mask space, so center it:
	            offsetY = (1f - scaleY) * 0.5f;
	        }else{
	            // The viewport is relatively narrower than the mask.
	            // We'll match full heights => scaleY = 1, scaleX < 1
	            scaleY = 1f;
	            scaleX = viewAspect / maskAspect;
	            // This means we have empty space on the sides, so center horizontally:
	            offsetX = (1f - scaleX) * 0.5f;
	        }
	        // 6) Finally, compute the mask-space UV:
	        //    - pos.x and pos.y are 01 in the viewport
	        //    - we rescale and offset so they map to the masks 01 space
	        //    - offset accounts for letterbox/pillarbox
	        //    - scale shrinks the viewport area within the mask
	        Vector2 brushUVinMask = new Vector2(
	            offsetX + pos.x * scaleX,
	            offsetY + pos.y * scaleY
	        );
	        return brushUVinMask;
	    }

	    // backgrounds have the same resolution as their mask.
	    protected override Vector3Int maskResolution(){
	        RenderUdims maskUDIM = current_BG_MaskRenderUdim();
	        if(maskUDIM == null){ return new Vector3Int(1024, 1024,1); }// fallback
	        return new Vector3Int( maskUDIM.width, maskUDIM.height, 1 );
	    }

	    protected override float getBrushStrength(){//strength [0,1] --> [-1,1]}
	        var orib = Gen3D_WorkflowOptionsRibbon_UI.instance;
	        float strength = 1.0f; //Always 100% to avoid semi-transparency. (bad for 3d generations)
	        return strength * (orib._brush_isPositive?1:-1);
	    }

    
	    // Show the brush only if dimension mode = dim_bg
	    protected override bool isAllowedToShow_BrushCursorNow(){
	        return DimensionMode_MGR.instance._dimensionMode == DimensionMode.dim_gen_3d
	               && MainViewport_UI.instance.showing == MainViewport_UI.Showing.UsualView
	               && Gen3D_WorkflowOptionsRibbon_UI.instance._is_can_adjust_BG;
	    }

	    // Paint only if dimension mode = dim_bg
	    protected override bool isAllowedToPaintNow(bool also_check_viewportHovered){
	        bool isAllowed = DimensionMode_MGR.instance?._dimensionMode == DimensionMode.dim_gen_3d;
	        isAllowed     &= MainViewport_UI.instance?.showing == MainViewport_UI.Showing.UsualView;
	        isAllowed     &= Gen3D_WorkflowOptionsRibbon_UI.instance?._is_can_adjust_BG?? false;
	        isAllowed     &= MultiView_Ribbon_UI.instance?._isEditingMode?? false; 
	        isAllowed     &= !SD_WorkflowOptionsRibbon_UI.instance?.IsEyeDropperMagnified?? false;
	        isAllowed     &= !ClickSelect_Meshes_MGR.instance?._isSelectMode?? false;
	        isAllowed     &= !GlobalClickBlocker.isLocked();
	        if (also_check_viewportHovered){
	            isAllowed &= MainViewport_UI.instance?.isCursorHoveringMe()?? false;
	        }
	        return isAllowed;
	    }


	    // We only allocate new RT if the user picks a new background with a different size
	    // (the base class calls this whenever we begin painting).
	    protected override void InitTextures(int width, int height, int numSlices,
	                                        out RenderTexture prevBrushPath_, out RenderTexture currBrushPath_){
	        // because the mask of the backgrounds are stored as texture-array,
	        // we will create texture-array for prev and curr brush-path. 
	        // Backgrounds always use only 1 udim, so we keep numSlices as 1:
	        var widthHeight = new Vector2Int(width, height);
	        prevBrushPath_ = TextureTools_SPZ.CreateTextureArray(widthHeight, GraphicsFormat.R8_UNorm, 
	                                                            FilterMode.Bilinear, numSlices:1, depthBits:0);
	        currBrushPath_ = TextureTools_SPZ.CreateTextureArray(widthHeight, GraphicsFormat.R8_UNorm, 
	                                                            FilterMode.Bilinear, numSlices:1, depthBits:0);
	        TextureTools_SPZ.ClearRenderTexture(prevBrushPath_, Color.black);
	        TextureTools_SPZ.ClearRenderTexture(currBrushPath_, Color.black);
	    }


	    // we need to scale the brush additionally. Usually it's based on the % of main viewport.
	    // But we need to increase it.
	    // It will depend on how taller the background is, beyond the main viewport.
	    // Because ultimatelly it's the background which receives the painting.
	    protected override float getBrushExtraScaling_due_viewport(){
	        Rect rectOutter = MainViewport_UI.instance.mainViewportRect.rect;
	        float aspect_outter = rectOutter.width / (float)rectOutter.height;
	        RectTransform bgRect = getBackgroundRect();
	        return rectOutter.height/bgRect.rect.height;
	    }

	    // Each frame we paint the "current stroke" into the `_currBrushPath_R8`.
	    protected override void OnRenderIntoCurrTex_please(RenderTexture prevBrushStroke_R8, RenderTexture currBrushStroke_R8,
	                                                       bool isFirstFrameOfStroke, float suggested_brushStrength){
	        // force opacity either -1 or 1, to avoid semi-transparency of the background.
	        // Semi-transparency can cause issues with 3D generation.
	        suggested_brushStrength = Math.Sign(suggested_brushStrength);

	        if (isFirstFrameOfStroke){ _prevStrength = suggested_brushStrength; }

	        RenderUdims masks = current_BG_MaskRenderUdim();
	        if(masks == null){ return; }

	        Texture2D hardTex   = BrushRibbon_UI.instance.brushHardnessUI.readSpecificHardnessTex(2);//aways hard.
	        float brushStrength = Mathf.Sign(suggested_brushStrength);//always -1 or 1, to avoid semi-transparent alphas.

	        _brushMaterial.SetTexture("_PrevBrushPathTex", prevBrushStroke_R8);
	        _brushMaterial.SetTexture("_BrushStamp", hardTex);
	        _brushMaterial.SetVector("_BrushStrength", new Vector4(brushStrength,brushStrength,0,0) );
	        Rect viewRect = MainViewport_UI.instance.innerViewportRect.rect;

	        Vector2Int mask_wh = masks.widthHeight;
	        float mask_aspect = mask_wh.x / (float)mask_wh.y;
	        _brushMaterial.SetFloat( "_ScreenAspectRatio", mask_aspect);

	        // Full-screen pass that reads the previous stroke and merges in new dab:
	        Graphics.Blit(prevBrushStroke_R8, currBrushStroke_R8, _brushMaterial);

	        _prevStrength = suggested_brushStrength;
	        _latestCurrBrushStroke_R8 = currBrushStroke_R8;
	    }
    

	    // Once user lifts the mouse, we finalize the stroke into the GenData2D mask (the single UDIM).
	    protected override void OnFinal_ApplyIncomingVals_intoMask(RenderTexture prevBrushStroke_R8,
	                                                               RenderTexture currBrushStroke_R8){
	        RenderUdims renderUdims = current_BG_MaskRenderUdim();
	        if(renderUdims == null){ return; }

	        // force opacity either -1 or 1, to avoid semi-transparency of the background.
	        // Semi-transparency can cause issues with 3D generation.
	        float sign        = Mathf.Sign(_prevStrength);
	        float maxStrength = Mathf.Abs(sign);

	        Material mat = sign>0? _applyFinalBrushStroke_add_mat : _applyFinalBrushStroke_rmv_mat;
	        mat.SetTexture("_CurrBrushStroke", currBrushStroke_R8);
	        mat.SetFloat("_MaxStrength", maxStrength);
	        TextureTools_SPZ.Blit(null, renderUdims.texArray, mat);

	        _prevStrength = 0;
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();
	        Act_OnPaintStrokeEnd?.Invoke();
	    }


	    // Bucket-fill entire mask with white or black.
	    protected override void OnBucketFill_button(){
	        if(DimensionMode_MGR.instance._dimensionMode != DimensionMode.dim_gen_3d){ return; }
	        RenderUdims maskUDIM = current_BG_MaskRenderUdim();
	        if(maskUDIM==null){ return; }
	        // Fill with 1 or 0:
	        Color fillC =  SD_WorkflowOptionsRibbon_UI.instance.isPositive? Color.white : Color.black;
	        TextureTools_SPZ.ClearRenderTexture(maskUDIM.texArray, fillC);
	    }

	    protected override void OnDelete_button(){
	        if(DimensionMode_MGR.instance._dimensionMode != DimensionMode.dim_gen_3d){ return; }
	        RenderUdims maskUDIM = current_BG_MaskRenderUdim();
	        if(maskUDIM == null){ return; }
	        TextureTools_SPZ.ClearRenderTexture(maskUDIM.texArray, Color.black);
	    }


	    protected override void Awake(){
	      #if UNITY_EDITOR
	        if(UnityEditor.EditorApplication.isPlaying == false) return;
	      #endif
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;

	        _applyFinalBrushStroke_add_mat = new Material(_applyFinalBrushStroke_add_shader);
	        _applyFinalBrushStroke_rmv_mat = new Material(_applyFinalBrushStroke_rmv_shader);
	        base.Awake();
	    }

	    protected override void OnDestroy(){
	        base.OnDestroy();
	        if (this == instance){ instance = null; }
	        DestroyImmediate(_applyFinalBrushStroke_add_mat);
	        DestroyImmediate(_applyFinalBrushStroke_rmv_mat);
	    }

	}
}//end namespace
