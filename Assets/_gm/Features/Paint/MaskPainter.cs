using UnityEngine;
using UnityEngine.InputSystem;

namespace spz {

	public abstract class MaskPainter : MonoBehaviour{
	    [Space(10)]
	    [SerializeField] float _brushResizeDrag_speed = 5; //for shift+rmb dragging, to resize the cursor in viewport
	    [SerializeField] Shader _brushShader;
	    [SerializeField] AnimationCurve _brushSizeScale;
	    [SerializeField] AnimationCurve _pressureBrushSizeCurve;//tablet pressure
	    [SerializeField] AnimationCurve _pressureOpacitySizeCurve;
	    [Space(10)]
	    [SerializeField] Shader _fill_UV_chunks_shader;//for using bucket-fill tool.

	    protected Material _brushMaterial;//for modifying mask.
	    Material _fillUVchunks_mat;//when using bucket fill tool.

	    //a cheap texture for "detecting" + remembering the current stroke of brush.
	    //Cleared to black as soon as left mouse button is released.
	    RenderTexture _prevBrushPath_R8;//from previous frame
	    RenderTexture _currBrushPath_R8;//from current frame.

	    Vector2 _prevPaintPosition;//only updated during painting
	    float _lastBrushSize;
	    bool _isFirstFrameOfStroke = true;
    

	    public float visibleBrushSize(){
	        if(_isPainting){ return _lastBrushSize; }
	        return _brushSizeScale.Evaluate(SD_WorkflowOptionsRibbon_UI.instance.brushSize01);
	    }

	    public bool _isPainting { get; private set; } = false;//is mouse currently pressed and are we dragging (painting).


	    public abstract Vector2 getViewportCursorPos01(bool forceMainViewport=false);
	    public abstract Vector2 getViewportSize();

	    // scale the brush additionally. It's based on the % of main viewport:
	    protected virtual float getBrushExtraScaling_due_viewport() => 1.0f;

	    protected abstract Vector3Int maskResolution();
	    protected abstract bool isAllowedToShow_BrushCursorNow();

	    // We always pass false, but KEEP THE ARGUMENT FOR CLARITY. Child classes should never check viewport hovering.
	    // It's only this parent class who checks if the viewport is hovered, ONLY checks it during on mouse down.
	    // - might want to keep painting even if cursor went outside the viewport momentarily.
	    // Useful when adjusting the backgrounds near the viewport border.
	    protected abstract bool isAllowedToPaintNow(bool also_check_viewportHovered);

    
	    public virtual void ResetPaintMask(){
	        TextureTools_SPZ.ClearRenderTexture(_prevBrushPath_R8, Color.black); //texture might be textureArray!
	        TextureTools_SPZ.ClearRenderTexture(_currBrushPath_R8, Color.black);
	    }


	    public void SetCurrentBrushStroke(Material here, string shaderPropertyName){
	        Texture tex = (_isPainting==false || _currBrushPath_R8==null)?  Texture2D.blackTexture : _currBrushPath_R8;
	        here.SetTexture(shaderPropertyName, tex);
	    }


	    void OnUpdate(){
	        if(isAllowedToShow_BrushCursorNow()){
	            CursorPreviewUI_Reposition();
	        }
	        if(isAllowedToPaintNow(also_check_viewportHovered:false)  &&  MainViewport_UI.instance.isCursorHoveringMe() ){ 
	            OnPointerDown_maybe();
	        }
	        OnDrag_maybe();
	        OnPointerUp_maybe();

	        OnUpdateChildren();
        
	        if(!_isPainting){ return; }
	        Graphics.Blit(_currBrushPath_R8, _prevBrushPath_R8);
	    }

	    protected virtual void OnUpdateChildren(){ }


