using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using SimpleFileBrowser;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.Experimental.Rendering;

namespace spz {

	public class ProjectSaveLoad_Helper : MonoBehaviour {

	    public static Action _onWillMake_FinalCompositeImg { get; set; } = null;
	    public static Action _onMade_FinalCompositeImg { get; set; } = null;

	    Coroutine _finalComposite_crtn;

	    // What user used, to save the project. 
	    // We can re-use it for the next saving, so user doesn't have to type it again.
	    string _last_saveFilepath = "";


	    public void SaveProject( Action<string> saveFinalTex,  Action<string> onResultMessage){
	        StopAllCoroutines();
	        StartCoroutine(SaveProj_crtn(saveFinalTex, onResultMessage));
	    }


	    public void Save_FinalCompositeTexture(Action saveFinalTex){
	        if(_finalComposite_crtn != null){ StopCoroutine(_finalComposite_crtn); } 
	        _finalComposite_crtn = StartCoroutine( Save_FinalCompositeTexture_crtn(saveFinalTex) );
	    }


	    IEnumerator SaveProj_crtn( Action<string> saveFinalTexs,  Action<string> onResultMessage ){

	        string defaultName = _last_saveFilepath == "" ? "SPZ_Project" : Path.GetFileNameWithoutExtension(_last_saveFilepath);
        
	        // CHANGED: Using SimpleFileBrowser Coroutine pattern for saving.
	        // Sets up filter for .spz files
	        FileBrowser.SetFilters(true, new FileBrowser.Filter("Project", "spz"));
	        FileBrowser.SetDefaultFilter("spz");

	        // Wait for the dialog to close
	        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, null, defaultName, "Save Project", "Save");

	        if (!FileBrowser.Success){
	            onResultMessage?.Invoke("Didn't save - no file selected.");
	            saveFinalTexs?.Invoke(null);
	            yield break;
	        }

	        string saveFile = FileBrowser.Result[0];

	        if (StableDiffusion_Hub.instance._generating) {
	            onResultMessage?.Invoke("Can't save while generating.");
	            saveFinalTexs?.Invoke(null);
	            yield break;
	        }

	        // NOTE: SimpleFileBrowser automatically handles the "Overwrite?" popup internally, 
	        // so the manual ConfirmPopup_UI logic is removed here to prevent double prompts.

	        var spz = new StableProjectorz_SL();
	        spz.filepath_with_exten = saveFile;
	        spz.filepath_dataDir = CreateDataDir(saveFile);

	        spz.sd_genSettingsInput = new SD_GenSettingsInput_UI();
	        spz.generate3D_inputs = new Generate3D_Inputs_SL();

	        Performance_MGR.instance.Save(spz);
	        LeftRibbon_UI.instance.Save(spz);
	        UserCameras_MGR.instance.Save(spz);

	        ProjectorCameras_MGR.instance.Save(spz);
	        SD_InputPanel_UI.instance.Save(spz.sd_genSettingsInput);
	        StableDiffusion_Prompts_UI.instance.Save(spz.sd_genSettingsInput);

	        //Jan 2025 not saving for now, because the layout is dynamically created from a text string
	        //TrellisInputTabs_MGR_UI.instance.Save(spz.generate3D_inputs, spz.filepath_dataDir);

	        WorkflowRibbon_UI.instance.Save(spz);
	        SD_WorkflowOptionsRibbon_UI.instance.Save(spz);
	        Gen3D_WorkflowOptionsRibbon_UI.instance.Save(spz);

	        GenData2D_Archive.instance.Save(spz);
	        SD_ControlNetsList_UI.instance.Save(spz);
	        ModelsHandler_3D.instance.Save(spz);
	        ModelsHandler_3D_UI.instance.Save(spz);
	        SkyboxColorButtons_UI_MGR.instance.Save(spz);
	        Art2D_IconsUI_List.instance.Save(spz);
	        ArtBG_IconsUI_List.instance.Save(spz);
	        Connection_MGR.instance.Save(spz);

	        // DataFolder should be relative to filepath of the project-file
	        string resultMessage;
	        Serialize_SPZ_toJSON(saveFile, spz, out resultMessage);
	        onResultMessage?.Invoke(resultMessage);

	        // Now, save the final composite-texture, combinging all projections.
	        // This is important, in case the spz file gets corrupted. At least the user will have the png:
	        Action onSaveFinalTex =  ()=>saveFinalTexs( spz.filepath_dataDir + "/FINAL_COMPOSITE_4K.png" );
        
	        if(_finalComposite_crtn != null){ StopCoroutine(_finalComposite_crtn); } 
	        _finalComposite_crtn = StartCoroutine( Save_FinalCompositeTexture_crtn(onSaveFinalTex) );
	        yield return _finalComposite_crtn;

	        _last_saveFilepath = saveFile;
	        _onMade_FinalCompositeImg?.Invoke();
	    }



