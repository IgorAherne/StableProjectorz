using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;


namespace spz {

	public enum Generate_RequestingWhat{
	    nothing, somethingCustom, txt2img, img2img, upscale, ctrlnetDetect, Shadow_R_delighting, rembg_backgroundRemoval,
	}

	public class SD_GenRequests_Helper : MonoBehaviour{

	    [SerializeField] SD_Generate_PayloadMaker _payload_maker;
	    [SerializeField] SD_Generate_NetworkSender _generate_sender;
	    [SerializeField] float _generationCooldown = 1.3f;

	    public Texture _latestDepthTex_sent; //for an easier debug.
	    public Texture _latestScreenMask_sent;
	    public bool _latest_isImg2img;
	    public InpaintingFill _latest_Fill;

	    [Header("we sent WIHOUT anti-edge. And this one is used internally in StableProjectorz:")]
	    public Texture _latestUsualView_used;

	    // KEEPING HERE, not in Generations_Dictionary.
	    // Because user might Load image while we are still doing txt2img, etc.
	    // This would create generation for that image and cause that one to be "latest".
	    // We are using this variable only for txt2img and img2img requests:
	    GenData2D _latestGenData = null;

	    public bool _finalPreparations_beforeGen { get; private set; } = false;
	    public Generate_RequestingWhat _isGeneratingWhat { get; private set; } = Generate_RequestingWhat.nothing;//reset to 'nothing' once generation is done.
	    public float _generationCooldownUntil { get; private set; } = -9999; //to prevent relaunching generation too quickly (for example after Interrupting).


	#if UNITY_EDITOR
	    public bool _dumTextures_toFile;
	    void OnValidate(){
	        if (!_dumTextures_toFile) { return; }
	        _dumTextures_toFile = false;
	        TextureTools_SPZ.EncodeAndSaveTexture(_latestScreenMask_sent as Texture2D, Directory.GetParent(Application.dataPath).FullName + "/_latestMask.png");
	        TextureTools_SPZ.EncodeAndSaveTexture(_latestUsualView_used as Texture2D, Directory.GetParent(Application.dataPath).FullName + "/_latestView.png");
	    }
	#endif


	    public void Generate_txt2Img(bool isMakingBackgrounds,  Action onRequested=null ){
	        StartCoroutine( Generate_txt2Img_crtn(isMakingBackgrounds, onRequested) );
	    }

	    public void Generate_img2img(bool isMakingBackgrounds,  Action onRequested=null ){
	        StartCoroutine( Generate_img2img_crtn(isMakingBackgrounds, onRequested) );
	    }


	    public void Upscale_img2extra(float upscaleBy,  GenData2D genData_canBeNull=null, 
	                                  Texture2D imgForSending=null, Action onRequested=null){
	        StartCoroutine( Upscale_img2extra_crtn(upscaleBy, genData_canBeNull, imgForSending, onRequested) );
	    }

    
	    public void Submit_CtrlnetDetectRequest( SD_ControlnetDetect_payload payload, 
	                                             Action<SD_ControlnetDetect_Response> onDetected ){
	        StartCoroutine( Submit_CtrlDetect_crtn(payload, onDetected) );
	    }

    
	    public bool SubmitCustomWorkflow( Generate_RequestingWhat what, bool sendPayload,  
	                                      SD_img2img_payload payload, Action<UnityWebRequest> onProgress, 
	                                      Action<UnityWebRequest> onCompleted ){
	        if(_isGeneratingWhat != Generate_RequestingWhat.nothing){ return false; }//already busy
	        _isGeneratingWhat = what;
	        if (sendPayload && payload!=null){
	            _generate_sender.Send_GenerateRequest(payload, onProgress, onCompleted);
	        }
	        return true;
	    }

	    public void MarkCustomWorkflow_Done(){
	        _isGeneratingWhat = Generate_RequestingWhat.nothing;
	    }



