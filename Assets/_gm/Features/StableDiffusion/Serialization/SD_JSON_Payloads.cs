using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;



namespace spz {

	[Serializable] //arrives from stable diffusion back into StableProjectorz
	public class SD_txt2imgResponse{
	    public string[] images;//Base64 image string
	    public object parameters;//won't parse this currently
	    public string info;
	}


	[Serializable] //arrives from stable diffusion back into StableProjectorz
	public class SD_Generate_ProgressResponse{
	    public float progress;
	    public float eta_relative;
	    public SD_GenProgressResponseState state;
	    public string current_image; //Base64 image string
	    public string textinfo;
	}

	[Serializable] //arrives from stable diffusion back into StableProjectorz
	public class SD_GenProgressResponseState{
	    public bool skipped;
	    public bool interrupted;
	    public string job;
	    public int job_count;
	    public string job_timestamp;
	    public int job_no;
	    public int sampling_step;
	    public int sampling_steps;
	}


	[Serializable]
	public class SD_txt2img_payload{
	    public string prompt;
	    public string negative_prompt;
	    public string sampler_name;
	    public string scheduler;
	    public int batch_size;
	    public int n_iter;
	    public int steps;
	    public float cfg_scale;
	    public int apparentFinalWidth() => width;
	    public int width;
	    public int apparentFinalHeight() => height;
	    public int height;

	    public int seed;
	    public string refiner_checkpoint;
	    public float refiner_switch_at;

	    // NOTICE: To truly permit tiling, webui wants tiling to also be enabled via Options, that are to be sent separately.
	    public bool tiling;
    
	    //high-res fix (upscaler)
	    public bool enable_hr;
	    public string hr_upscaler;
	    public string hr_sampler_name = "Euler";
	    public float hr_scale=1; //by how much to increase image resolution.
	    public float denoising_strength = 0;//needed for upscaler. https://github.com/AUTOMATIC1111/stable-diffusion-webui/discussions/13297#discussioncomment-7030902
	    public int hr_second_pass_steps;

	    public Dictionary<string,AlwaysOn_Value> alwayson_scripts; //https://github.com/Mikubill/sd-webui-controlnet/wiki/API#integrating-sdapiv12img
	    // Add other fields as needed
	    public SD_txt2img_payload Clone(){
	        var clone = (SD_txt2img_payload)this.MemberwiseClone();
	        clone.alwayson_scripts = this.alwayson_scripts.ToDictionary(kvp=>kvp.Key, kvp=>kvp.Value.Clone() );
	        return clone;
	    }
	}


	[Serializable]
	public class SD_img2img_payload{
	    public string prompt;
	    public string negative_prompt;
	    public string sampler_name;
	    public string scheduler;
	    public int batch_size;
	    public int n_iter;
	    public int steps;
	    public float cfg_scale;
	    public int apparentFinalWidth() => width;
	    public int width;
	    public int apparentFinalHeight() => height;
	    public int height;

	    public bool enable_hr_spz;//ignored by webui, but helps us inside StableProjectorz.

	    public float hr_scale_spz = 1;//ignored too.  Instead, width and height will be used by webui.
	                                  //the upscaler for img2img is set inside Options, that were sent separatelly.
	    public int seed;
	    public string refiner_checkpoint;
	    public float refiner_switch_at;

	    // NOTICE: To truly permit tiling, webui wants tiling to also be enabled via Options, that are to be sent separately.
	    public bool tiling;

	    public int inpainting_fill; //https://github.com/AUTOMATIC1111/stable-diffusion-webui/discussions/5529#discussion-4639917
	    public int inpaint_full_res; //Inpaint area (Whole picture=0, only masked=1) https://github.com/AUTOMATIC1111/stable-diffusion-webui/discussions/9739#discussioncomment-5658646
	    public int inpaint_full_res_padding; //Only masked padding, pixels.
	    public int inpainting_mask_invert; //inpaint area:  0 inpaint masked, 1 inpaint whole image
	    public int mask_blur = 8; //8 because user can use soft brush manually, inside StableProjectorz. Don't put zero - I think it messes up Soft Inpaint (July 2024)
	    public float denoising_strength;
	    public bool include_init_images = true; //https://blog.runpod.io/runpod-partners-with-randomseed-to-provide-accessible-user-friendly-stable-diffusion-api-access/
	    public string[] init_images; // Base64-encoded initial images
	    public string mask; // Base64-encoded mask

