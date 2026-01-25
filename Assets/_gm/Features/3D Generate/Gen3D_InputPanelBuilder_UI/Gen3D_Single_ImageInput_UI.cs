using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;

namespace spz {

	public class Gen3D_SingleImageInput_UI : Gen3D_ImageInputs_UI
	{
	     [SerializeField] Trellis_ImageSlot _mySlot;

	    public override int NumImages() => _mySlot.has_image()? 1 : 0;

	    public override List<string> get_images_asBase64()
	        => new List<string>{ _mySlot.image_as_base64() };


	    protected override void OnTakeScreenshotTexture(Vector2 screen_min01, Vector2 screen_max01, Texture2D tex2D_takeOwnership){ 
	        if(gameObject.activeSelf==false){ return; }
	        _mySlot.SwapWithNewImage(tex2D_takeOwnership);
	    }

	    protected override void OnImportedImage(Trellis_ImageSlot slot, Texture2D tex){
	        if(slot != _mySlot){ return; }//maybe it's from MultiImage_inputs, so skip.
	        slot.SwapWithNewImage(tex);
	    }

	    protected override void OnImportedImage_Closed(Trellis_ImageSlot slot){
	        if(slot != _mySlot){ return; }//maybe it's from MultiImage_inputs, so skip.
	        slot.DisposeTheImage();
	    }

	    protected override void OnImage_Mirrored(Trellis_ImageSlot slot){
	        if (slot != _mySlot){ return; }
	        var mirrored_rt = new RenderTexture( slot.visibleTexture_ref.width, slot.visibleTexture_ref.height, 
	                                              depth:0,  GraphicsFormat.R8G8B8A8_UNorm,  mipCount:1 );
	        TextureTools_SPZ.Blit(slot.visibleTexture_ref, mirrored_rt, base._mirrorImage_mat);
	        Texture2D mirorred2D = TextureTools_SPZ.RenderTextureToTexture2D(mirrored_rt);
	        DestroyImmediate(mirrored_rt);
	        slot.SwapWithNewImage( tex_takeOwnership:mirorred2D );
	    }

	    public override void OnDragAndDroppedTextures(List<string> filepaths){
	        if (gameObject.activeSelf == false){ return; }
	        if (filepaths.Count > 1){ 
	            filepaths.RemoveRange(1, filepaths.Count-1);//only keep 1 entry
	        }
	        //will only contain 1 entry
	        List<Texture2D> texList = TextureTools_SPZ.LoadTextures_FromFiles(filepaths);
	        if(texList.Count > 0 && texList[0]!=null){
	            _mySlot.SwapWithNewImage(tex_takeOwnership: texList[0]);
	        }
	    }

	    protected override void Awake(){
	        base.Awake();
	    }
	}



	// Abstract class, manages inputs for generating 3D.
	// Listens to the press of the Generate buttons, and can tell the API to render.
	// Contains the most common inputs, child classes can add more.
	// Allows to either:
	//  begin generate --> get mesh --> complete
	// or
	//  begin generate --> get video --> confirm video --> get mesh --> complete
	public abstract class Gen3D_ImageInputs_UI : MonoBehaviour{

	    [SerializeField] protected Shader _mirrorImage_shader;
    
	    protected Material _mirrorImage_mat;//for flipping textures horizontally, during Blit

	    public abstract int NumImages();
	    public abstract List<string> get_images_asBase64();

	    protected abstract void OnTakeScreenshotTexture(Vector2 screen_min01, Vector2 screen_max01, Texture2D tex2D_takeOwnership);
	    protected abstract void OnImportedImage(Trellis_ImageSlot slot, Texture2D tex);
	    protected abstract void OnImportedImage_Closed(Trellis_ImageSlot slot);
	    protected abstract void OnImage_Mirrored(Trellis_ImageSlot slot);
	    public abstract void OnDragAndDroppedTextures(List<string> filepaths);