	    // updates position of the UI element ("outline" of the brush)
	    // This helps the user to see where the brush is about to paint.
	    void CursorPreviewUI_Reposition(){
	        var orib = SD_WorkflowOptionsRibbon_UI.instance;
	        if(KeyMousePenInput.isKey_Shift_pressed()){
	            Vector2 delta =  KeyMousePenInput.delta_while_RMBpressed( normalizeByScreenDiagonal:true );
	            float predominantAxisValue   = Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? delta.x : delta.y;
	            float mouseMovementMagnitude = Mathf.Abs(predominantAxisValue);
	            float resizeDir   = predominantAxisValue >= 0 ? 1 : -1;
	            float brushResize = mouseMovementMagnitude * resizeDir * _brushResizeDrag_speed;
	            float sliderVal   = Mathf.Clamp(orib.brushSize01 + brushResize, 0.001f, 1);
	            orib.SetBrushSize(sliderVal);
	        }
	        Cursor_UI.instance.SetCursorThickness(orib.brushSize01);
	        Cursor_UI.instance.PositionCursor( _brushSizeScale.Evaluate(orib.brushSize01) );
	    }


	    void OnPointerDown_maybe(){
	        if(!KeyMousePenInput.isLMBpressedThisFrame()){ return; }
	        if(isDoingSomethingElse()){ return; }
	        _isPainting = true;

	        Vector3Int textureRes = maskResolution();
	        initTextures_Maybe(textureRes.x, textureRes.y, textureRes.z);
        
	        float brushSize    = _brushSizeScale.Evaluate(SD_WorkflowOptionsRibbon_UI.instance.brushSize01 );
	        float brushOpacity = SD_WorkflowOptionsRibbon_UI.instance.maskBrushOpacity;

	        AffectByPressure(ref brushSize, ref brushOpacity);
	        brushSize = Mathf.Max(0.001f, brushSize);

	        // NOTICE: don't clear if User holds Shift, to continue drawing a straight line.
	        // Had we cleared here, there would be spots on the joints (between the old and the new brush stroke).
	        // So we will only clear in PaintOnTexture().
	        Vector2 pointInViewport01 = getViewportCursorPos01();

	        bool shift = KeyMousePenInput.isKey_Shift_pressed();
	             shift = false;//DISABLE FOR NOW (Jan 2025) - doesn't look good, and on the other hand, might mess up someone's paiting.
	        _isFirstFrameOfStroke = !shift;//if shift, don't reset position (like in photoshop)

	        if (_isFirstFrameOfStroke){
	            _prevPaintPosition = pointInViewport01;
	            _lastBrushSize = brushSize;
	            TextureTools_SPZ.ClearRenderTexture(_prevBrushPath_R8, Color.black);
	            TextureTools_SPZ.ClearRenderTexture(_currBrushPath_R8, Color.black);//Might be textureArray!
	        }
	    }


	    void AffectByPressure(ref float size_, ref float opacity_){
       
	        if (KeyMousePenInput.isLMBpressed(checkOnlyPen:true) == false){ return; }//tablet not used.

	        float penPressure01 = Pen.current.pressure.ReadValue();

	        switch (SD_WorkflowOptionsRibbon_UI.instance.tabletPressureMode){
	            case TabletPressureMode.AffectSize: 
	                size_ *= _pressureBrushSizeCurve.Evaluate(penPressure01);
	                if(KeyMousePenInput.isLMBreleasedThisFrame()){ size_ = 0; }
	                break;

	            case TabletPressureMode.AffectOpacity: 
	                opacity_ *= _pressureOpacitySizeCurve.Evaluate(penPressure01);
	                if(KeyMousePenInput.isLMBreleasedThisFrame()){ opacity_ = 0; }
	                break;

	            case TabletPressureMode.AffectBoth: 
	                size_ *= _pressureBrushSizeCurve.Evaluate(penPressure01);
	                opacity_ *= _pressureOpacitySizeCurve.Evaluate(penPressure01);
	                if(KeyMousePenInput.isLMBreleasedThisFrame()){ size_=0;  opacity_ = 0; }
	                break;

	            case TabletPressureMode.AffectNone:
	            default:
	                break;
	        }

	    }


	    void OnPointerUp_maybe(){
	        if(!KeyMousePenInput.isLMBreleasedThisFrame()){ return; }
	        if (!_isPainting){ return; }//possibly clicked somewhere else in StableProjectorz, etc.

	        if(_currBrushPath_R8 != null){ 
	            //To finalize the brush stroke, apply the brush stroke to the mask:
	            OnFinal_ApplyIncomingVals_intoMask(_prevBrushPath_R8, _currBrushPath_R8);
	        }
	        _isPainting = false;
	        _isFirstFrameOfStroke = true;//Reset when the brush stroke ends
	    }