	    // https://github.com/Mikubill/sd-webui-controlnet/wiki/API#integrating-sdapiv12img
	    // https://github.com/AUTOMATIC1111/stable-diffusion-webui/discussions/15138#discussioncomment-8686916
	    public Dictionary<string, AlwaysOn_Value> alwayson_scripts;

	    public SD_img2img_payload Clone(){
	        var clone = (SD_img2img_payload)this.MemberwiseClone();
	        clone.alwayson_scripts = this.alwayson_scripts.ToDictionary(kvp=>kvp.Key, kvp=>kvp.Value.Clone() );
	        return clone;
	    }
	}



	// For manually running a preprocessor.
	// For example, to detect depth from a usual image
	[Serializable]
	public class SD_ControlnetDetect_payload{
	    public string controlnet_module = "none";
	    public string[] controlnet_input_images;
	    public int controlnet_processor_res = -1;
	    public float controlnet_threshold_a = 0.5f;
	    public float controlnet_threshold_b = 0.5f;
	    public int width_spz;
	    public int height_spz;
	}


	[SerializeField]
	public class SD_ControlnetDetect_Response{
	    public string[] images;//Base64 image strings
	}



	// For upscaling existing image.
	// From this example https://gist.github.com/harfqaf/7cf1f4a507ad6cdbff7bffc8f8cdefea
	[Serializable]
	public class SD_img2extra_payload{
	    public int resize_mode = 0;
	    public float gfpgan_visibility = 0;
	    public float codeformer_visibility = 0;
	    public float codeformer_weight = 0;
	    public float upscaling_resize = 2;
	    public string upscaler_1 = "R-ESRGAN 4x+";
	    public string upscaler_2 = "None";
	    public float extras_upscaler_2_visibility = 0;
	    public bool upscale_first = false;// upscale before restoring faces (not applied if visibility = 0?).
	    public List<SD_Img2Extra_Image> imageList;
	    public int rslt_imageWidths;//ignored by webui, but needed inside StableProjectorz.
	    public int rslt_imageHeights;
	    public SD_img2extra_payload Clone(){
	        var clone = (SD_img2extra_payload)this.MemberwiseClone();
	        return clone;
	    }
	}

	[Serializable]
	public class SD_Img2Extra_Image{
	    public string data; //base64 image
	    public string name;
	}


	// will be placed inside the 'alwayson_scripts' dictionary of 'img2img' or 'txt2img' payload.
	public abstract class AlwaysOn_Value{
	    public abstract AlwaysOn_Value Clone();
	}


	// Describes parameters we currently want for some specific ControlNet Unit.
	// Documentation explaning the arguments:
	// https://github.com/Mikubill/sd-webui-controlnet/wiki/API#controlnetunitrequest-json-object
	[Serializable]
	public class ControlNetUnit_NetworkArgs{
	    public bool enabled = true;//A1111 sd-webui-controlnet requires this (introduced since May 2024).
    
	    public string image; //Forge (new, since March 2024) for example depth, if this control-net-unit is using depth
	    public string image_fg;
    
	    // Commented out for now Sept 2024. Not needed by A1111 nor Forge:
	    //
	    //   public string image_mask = null; //old, for compatibility with other webuis - before Aug 2024 (when Flux was created)
	    //   public object mask_image = null;
	    //   public object mask_image_fg = null;

	    public string resize_mode = "Scale to Fit (Inner Fit)";//Useful if input_image is texture loaded from disk, for style-controlnet, etc.
	    public string module = "None";//preprocessor.
	    public string model ="None"; //actual neural net model
	    public float weight = 1;
	    public bool low_vram = false;
	    public float processor_res = -1;
	    public float threshold_a = 0.5f;// -1 is default BUT Most expect 0.5 (like Depth, Normalbae, or preprocessor that's reference_only) and -1 doesn't really work well for them.
	    public float threshold_b = 0.5f;// But some might expect other specific values. This can be seen from Browser, beneath generated image.
	    public float guidance_start = 0.0f;
	    public float guidance_end = 1.0f;
	    public string control_mode = "Balanced";
	    public bool pixel_perfect = false;
	    // new vars since Aug 2024 (when Flux was created)
	    public string batch_image_dir = "";
	    public object batch_input_gallery = null;
	    public string batch_mask_dir = "";
	    public object batch_mask_gallery = null;
	    public object generated_image = null;
	    public string hr_option = "Both";
	    public string input_mode = "simple";
	    public bool save_detected_map = true;
	    public bool use_preview_as_input = false;
	    public ControlNetUnit_NetworkArgs Clone() => (ControlNetUnit_NetworkArgs)this.MemberwiseClone();
	}