	    protected void SaveSlots( string path_dataFolder,  string imgs_subfolderName, 
	                              List<Trellis_ImageSlot> imgSlots,  ref List<string> imgPaths_saveHere_){
	        string subfolder_full = Path.Combine(path_dataFolder, imgs_subfolderName);
	        Directory.CreateDirectory( subfolder_full );
	        for(int i=0; i<imgSlots.Count; i++){
	            string texPath = Path.Combine(subfolder_full, $"img{i}.png");
	            imgPaths_saveHere_.Add(texPath);
	            var asRt = imgSlots[i].visibleTexture_ref as RenderTexture;
	            var as2D = imgSlots[i].visibleTexture_ref as Texture2D;
	            if (asRt != null){
	                ProjectSaveLoad_Helper.Save_RT_To_DataFolder( asRt, path_dataFolder, texPath);
	            }if(as2D != null){
	                ProjectSaveLoad_Helper.Save_Tex2D_To_DataFolder( as2D, path_dataFolder, texPath);
	            }
	        }
	    }

	    protected List<Texture2D> LoadSlotImages(string path_dataFolder, string imgs_subfolderName, List<string>img_filenames_withExten){
	        string subdir  = imgs_subfolderName;
	        string subdir_full = Path.Combine(path_dataFolder, subdir);
	        var textures = new List<Texture2D>();
	        for(int i=0; i<img_filenames_withExten.Count; ++i){
	            string imgPath = Path.Combine(subdir_full, img_filenames_withExten[i]);
	            if(File.Exists(imgPath) == false){ continue; }
	            Texture2D tex = ProjectSaveLoad_Helper.Load_Texture2D_from_DataFolder(path_dataFolder, imgPath,
	                                                                                  GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.R8G8B8A8_UNorm);
	            textures.Add(tex);
	        }
	        return textures;
	    }


	    protected virtual void Awake(){
	        Trellis_ImageSlot._Act_OnImageFile  += OnImportedImage;
	        Trellis_ImageSlot._Act_onCloseButton += OnImportedImage_Closed;
	        Trellis_ImageSlot._Act_onMirrorButton += OnImage_Mirrored;
	        _mirrorImage_mat = new Material(_mirrorImage_shader);
	    }

	    protected virtual void Start(){
	        Gen3D_WorkflowOptionsRibbon_UI.instance.Act_AllowTakeScreenshots += OnAllowTakeScreenshots;

	        if(Screenshot_MGR.instance != null){ 
	            Screenshot_MGR.instance._Act_OnTakeScreenshotTexture -= OnTakeScreenshotTexture;//UNSUB first!
	            Screenshot_MGR.instance._Act_OnTakeScreenshotTexture += OnTakeScreenshotTexture;
	            OnAllowTakeScreenshots( Gen3D_WorkflowOptionsRibbon_UI.instance._is_can_take_screenshots );
	        }
	    }

	    void OnAllowTakeScreenshots(bool isOn){
	        if(Screenshot_MGR.instance == null){ return; }
	        if (isOn){
	            Screenshot_MGR.instance.PrefferCaptureSnippets(requestor:this);
	        }else { 
	            Screenshot_MGR.instance.PrefferAvoidSnippets(originalRequestor:this);
	        }
	    }

	    protected virtual void OnEnable(){
	        if(Screenshot_MGR.instance != null){ 
	            Screenshot_MGR.instance._Act_OnTakeScreenshotTexture -= OnTakeScreenshotTexture;
	            Screenshot_MGR.instance._Act_OnTakeScreenshotTexture += OnTakeScreenshotTexture;
	            OnAllowTakeScreenshots( Gen3D_WorkflowOptionsRibbon_UI.instance._is_can_take_screenshots );
	        }
	    }
    
	    protected virtual void OnDisable(){
	        // unsubscribe as soon as inactive, to avoid receiving a personal texture2D copy when a screenshot is done:
	        Screenshot_MGR.instance._Act_OnTakeScreenshotTexture -= OnTakeScreenshotTexture;
	        Screenshot_MGR.instance.PrefferAvoidSnippets(originalRequestor:this);
	    }

	    void OnDestroy(){
	        Trellis_ImageSlot._Act_OnImageFile  -= OnImportedImage;
	        Trellis_ImageSlot._Act_onCloseButton -= OnImportedImage_Closed;
	        Trellis_ImageSlot._Act_onMirrorButton -= OnImage_Mirrored;

	        if (Screenshot_MGR.instance != null){ 
	            Screenshot_MGR.instance._Act_OnTakeScreenshotTexture -= OnTakeScreenshotTexture;
	        }
	        DestroyImmediate(_mirrorImage_mat);
	    }
	}
}//end namespace