	    void OnDrag_maybe(){
	        if(!KeyMousePenInput.isLMBpressed()){ return; }
	        if(!_isPainting){ return; }
	        if(isDoingSomethingElse()){ return; }
	        PaintOnTexture();
	    }

	    bool isDoingSomethingElse(){
	        if (KeyMousePenInput.isKey_alt_pressed()){ return true; }//maybe orbiting
	        if (KeyMousePenInput.isKey_CtrlOrCommand_pressed()){ return true; }//maybe zooming
	        if (Images_ImportHelper.instance.isImporting){ return true; }//prevent start paint when actually clicking some file.
	        // DON't CHECK IS HOVERING VIEWPORT - might want to keep painting
	        // even if cursor went outside the viewport momentarily.
	        // Useful when adjusting the backgrounds near the viewport border.

	        bool is_img2img        = WorkflowRibbon_UI.instance.isMode_using_img2img();
	        bool isProjectionsMask = WorkflowRibbon_UI.instance.currentMode() == WorkflowRibbon_CurrMode.ProjectionsMasking;

	        bool correctMode  = (this as Inpaint_MaskPainter) != null  &&  is_img2img;
	             correctMode |= (this as Projections_MaskPainter)!=null  &&  isProjectionsMask;
	             correctMode |= (this as Background_Painter)!=null;

	             correctMode &= MainViewport_UI.instance.showing == MainViewport_UI.Showing.UsualView;

	        return !correctMode;
	    }


	    protected abstract float getBrushStrength();

	    void PaintOnTexture(){
	        var orib = SD_WorkflowOptionsRibbon_UI.instance;

	        float brushSize =  _brushSizeScale.Evaluate(orib.brushSize01);
	        float suggested_brushOpacity = getBrushStrength();
        
	        AffectByPressure(ref brushSize, ref suggested_brushOpacity);

	        Vector2 pointInViewport01 = getViewportCursorPos01();
        
	        var brushSizeVec =  new Vector4(_lastBrushSize,brushSize,0,0) * getBrushExtraScaling_due_viewport();
	        if (_isFirstFrameOfStroke){  brushSizeVec.z = 1.0f;  }
        
	        _brushMaterial.SetVector("_PrevNewBrushScreenCoord", new Vector4(_prevPaintPosition.x, _prevPaintPosition.y, pointInViewport01.x, pointInViewport01.y)); 
	        _brushMaterial.SetVector("_BrushSize_andFirstFrameFlag", brushSizeVec );
	        _brushMaterial.SetFloat("_ScreenAspectRatio", getViewportSize().x/getViewportSize().y);
        
	        OnRenderIntoCurrTex_please( _prevBrushPath_R8, _currBrushPath_R8, _isFirstFrameOfStroke, suggested_brushOpacity);

	        _prevPaintPosition = pointInViewport01;
	        _lastBrushSize = brushSize;
	        _isFirstFrameOfStroke = false;
	    }


	    protected abstract void OnRenderIntoCurrTex_please( RenderTexture prevBrushStroke_R8,  RenderTexture currBrushStroke_R8, 
	                                                        bool isFirstFrameOfStroke,  float suggested_brushStrength );

    
	    //To finalize the brush stroke, apply the brush stroke to the mask:
	    protected abstract void OnFinal_ApplyIncomingVals_intoMask( RenderTexture prevBrushStroke_R8,  RenderTexture currBrushStroke_R8 );


	    bool initTextures_Maybe( int width, int height, int numSlices ){
	        bool all_ok =  _currBrushPath_R8 != null 
	                       && _currBrushPath_R8.width==width  &&  _currBrushPath_R8.height==height
	                       && _currBrushPath_R8.volumeDepth==numSlices;
	        if(all_ok){ return false; }
	        TextureTools_SPZ.Dispose_RT(ref _prevBrushPath_R8, isTemporary:false);
	        TextureTools_SPZ.Dispose_RT(ref _currBrushPath_R8, isTemporary:false);
	        InitTextures(width, height, numSlices, out _prevBrushPath_R8, out _currBrushPath_R8);
	        return true;
	    }