	    IEnumerator Generate_txt2Img_crtn(bool isMakingBackgrounds, Action onRequested = null) {
        
	        if(!Start_GenerationRequest(Generate_RequestingWhat.txt2img)){ yield break; }

	        UserCameras_Permissions.Force_KeepRenderingCameras(true);

	        //for inpaint to apply itself, etc. (or to avoid checker pattern if had No-Color Mask)
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();

	            for (int i=0; i<3; ++i){ yield return null; }//give time for cameras to render the target textures.
            
	            GenerationData_Kind genData_kind = isMakingBackgrounds? GenerationData_Kind.SD_Backgrounds 
	                                                                  : GenerationData_Kind.SD_ProjTextures;
	            SD_txt2img_payload payload;
	            SD_GenRequestArgs_byproducts intermediates;
	            _payload_maker.Create_txt2img_payload(out payload, out intermediates);

	            _generate_sender.Send_GenerateRequest(payload, OnProgressResponse, OnGeneratedResult);

	            _latestGenData = GenData2D_Maker.make_txt2img(payload, intermediates, genData_kind);

	            Finalize_GenerationRequest( payload.width,  payload.height,  payload.n_iter,  
	                                        payload.batch_size, "txt2img" );
            
	        UserCameras_Permissions.Force_KeepRenderingCameras(false);
	        RememberSentVals_forDebug(intermediates,  isImg2Img:false );
	        onRequested?.Invoke();
	    }


	    IEnumerator Generate_img2img_crtn(bool isMakingBackgrounds,  Action onRequested=null){
        
	        if( !Start_GenerationRequest(Generate_RequestingWhat.img2img) ){ yield break; }

	        //for inpaint to apply itself, etc. (or to avoid checker pattern if had No-Color Mask)
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();

	        UserCameras_Permissions.Force_KeepRenderingCameras(true);
	            for(int i=0; i<3; ++i){ yield return null; }//give time for cameras to render the target textures.

	            GenerationData_Kind genData_kind = isMakingBackgrounds? GenerationData_Kind.SD_Backgrounds 
	                                                                   : GenerationData_Kind.SD_ProjTextures;
	            SD_img2img_payload payload;
	            SD_GenRequestArgs_byproducts intermediates;
	            _payload_maker.Create_img2img_payload(isMakingBackgrounds, out payload, out intermediates);

	            _generate_sender.Send_GenerateRequest(payload, OnProgressResponse, OnGeneratedResult);

	            _latestGenData = GenData2D_Maker.make_img2img(payload, intermediates, genData_kind);

	            Finalize_GenerationRequest( payload.width,  payload.height,  payload.n_iter,  
	                                        payload.batch_size, "img2img" );

	        UserCameras_Permissions.Force_KeepRenderingCameras(false);
	        RememberSentVals_forDebug(intermediates,  isImg2Img:true,  (InpaintingFill)payload.inpainting_fill );
	        onRequested?.Invoke();
	    }


	    IEnumerator Upscale_img2extra_crtn( float upscaleBy, GenData2D fromGen_canBeNull=null, 
	                                        Texture2D tex2D=null,  Action onRequested=null ){
	        if(!Start_GenerationRequest(Generate_RequestingWhat.upscale)){ yield break; }

	        if(fromGen_canBeNull == null){ //genData not provided, render the scene to submit the ViewTexture for upscale.
	            UserCameras_Permissions.Force_KeepRenderingCameras(true);
	            Objects_Renderer_MGR.instance.ReRenderAll_soon();
	            for(int i=0; i<3; ++i){ yield return null; }//give time for cameras to render the target textures.
	        }

	        SD_GenRequestArgs_byproducts intermediates = null;
	        SD_img2extra_payload payload = null;

	        if(fromGen_canBeNull != null){
	            _payload_maker.Create_upscale_payload(tex2D, upscaleBy, out payload);
	        }else{
	            _payload_maker.Create_upscale_payload(upscaleBy, out payload, out intermediates);
	        }

	        _generate_sender.Send_GenerateRequest(payload, OnProgressResponse, OnGeneratedResult);
        
	        _latestGenData = GenData2D_Maker.make_img2extra(payload, fromGen_canBeNull, intermediates);

	        Finalize_GenerationRequest( payload.rslt_imageWidths,  payload.rslt_imageHeights, 
	                                    1, 1, "img2extra", noSdxlAdvice:true );

	        RememberSentVals_forDebug(null,  isImg2Img:true,  InpaintingFill.Original);//to reset previous values.
	        onRequested?.Invoke();
	    }

    

