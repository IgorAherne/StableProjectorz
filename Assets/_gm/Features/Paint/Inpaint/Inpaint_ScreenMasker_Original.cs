using UnityEngine;

namespace spz {

	// Helper class for the InpaintPainter.
	// Able to visualize the painted colormask, like the camera sees it.
	public class Inpaint_ScreenMasker_Original : MonoBehaviour{

	    [SerializeField] Shader _uvColorMask_to_screen_shader; //masks all inpaint-brushed areas.
	    [SerializeField] Shader _uvWhereEmpty_to_screen_shader;//masks all empty areas (those without projections)
	    [SerializeField] Shader _Blit_RemoveEdges_ScreenMask_shader;
	    [Space(10)]
	    [SerializeField] Depth_Contrast_Helper _depthContrastHelper;
	    [SerializeField] ScreenDepth_EdgesDetector _edgesDetector;
	    [SerializeField] PreventBlur_if_DistanceFromMask _preventBlur_ifDistanceMask;

	    Material _uvWhereEmpty_to_screen_mat;
	    Material _uvColorMask_to_screen_mat;
	    Material _Blit_RemoveEdges_ScreenMask_mat;


	    //helper buffer, stores mask+dilation. But without any blurs etc.
	    RenderTexture _screenMaskRT_fixedRes_noBlur = null;

	    //same as _screenMaskRT_fixedRes, but never with anti-edge applied.
	    public RenderTexture _screenMaskRT_skipAntiEdge{ get; private set; } = null;

	    //viewport texture whose greatest side is INPAINT_MASK_MAX_PX_SIZE pixels.
	    //this small size allows to do wide blur efficiently, with a fixed number of blur iterations.
	    public RenderTexture _screenMaskRT_fixedRes_antiEdge { get; private set; } = null;


	    public static readonly int INPAINT_MASK_MAX_PX_SIZE = 512;


	    public enum MaskingMode{ MaskWhereBrushed, MaskFullSilhouette, MaskFull_and_AntiEdge, MaskWhereEmpty, }

	    public class Args_MakeScreenMask{
	        public MaskingMode maskingMode;
	        public bool dilationIsExpand = true;
	        public int dilationNumTexels = 1; // 0 would be too harsh, one allows for a tiny buffer aura, resulting in better details overall.
	                                          // If you use blur, your value will be ignored and will be recalculated to work with the blur.
	                                          // But the recalculated value will NEVER be less than your dilation value.
	        //for entire mask (not edges)
	        public bool applyBlur_entireMask = false;
	        public float entireMaskBlurStep01 = 0.5f;

	        //If true, we'll detect borders of the silhuette and will SUBTRACT those from the mask.
	        //Helps to avoid masking slanted side-surfaces, thus avoiding possible projection-stretches in the future.
	        public bool detectEdges = false;
	        public float edgeThick01 = 0.5f;
	        public float edgeBlur01 = 0.5f; 
	        public float edgeThresh01 = 0.5f;
	    }


	    class HelperArg{
	        public RenderTexture depthLinear_ref { get; private set; } = null;
	        public RenderTexture depthNonLinear_contrast_ref { get; private set; } = null;//non-linear depth, which was improved byContrast
	        public RenderTexture mask {  get; private set; } = null;
	        public RenderTexture edges { get; private set; } = null;
	        public RenderTexture edgesBuffer { get; private set; } = null;//helper texture during edge-detection.
	        public bool canDetectEdges => edges != null;
	        public float edgeThresh01 { get; private set; }
	        public float edgeThick01{ get; private set; }
	        public float edgeBlur01 { get; private set; }

	        public void Init( Args_MakeScreenMask arg, RenderTexture screenMask_fixedRes ){
	            //we always have this texture, because we'll output into it even if not doing edgeDetect:
	            var descriptor = screenMask_fixedRes.descriptor;//A copy of the struct. Ensure it's single chanel, to hold dot product:
	                descriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8_SNorm;
	            mask = RenderTexture.GetTemporary( descriptor );
	            mask.enableRandomWrite = true;

	            depthLinear_ref             = UserCameras_MGR.instance.camTextures._SD_depthCam_RT_R32_linear;
	            depthNonLinear_contrast_ref = UserCameras_MGR.instance.camTextures._SD_depthCam_RT_R32_contrast;

	            if (!arg.detectEdges){ return; }
	            edges       = RenderTexture.GetTemporary(depthNonLinear_contrast_ref.width, depthNonLinear_contrast_ref.height, depthBuffer: 0, RenderTextureFormat.R16);
	            edgesBuffer = RenderTexture.GetTemporary(depthNonLinear_contrast_ref.width, depthNonLinear_contrast_ref.height, depthBuffer: 0, RenderTextureFormat.R16);
	            edges.enableRandomWrite = true;
	            edgesBuffer.enableRandomWrite = true;
	            edgeThresh01 = arg.edgeThresh01;
	            edgeThick01  = arg.edgeThick01;
	            edgeBlur01   = arg.edgeBlur01;
	        }
	        public void Dispose(){
	            mask?.Release();  
	            mask = null; 
	            edges?.Release();  edges = null;
	            edgesBuffer?.Release(); edgesBuffer = null;
	            depthLinear_ref = null;//depth doesn't belong to us, don't release just forget.
	            depthNonLinear_contrast_ref = null;
	        }
	    }


