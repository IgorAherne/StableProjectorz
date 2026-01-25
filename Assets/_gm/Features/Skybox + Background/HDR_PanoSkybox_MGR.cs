using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking;


namespace spz {

	public class HDR_PanoSkybox_MGR : MonoBehaviour{
	    public static HDR_PanoSkybox_MGR instance { get; private set; } = null;

	    [SerializeField] ComputeShader _applySphereOnDepth_sh;
	    [SerializeField] Texture2D _sphereMask;

	    public Texture2D _guessedDepth = null;
	    List<GenData2D> _sphere_renders = new List<GenData2D>();//of a current iteration (might contain batch of 4+ images, etc)
	    GenData2D _latestGenData = null;
	    bool _sphereGenIterCompleted = false;
	    bool _sphereGen_error = false;

	    public Vector4 _SphereCoordAndScale = new Vector4(0.5f, 0.5f, 0.25f, 0.25f);
	    public Texture _depth_DEBUG;//MODIF
	    public Texture _combinedDepth_DEBUG;//MODIF
	    public Texture _depthWithSphere_DEBUG;//MODIF
	    public Texture _maskWithSphere_DEBUG;//MODIF

	    public string _depthModel = "control_v11f1p_sd15_depth [cfd03158]";


	    public void Generate_PanoramicHDR( GenData2D genData_from, Guid guid_originalTex ){
	        bool cantGen = StableDiffusion_Hub.instance.DenyWithMessage_ifCantGenerate(allow_without_controlnets:true);
	        if(cantGen){ return; }

	        bool correctKind = genData_from.kind == GenerationData_Kind.SD_ProjTextures ||
	                           genData_from.kind == GenerationData_Kind.SD_Backgrounds;
	        if (!correctKind){
	            string msg = "Can only work with individual Projection or Background textures.";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 4, false);
	        }
	        StartCoroutine(Generate_PanoramicHDR_crtn(genData_from, guid_originalTex) );
	    }


	    IEnumerator Generate_PanoramicHDR_crtn( GenData2D genData_from,  Guid guid_originalTex ){

	        yield return StartCoroutine( GuessDepthFromArt_crtn(genData_from, guid_originalTex) );

	        Texture2D depth = genData_from._byproductsOfRequest.depth_disposableTex;
	        RenderTexture combinedDepth  = Combine_Depth_and_GuessedDepth(depth, _guessedDepth);
	        RenderTexture depthWithSphere;
	        RenderTexture maskWithSphere;
	        PutSphere_onto_Depth(combinedDepth, out depthWithSphere, out maskWithSphere);

	        _depth_DEBUG = depth;
	        _combinedDepth_DEBUG = combinedDepth;
	        _depthWithSphere_DEBUG = depthWithSphere;
	        _maskWithSphere_DEBUG = maskWithSphere;

	        SD_WorkflowOptionsRibbon_UI.instance.SetIsTileable_from_script(false);
	        SD_WorkflowOptionsRibbon_UI.instance.SetIsSoftInpaint_from_script(false);

	        _sphere_renders.Clear();
	        _latestGenData = null;

	        SD_GenRequestArgs_byproducts intermediates;
	        SD_img2img_payload payload = make_i2i_payload( genData_from,  guid_originalTex,
	                                                       depthWithSphere, maskWithSphere,  out intermediates );
	        payload.negative_prompt = "matte, diffuse, flat, dull";

	        for (int i=0; i<2; i++){
	            yield return StartCoroutine( Wait_SphereGenerate_iteration( genData_from.kind, isDark:true,  
	                                                                        payload, intermediates) );

	            yield return StartCoroutine( Wait_SphereGenerate_iteration( genData_from.kind, isDark:false, 
	                                                                        payload, intermediates) );
	        }
	        DestroyImmediate( combinedDepth );
	    }


	    // Estimate depth from art, for example via Zoedepth
	    IEnumerator GuessDepthFromArt_crtn( GenData2D genData_from,  Guid guid_originalTex ){
	        Texture2D artTex = genData_from.GetTexture_ref(guid_originalTex).tex2D;
	        SD_ControlnetDetect_payload ctrlPayload = make_ctrlnetDetect_payload( artTex );

	        bool isError = false;
	        SD_ControlnetDetect_Response response = null;
	        StableDiffusion_Hub.instance.ManuallyControlnetDetect(ctrlPayload, onDetected);

	        void onDetected(SD_ControlnetDetect_Response resp ){
	            isError =  resp==null;
	            response = resp;
	        }
	        while(response==null && !isError){
	            yield return null; 
	        }

	        if(_guessedDepth != null){ Texture.DestroyImmediate(_guessedDepth);  }
	        _guessedDepth = TextureTools_SPZ.Base64ToTexture(response.images[0]);
	    }