	    //for example, detecting depth from art image (zoedepth)
	    IEnumerator Submit_CtrlDetect_crtn( SD_ControlnetDetect_payload payload, 
	                                        Action<SD_ControlnetDetect_Response> onDetected ){
        
	        if(_isGeneratingWhat!=Generate_RequestingWhat.nothing){ yield break; }
	        _isGeneratingWhat = Generate_RequestingWhat.ctrlnetDetect;

	        _generate_sender.Send_GenerateRequest(payload, OnDone);

	        Finalize_GenerationRequest( payload.width_spz, payload.height_spz,
	                                    1, 1, "ctrlDetect", noSdxlAdvice: true);

	        bool isError = false;
	        SD_ControlnetDetect_Response response = null;

	        void OnDone(UnityWebRequest req){
	            if(Finish_if_ResultError(req)){
	                isError = true;
	                response = null;
	                return;
	            }
	            // Use class-type information, to support inheritance of objects:
	            string json = req.downloadHandler.text;
	            var settings = new JsonSerializerSettings{ TypeNameHandling = TypeNameHandling.Auto, };
	            response = JsonConvert.DeserializeObject<SD_ControlnetDetect_Response>(json, settings);
	        }

	        while(response == null && !isError){ yield return null;  }

	        GenerateButtons_UI.OnConfirmed_FinishedGenerate(canceled:false);
	        _isGeneratingWhat = Generate_RequestingWhat.nothing;
	        onDetected?.Invoke(response);
	    }


	    void RememberSentVals_forDebug( SD_GenRequestArgs_byproducts intermediates, 
	                                    bool isImg2Img,  InpaintingFill fill = InpaintingFill.Original ){
	        _latestDepthTex_sent   = intermediates?.depth_disposableTex;
	        _latestScreenMask_sent = intermediates?.screenSpaceMask_WE_disposableTex;
	        _latestUsualView_used  = intermediates?.usualView_disposableTexture;
	        _latest_isImg2img = isImg2Img;
	        _latest_Fill = fill;
	    }


    
	    bool Start_GenerationRequest( Generate_RequestingWhat what ){
	        if(_isGeneratingWhat!=Generate_RequestingWhat.nothing){ return false; }//still waiting for a previous request to complete.
	        _isGeneratingWhat = what;
	        _finalPreparations_beforeGen = true;

	        var inp = SD_InputPanel_UI.instance;

	        if (inp.models.selectedModel_name == null){
	            _isGeneratingWhat = Generate_RequestingWhat.nothing;
	            _finalPreparations_beforeGen = false;
	            Viewport_StatusText.instance.ShowStatusText("No Models detected in the Input panel. Enter PlayMode only after WebUI was launched", false, 10, progressVisibility:false);
	            return false;//no models available. User should try clicking the refresh button next to dropdown.
	        }
	        if(inp.samplers?.value == null){
	            _isGeneratingWhat = Generate_RequestingWhat.nothing;
	            _finalPreparations_beforeGen = false;
	            Viewport_StatusText.instance.ShowStatusText("No Samplers detected in the Input panel", false, 10, progressVisibility:false);
	            return false; //no samplers available.
	        }
	        return true;
	    }