	    // Visualize the mask from the camera's vision.
	    // The result is the silhuette, of the inpaint-color-mask that was applied to objects.
	    public void RenderScreenMask( RenderUdims objectUV_brushedColorRGBA ){
	        Args_MakeScreenMask arg = makeMaskArg();
	        Alloc_ScreenMaskRT_maybe();

	        var helper = new HelperArg();
	        helper.Init(arg, _screenMaskRT_fixedRes_antiEdge);
	        DetectEdges_maybe(helper);

	        Equip_Obj_Material(arg, objectUV_brushedColorRGBA);
	        Render_3D_into_ScreenMask( helper );

	        Shrink_into_NoBlurMask( helper );
	        //before blur. Generate texture that will help us search neighbor-texel depths later:
	        _preventBlur_ifDistanceMask.CalcDownscaledDepths_of_Mask( _screenMaskRT_fixedRes_noBlur, helper.depthLinear_ref );
	        Expand_the_NoBlurMask();

	        int blurNumIters =  arg.applyBlur_entireMask?  4 : 0;
	        int blurKernelHalfSize = 12;
	        DilateTheWholeMask(maskToDilate:helper.mask,  arg, blurNumIters, blurKernelHalfSize);
	        BlurTheWholeMask_maybe(helper, blurNumIters, blurKernelHalfSize, arg.entireMaskBlurStep01);

	        //remember mask, before we do anti-blur and anti-edge.
	        //We can send this mask to SD, but will use anti-edged
	        //mask in StableProjectorz, when applying projections etc.
	        TextureTools_SPZ.Blit(helper.mask, _screenMaskRT_skipAntiEdge);

	        AntiEdge_and_FinalResults(arg, helper);
	        Final_Dilate_and_Erode();

	        helper.Dispose();
	    }


	    void Final_Dilate_and_Erode(){
	        // the Skip-Anti-Edge is ALWAYS 1 pixel wider than the 3D object. Helps to cover any black empty areas.
	        // the Anit-Edge is ALWAYS 1 pixel tighter than the 3D object. 
	        var dilateArg = new DilationArg(_screenMaskRT_skipAntiEdge, 1, DilateByChannel.R, null, isRunInstantly:true);
	        var erodeArg  = new DilationArg(_screenMaskRT_fixedRes_antiEdge, 1, DilateByChannel.R, null, isRunInstantly:true);
	        erodeArg.isExpand = false;
	        TextureDilation_MGR.instance.Dillate(dilateArg);
	        TextureDilation_MGR.instance.Dillate(erodeArg);
	    }


