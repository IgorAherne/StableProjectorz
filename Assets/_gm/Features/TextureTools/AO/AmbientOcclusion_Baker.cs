using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace spz {

	public class AmbientOcclusionBake_Args{
	    public bool withBlur;
	    public bool darkerBelow;
	}

	public class AmbientOcclusion_Baker : MonoBehaviour{
	    public static AmbientOcclusion_Baker instance { get; private set; } = null;

	    [SerializeField] Camera _ao_Camera;
	    [SerializeField] Transform _pivot; //will rotate this, like a selfie-stick
	    [SerializeField] Camera_KeepBoundingBoxVisible _aoCamera_focuser;
	    [SerializeField] Material _screenSpaceAO_bake_mat; //for capturing AO from different angles. Use '_screenSpaceAO_bake_matCopy'.
	    [SerializeField] Material _AO_blend_AO_mat; //for copying resulting AO texture into some projections-texture. use '_AO_blend_AO_matCopy'
    
	    [Tooltip("higher value = slower result (more render iters needed), but of higher quality.")]
	    [Range(0.85f, 0.998f)] [SerializeField] float _shaderAccumulStability = 0.99f; 
	    [SerializeField]             int _numAO_renderIterations = 1024;
	    [SerializeField][Range(0,3)] float _botDarkerCoeff = 1;
    
	    [Header("Usual AO shader settings:")]
	    [SerializeField][Range(0,6)]         float _usual_AO_strength;//how pronounced the overal effect is
	    [SerializeField][Range(0.05f, 0.4f)] float _usual_AO_fullEffectDepthDiff; //how great the depth difference should be between samples, before effect is 100 pronounced.
	    [SerializeField][Range(0.03f, 0.5f)] float _usual_AO_radius; //how far we search from current fragment (pixel)

	    // wraps around the object. we are rendering Ambient Occlusion shadowing into it,
	    // by looking at the object from various angles.
	    RenderUdims _ao_accumulation_uvTex = null;
	    RenderUdims _helper_uvTex = null;

	    RenderUdims _preview_AO_texture1 = null;
	    RenderUdims _preview_AO_texture2 = null;

	    Material _screenSpaceAO_bake_matCopy;
	    Material _AO_blend_AO_matCopy;

	    RenderTexture _depth_tempTargTexture;

	    bool _interruptBake_asap = false;

	    public bool isGeneratingAO { get; private set; } = false;

  
	    //for example, if you need to save it to file:
	    public Dictionary<Texture2D,UDIM_Sector> getDisposable_AO_texture( IconUI fromIcon,  out bool destroyWhenDone_ ){
        
	        var dict = new Dictionary<Texture2D, UDIM_Sector>();
	        destroyWhenDone_ = true;

	        if (fromIcon == null){ return dict; }
	        if (fromIcon._genData == null){ return dict; }

	        //convert into Texture2D to output to user, so they can save it, etc:
	        dict = fromIcon._genData.GetTextures2D_expensive(out destroyWhenDone_);
	        return dict;
	    }


	    public void Blend_AO_into_some_RT( IconUI from_aoIcon, RenderUdims intoHere ){

	        RenderUdims.SetNumUdims(isUsingArray:true, intoHere.texArray.volumeDepth, _AO_blend_AO_matCopy );
	        //apply effects (contrast etc) together with our AO, to get final AO:
	        Texture blitFromAO;
	        Setup_AO_intoBlendingMat(_AO_blend_AO_matCopy, from_aoIcon, out blitFromAO);
	        TextureTools_SPZ.Blit( blitFromAO, intoHere.texArray, _AO_blend_AO_matCopy);
	    }

    
	    void Setup_AO_intoBlendingMat(Material mat, IconUI fromIcon, out Texture blitFromAO_){
	        // if we early-return but mat had our AO texture before, it will cause material to be darkened.
	        // Ensure there is no Ambient Occlusion texture before our 'return checks', just in case.
	        mat.SetTexture("_SrcTex", null);
	        blitFromAO_ = null;

	        if (fromIcon==null){ return; }
	        if(ProjectorCameras_MGR.instance._showOrderOfProjections){ return; }//don't show AO while user is previewing the order of layers (in black & white).

	        AmbientOcclusionInfo aoInfo = fromIcon.aoInfo();

	        GenData_TextureRef tex_ref =  fromIcon._genData.GetTexture_ref0();
	        Debug.Assert(tex_ref.texturePreference == TexturePreference.Tex2DArray,
	                     "AO Icon should be using Texture-array, not separate texture2D.");

	        blitFromAO_ = tex_ref.texArray;
	        mat.SetTexture("_SrcTex", tex_ref.texArray);
	        mat.SetFloat("_AO_Visibility", aoInfo.visibility);
	        mat.SetFloat("_AO_Pivot", aoInfo.pivot);
	        mat.SetFloat("_AO_Darks", aoInfo.darkCoeff);
	        mat.SetFloat("_AO_Midtones", aoInfo.midtonesCoeff);
	        mat.SetFloat("_AO_Highlights", aoInfo.highlightsCoeff);
	    }


	    public void BakeAO( AmbientOcclusionBake_Args args,  System.Action<bool> onBakeComplete ){
	        if(StableDiffusion_Hub.instance._generating){
	            Viewport_StatusText.instance.ShowStatusText("Can't Bake AO while Generating images. Please wait.", false, 1.5f, true);
	            onBakeComplete?.Invoke(false);
	            return; 
	        }if(isGeneratingAO){ //already baking
	            onBakeComplete?.Invoke(false);
	            return;
	        }if(ModelsHandler_3D.instance.hasModelRootGO==false){
	            Viewport_StatusText.instance.ShowStatusText("Can't Bake AO because there are no 3D models. Please import them.", false, 1.5f, false);
	            onBakeComplete?.Invoke(false);
	            return;
	        }
	        if(ModelsHandler_3D.instance._isImportingModel){
	            Viewport_StatusText.instance.ShowStatusText("Can't Bake AO while loading a 3D model file. Please wait.", false, 1.5f, false);
	            return;
	        }
	        isGeneratingAO = true;
	        _interruptBake_asap = false;
	        StartCoroutine( BakeAO_crtn(args, onBakeComplete) );
	    }


	    public void InterruptBake(){
	        _interruptBake_asap = true;
	    }


	    IEnumerator BakeAO_crtn( AmbientOcclusionBake_Args args,  System.Action<bool> onBakeComplete ){

	        BakeOA_Preliminaries();
	        Bounds meshesBounds = ModelsHandler_3D.instance.GetTotalBounds_ofSelectedMeshes();

	        int pcnt20 = (int)(_numAO_renderIterations/0.2);
	        int pcnt40 = (int)(_numAO_renderIterations/0.4);
	        int pcnt60 = (int)(_numAO_renderIterations/0.6);
	        int pcnt80 = (int)(_numAO_renderIterations/0.8);
	        float starTime = Time.unscaledTime;
	        float comfortPauseDur = 0.25f;//will pause for this long, to allow users with fast GPU to Interrupt generation midway.

	        var cmd = new CommandBufferScope("Bake SSAO from dir");

	        for (int i=0; i<_numAO_renderIterations; ++i){
	            if(_interruptBake_asap){ break; }
	            RenderFromDir( args, cmd, _helper_uvTex, Random.insideUnitSphere, Vector3.up, ref meshesBounds);
	            RenderFromDir( args, cmd, _helper_uvTex, Random.insideUnitSphere, Vector3.up, ref meshesBounds);
	            int blurIters = args.withBlur? 3:0; //preview texture has smaller res, so using fewer iters than in the final Blur.
	            BakeAO_previewResult( blurIters );

	            Viewport_StatusText.instance.ShowStatusText("Baking Ambient Occlusion...", false,  1.0f,  progressVisibility:true);
	            float progress01 =  i / ((float)_numAO_renderIterations-2);
	            Viewport_StatusText.instance.ReportProgress(progress01);

	            float elapsed =  Time.unscaledTime - starTime;
	            if(elapsed<comfortPauseDur*1 && i==pcnt20){ yield return new WaitForSeconds(0.5f); }//wait on purpose, to give user time click Interrupt 
	            if(elapsed<comfortPauseDur*2 && i==pcnt40){ yield return new WaitForSeconds(0.5f); }//if they want to stop early (for artistic look).
	            if(elapsed<comfortPauseDur*3 && i==pcnt60){ yield return new WaitForSeconds(0.5f); }
	            if(elapsed<comfortPauseDur*4 && i==pcnt80){ yield return new WaitForSeconds(0.5f); }
	            yield return null;
	        }

	        int blurIterss  = args.withBlur? 8 : 5;//at least 3 iter to remove the noisy look.
	        float blurStep = args.withBlur? 0.15f : 0.2f;
	        Blur(_ao_accumulation_uvTex, _helper_uvTex, blurIterss, blurStep);
	        yield return StartCoroutine( BakeAO_FinalDilate_crtn() );

	        RenderUdims clone_takeOwnershipPlz =  _ao_accumulation_uvTex.Clone();
	        var genData =  GenData2D_Maker.make_AmbientOcclusion( clone_takeOwnershipPlz.texArray, clone_takeOwnershipPlz.udims_sectors);
	        //DON'T Destroy 'clone_takeOwnershipPlz', because it was already "adopted" by the GenData2D.

	        CompleteBake_and_Cleanup();
	        onBakeComplete?.Invoke(true);
	    }


	    void BakeOA_Preliminaries(){
	        Init_ao_renderTex();
	        //black BUT WITH ZERO ALPHA (zero alpha is important for later, during dilation)
	        _ao_accumulation_uvTex.ClearTheTextures(Color.clear);

	        InitHelperTex();//AFTER the AO-render-tex

	        RenderUdims.SetNumUdims(_ao_accumulation_uvTex, _screenSpaceAO_bake_matCopy);

	        _screenSpaceAO_bake_matCopy.SetFloat("_ShaderAccumulStability", _shaderAccumulStability);
	        _screenSpaceAO_bake_matCopy.SetFloat("_TotalStrength", _usual_AO_strength);
	        _screenSpaceAO_bake_matCopy.SetFloat("_SearchRadius", _usual_AO_radius);
	        _screenSpaceAO_bake_matCopy.SetFloat("_FullDepth01Difference", _usual_AO_fullEffectDepthDiff);
	    }
    

	    void Init_ao_renderTex(){
	        // Just in case, if non-null (maybe had exception previously, etc):
	        // NOTICE: don't dispose '_ao_accumulation_uvTex'.
	        _helper_uvTex?.Dispose();
	        _preview_AO_texture1?.Dispose();
	        _preview_AO_texture2?.Dispose();
        
	        int res = SceneResolution_MGR.resultTexQuality;
	            res = Mathf.Max(2048, res);//ambient occlusion looks bad on lower res textures. Keep at least 2048 even on pixel-looks.

	        bool skip  = _ao_accumulation_uvTex!=null && _ao_accumulation_uvTex.width == res;
	             skip &= _ao_accumulation_uvTex?.UdimsCount == UDIMs_Helper._allKnownUdims.Count;
	        if(skip){ return; }

	        _ao_accumulation_uvTex?.Dispose();
	        IReadOnlyList<UDIM_Sector> allUdims = ModelsHandler_3D.instance._allKnownUdims;
	        _ao_accumulation_uvTex =  new RenderUdims( allUdims,  Vector2Int.one*res,
	                                                   GraphicsFormat.R8G8_UNorm,  FilterMode.Point,  Color.clear);
	    }                                            //Point: important, instead of default Bilenear. Important during Dilation.


	    void InitHelperTex(){
	        _helper_uvTex = _ao_accumulation_uvTex.Clone( copyTextureContents:false );
	        Vector2Int widthHeight = _helper_uvTex.widthHeight;
	        _preview_AO_texture1 =  new RenderUdims( _helper_uvTex.udims_sectors,  widthHeight/4,
	                                                 _helper_uvTex.graphicsFormat, FilterMode.Bilinear, Color.clear);
	                                                 //Bilinear (not point) beause not used in dilation, gives smoother preview.
	        _preview_AO_texture2 = _preview_AO_texture1.Clone();

	        //will only be used for depth camera texture, so use 32 depth-bits and just R8 format.
	        //Not using depth of render-arrays, because they will be large and we just need for 2d screen.
	        _depth_tempTargTexture = RenderTexture.GetTemporary(_preview_AO_texture2.width, _preview_AO_texture2.height, 
	                                                            32, GraphicsFormat.R8_UNorm);
	    }


	    void RenderFromDir( AmbientOcclusionBake_Args args,  CommandBufferScope cmd,  RenderUdims currAO_so_far,  
	                        Vector3 dir,  Vector3 up, ref Bounds meshesBounds ){
	        if (args.darkerBelow){
	            dir.y -= _botDarkerCoeff*Random.Range(0.1f, 1);
	            dir = dir.normalized;
	        }
	        TextureTools_SPZ.Blit(_ao_accumulation_uvTex.texArray, currAO_so_far.texArray);
	        _screenSpaceAO_bake_matCopy.SetTexture("_CurrentAO_uvTexture", _helper_uvTex.texArray);

	        _pivot.transform.LookAt(transform.position+dir, up);
	        _aoCamera_focuser.Ensure_BoundsVisible(ref meshesBounds, _pivot);
	        _screenSpaceAO_bake_matCopy.SetVector("_CameraWorldPos", _ao_Camera.transform.position);

	        Objects_Renderer_MGR.instance.EquipMaterial_on_ALL( _screenSpaceAO_bake_matCopy );

	        //render objects with a cheap shader, to get screen-space depth, into a square 2D texture.
	        //putting depth into 2d texture is cheaper than making texture-array support the depth bits.
	        _ao_Camera.targetTexture = _depth_tempTargTexture;
	        _ao_Camera.RenderWithShader( StaticShaders_MGR.instance.Depth_ShadowcasterSimple, "" );

	        // don't clear color, build up:
	        cmd.RenderIntoTextureArray( _ao_Camera,  ModelsHandler_3D.instance.selectedRenderers, _screenSpaceAO_bake_matCopy, 
	                                    useClearingColor:false, clearTheDepth:false, Color.clear,  _ao_accumulation_uvTex.texArray );
	    }

    
	    void Blur(RenderUdims from, RenderUdims to, int blurNumIters_1_to_8, float stepLength=0.3f){
	        var blurArg = new BlurTextures_MGR.BlurTextureArg(from.texArray, to.texArray, blurNumIters_1_to_8, stepLength);
	        blurArg.blurByChannel = BlurByChannel.G;//because our texture has only R and G channel (for compactness).
	        blurArg.is_for_uv_chunks = true;
	        blurArg.farSteps_amplification01 = 0.8f;
	        BlurTextures_MGR.instance.Blur_texture(blurArg);
	    }

	    IEnumerator BakeAO_FinalDilate_crtn(){
	        Objects_Renderer_MGR.instance.ShowFinalMat_on_ALL(finalTextureColor: _ao_accumulation_uvTex);
        
	        bool dilationDone = false;
	        System.Action<RenderTexture> onDilated = (rt) => dilationDone = true;
	        int numDilationIters =  Mathf.Max(_ao_accumulation_uvTex.width, _ao_accumulation_uvTex.height) / 16;  //for exmaple  2048 --> 128 pixels dilated.

	        var dilationArg =  new DilationArg( _ao_accumulation_uvTex.texArray,  numDilationIters,  DilateByChannel.G,  onDilated);
	        dilationArg.blitHelperTex = _helper_uvTex.texArray;
	        //dilationArg.findUVchunkBorders_thenBlurThem = true;
	        dilationArg.rule = DilationRule.NineSamplesAveraged;
	        TextureDilation_MGR.instance.Dillate(dilationArg);
	        while(!dilationDone){ yield return null; }
	    }

	    void CompleteBake_and_Cleanup(){
	        isGeneratingAO = false;
	        _interruptBake_asap = false;
	        Viewport_StatusText.instance.ShowStatusText("", false, 0.1f, progressVisibility:false);

	        Objects_Renderer_MGR.instance.ShowFinalMat_on_ALL( finalTextureColor:_ao_accumulation_uvTex );

	        _helper_uvTex.Dispose();
	        _preview_AO_texture1.Dispose();
	        _preview_AO_texture2.Dispose();
	        TextureTools_SPZ.Dispose_RT(ref _depth_tempTargTexture, isTemporary:true);
	        _helper_uvTex = null;
	        _preview_AO_texture1 = null;
	        _preview_AO_texture2 = null;
	        //NOTICE: Released '_helper_uvTex' but don't destroy '_ao_accumulation_uvTex'.
	        Resources.UnloadUnusedAssets();
	    }

    
	    void BakeAO_previewResult(int blurNumIters_1_to_8){
	        TextureTools_SPZ.Blit(_ao_accumulation_uvTex.texArray, _preview_AO_texture1.texArray);

	        Blur(_preview_AO_texture1, _preview_AO_texture2, blurNumIters_1_to_8, 0.3f);

	        Objects_Renderer_MGR.instance.ShowFinalMat_on_ALL( finalTextureColor: _preview_AO_texture1 );
	    }


	    void On_Will_Import_3dModel(ModelsHandler_ImporingInfo info){
	        if(info.isKeep_AO_Icons){ return; }
	        var genDict = GenData2D_Archive.instance;
	        List<GenData2D> ao_genData = genDict.FindAll_GenData_ofKind( GenerationData_Kind.AmbientOcclusion );
	        ao_genData.ForEach(d=>genDict.DisposeGenerationData(d.total_GUID));
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	        _ao_Camera.enabled = false;
	        _ao_Camera.depthTextureMode = DepthTextureMode.Depth;
	        _ao_Camera.clearFlags = CameraClearFlags.Depth;
	        _ao_Camera.allowMSAA = false;//important to disable, otherwise edges of UV chunks remain blurry and affect dillation.

	        _screenSpaceAO_bake_matCopy = new Material(_screenSpaceAO_bake_mat);
	        _AO_blend_AO_matCopy = new Material(_AO_blend_AO_mat);

	        _screenSpaceAO_bake_mat = null; //to avoid modifying it accidentally. Now use the copy instead.
	        _AO_blend_AO_mat = null;

	        ModelsHandler_3D.Act_onWillLoadModel += On_Will_Import_3dModel;
	    }


	    void OnDestroy(){
	        _ao_accumulation_uvTex?.Dispose();
	        _helper_uvTex?.Dispose();
	        _preview_AO_texture1?.Dispose();
	        _preview_AO_texture2?.Dispose();

	        DestroyImmediate(_screenSpaceAO_bake_matCopy);
	        DestroyImmediate(_AO_blend_AO_matCopy);

	        ModelsHandler_3D.Act_onWillLoadModel -= On_Will_Import_3dModel; 
	    }

	}
}//end namespace