	    // Put depth that StableProjectorz rendered on top of the depth estimated by ZoeDepth, etc.
	    // This makes the resulting depth image more "interesting" without an empty void arround the model
	    RenderTexture Combine_Depth_and_GuessedDepth(Texture2D depth, Texture2D guessedDepth){
	        // Notice, guessedDepth can have larger resolution (if generated via Envelope mode).
	        // So, using width and height of the depth, instead:
	        RenderTexture combinedDepth = new RenderTexture(depth.width,depth.height, depth:0, 
	                                                        GraphicsFormat.R16_UNorm, mipCount:1);
	        combinedDepth.enableRandomWrite = true;
	        int kernelHandle = _applySphereOnDepth_sh.FindKernel("Combine_Depth_and_GuessedDepth");
	        _applySphereOnDepth_sh.SetTexture(kernelHandle, "DepthTex", depth);
	        _applySphereOnDepth_sh.SetTexture(kernelHandle, "GuessedDepthTex", guessedDepth);
	        _applySphereOnDepth_sh.SetTexture(kernelHandle, "CombinedDepth_rsltTex", combinedDepth);

	        _applySphereOnDepth_sh.SetVector("DepthTex_Size", new Vector4(depth.width, depth.height, 
	                                                                      depth.width, depth.height));
        
	        _applySphereOnDepth_sh.SetVector("GuessedDepthTex_Size", new Vector4(guessedDepth.width, guessedDepth.height, 
	                                                                             guessedDepth.width, guessedDepth.height));

	        _applySphereOnDepth_sh.SetVector("CombinedDepth_rsltTexSize", new Vector4(combinedDepth.width, combinedDepth.height,
	                                                                                  combinedDepth.width, combinedDepth.height));

	        Vector3Int numGroups = ComputeShaders_MGR.calcNumGroups(depth.width, depth.height);
	        _applySphereOnDepth_sh.Dispatch(kernelHandle, numGroups.x, numGroups.y, numGroups.z);

	        return combinedDepth;
	    }



	    void PutSphere_onto_Depth(RenderTexture depth, 
	                              out RenderTexture depthWithSphere_, out RenderTexture maskWithSphere_){

	        depthWithSphere_ = new RenderTexture(depth.descriptor);
	        maskWithSphere_ = new RenderTexture(depth.descriptor);
	        depthWithSphere_.enableRandomWrite = true;
	        maskWithSphere_.enableRandomWrite = true;

	        int kernelHandle = _applySphereOnDepth_sh.FindKernel("ApplySphere");
	        _applySphereOnDepth_sh.SetTexture(kernelHandle, "DepthSrcTex", depth);
	        _applySphereOnDepth_sh.SetTexture(kernelHandle, "SphereTex", _sphereMask);
	        _applySphereOnDepth_sh.SetTexture(kernelHandle, "Depth_OutputTex", depthWithSphere_); //stores here
	        _applySphereOnDepth_sh.SetTexture(kernelHandle, "SphereMask_OutputTex", maskWithSphere_);//stores here.
	        _applySphereOnDepth_sh.SetInt("Output_Width", depthWithSphere_.width);
	        _applySphereOnDepth_sh.SetInt("Output_Height", depthWithSphere_.height);
	        _applySphereOnDepth_sh.SetInt("Sphere_Width", _sphereMask.width);
	        _applySphereOnDepth_sh.SetInt("Sphere_Height", _sphereMask.height);
	        _applySphereOnDepth_sh.SetVector("SphereCoordAndScale", _SphereCoordAndScale );

	        Vector3Int numGroups = ComputeShaders_MGR.calcNumGroups(depth.width, depth.height);
	        _applySphereOnDepth_sh.Dispatch(kernelHandle, numGroups.x, numGroups.y, numGroups.z);
	    }