	    Args_MakeScreenMask makeMaskArg(){
	        var optRib   = SD_WorkflowOptionsRibbon_UI.instance;
	        var toolsRib = WorkflowRibbon_UI.instance;
	        bool hasBrushedMask = Inpaint_MaskPainter.instance.isPaintMaskEmpty==false;
	        bool hasBackground  = ArtBG_IconsUI_List.instance.hasBackground(considerGradientColors:true);
	        bool isMode_Img2Img  = toolsRib.isMode_using_img2img();

	        var maskMakeArgs = new Args_MakeScreenMask();


	        bool isWhereEmpty    =  toolsRib.currentMode() == WorkflowRibbon_CurrMode.WhereEmpty;

	        bool isFullSilhuette =   (hasBackground && !hasBrushedMask)
	                               || (hasBackground && !isMode_Img2Img);

	        bool isFull_andAntiEdge = toolsRib.currentMode()==WorkflowRibbon_CurrMode.TotalObject;

	        maskMakeArgs.edgeThick01 = 0.24f;//same as in 'Inpaint_ScreenMasker_EmptyNothing.cs'

	        if (isWhereEmpty){
	            maskMakeArgs.maskingMode = MaskingMode.MaskWhereEmpty;
	            maskMakeArgs.dilationIsExpand  = true;
	            maskMakeArgs.dilationNumTexels = 0; //DO NOT DILATE. We'll dilate  in Final_Dilate_and_Erode()

	            maskMakeArgs.applyBlur_entireMask = true;
	            maskMakeArgs.entireMaskBlurStep01 = optRib.maskBlur_StepLength01;
	        }
	        else if (isFull_andAntiEdge){
	            maskMakeArgs.maskingMode = MaskingMode.MaskFull_and_AntiEdge;
	            maskMakeArgs.dilationNumTexels = 0; //DO NOT DILATE. We'll dilate  in Final_Dilate_and_Erode()

	            maskMakeArgs.applyBlur_entireMask = false;

	            maskMakeArgs.detectEdges = optRib.edgeThresh < 0.9998f  &&  optRib.maskBlur_StepLength01 > 0.001f;
	            maskMakeArgs.edgeBlur01  = optRib.maskBlur_StepLength01;
	            maskMakeArgs.edgeThick01 = optRib.maskBlur_StepLength01; //using blur for thickness, because other slider is for EdgeThresh (sensitivity)

	            maskMakeArgs.edgeThresh01 = optRib.edgeThresh;
	        }
	        else if(isFullSilhuette){
	            maskMakeArgs.maskingMode = MaskingMode.MaskFullSilhouette;
	            maskMakeArgs.dilationIsExpand  = true;
	            maskMakeArgs.dilationNumTexels = 0; //DO NOT DILATE. We'll dilate  in Final_Dilate_and_Erode()
	        }
	        else{//brushed mask or default:
	            maskMakeArgs.maskingMode = MaskingMode.MaskWhereBrushed;
	            maskMakeArgs.dilationIsExpand  = true;
	            maskMakeArgs.dilationNumTexels = 0; //DO NOT DILATE. We'll dilate  in Final_Dilate_and_Erode()

	            maskMakeArgs.applyBlur_entireMask = true;
	            maskMakeArgs.entireMaskBlurStep01 = optRib.maskBlur_StepLength01;//dilation will be auto-computed if bluring.

	            maskMakeArgs.detectEdges  = optRib.edgeThresh < 0.9998;
	            maskMakeArgs.edgeThresh01 = optRib.edgeThresh;
	            maskMakeArgs.edgeBlur01   = optRib.edgeBlur;
	            maskMakeArgs.edgeThick01  = optRib.edgeThick;
	        }
	        return maskMakeArgs;
	    }


	    //if edge parameters are provided, perform edge-detection and enhance the mask by it:
	    void DetectEdges_maybe( HelperArg helper ){
	        if(!helper.canDetectEdges){ return; }

	        var tArg = new ScreenDepth_EdgesDetector.TexArgs{
	            depthNonLinear_contrast_R16 = helper.depthNonLinear_contrast_ref,
	            result_edges_R16 = helper.edges,
	            screenBrushMask_R = null,
	            edgesThresh01     = helper.edgeThresh01,
	        };

	        var bArg = new ScreenDepth_EdgesDetector.BlurArgs{
	            edgeBlurStride_01 = helper.edgeThick01,
	            edgeBlurPow_01    = helper.edgeBlur01,
	            bufferTex         = helper.edgesBuffer,
	        };
	        _edgesDetector.DetectEdges_ByDepth( tArg );
	        _edgesDetector.BlurEdges_ofDepth(tArg, bArg);
	    }


	    void Render_3D_into_ScreenMask(HelperArg helper){
        
	        TextureTools_SPZ.ClearRenderTexture(_screenMaskRT_fixedRes_antiEdge, Color.clear);
	        _screenMaskRT_fixedRes_antiEdge.filterMode = FilterMode.Point;

	        //NOTICE: we already setup our mat above.
	        var clearFlags = CameraClearFlags.SolidColor;//Clear color, NOT SKYBOX. Else masking shader will flood whole screen;
	        UserCameras_MGR.instance.RenderContentCameras( helper.mask, force_noWireframeMat:false, clearFlags );
	    }