	    IEnumerator Save_FinalCompositeTexture_crtn( Action saveFinalTex ){
	        _onWillMake_FinalCompositeImg?.Invoke();

	            yield return null;//allows any temporary resolution adjustments to occur and be noticed by cameras.
	            yield return null;

	            saveFinalTex();
        
	            while (Save_MGR.instance._isSaving){ yield return null; }

	            _finalComposite_crtn = null;

	        _onMade_FinalCompositeImg?.Invoke();
	    }
    
    
	    // CHANGED: Method signature updated to use Callback Action<string> instead of 'out string',
	    // because SimpleFileBrowser operates asynchronously.
	    public void LoadProject( Action<string> onResult ){
        
	        FileBrowser.SetFilters(true, new FileBrowser.Filter("Project", "spz"));
	        FileBrowser.SetDefaultFilter("spz");

	        FileBrowser.ShowLoadDialog((paths) => {
	            if (paths.Length == 0){
	                onResult?.Invoke("Didn't save - no file selected.");
	                return;
	            }

	            string spzFilepath = paths[0];

	            if (StableDiffusion_Hub.instance._generating){
	                onResult?.Invoke("Can't Load while generating.");
	                return;
	            }
	            string json = File.ReadAllText(spzFilepath);
	            if (string.IsNullOrEmpty(json)){
	                onResult?.Invoke("Error loading the project file. It's empty");
	                return;
	            }

	            string resultMessage_;
	            StableProjectorz_SL spz = StableProjectorz_SL.CreateFromJSON(json, out resultMessage_);
	            spz?.update_dataDir_toCurrent(spzFilepath);
	            if(spz == null){
	                onResult?.Invoke("Error loading the project file. The file is corrupted, or an unsupported version");
	                return;
	            }
	            Performance_MGR.instance.Load(spz);
	            LeftRibbon_UI.instance.Load(spz);
	            UserCameras_MGR.instance.Load(spz);

	            ModelsHandler_3D.instance.Load(spz);//befores the projector cameras
	            ModelsHandler_3D_UI.instance.Load(spz);
	            ProjectorCameras_MGR.instance.Load(spz);
	            SD_InputPanel_UI.instance.Load(spz);
	            StableDiffusion_Prompts_UI.instance.Load( spz.sd_genSettingsInput );

	            //Jan 2025 not saving for now, because the layout is dynamically created from a text string
	            //TrellisInputTabs_MGR_UI.instance.Load(spz.generate3D_inputs, spz.filepath_dataDir);

	            WorkflowRibbon_UI.instance.Load(spz);
	            SD_WorkflowOptionsRibbon_UI.instance.Load(spz);
	            Gen3D_WorkflowOptionsRibbon_UI.instance.Load(spz);

	            GenData2D_Archive.instance.Load(spz);
	            SD_ControlNetsList_UI.instance.Load(spz);
	            SkyboxColorButtons_UI_MGR.instance.Load(spz);
	            Art2D_IconsUI_List.instance.Load(spz);
	            ArtBG_IconsUI_List.instance.Load(spz);
	            Connection_MGR.instance.Load(spz);
	            //2D BACKGROUND mgr?

	            UserCameras_MGR.instance.OnAfter_AllLoaded();
	            ProjectorCameras_MGR.instance.OnAfterLoadedAll();
	            GenData2D_Archive.instance.OnAfter_AllLoaded(spz);
	            Art2D_IconsUI_List.instance.OnAfter_AllLoaded();
	            ArtBG_IconsUI_List.instance.OnAfter_AllLoaded();

	            Objects_Renderer_MGR.instance.ReRenderAll_soon();
            
	            onResult?.Invoke(resultMessage_);

	        }, 
	        () => {
	             // Cancelled
	             onResult?.Invoke("Load Cancelled");
	        },
	        FileBrowser.PickMode.Files, false, null, null, "Load Project", "Load");
	    }


	    // A folder with the same name as the project-file, but with _Data suffix.
	    // we can store all necessary things in that directory. Textures, etc.
	    string CreateDataDir(string project_file){
	        var directory = Path.GetDirectoryName(project_file);
	        var filenameWithoutExtension = Path.GetFileNameWithoutExtension(project_file);
	        var newDirectoryPath = Path.Combine(directory, filenameWithoutExtension + "_Data");
        
	        if (Directory.Exists(newDirectoryPath)){
	            // Delete all files in the directory
	            // Delete all subdirectories and their contents
	            foreach (var file in Directory.GetFiles(newDirectoryPath)){  File.Delete(file);  }
	            foreach (var dir in Directory.GetDirectories(newDirectoryPath)){  Directory.Delete(dir, true);  }
	        }else{
	            Directory.CreateDirectory(newDirectoryPath);
	        }
	        return newDirectoryPath;
	    }

