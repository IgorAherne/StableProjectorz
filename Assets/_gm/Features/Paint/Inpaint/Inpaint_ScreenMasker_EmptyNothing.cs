using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	public class Inpaint_ScreenMasker_EmptyNothing : MonoBehaviour
	{
	    [SerializeField] Shader _uvColorMask_to_screen_shader; //masks all inpaint-brushed areas.
	    [SerializeField] Shader _uvWhereEmpty_to_screen_shader;//masks all empty areas (those without projections)
	    [Space(10)]
	    [SerializeField] Depth_Contrast_Helper _depthContrastHelper;
	    [SerializeField] ScreenDepth_EdgesDetector _edgesDetector;
	    [SerializeField] PreventBlur_if_DistanceFromMask _preventBlur_ifDistanceMask;

	    Material _uvWhereEmpty_to_screen_mat;
	    Material _uvColorMask_to_screen_mat;

	    //helper buffer, stores mask+dilation. But without any blurs etc.
	    RenderTexture _screenMaskRT_fixedRes_noBlur = null;

	    //same as _screenMaskRT_fixedRes, but never with anti-edge applied.
	    public RenderTexture _screenMaskRT_skipAntiEdge{ get; private set; } = null;

	    //viewport texture whose greatest side is INPAINT_MASK_MAX_PX_SIZE pixels.
	    //this small size allows to do wide blur efficiently, with a fixed number of blur iterations.
	    public RenderTexture _screenMaskRT_fixedRes_antiEdge{ get; private set; } = null;


	    public static readonly int INPAINT_MASK_MAX_PX_SIZE = 512;


	    class HelperArg{
	        public RenderTexture depthLinear_ref { get; private set; } = null;
	        public RenderTexture depthNonLinear_contrast_ref { get; private set; } = null;//non-linear depth, which was improved byContrast
	        public RenderTexture mask {  get; private set; } = null;
	        public RenderTexture edges { get; private set; } = null;
	        public RenderTexture edgesBuffer { get; private set; } = null;//helper texture during edge-detection.
	        public float edgeThresh01 { get; private set; }
	        public float edgeThick01 { get; private set; }
	        public float edgeBlur01 { get; private set; }

	        public void Init(float thick, RenderTexture screenMask_fixedRes ){
	            var descriptor = screenMask_fixedRes.descriptor;//A copy of the struct. Ensuring it's single chanel:
	                descriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8_SNorm;
	            mask = RenderTexture.GetTemporary( descriptor );
	            mask.enableRandomWrite = true;

	            depthLinear_ref             = UserCameras_MGR.instance.camTextures._SD_depthCam_RT_R32_linear;
	            depthNonLinear_contrast_ref = UserCameras_MGR.instance.camTextures._SD_depthCam_RT_R32_contrast;

	            edges       = RenderTexture.GetTemporary(depthNonLinear_contrast_ref.width, depthNonLinear_contrast_ref.height, depthBuffer: 0, RenderTextureFormat.R16);
	            edgesBuffer = RenderTexture.GetTemporary(depthNonLinear_contrast_ref.width, depthNonLinear_contrast_ref.height, depthBuffer: 0, RenderTextureFormat.R16);
	            edges.enableRandomWrite = true;
	            edgesBuffer.enableRandomWrite = true;
	            var oRib = SD_WorkflowOptionsRibbon_UI.instance;
	            edgeThresh01 = oRib.edgeThresh;
	            edgeThick01  = thick;
	            edgeBlur01   = oRib.edgeBlur;
	        }
	        public void Dispose(){
	            mask?.Release(); 
	            mask = null; 
	            edges?.Release(); edges = null;
	            edgesBuffer?.Release(); edgesBuffer = null;
	            depthLinear_ref = null;//depth doesn't belong to us, don't release just forget.
	            depthNonLinear_contrast_ref = null;
	        }
	    }


	    // Visualize the mask from the camera's vision.
	    // The result is the silhuette, of the inpaint-color-mask that was applied to objects.
	    public void RenderScreenMask( RenderUdims objectUV_brushedColorRGBA ){
	        Alloc_ScreenMaskRT_maybe();

	        var helper = new HelperArg();
	        //use 0.24, same as in 'Inpaint_ScreenMasker_EmptyNothing.cs'
	        helper.Init(thick:0.24f, _screenMaskRT_fixedRes_antiEdge);
	        DetectEdges(helper);

	        Equip_Obj_Material(objectUV_brushedColorRGBA);
	        Render_3D_into_ScreenMask( helper );

	        Shrink_into_NoBlurMask( helper );//dilate
	        //before blur. Generate texture that will help us search neighbor-texel depths later:
	        _preventBlur_ifDistanceMask.CalcDownscaledDepths_of_Mask( _screenMaskRT_fixedRes_noBlur, helper.depthLinear_ref );
	        Expand_the_NoBlurMask();//erode to undo the dilate.

	        int blurNumIters = 4; 
	        int blurKernelHalfSize = 12;
	        float blurStepLen01 = 0.25f*SD_WorkflowOptionsRibbon_UI.instance.maskBlur_StepLength01;
	        Dilate_the_WholeMask(maskToDilate:helper.mask, blurNumIters, blurKernelHalfSize, blurStepLen01);
	        //EmptyNothing doesn't use soft inpaint. Blur is irrelevant, so only use dilate (v2.0.4)
	        //Maybe in future use the blurNumIters = 4, if soft inpaint will support LatentNothing.
	        //   Blur_the_WholeMask(helper, blurNumIters, blurKernelHalfSize);

	        //remember mask, before we do anti-blur and anti-edge.
	        //We can send this mask to SD, but will use anti-edged
	        //mask in StableProjectorz, when applying projections etc.
	        TextureTools_SPZ.Blit(helper.mask, _screenMaskRT_skipAntiEdge);

	        AntiEdge_and_FinalResults(helper);
	        Final_Dilate_and_Erode();

	        #if UNITY_EDITOR
	          if (_preview==1){
	              TextureTools_SPZ.Blit(_preventBlur_ifDistanceMask._screenDepthMaskDownsampled, _screenMaskRT_fixedRes_antiEdge);
	          }
	          else if (_preview>1){
	              TextureTools_SPZ.Blit(helper.edges, _screenMaskRT_fixedRes_antiEdge);
	          }
	        #endif
	        helper.Dispose();
	    }

	    public int _preview = 0;


	    void DetectEdges( HelperArg helper ){
	        var tArg = new ScreenDepth_EdgesDetector.TexArgs{
	            depthNonLinear_contrast_R16 = helper.depthNonLinear_contrast_ref,
	            result_edges_R16  = helper.edges,
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


	    //Expand EXACTLY by 1 texel. Earlier we eroded by 1 texel.
	    void Expand_the_NoBlurMask(){
	        var dilArg = new DilationArg(_screenMaskRT_fixedRes_noBlur, 1, DilateByChannel.R, null, isRunInstantly:true);
	        TextureDilation_MGR.instance.Dillate(dilArg);
	    }


	    void Dilate_the_WholeMask( RenderTexture maskToDilate, int blurNumIters, int blurKernelHalfSize, float blurStepLen01 ){

	        int dilateNum =  CalculateBoxBlurDilation(blurKernelHalfSize, blurNumIters, blurStepLen01);
	            dilateNum =  Mathf.Max(dilateNum, 1 );

	        var dil = new DilationArg( maskToDilate, dilateNum, DilateByChannel.R, null, isRunInstantly: true);
	        dil.rule = DilationRule.One_of_4_samples;
	        TextureDilation_MGR.instance.Dillate(dil);
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


	    void Blur_the_WholeMask(HelperArg helper, int blurNumIters, int blurKernelHalfSize){
	        if(blurNumIters <= 0){ return; }
  
	        var blurArg = new BlurTextures_MGR.BlurTextureArg( helper.mask, null, 
	                                                           blurBoxHalfSize_1_to_12:blurKernelHalfSize,
	                                                           SD_WorkflowOptionsRibbon_UI.instance.maskBlur_StepLength01 );
	        blurArg.blurByChannel = BlurByChannel.R;
	        float blurStepAplification = 0;//don't use it, else our 'dilateNumTexels' would probably have to change.
	        blurArg.farSteps_amplification01 = blurStepAplification;
               
	        for(int i=0; i<blurNumIters; i++){
	            BlurTextures_MGR.instance.Blur_texture(blurArg);
	        }
	    }


	    void AntiEdge_and_FinalResults( HelperArg helper ){
	        _preventBlur_ifDistanceMask.PreventBlurredMask_whereFar( _screenMaskRT_fixedRes_noBlur,//with erosion, without blur.
	                                                                 helper.edges,  helper.mask, helper.depthLinear_ref, 
	                                                                 featherEdges:false );
	        TextureTools_SPZ.Blit(helper.mask, _screenMaskRT_fixedRes_antiEdge);
	    }


	    void Final_Dilate_and_Erode(){
	        var dilateArg = new DilationArg(_screenMaskRT_skipAntiEdge, 1, DilateByChannel.R, null, isRunInstantly:true);
	        var erodeArg  = new DilationArg(_screenMaskRT_fixedRes_antiEdge, 1, DilateByChannel.R, null, isRunInstantly:true);
	        erodeArg.isExpand = false;
	        TextureDilation_MGR.instance.Dillate(dilateArg);
	        TextureDilation_MGR.instance.Dillate(erodeArg);
	    }


	    void Equip_Obj_Material( RenderUdims objectUV_brushedColorRGBA ){
	        Material mat; 
	        mat = _uvWhereEmpty_to_screen_mat;
	        RenderUdims.SetNumUdims(objectUV_brushedColorRGBA, mat);
	        RenderUdims accumTex =  Objects_Renderer_MGR.instance.accumulationTextures_ref();
	        mat.SetTexture("_AccumulatedArt_Tex", accumTex?.texArray );
	            TextureTools_SPZ.SetKeyword_Material(mat, "EXTRA_WHERE_NORMALS_OK", false);
           
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
	    }
	}
}//end namespace