	    // Remember the mask as it was, without blur etc.
	    // We will dilate inwards (erode) it a little, so that it remains inside the brush mask.
	    // This is important later on, to avoid grabbing depth pixels outside the painted mask (will be downsampled)
	    void Shrink_into_NoBlurMask( HelperArg helper ){
	         TextureTools_SPZ.Blit(helper.mask, _screenMaskRT_fixedRes_noBlur); //erode EXACTLY BY 1 texel
	        var dilArg = new DilationArg(_screenMaskRT_fixedRes_noBlur, 1, DilateByChannel.R, null, isRunInstantly: true);
	        dilArg.isExpand = false;
	        TextureDilation_MGR.instance.Dillate(dilArg);
	    }


	    //Expand EXACTLY by 1 texel. Earlier we eroded by 1 texel. This ensures it's back where it started from.
	    void Expand_the_NoBlurMask(){
	        var dilArg = new DilationArg(_screenMaskRT_fixedRes_noBlur, 1, DilateByChannel.R, null, isRunInstantly:true);
	        TextureDilation_MGR.instance.Dillate(dilArg);
	    }


	    void DilateTheWholeMask( RenderTexture maskToDilate, Args_MakeScreenMask arg, int blurNumIters, int blurKernelHalfSize ){

	        int dilateNum_ifBlur = CalculateBoxBlurDilation(blurKernelHalfSize, blurNumIters, arg.entireMaskBlurStep01);

	        int dilateNum =  arg.applyBlur_entireMask?  dilateNum_ifBlur : arg.dilationNumTexels;
	            dilateNum =  Mathf.Max(dilateNum, arg.dilationNumTexels );//never less than the dilation you wanted.

	        var dil = new DilationArg( maskToDilate, dilateNum,
	                                   DilateByChannel.R, null, isRunInstantly: true);
	        dil.isExpand = arg.dilationIsExpand;
	        dil.rule = DilationRule.One_of_4_samples;
	        TextureDilation_MGR.instance.Dillate(dil);
	    }


	    void AntiEdge_and_FinalResults(Args_MakeScreenMask arg,  HelperArg helper ){
	        _preventBlur_ifDistanceMask.PreventBlurredMask_whereFar(_screenMaskRT_fixedRes_noBlur,//with erosion, without blur.
	                                                                 helper.edges, helper.mask,
	                                                                 helper.depthLinear_ref);
	        if (helper.canDetectEdges){
	            _Blit_RemoveEdges_ScreenMask_mat.SetTexture("_EdgesTex", helper.edges);
	            TextureTools_SPZ.Blit(helper.mask, _screenMaskRT_fixedRes_antiEdge);
	            TextureTools_SPZ.Blit(null, _screenMaskRT_fixedRes_antiEdge, _Blit_RemoveEdges_ScreenMask_mat);
	        }else{
	            TextureTools_SPZ.Blit(helper.mask, _screenMaskRT_fixedRes_antiEdge);
	        }
	    }


	    void BlurTheWholeMask_maybe(HelperArg helper, int blurNumIters, int blurKernelHalfSize, float blurStepLength01){
	        if(blurNumIters <= 0){ return; }

	        float blurStepAplification = 0;//don't use it, else our 'dilateNumTexels' would probably have to change.
               
	        var blurArg = new BlurTextures_MGR.BlurTextureArg( helper.mask, null, 
	                                                           blurBoxHalfSize_1_to_12:blurKernelHalfSize,
	                                                           blurStepLength01);
	        blurArg.blurByChannel = BlurByChannel.R;
	        blurArg.farSteps_amplification01 = blurStepAplification;
               
	        for(int i=0; i<blurNumIters; i++){
	            BlurTextures_MGR.instance.Blur_texture(blurArg);
	        }
	    }


	    int CalculateBoxBlurDilation(int kernelHalfSize, int iterations, float blurStepSize01){
	        int kernelRadius = kernelHalfSize;
	        int dilationTexels = kernelRadius * iterations;
	        int dilationNum = Mathf.RoundToInt(dilationTexels*blurStepSize01);

	        if (WorkflowRibbon_UI.instance.currentMode() != WorkflowRibbon_CurrMode.Inpaint_NoColor){
	            // Feels too much, so dividing by 1.5. This keeps dilation tighter yet doesn't allow blur to creep "inwards" :)
	            // Probably it works just for the current 'INPAINT_MASK_MAX_PX_SIZE'.
	            dilationNum = Mathf.RoundToInt(dilationNum/1.5f); 
	        }                     
	        return dilationNum;   
	    }