	[Serializable]
	public class ControlNet_NetworkArgs : AlwaysOn_Value{//arguments of all control net units.
	    public ControlNetUnit_NetworkArgs[] args;
	    public override AlwaysOn_Value Clone(){
	        var clone  = (ControlNet_NetworkArgs)this.MemberwiseClone();
	        clone.args = args.Select(a=>a.Clone()).ToArray();
	        return clone;
	    }
	}


	//parameters for using the Soft Inpainting (Differential Inpainting).
	[Serializable]
	public class SoftInpaintingArgsEntry{
	    [JsonProperty("Soft inpainting")] public bool Soft_inpainting = true;
	    [JsonProperty("Schedule bias")]   public float Schedule_bias = 1;
	    [JsonProperty("Preservation strength")]     public float Preservation_strength = 0.5f;
	    [JsonProperty("Transition contrast boost")] public float Transition_contrast_boost = 4;
	    [JsonProperty("Mask influence")]      public float Mask_influence = 0;
	    [JsonProperty("Difference threshold")]public float Difference_threshold = 0.5f;
	    [JsonProperty("Difference contrast")] public float Difference_contrast = 2;
	    public SoftInpaintingArgsEntry Clone() => (SoftInpaintingArgsEntry)this.MemberwiseClone();
	}

	[Serializable]
	public class SoftInpaintingArgs : AlwaysOn_Value{
	    public SoftInpaintingArgsEntry[] args;
	    public override AlwaysOn_Value Clone(){
	        var clone  = (SoftInpaintingArgs)this.MemberwiseClone();
	        clone.args = args.Select(a=>a.Clone()).ToArray();
	        return clone;
	    }
	}



	// Textures that were created during collection of various arguments for the generate-request.
	// We keep them here, because it's pitty to destroy them right away while collecting arguments.
	// They can be reused, displayed, etc.
	public class SD_GenRequestArgs_byproducts{
	    public Texture2D usualView_disposableTexture = null;//optional. Whatever was visible to the Content camera (in color).
	    public Texture2D depth_disposableTex = null;//optional, can be useful for control nets
	    public Texture2D viewNormals_disposableTex = null;
	    public Texture2D vertexColors_disposableTex = null;
	    public Texture2D screenSpaceMask_NE_disposableTex = null;//optional, can be useful for image2image, inpainting. (NE = no anti-edge applied).
	    public Texture2D screenSpaceMask_WE_disposableTex = null;//optional, can be useful for image2image, inpainting. (WE= With anti-edge)
	    public bool isScreenMask_forSoftInpaint = false; //was our 'screenSpaceMask_disposableTex' sent to SD for soft inpaint, or for usual.

	    public void Dispose(){
	        DisposeTexture_maybe(usualView_disposableTexture);
	        DisposeTexture_maybe(depth_disposableTex);
	        DisposeTexture_maybe(viewNormals_disposableTex);
	        DisposeTexture_maybe(vertexColors_disposableTex);
	        DisposeTexture_maybe(screenSpaceMask_NE_disposableTex);
	        DisposeTexture_maybe(screenSpaceMask_WE_disposableTex);
	    }

	    void DisposeTexture_maybe(Texture tex){
	        if(tex == null){ return; }
	        RenderTexture asRT = tex as RenderTexture;
	        if(asRT != null){ asRT.Release(); }
	        GameObject.DestroyImmediate(tex);
	    }

	    public SD_GenRequestArgs_byproducts Clone(){
	        var clone = (SD_GenRequestArgs_byproducts)this.MemberwiseClone();
	        TextureTools_SPZ.Clone_Tex2D(usualView_disposableTexture, ref clone.usualView_disposableTexture);
	        TextureTools_SPZ.Clone_Tex2D(depth_disposableTex, ref clone.depth_disposableTex);
	        TextureTools_SPZ.Clone_Tex2D(viewNormals_disposableTex, ref clone.viewNormals_disposableTex);
	        TextureTools_SPZ.Clone_Tex2D(vertexColors_disposableTex, ref clone.vertexColors_disposableTex);
	        TextureTools_SPZ.Clone_Tex2D(screenSpaceMask_NE_disposableTex, ref clone.screenSpaceMask_NE_disposableTex);
	        TextureTools_SPZ.Clone_Tex2D(screenSpaceMask_WE_disposableTex, ref clone.screenSpaceMask_WE_disposableTex);
	        return clone;
	    }
    