	    void Serialize_SPZ_toJSON(string file, StableProjectorz_SL spz, out string resultMessage_){
	        var settings = new JsonSerializerSettings{
	            Formatting = Formatting.Indented,
	            TypeNameHandling = TypeNameHandling.Auto, //automatically resolve inheritance/abstract classes.
	            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,//skip cyclical references (Color.linear.linear.linear etc)
	        };
	        string json = JsonConvert.SerializeObject(spz, settings);
	        try{
	            File.WriteAllText(file, json);
	        }catch(IOException ex){
	            resultMessage_ = "Didn't save - " + ex.Message;
	            return;
	        }
	        resultMessage_ = "Saved the project to " + file;
	    }

    
	    public static void Save_Tex2D_To_DataFolder( Texture2D texture2D,  string dataFolder, string pathInDataFolder ){
	        if(texture2D == null){ return; }
	        string filePath = Path.Combine(dataFolder, pathInDataFolder);
	        // Save Texture2D as PNG
	        byte[] bytes = texture2D.EncodeToPNG();
	        File.WriteAllBytes(filePath, bytes);
	        //don't destroy Texture2D, it was provided to us.
	    }


	    public static void Save_RT_To_DataFolder( RenderTexture rt, string dataFolder, string pathInDataFolder ){
	        if(rt == null){ return; }
	        Texture2D texture2D = TextureTools_SPZ.RenderTextureToTexture2D(rt);
	        Save_Tex2D_To_DataFolder(texture2D, dataFolder, pathInDataFolder);
	        GameObject.DestroyImmediate(texture2D);// Clean up, we created it.
	    }


	    public static Texture2D Load_Texture2D_from_DataFolder( string dataFolder, string pathInDataFolder, 
	                                                            GraphicsFormat rtFormat, GraphicsFormat format, 
	                                                            Action<RenderTexture> onBeforeCreate=null,
	                                                            Material blitMat=null){
	        if(string.IsNullOrEmpty(dataFolder)){ return null; }
	        if(string.IsNullOrEmpty(pathInDataFolder)){ return null; }
	        string path = Path.Combine(dataFolder, pathInDataFolder);
	        if (!File.Exists(path)){
	            Debug.LogError($"File not found at {path}");
	            return null;
	        }
	        // Load the image into a temporary texture
	        Texture2D tex2D_temp = new Texture2D(2, 2);
	        if (!tex2D_temp.LoadImage( File.ReadAllBytes(path) )){
	            Debug.LogError($"Failed to load texture at {path}");
	            return null; // Early return on load failure
	        }
	        // Create a RenderTexture with the desired format
	        RenderTexture rt = new RenderTexture(tex2D_temp.width, tex2D_temp.height, 0, rtFormat, 0);
	        onBeforeCreate?.Invoke(rt);
	        rt.Create();

	        if(blitMat==null){  Graphics.Blit(tex2D_temp, rt);  }
	        else{  Graphics.Blit(tex2D_temp, rt, blitMat);  }

	        // Now, transfer the RenderTexture content to a new Texture2D
	        Texture2D tex2D_result = new Texture2D(tex2D_temp.width, tex2D_temp.height, format, 0, TextureCreationFlags.None);
	        RenderTexture.active = rt;
	        tex2D_result.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
	        tex2D_result.Apply();

	        // Clean up
	        RenderTexture.active = null; // Reset the active RenderTexture
	        Destroy(rt);
	        Destroy(tex2D_temp);

	        return tex2D_result;
	    }


	    public static RenderTexture Load_RT_from_DataFolder( string dataFolder,  string pathInDataFolder, 
	                                                         GraphicsFormat rtFormat,  Action<RenderTexture> onBeforeCreate = null, 
	                                                         Material blitMat=null ){
	        if(string.IsNullOrEmpty(dataFolder)){ return null; }
	        if(string.IsNullOrEmpty(pathInDataFolder)){ return null; }
	        string path = Path.Combine(dataFolder, pathInDataFolder);
	        if (!File.Exists(path)){
	            Debug.LogError($"File not found at {path}");
	            return null;
	        }
	        // Load the image into a temporary texture
	        Texture2D tempTex2D = new Texture2D(2, 2);
	        if (!tempTex2D.LoadImage( File.ReadAllBytes(path) )){
	            Debug.LogError($"Failed to load texture at {path}");
	            return null; // Early return on load failure
	        }
	        // Create a RenderTexture with the desired format
	        RenderTexture rt = new RenderTexture(tempTex2D.width, tempTex2D.height, 0, rtFormat, 0);
	        onBeforeCreate?.Invoke(rt);
	        rt.Create();

	        if(blitMat==null){  Graphics.Blit(tempTex2D, rt);  }
	        else{  Graphics.Blit(tempTex2D, rt, blitMat);  }

	        Destroy(tempTex2D);
	        return rt;
	    }

	}
}//end namespace