	    IEnumerator Wait_SphereGenerate_iteration( GenerationData_Kind kind,  bool isDark, 
	                                               SD_img2img_payload sphere_payload, 
	                                               SD_GenRequestArgs_byproducts intermediates )
	    {
	        //manual (not -1), so we can re-use it inside StableProjectorz if needed.
	        sphere_payload.seed   = UnityEngine.Random.Range(0, int.MaxValue);

	        sphere_payload.prompt = isDark? "a perfect black dark mirrored reflective chrome ball sphere"
	                                      : "a perfect mirrored reflective chrome ball sphere";

	        _latestGenData = GenData2D_Maker.make_img2img(sphere_payload,  intermediates, kind);
	        _sphere_renders.Add(_latestGenData);

	        _sphereGenIterCompleted = false;
	        _sphereGen_error = false;
	        StableDiffusion_Hub.instance.SubmitCustomWorkflow(Generate_RequestingWhat.somethingCustom, sendPayload:true, sphere_payload, OnGeneratedSpheres_Progress, OnGeneratedSpheres_Result);

	        while (_sphereGenIterCompleted==false){ yield return null; }

	        if(_sphereGen_error ){
	            for(int i=0; i<_sphere_renders.Count; ++i){ 
	                GenData2D genData2D = _sphere_renders[i];
	                GenData2D_Archive.instance.DisposeGenerationData(genData2D.total_GUID);
	            }
	            _sphere_renders.Clear();
	            _latestGenData = null;
	        }
	        MergeSphereRenders();
	    }


