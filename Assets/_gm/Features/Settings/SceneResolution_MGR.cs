using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace spz {

	public class SceneResolution_MGR : MonoBehaviour{
	    [Space(10)]
	    [SerializeField] TextMeshProUGUI _save_texResQuality_text;
	    [SerializeField] Button _sub_texResolutionQuality;
	    [SerializeField] Button _add_texResolutionQuality;
	    [SerializeField] Toggle _textureFilterPoint_toggle;
	    [SerializeField] Toggle _textureFilterBilinear_toggle;

	    int _memorized_res = -1;//usually used before we reduce resolution, during generation, to help performance.
	    int _memorized_res_b4_projectSave;
	    int _memorized_res_b4_pixels_boost;//when generating we INCREASE resolution to 5k for small moment, to avoid blurring when inpainting.

	    //determines resolution/quality of texture that we'll save to file.
	    // Also affects the quality of projections (ProjectionCameras.instance.accomulation_RT)
	    public static int resultTexQuality { get; private set; } = 2048;
	    public bool _isSavingProject_keepResolution4k { get; private set; } = false;
	    public bool _isWillGenArt_keepResolution5k { get; private set; } = false;
	    public static FilterMode resultTexFilterMode { get; private set; } = FilterMode.Bilinear;

	    public bool HasMemorizeRes() 
	        => _memorized_res != -1;
    

	    public void RevertRes_from_Memorized(){
	        if(!HasMemorizeRes()){ return; }
	        OnAdd_texResolutionQuality(true, force_pickThisRes:_memorized_res);
	        _memorized_res = -1;
	    }


	    public void OnAdd_texResolutionQuality(bool increase, int force_pickThisRes=-1, bool memorize_before=false){
	        if(memorize_before){
	            _memorized_res = resultTexQuality;
	        }
	        switch (resultTexQuality){
	            case 32:  resultTexQuality =  increase?48:32;  break; //low res is needed for pixelated styl
	            case 48:  resultTexQuality =  increase?64:32;  break; //(point-filtering). Gradual decrease near low vals.
	            case 64:  resultTexQuality =  increase?96:48;  break;
	            case 96:  resultTexQuality =  increase?128:64;  break;
	            case 128:  resultTexQuality =  increase?192:96;  break;
	            case 192:  resultTexQuality =  increase?256:128;  break;
	            case 256:  resultTexQuality =  increase?384:192;  break;
	            case 384:  resultTexQuality =  increase?512:256;  break;
	            case 512:  resultTexQuality =  increase?768:384;  break;
	            case 768:  resultTexQuality =  increase?1024:512;  break;
	            case 1024:  resultTexQuality =  increase?2048:768;  break;
	            case 2048:  resultTexQuality =  increase?3072:1024;  break;
	            case 3072:  resultTexQuality =  increase?4096:2048;  break;
	            case 4096:  resultTexQuality =  increase?5120:3072;  break;
	            case 5120:  resultTexQuality =  increase?6144:4096;  break;
	            case 6144:  resultTexQuality =  increase?7168:5120;  break;
	            case 7168:  resultTexQuality =  increase?8192:6144;  break;
	            case 8192:  resultTexQuality =  increase?8192:7168;  break;
	        }
	        resultTexQuality = force_pickThisRes>0? force_pickThisRes :  Mathf.Clamp(resultTexQuality, 32, 4096*2);
	        Objects_Renderer_MGR.instance.Resize_AccumulationTexture( resultTexQuality );
	        string abbr = textureRes_to_abbreviation();
	        _save_texResQuality_text.text = "SAVE " + abbr;
	    } 


	    string textureRes_to_abbreviation(){
	        string qualityTxt = resultTexQuality>=32 ? "32" : "?";
	        qualityTxt = resultTexQuality>=48 ? "48" : qualityTxt;
	        qualityTxt = resultTexQuality>=64 ? "64" : qualityTxt;
	        qualityTxt = resultTexQuality>=96 ? "96" : qualityTxt;
	        qualityTxt = resultTexQuality>=128 ? "128" : qualityTxt;
	        qualityTxt = resultTexQuality>=192 ? "192" : qualityTxt;
	        qualityTxt = resultTexQuality>=256 ? "256" : qualityTxt;
	        qualityTxt = resultTexQuality>=500 ? "0.5K": qualityTxt;
	        qualityTxt = resultTexQuality>=1000 ? "1K" : qualityTxt;
	        qualityTxt = resultTexQuality>=2000 ? "2K" : qualityTxt;
	        qualityTxt = resultTexQuality>=3000 ? "3K" : qualityTxt;
	        qualityTxt = resultTexQuality>=4000 ? "4K" : qualityTxt;
	        qualityTxt = resultTexQuality>=5000 ? "5K" : qualityTxt;
	        qualityTxt = resultTexQuality>=6000 ? "6K" : qualityTxt;
	        qualityTxt = resultTexQuality>=7000 ? "7K" : qualityTxt;
	        qualityTxt = resultTexQuality>=8000 ? "8K" : qualityTxt;
	        return qualityTxt;
	    }


	    void OnTextureFilterMode_Toggle(bool isOn, Toggle toggle){
	        bool allToggles_off =  _textureFilterPoint_toggle.isOn==false && _textureFilterBilinear_toggle==false;
        
	        if(allToggles_off){//disallow the total disabling. One toggle must remain on:
	            toggle.SetIsOnWithoutNotify(toggle.isOn);
	        }
	        if(!isOn){ return; }
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();
	        // Toggle-group doesn't work on these toggles because some might be hidden (slide out panel is concealed).
	        // So, untoggling them manually:
	        if(toggle == _textureFilterPoint_toggle){  
	            _textureFilterBilinear_toggle.isOn = false;  
	            resultTexFilterMode=FilterMode.Point;
	        }
	        if(toggle == _textureFilterBilinear_toggle){  
	            _textureFilterPoint_toggle.isOn = false; 
	            resultTexFilterMode=FilterMode.Bilinear; 
	        }
	    }


	    //NOTICE: very important, because we will send a view of our scene to stableDiffusion img2img.
	    // It is crucial that we send the highest-res possible, not 1k or 2k.
	    // Otherwise, SD will preserve it around the faded-borders of the mask.
	    // And we would be projecting such returned result into our 1k or 2k accumulation-texture.
	    // This would cause quality to degrade with each generation inside that inpaint location.
	    void OnWillRequest_img2img(){
	        _isWillGenArt_keepResolution5k = true;
	        _memorized_res_b4_pixels_boost = resultTexQuality;
	        if(resultTexQuality >= 5120){ return; }
	        OnAdd_texResolutionQuality(true, force_pickThisRes:5120);//while generating, ensure at least 5k res.
	    }

	    void OnRequested_img2img(GenData2D genData){
	        _isWillGenArt_keepResolution5k = false;
	        OnAdd_texResolutionQuality(false, force_pickThisRes:_memorized_res_b4_pixels_boost);
	    }
    
	    void OnWillMake_FinalCompositeImg(){
	        _isSavingProject_keepResolution4k = true;
	        _memorized_res_b4_projectSave = resultTexQuality;
	        if(resultTexQuality >= 4096){ return; }
	        OnAdd_texResolutionQuality(true, force_pickThisRes:4096);//while saving, ensure at least 4k res.
	    }

	    void OnMade_FinalCompositeImg(){
	        _isSavingProject_keepResolution4k = false;
	        OnAdd_texResolutionQuality(false, force_pickThisRes:_memorized_res_b4_projectSave);
	    }


	    public void Save(StableProjectorz_SL spz){
	        spz.sceneResolution = new SceneResolution_SL();
	        spz.sceneResolution.scene_texResolution = resultTexQuality;
	        spz.sceneResolution.scene_texFilterMode = resultTexFilterMode.ToString();
	    }

	    public void Load(StableProjectorz_SL spz){
	        int newRes = spz.sceneResolution.scene_texResolution;
	        bool isIncreaseRes =  newRes>= resultTexQuality;
	        OnAdd_texResolutionQuality(isIncreaseRes, force_pickThisRes:newRes);
        
	        bool isPoint = spz.sceneResolution.scene_texFilterMode.ToLower().Contains("point");
	        Toggle filterToggle = isPoint? _textureFilterPoint_toggle : _textureFilterBilinear_toggle;
	        OnTextureFilterMode_Toggle(true, filterToggle);
	    }


	    void Awake(){
	        EventsBinder.Bind_Clickable_to_event(nameof(SceneResolution_MGR), this);

	        _sub_texResolutionQuality.onClick.AddListener( ()=>OnAdd_texResolutionQuality(increase:false) );
	        _add_texResolutionQuality.onClick.AddListener( ()=>OnAdd_texResolutionQuality(increase:true) );

	        _textureFilterPoint_toggle.onValueChanged.AddListener( isOn=>OnTextureFilterMode_Toggle(isOn, _textureFilterPoint_toggle) );
	        _textureFilterBilinear_toggle.onValueChanged.AddListener( isOn=>OnTextureFilterMode_Toggle(isOn, _textureFilterBilinear_toggle) );
	        _save_texResQuality_text.text = "SAVE " + textureRes_to_abbreviation();

	        StableDiffusion_Hub._Act_img2img_willRequest += OnWillRequest_img2img;
	        StableDiffusion_Hub._Act_img2img_requested += OnRequested_img2img;

	        ProjectSaveLoad_Helper._onWillMake_FinalCompositeImg += OnWillMake_FinalCompositeImg;
	        ProjectSaveLoad_Helper._onMade_FinalCompositeImg += OnMade_FinalCompositeImg;
	    }
	}
}//end namespace