	    //requestCategory: txt2img, img2img, etc.
	    void Finalize_GenerationRequest( int width, int height, int n_iter, int batch_size, string requestCategory, 
	                                     bool noSdxlAdvice=false ){
	        string statusMsg = $"Generating {width} x {height} images <b>({requestCategory})</b>.  Num: {n_iter}x{batch_size}";
	        if (!noSdxlAdvice){
	            apppend_sdxl_ctrlnet_advice_maybe( ref statusMsg );
	            append_sdxl_size_advice_maybe(ref statusMsg );
	        }
	        Viewport_StatusText.instance.ReportProgress(0);
	        Viewport_StatusText.instance.ShowStatusText(statusMsg, false, 999999, progressVisibility:true );

	        GenerateButtons_UI.OnConfirmed_StartedGenerate();

	        _finalPreparations_beforeGen = false;
	        _isGeneratingWhat = _isGeneratingWhat;//kept the SAME (waiting for results).
	    }



	    //check the names of the selected models.
	    //If base input model contains XL in its name, then we want all the Depth or Normal ctrlNetUnits to have XL in their name as well.
	    void apppend_sdxl_ctrlnet_advice_maybe(ref string currMsg_){
	        string sd_model = SD_InputPanel_UI.instance.models.selectedModel_name;
	        List<string> ctrl_models = SD_ControlNetsList_UI.instance.curentModels_of_DepthOrNormal_units();

	        bool sd_likely_sdxl = sd_model.ToLower().Contains("xl");
	        bool mismatch=false;
        
	        for(int i=0; i<ctrl_models.Count; ++i){
	            string unitModelName = ctrl_models[i];
	            bool ok =  sd_likely_sdxl == unitModelName.ToLower().Contains("xl");
	            if(ok){ continue; }
	            mismatch=true; 
	            break;
	        }
	        if(!mismatch){ return; }
	        currMsg_ += sd_likely_sdxl? "\nCareful: your Input Model name mentions <b>XL</b>, but some CTRL Nets don't."
	                                   :"\nCareful: your Input Model name doesn't mention <b>XL</b>, but some CTRL Nets do.";
	    }


	    void append_sdxl_size_advice_maybe(ref string currMsg_){
	        var inp = SD_InputPanel_UI.instance;
	        string sd_model = inp.models.selectedModel_name;
	        bool sd_likely_sdxl = sd_model.ToLower().Contains("xl");
	        if(!sd_likely_sdxl){ return; }
	        if(inp.width > 768 || inp.height > 768){ return; }
	        currMsg_ += "\nCareful: your Input Model name mentions <b>XL</b>, but Width and Height is less than 1024.";
	    }


	    void OnProgressResponse( UnityWebRequest request ){

	        Objects_Renderer_MGR.instance.ReRenderAll_soon();

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

	       // Format a string with all the relevant information
	        string debugMessage = string.Format(
	            "Progress: {0:P2}, ETA: {1} seconds\n" +
	            "Job: {2}, Job Count: {3}, Job No: {4}, Job Timestamp: {5}\n" +
	            "Sampling Step: {6} / {7}\n" +
	            "State: Skipped - {8}, Interrupted - {9}\n" +
	            "Text Info: {10}",
	            progressResponse.progress, // P2 format specifier for percentage
	            progressResponse.eta_relative,
	            progressResponse.state.job,
	            progressResponse.state.job_count,
	            progressResponse.state.job_no,
	            progressResponse.state.job_timestamp,
	            progressResponse.state.sampling_step,
	            progressResponse.state.sampling_steps,
	            progressResponse.state.skipped,
	            progressResponse.state.interrupted,
	            progressResponse.textinfo ?? "N/A" // Handling null case
	        );
	        //Debug.Log(debugMessage);
	        if(progressResponse.current_image == null || progressResponse.current_image==""){ return; }

	        //using ? in case SD had exception
	        _latestGenData?.Update_PendingImages( progressResponse.state.job_no,  progressResponse.current_image ); 

	        float progressTotal = progressResponse.progress;
	        Viewport_StatusText.instance.ReportProgress( progressTotal );

	        string etaStr = Mathf.RoundToInt(progressResponse.eta_relative).ToString() + " sec";
	        bool isTextETA = true;
	        Viewport_StatusText.instance.ShowStatusText(etaStr, isTextETA, textVisibleDur:999999, progressVisibility:true );
	    }