	    void OnGeneratedSpheres_Progress(UnityWebRequest request){
	        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError){
	            Viewport_StatusText.instance.ReportProgress(0);
	            Viewport_StatusText.instance.ShowStatusText("Error fetching progress: "+request.error,  false,  5,  progressVisibility:false );
	        }
	        // Deserialize the JSON response to the ProgressResponse class
	        // Use class-type information, to support inheritance of objects:
	        var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, };
	        SD_Generate_ProgressResponse progressResponse 
	            = JsonConvert.DeserializeObject<SD_Generate_ProgressResponse>(request.downloadHandler.text, settings);
        
	        if(progressResponse==null){ return; }//ComfyUI doesn't return progress (Forge and A1111 would).

	        //using ? in case SD had exception
	        _latestGenData?.Update_PendingImages( progressResponse.state.job_no,  progressResponse.current_image ); 

	        float progressTotal = progressResponse.progress;
	        Viewport_StatusText.instance.ReportProgress( progressTotal );

	        string etaStr = Mathf.RoundToInt(progressResponse.eta_relative).ToString() + " sec";
	        bool isTextETA = true;
	        Viewport_StatusText.instance.ShowStatusText(etaStr, isTextETA, textVisibleDur:999999, progressVisibility:true );
	    }


	    void OnGeneratedSpheres_Result( UnityWebRequest result){
        
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();
	        GenerateButtons_UI.OnConfirmed_FinishedGenerate(canceled:false);

	        bool err =  result.result==UnityWebRequest.Result.ConnectionError 
	                 || result.result==UnityWebRequest.Result.ProtocolError;
        
	        string json = result.downloadHandler.text;

	        if(err || json==""){
	            var jsonLow = json.ToLower();
	            json += jsonLow.Contains("cannot be multiplied") || jsonLow.Contains("server error") ?  
	                        " ..Maybe you are mixing SDXL model with SD 1.5 Controlnet?"  : "";
	            Viewport_StatusText.instance.ShowStatusText("Error: " + json, false, 15, progressVisibility:false);
	            _latestGenData?.Complete_PendingImages( null ); //using ? in case SD had exception
	            StableDiffusion_Hub.instance.MarkCustomWorkflow_Done();
	            _sphereGenIterCompleted = true;
	            _sphereGen_error = true;
	            return; 
	        }

	        // Use class-type information, to support inheritance of objects:
	        var settings = new JsonSerializerSettings{ TypeNameHandling = TypeNameHandling.Auto, };
	        SD_txt2imgResponse response = JsonConvert.DeserializeObject<SD_txt2imgResponse>(json, settings);
        
	        if(json == "{}"){
	            _latestGenData?.Complete_PendingImages(null); //using ? in case SD had exception
	            StableDiffusion_Hub.instance.MarkCustomWorkflow_Done();
	            _sphereGenIterCompleted = true;
	            _sphereGen_error = true; 
	            return;
	        }
	        _latestGenData?.Complete_PendingImages( response.images ); //using ? in case SD had exception
	        StableDiffusion_Hub.instance.MarkCustomWorkflow_Done();
	        _sphereGenIterCompleted = true;
	        _sphereGen_error = false;
	    }


    
	    SD_ControlnetDetect_payload make_ctrlnetDetect_payload(Texture2D artTex){
	        return new SD_ControlnetDetect_payload{
	            controlnet_module = "depth_anything_v2",
	            controlnet_input_images = new string[]{ TextureTools_SPZ.TextureToBase64(artTex) },
	            controlnet_processor_res= artTex.width,
	            controlnet_threshold_a  = artTex.height,
	            controlnet_threshold_b  = 0.5f,
	        };
	    }


	    SD_img2img_payload make_i2i_payload( GenData2D genData_from, Guid guid_originalTex, 
	                                         RenderTexture depthWithSphere, RenderTexture maskWithSphere,
	                                         out SD_GenRequestArgs_byproducts intermediates_ ){

	        intermediates_ = new SD_GenRequestArgs_byproducts();

	        GenData_TextureRef originalArt_texRef = genData_from.GetTexture_ref(guid_originalTex);
	        Texture2D originalTex = originalArt_texRef.tex_by_preference() as Texture2D;
	        if(originalTex == null){ 
	            return new SD_img2img_payload(); 
	        }
	        int num_iter   = 4;
	        int batch_size = 1;

	        var payload = new SD_img2img_payload(){
	            batch_size = batch_size,
	            n_iter     = num_iter,
	            steps      = Mathf.RoundToInt(SD_InputPanel_UI.instance.sampleSteps_slider.value),
	            sampler_name = SD_InputPanel_UI.instance.samplers.value?.name ?? "",
	            scheduler = SD_InputPanel_UI.instance.scheduler.value?.name ?? "",
	            cfg_scale = SD_InputPanel_UI.instance.CFG_scale_slider.value,

	            width  = genData_from.textureSize(false).x,
	            height = genData_from.textureSize(false).y,

	            tiling = false,

	            inpaint_full_res         = (int)0,//whole picture always. User can zoom up if they need to.
	            inpainting_mask_invert   = (int)0, //always mask inside the mask, don't invert. Jul 2024.

	            inpaint_full_res_padding = 0, //how many pixels to add to the mask.  Note, in case of entireShape, silhuette was already dilated by correct number of pixels.
	                                          //For brushed masks, padding is undesirable, could mess up around brushed borders in StableProjectorz (in projection shader)
	            include_init_images = true,
	            init_images         = new string[]{ TextureTools_SPZ.TextureToBase64(originalTex), },
	            mask                = TextureTools_SPZ.TextureToBase64(maskWithSphere),
	            alwayson_scripts    = new Dictionary<string, AlwaysOn_Value>(),

	            mask_blur           = 0, //ZERO, to force stable diffusion to render the ball inside the requested zone.

	            denoising_strength  = 1.0f,

	            inpainting_fill = (int)InpaintingFill.LatentNoise,
	        };
	        var ctrlList = SD_ControlNetsList_UI.instance;
	        ControlNet_NetworkArgs ctrlNets_args = new ControlNet_NetworkArgs();
	        ctrlNets_args.args = new ControlNetUnit_NetworkArgs[1];
	        ctrlNets_args.args[0] = getCtrlNetArgs(depthWithSphere);

	        payload.alwayson_scripts.Add("controlnet", ctrlNets_args);//https://github.com/Mikubill/sd-webui-controlnet/wiki/API#examples-1

	        return payload;
	    }


	    public HowToResizeImg_CTRLNET howResize;//MODIF
	    ControlNetUnit_NetworkArgs getCtrlNetArgs(RenderTexture depthWithSphere){

	        Vector2 widthHeight = new Vector2(depthWithSphere.width, depthWithSphere.height);

	        return new ControlNetUnit_NetworkArgs {
	            image = TextureTools_SPZ.TextureToBase64(depthWithSphere),
	            resize_mode =  ControlNetUnit_ImagesDisplay.HowToResizeImg_tostr(howResize),
	            low_vram    =  false,
	            processor_res = Mathf.RoundToInt( Mathf.Max(widthHeight.x, widthHeight.y) ),
	            threshold_a = 0.5f,
	            threshold_b = 0.5f,
	            model = _depthModel,
	            module = "None",
	            weight = 1,
	            guidance_start = 0,
	            guidance_end = 1,
	            control_mode = ControlNetUnit_UI.ControlMode_tostr( ControlNetUnit_UI.ControlMode.Balanced ),
	        };
	    }



	    //average out the renders of the sphere.
	    void MergeSphereRenders(){

	    }


	    void Update(){//MODIF: this update is only used for testing
        
	        //if (KeyMousePenInput.isKey_alt_pressed() && Input.GetKeyDown(KeyCode.D)){
	        //    GenData2D genData =  GenData2D_Archive.instance.GenerationGUID_toData( GenData2D_MGR.instance.latestGeneration_GUID );
	        //    Guid tex0_guid = genData.GetTexture_ref0().guid;
	        //    Generate_PanoramicHDR(genData, tex0_guid);
	        //}
	    }

	    void Awake(){
	        if(instance!=null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;
	    }

	}
}//end namespace