	    void Equip_Obj_Material( Args_MakeScreenMask arg,  RenderUdims objectUV_brushedColorRGBA ){
	        Material mat; 
	        bool isColorless = WorkflowRibbon_UI.instance.currentMode() == WorkflowRibbon_CurrMode.Inpaint_NoColor;

	        switch (arg.maskingMode){
	            case MaskingMode.MaskWhereEmpty:{
	                mat = _uvWhereEmpty_to_screen_mat;
	                RenderUdims.SetNumUdims(objectUV_brushedColorRGBA, mat);
	                RenderUdims accumTex =  Objects_Renderer_MGR.instance.accumulationTextures_ref();
	                mat.SetTexture("_AccumulatedArt_Tex", accumTex?.texArray );
	                    TextureTools_SPZ.SetKeyword_Material(mat, "EXTRA_WHERE_NORMALS_OK", false);
	                break;
	            }
	            case MaskingMode.MaskWhereBrushed: 
	            case MaskingMode.MaskFullSilhouette:
	            case MaskingMode.MaskFull_and_AntiEdge:
	            default:
	                bool isFullyWhite = arg.maskingMode == MaskingMode.MaskFullSilhouette
	                                   || arg.maskingMode == MaskingMode.MaskFull_and_AntiEdge;
	                mat = _uvColorMask_to_screen_mat; 
	                RenderUdims.SetNumUdims(objectUV_brushedColorRGBA, mat);
	                mat.SetTexture("_ObjectUV_MaskTex", objectUV_brushedColorRGBA.texArray);
	                mat.SetFloat("_isColorlessMask", isColorless? 1:0);
	                mat.SetFloat("_IsFullyWhite", isFullyWhite?1:0);
	                TextureTools_SPZ.SetKeyword_Material(mat, "ONLY_WHERE_NORMALS_OK", false);
	                break;
	        }
	        Objects_Renderer_MGR.instance.EquipMaterial_on_ALL( mat );
	    }


	    void Alloc_ScreenMaskRT_maybe(){
	        RenderTexture content = UserCameras_MGR.instance.camTextures._contentCam_RT_ref;
	        Vector2 newSize  = Calc_NewMask_Size(content.width, content.height);
	        int neededWidth  = Mathf.RoundToInt(newSize.x);
	        int neededHeight = Mathf.RoundToInt(newSize.y);
	        bool isRecreate  = _screenMaskRT_fixedRes_antiEdge == null || _screenMaskRT_fixedRes_antiEdge.width != neededWidth 
	                                                          || _screenMaskRT_fixedRes_antiEdge.height != neededHeight;
	        if(isRecreate){
	            if(_screenMaskRT_fixedRes_antiEdge != null){ 
	                DestroyImmediate(_screenMaskRT_fixedRes_antiEdge);
	                DestroyImmediate(_screenMaskRT_fixedRes_noBlur);
	                DestroyImmediate(_screenMaskRT_skipAntiEdge);
	            }
	            var desc = content.descriptor;
	            desc.width = neededWidth;
	            desc.height = neededHeight;
	            desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm;
	            _screenMaskRT_fixedRes_antiEdge = new RenderTexture(desc);
	            _screenMaskRT_fixedRes_antiEdge.filterMode = FilterMode.Point;

	            _screenMaskRT_fixedRes_noBlur = new RenderTexture(desc);
	            _screenMaskRT_fixedRes_noBlur.filterMode = FilterMode.Point;

	            _screenMaskRT_skipAntiEdge = new RenderTexture(desc);
	            _screenMaskRT_skipAntiEdge.filterMode = FilterMode.Point;
	        }
	    }


	    Vector2 Calc_NewMask_Size(int originalWidth, int originalHeight){
	        float aspect = originalWidth / (float)originalHeight;
	        Vector2 newSize;
    
	        if(originalWidth > originalHeight){
	            newSize.x = INPAINT_MASK_MAX_PX_SIZE;
	            newSize.y = INPAINT_MASK_MAX_PX_SIZE / aspect;
	        }else{
	            newSize.y = INPAINT_MASK_MAX_PX_SIZE;
	            newSize.x = INPAINT_MASK_MAX_PX_SIZE * aspect;
	        }
	        return newSize;
	    }

    
	    void Start(){
	        _uvWhereEmpty_to_screen_mat = new Material(_uvWhereEmpty_to_screen_shader);
	        _uvColorMask_to_screen_mat  = new Material(_uvColorMask_to_screen_shader);
	        _Blit_RemoveEdges_ScreenMask_mat = new Material(_Blit_RemoveEdges_ScreenMask_shader);
	    }

	}
}//end namespace