	    protected abstract void InitTextures( int width,  int height,  int numSlices, 
	                                          out RenderTexture prevBrushPath_,  out RenderTexture currBrushPath_);

	    protected virtual void On_3dModel_Imported(GameObject go){
	        Vector3Int textureRes = maskResolution();
	        bool did_init = initTextures_Maybe(textureRes.x, textureRes.y, textureRes.z);
	        if(!did_init){ ResetPaintMask(); }
	    }


	    protected abstract void OnBucketFill_button();
	    protected abstract void OnDelete_button();//different to ResetPaintMask(), might be only for some isolated mesh.

	    //fill, but only uv chunks of currently selected (isolated) meshes.
	    protected void OnBucketFill_orDelete_button( Color fillColor, RenderTexture dest, RenderTexture visibilTex=null){
	        _fillUVchunks_mat.SetColor("_COL_UVCH_Color", fillColor);
	        _fillUVchunks_mat.SetTexture("_ProjVisibility", visibilTex);
	        TextureTools_SPZ.SetKeyword_Material(_fillUVchunks_mat, "USE_VISIBIL_TEX", visibilTex!=null);
	        TextureTools_SPZ.SetKeyword_Material(_fillUVchunks_mat, "VERTEX_COLORS", false);

	        RenderUdims.SetNumUdims(UDIMs_Helper._allSelectedUdims, _fillUVchunks_mat);

	        //render into temp, dilate it (expand borders), and paste into dest:
	        RenderTexture destTemp = new RenderTexture(dest.descriptor); 
	        TextureTools_SPZ.ClearRenderTexture(destTemp, Color.clear);

	        // NOT using clear color;  Ignore non-selected.
	        // NOT using frustum cull: even if camera is looking at the object, remember that we are going to render into UVs.
	        // This would likely cause the camera to ignore the object.
	        UserCameras_MGR.instance._curr_viewCamera.RenderImmediate_Arr( destTemp,  ignore_nonSelected_meshes:true, 
	                                                                       _fillUVchunks_mat,  useClearingColor:false,//NOT clearing.
	                                                                       Color.clear,  dontFrustumCull:true );
	        var dilRule = TextureTools_SPZ.GetChannelCount(dest)==4? DilateByChannel.A 
	                                                              : DilateByChannel.R;
	        // ONLY DILATE BY 1 TEXEL.
	        // 2 is already too much, it would creep through seams of nearby uv islands
	        // and be on various objects in Catacombs mesh (Oct 2024)
	        var dilArg  = new DilationArg( destTemp,  numberOfTexelsExpand:1, 
	                                       dilRule,  null,  isRunInstantly:true );
	        TextureDilation_MGR.instance.Dillate(dilArg);

	        TextureTools_SPZ.Blit(destTemp, dest);
	        DestroyImmediate(destTemp);

	        Objects_Renderer_MGR.instance.ReRenderAll_soon();
	    }


	    protected virtual void Update(){}


	    protected virtual void Awake(){
	        _brushMaterial    = new Material(_brushShader);
	        _fillUVchunks_mat = new Material(_fill_UV_chunks_shader);
	        ModelsHandler_3D.Act_onImported += On_3dModel_Imported;
        
	        BrushRibbon_UI_BucketFill._Act_onClicked += OnBucketFill_button;
	        BrushRibbon_UI_DeleteButton.onClicked    += OnDelete_button;
	    }

	    protected virtual void Start(){
	        Update_callbacks_MGR.brushing += OnUpdate;
	    }


	    protected virtual void OnDestroy(){
	        DestroyImmediate(_brushMaterial);
	        DestroyImmediate(_fillUVchunks_mat);
	        TextureTools_SPZ.Dispose_RT(ref _prevBrushPath_R8, isTemporary:false);
	        TextureTools_SPZ.Dispose_RT(ref _currBrushPath_R8, isTemporary:false);

	        Update_callbacks_MGR.brushing -= OnUpdate;
	        ModelsHandler_3D.Act_onImported -= On_3dModel_Imported;
	    }
	}
}//end namespace