	    void OnGeneratedResult( UnityWebRequest result){

	        GenerateButtons_UI.OnConfirmed_FinishedGenerate(canceled:false);

	        if(Finish_if_ResultError(result)){ return; }
        
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();

	        // Use class-type information, to support inheritance of objects:
	        string json = result.downloadHandler.text;
	        var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, };
	        SD_txt2imgResponse response = JsonConvert.DeserializeObject<SD_txt2imgResponse>(json, settings);

	        _latestGenData?.Complete_PendingImages( response.images ); //using ? in case SD had exception
	        _latestGenData = null;

	        int numGenerations = PlayerPrefs.GetInt("numArtGenerated", 0);
	        string genCompleted_text = numGenerations < 20?  "Done! :)   Go to <b>Art</b> Tab, and right click the icon, to adjust."
	                                                       : "Generation Completed";
	        PlayerPrefs.SetInt("numArtGenerated", numGenerations+1);
	        PlayerPrefs.Save();
	        Viewport_StatusText.instance.ShowStatusText(genCompleted_text, false, 4, progressVisibility:false);

	        _isGeneratingWhat = Generate_RequestingWhat.nothing;
	    }


	    bool Finish_if_ResultError( UnityWebRequest result ){
	        string json = "";
	        if(result!=null && result.downloadHandler != null){ 
	            json = result.downloadHandler.text; 
	        }
	        bool err =  result.result==UnityWebRequest.Result.ConnectionError 
	                 || result.result==UnityWebRequest.Result.ProtocolError;

	        if(err || json==""){
	            var jsonLow = json.ToLower();
	            json += jsonLow.Contains("cannot be multiplied") || jsonLow.Contains("server error") ?  
	                        " ..Maybe you are mixing SDXL model with SD 1.5 Controlnet?"
	                        : "";
	            Viewport_StatusText.instance.ShowStatusText("Error: " + json, false, 15, progressVisibility:false);
	            _latestGenData?.Complete_PendingImages( null ); //using ? in case SD had exception
	            _isGeneratingWhat = Generate_RequestingWhat.nothing;
	            return true; 
	        }
        
	        if(json == "{}"){
	            OnFinishTheInterrupt();
	            _latestGenData?.Complete_PendingImages(null); //using ? in case SD had exception
	            _isGeneratingWhat = Generate_RequestingWhat.nothing;
	            return true;
	        }
	        return false; //no errors, all as expected
	    }

    

	    public void OnStopGenerate_Button(){
	        _generate_sender.Send_StopGenerateRequest();
	        float gracePeriod = 10;//wait at least 10 sec from server. If no response, then our coroutine will perform clean-up.
	        _finishTheInterrupt_ifStuck_crtn = StartCoroutine( FinishTheInterrupt_ifStuck(gracePeriod) );
	        GenerateButtons_UI.OnConfirmed_FinishedGenerate(canceled:true);
	        Viewport_StatusText.instance.ShowStatusText("Cancelling the generation...", false, gracePeriod, progressVisibility: false);
	    }


	    Coroutine _finishTheInterrupt_ifStuck_crtn = null;
	    IEnumerator FinishTheInterrupt_ifStuck(float graceDelay=10){
	        yield return new WaitForSeconds(graceDelay);
	        _finishTheInterrupt_ifStuck_crtn = null;//set null BEFORE finishInterrupt().
	        OnFinishTheInterrupt();
	    }


	    void OnFinishTheInterrupt(){
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();

	        if(_finishTheInterrupt_ifStuck_crtn!=null){ 
	            StopCoroutine(_finishTheInterrupt_ifStuck_crtn); 
	            _finishTheInterrupt_ifStuck_crtn=null; 
	        }
	        GenData2D_Archive.instance.OnTerminatedGeneration(_latestGenData);
	        _latestGenData = null;
	        _isGeneratingWhat = Generate_RequestingWhat.nothing;
	        _generationCooldownUntil = Time.unscaledTime + _generationCooldown;
	        Viewport_StatusText.instance.ShowStatusText("Interrupted the generation.", false, 3, progressVisibility: false);
	    }

	}
}//end namespace