	    public SD_GenRequest2D_args_byproducts_SL Save(StableProjectorz_SL spz, Guid generationGuid){
	        var saveLoad = new SD_GenRequest2D_args_byproducts_SL();
	        // Save each Texture2D and record its path:
	        if (usualView_disposableTexture != null){ 
	            string pathInDataFolder = "usualView_" +generationGuid+".png";
	            ProjectSaveLoad_Helper.Save_Tex2D_To_DataFolder(usualView_disposableTexture, spz.filepath_dataDir, pathInDataFolder);
	            saveLoad.usualView = pathInDataFolder;
	        }
	        if(depth_disposableTex != null){ 
	            string pathInDataFolder = "depth_" + generationGuid + ".png";
	            ProjectSaveLoad_Helper.Save_Tex2D_To_DataFolder(depth_disposableTex, spz.filepath_dataDir, pathInDataFolder);
	            saveLoad.depth = pathInDataFolder;
	        }
	        if(viewNormals_disposableTex != null){ 
	            string pathInDataFolder = "vnorms_" + generationGuid + ".png";
	            ProjectSaveLoad_Helper.Save_Tex2D_To_DataFolder(viewNormals_disposableTex, spz.filepath_dataDir, pathInDataFolder);
	            saveLoad.viewNormals = pathInDataFolder;
	        }
	        if(vertexColors_disposableTex != null){
	            string pathInDataFolder = "vcols_" + generationGuid + ".png";
	            ProjectSaveLoad_Helper.Save_Tex2D_To_DataFolder(vertexColors_disposableTex, spz.filepath_dataDir, pathInDataFolder);
	            saveLoad.vertexColors = pathInDataFolder;
	        }
	        if (screenSpaceMask_NE_disposableTex != null){ 
	            string pathInDataFolder = "screenSpaceMask_NE_" + generationGuid + ".png"; 
	            ProjectSaveLoad_Helper.Save_Tex2D_To_DataFolder(screenSpaceMask_NE_disposableTex, spz.filepath_dataDir, pathInDataFolder);
	            saveLoad.screenSpaceMask_NE = pathInDataFolder;
	        }
	        if (screenSpaceMask_WE_disposableTex != null){ 
	            string pathInDataFolder = "screenSpaceMask_WE_" + generationGuid + ".png"; 
	            ProjectSaveLoad_Helper.Save_Tex2D_To_DataFolder(screenSpaceMask_WE_disposableTex, spz.filepath_dataDir, pathInDataFolder);
	            saveLoad.screenSpaceMask_WE = pathInDataFolder;
	        }
	        return saveLoad;
	    }

	    public void Load(StableProjectorz_SL spz, SD_GenRequest2D_args_byproducts_SL byproductsSL){
	        var r8g8b8a8 = GraphicsFormat.R8G8B8A8_UNorm;
	        usualView_disposableTexture = ProjectSaveLoad_Helper.Load_Texture2D_from_DataFolder( spz.filepath_dataDir, byproductsSL.usualView,
	                                                                                           r8g8b8a8, r8g8b8a8);
        
	        depth_disposableTex = ProjectSaveLoad_Helper.Load_Texture2D_from_DataFolder( spz.filepath_dataDir, byproductsSL.depth, 
	                                                                                   r8g8b8a8, r8g8b8a8);

	        screenSpaceMask_NE_disposableTex = ProjectSaveLoad_Helper.Load_Texture2D_from_DataFolder( spz.filepath_dataDir, byproductsSL.screenSpaceMask_NE,
	                                                                                                  r8g8b8a8, r8g8b8a8);

	        screenSpaceMask_WE_disposableTex = ProjectSaveLoad_Helper.Load_Texture2D_from_DataFolder( spz.filepath_dataDir, byproductsSL.screenSpaceMask_WE,
	                                                                                                  r8g8b8a8, r8g8b8a8);

	        viewNormals_disposableTex = ProjectSaveLoad_Helper.Load_Texture2D_from_DataFolder(spz.filepath_dataDir, byproductsSL.viewNormals,
	                                                                                        r8g8b8a8, r8g8b8a8);

	        vertexColors_disposableTex = ProjectSaveLoad_Helper.Load_Texture2D_from_DataFolder(spz.filepath_dataDir, byproductsSL.vertexColors,
	                                                                                         r8g8b8a8, r8g8b8a8);
	    }
	}

}//end namespace
