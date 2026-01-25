using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace spz {

	public class SD_Generate_PayloadMaker : MonoBehaviour{
    
	    public void Create_txt2img_payload( out SD_txt2img_payload payload_,
	                                        out SD_GenRequestArgs_byproducts intermediates_){
	        var input = SD_InputPanel_UI.instance;
	        string samplerName = input.samplers.value?.name??"";
	        string scheduler = input.scheduler.value?.name??"";

	        intermediates_ = new SD_GenRequestArgs_byproducts();

	        string positivePrompt = StableDiffusion_Prompts_UI.instance.positivePrompt;
	        string negativePrompt = StableDiffusion_Prompts_UI.instance.negativePrompt;
	        PostProcess_Prompt(ref positivePrompt, ref negativePrompt);

	        payload_ = new SD_txt2img_payload{
	            prompt = positivePrompt,
	            negative_prompt = negativePrompt,
	            sampler_name = samplerName,
	            scheduler = scheduler,
	            batch_size = Mathf.RoundToInt(input.batch_size),
	            n_iter = Mathf.RoundToInt(input.batch_count),
	            steps = Mathf.RoundToInt(input.sampleSteps_slider.value),
	            cfg_scale = input.CFG_scale_slider.value,
	            width = Mathf.RoundToInt(input.width),
	            height = Mathf.RoundToInt(input.height),
	            seed = input.seed_intField.recentVal > 0 ? input.seed_intField.recentVal : UnityEngine.Random.Range(0, int.MaxValue),//manual (not -1), so we can show it in our icon instead of -1.

	            //webui also wants tiling to be enabled via SD_Options, that are sent separately.
	            //This has to be sent too though (else tiling might remain enabled at current webui version, May 2024)
	            tiling = SD_WorkflowOptionsRibbon_UI.instance.isTileable,

	            // Nov 2024: Turned off, - user will manually press x2 and x4 to upscale images using the img2extra url.
	            //   refiner_checkpoint = SD_Refiner.instance.selectedModel_name,
	            //   refiner_switch_at = SD_Refiner.instance.switchAt01,
	            //   enable_hr = SD_Upscalers.instance.selectedUpscaler_name != "",
	            //   hr_upscaler = SD_Upscalers.instance.selectedUpscaler_name,
	            //   hr_sampler_name = samplerName, //same as the base-model sampler. https://github.com/AUTOMATIC1111/stable-diffusion-webui/issues/8587#issuecomment-1468865769
	            //   hr_scale = SD_Upscalers.instance.upscaleBy,
	            //   hr_second_pass_steps = SD_Upscalers.instance.highresSteps,
	            //   denoising_strength = SD_Upscalers.instance.denoiseStrength,

	            alwayson_scripts = new Dictionary<string,AlwaysOn_Value>(),
	        };
       
	        ControlNet_NetworkArgs ctrlNets_args = SD_ControlNetsList_UI.instance.GetArgs_forGenerationRequest(intermediates_);
	        if (ctrlNets_args.args.Length > 0) {
	            payload_.alwayson_scripts.Add("controlnet", ctrlNets_args);//https://github.com/Mikubill/sd-webui-controlnet/wiki/API#examples-1
	        }
	    }


    
	    public void Create_img2img_payload( bool isMakingBackgrounds,  out SD_img2img_payload payload_, 
	                                        out SD_GenRequestArgs_byproducts intermediates_ ){
	        Texture2D screenMask_skipAntiEdge;
	        Texture2D screenMask_withAntiEdge;
	        Texture2D viewTex;
	        InpaintingFill inpaint_fill;
	        float denoise_strength;
	        img2img_GetTextures_andFill( forceFullWhiteMask:isMakingBackgrounds,  out screenMask_skipAntiEdge, out screenMask_withAntiEdge,
	                                     out viewTex, out inpaint_fill, out denoise_strength);

	        intermediates_ =  new SD_GenRequestArgs_byproducts{
	            screenSpaceMask_NE_disposableTex = screenMask_skipAntiEdge,
	            screenSpaceMask_WE_disposableTex = screenMask_withAntiEdge,//so that we can use it later, during projections etc.
	            usualView_disposableTexture   = viewTex,
	        };
        
	        string positivePrompt = StableDiffusion_Prompts_UI.instance.positivePrompt;
	        string negativePrompt = StableDiffusion_Prompts_UI.instance.negativePrompt;
	        PostProcess_Prompt(ref positivePrompt, ref negativePrompt);

	        var input = SD_InputPanel_UI.instance;

	        payload_ = new SD_img2img_payload {
	            prompt = positivePrompt,
	            negative_prompt = negativePrompt,
	            sampler_name = input.samplers.value?.name??"",
	            scheduler = input.scheduler.value?.name??"",
	            batch_size = Mathf.RoundToInt(input.batch_size),
	            n_iter = Mathf.RoundToInt(input.batch_count),
	            steps  = Mathf.RoundToInt(input.sampleSteps_slider.value),
	            cfg_scale = input.CFG_scale_slider.value,
	            seed   = input.seed_intField.recentVal>0?  input.seed_intField.recentVal : Random.Range(0, int.MaxValue),//manual (not -1), so we can show it in our icon instead of -1.

	            width  = Mathf.RoundToInt(input.width),
	            height = Mathf.RoundToInt(input.height),

	            // Nov 2024: Turned off, - user will manually press x2 and x4 to upscale images using the img2extra url.
	            //     width = Mathf.RoundToInt(input.width * SD_Upscalers.instance.upscaleBy),
	            //     height = Mathf.RoundToInt(input.height * SD_Upscalers.instance.upscaleBy),
	            //
	            //     webui wants Upscaler for img2img to be sent via Options, which we do separatelly.
	            //     So will be ignored by webui, but will help us later on, inside StableProjectorz.
	            //     enable_hr_spz = SD_Upscalers.instance.selectedUpscaler_name != "",
	            //     hr_scale_spz = SD_Upscalers.instance.upscaleBy,
	            //     
	            //     refiner_checkpoint = SD_Refiner.instance.selectedModel_name,
	            //     refiner_switch_at = SD_Refiner.instance.switchAt01,

	            //webui also wants tiling to be enabled via Options, that are sent separately.
	            //This has to be sent too though (else tiling might remain enabled at current webui version, May 2024)
	            tiling = SD_WorkflowOptionsRibbon_UI.instance.isTileable, 

	            inpaint_full_res = (int)0,//whole picture always. User can zoom up if they need to.
	            inpainting_mask_invert = (int)0, //always mask inside the mask, don't invert. Jul 2024.

	            inpaint_full_res_padding = 0, //how many pixels to add to the mask.  Note, in case of entireShape, silhuette was already dilated by correct number of pixels.
	                                          //For brushed masks, padding is undesirable, could mess up around brushed borders in StableProjectorz (in projection shader)
	            include_init_images = true,
	            init_images = new string[]{  TextureTools_SPZ.TextureToBase64(intermediates_.usualView_disposableTexture), },
	            mask = screenMask_skipAntiEdge == null?"" : TextureTools_SPZ.TextureToBase64(screenMask_skipAntiEdge), //send the SKIP-anti-edge. Avoids revealing any black untextured areas to SD.
	            alwayson_scripts = new Dictionary<string,AlwaysOn_Value>(),

	            mask_blur = 0,//ZERO. we don't want to add blur - we probably already added to the mask before by our BlurTextures_MGR.
	                          //Might mess up the blending later
	            denoising_strength = denoise_strength,

	            inpainting_fill = (int)inpaint_fill,
	        };

	        // Avoid softInpaint if rendering 'EntireShape' (when we have background active).
	        // We will use LatentNothing, and soft inpaint doesn't work with it.
	        // For more info - see comment inside img2img_GetTextures_andFill().
	        SoftInpaintingArgs softInpaint_args =  WorkflowRibbon_UI.instance.is_allow_SoftInpaint() ?
	                                                 Inpaint_MaskPainter.instance.GetArgs_for_SoftInpaint_GenRequest() : null;
	        if (softInpaint_args != null){
	            payload_.alwayson_scripts.Add("Soft Inpainting", softInpaint_args);
	            intermediates_.isScreenMask_forSoftInpaint = true;
	        }
	        ControlNet_NetworkArgs ctrlNets_args = SD_ControlNetsList_UI.instance.GetArgs_forGenerationRequest(intermediates_);
	        if(ctrlNets_args.args.Length > 0){ 
	            payload_.alwayson_scripts.Add("controlnet", ctrlNets_args);//https://github.com/Mikubill/sd-webui-controlnet/wiki/API#examples-1
	        }
	    }



	    public void Create_upscale_payload( Texture2D imgForSending, float upscaleBy, 
	                                        out SD_img2extra_payload payload_){
	        make_upscale_payload(imgForSending, upscaleBy, out payload_);
	    }

	    // requests a view texture and uses that for upscaing
	    public void Create_upscale_payload( float upscaleBy, 
	                                        out SD_img2extra_payload payload_, 
	                                        out SD_GenRequestArgs_byproducts byproducts){
	        Texture2D viewTex = UserCameras_MGR.instance.camTextures.GetDisposable_ContentCamTexture();
	        make_upscale_payload(viewTex, upscaleBy, out payload_);
	        byproducts = new SD_GenRequestArgs_byproducts();
	        byproducts.usualView_disposableTexture = viewTex;

	        var painter  = Inpaint_MaskPainter.instance;
	        Texture2D screenMask_skipAntiEdge_;
	        Texture2D screenMask_withAntiEdge_;

	        // If we are in projectionMask, request full white mask. Otherwise it would remain black.
	        // Else, proceed with whatever is needed (TotalObj with anti-edge, etc).
	        var currMode = WorkflowRibbon_UI.instance.currentMode();
	        bool forceFullWhite =  currMode== WorkflowRibbon_CurrMode.ProjectionsMasking;
	        painter.GetDisposable_ScreenMask( forceFullWhite:forceFullWhite,
	                                          out screenMask_skipAntiEdge_, out screenMask_withAntiEdge_);

	        byproducts.screenSpaceMask_NE_disposableTex = screenMask_skipAntiEdge_;
	        byproducts.screenSpaceMask_WE_disposableTex = screenMask_withAntiEdge_;//so that we can use it later, during projections etc.
	    }


	    void make_upscale_payload(Texture2D tex, float upscaleBy, out SD_img2extra_payload payload_){
	        var imgEntry = new SD_Img2Extra_Image(){  
	            data = TextureTools_SPZ.TextureToBase64(tex), 
	            name = "0"
	        };
	        string upscaler_name = SD_Upscalers.instance.selectedUpscaler_name;
	               upscaler_name = string.IsNullOrEmpty(upscaler_name) ? "R-ESRGAN 4x+" : upscaler_name;

	        payload_ = new SD_img2extra_payload{
	            upscaling_resize = upscaleBy,
	            upscaler_1 = upscaler_name,
	            imageList = new List<SD_Img2Extra_Image>{ imgEntry },
	            rslt_imageWidths  = Mathf.RoundToInt(tex.width * upscaleBy),
	            rslt_imageHeights = Mathf.RoundToInt(tex.height * upscaleBy),
	        };
	    }

    
	    // If we have bg, we will produce screenMask of the entireShape.
	    void img2img_GetTextures_andFill( bool forceFullWhiteMask, 
	                                      out Texture2D screenMask_skipAntiEdge_, out Texture2D screenMask_withAntiEdge_,
	                                      out Texture2D viewTex_, 
	                                      out InpaintingFill inpaint_fill_,  
	                                      out float denoiseStrength_ ){
	        var camerasMGR = UserCameras_MGR.instance;
	        var painter    = Inpaint_MaskPainter.instance;

	        viewTex_    = camerasMGR.camTextures.GetDisposable_ContentCamTexture();

	        painter.GetDisposable_ScreenMask( forceFullWhite:forceFullWhiteMask, 
	                                          out screenMask_skipAntiEdge_, out screenMask_withAntiEdge_ );
        
	        inpaint_fill_ = WorkflowRibbon_UI.instance.Get_InpaintFill();
        
	        //inpainting controls aren't available for any mode that's not original. In that case, always denoise by 100%:
	        denoiseStrength_ =  inpaint_fill_==InpaintingFill.Original?  
	                                SD_WorkflowOptionsRibbon_UI.instance.denoisingStrength : 1.0f;
	    }



	    void PostProcess_Prompt(ref string positive, ref string negative){
	        negative += Settings_MGR.instance.get_avoid_NSFW_generations() ? 
	                       ", NSFW, sex, porn, penis, vagina"//don't allow (adding to negative prompt)
	                      : ""; //allow
	    }
	}
}//end namespace
