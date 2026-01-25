using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SimpleFileBrowser;


namespace spz {

	// Singleton.
	// Allows to import files, interprets them as Texture2D, and then invokes callback.
	public class Images_ImportHelper : MonoBehaviour{
	    public static Images_ImportHelper instance { get; private set; } = null;

	    public bool isImporting { get; private set; } = false;

	    GenerationData_Kind _kind;
	    Action<GenerationData_Kind,Dictionary<Texture2D,UDIM_Sector>> _onComplete = null;
	    Action<GenerationData_Kind,string> _onFail = null;


	    //if onFail is null, error will automatically log to 'Viewport_StatusText'
	    //make sure to destroy textures 
	    public void ImportCustomImageButton( GenerationData_Kind kind,  bool allow_multipleFiles,
	                                         Action<GenerationData_Kind,Dictionary<Texture2D,UDIM_Sector>> onComplete_texturesWithoutOwner,  
	                                         Action<GenerationData_Kind,string> onFail = null ){
	        if (isImporting){ onFail?.Invoke(kind,"already importing"); return; }
	        isImporting = true;
	        _kind = kind;
	        _onComplete = onComplete_texturesWithoutOwner;
	        _onFail = onFail;

	        string headerMsg = allow_multipleFiles ? "Import several Custom Textures (png/jpg/tga)" 
	                                               : "Import 1 Custom Texture (png/jpg/tga)";

	        //NOTICE: Async version used here (SimpleFileBrowser standard)
	        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", "png", "jpg", "jpeg", "tga"));
	        FileBrowser.SetDefaultFilter("png");

	        FileBrowser.ShowLoadDialog( (paths) => {
	            // Success
	            OnImportCustomImage_FileConfirmed(paths);
	        }, 
	        () => {
	            // Canceled
	            isImporting = false; 
	            _onComplete = null; 
	            _onFail = null; 
	        },
	        FileBrowser.PickMode.Files, allow_multipleFiles, null, null, headerMsg, "Import");
	    }


	    public void OnImport_DragAndDrop( GenerationData_Kind kind, List<string> files, 
	                                      Action<GenerationData_Kind,Dictionary<Texture2D,UDIM_Sector>> onComplete_texturesWithoutOwner,  
	                                      Action<GenerationData_Kind,string> onFail = null ){
	        if(isImporting){ onFail?.Invoke(kind,"can't drag and drop images, already importing"); return; }
	        isImporting = true;
	        _kind = kind;
	        _onComplete = onComplete_texturesWithoutOwner;
	        _onFail = onFail;
	        OnImportCustomImage_FileConfirmed( files.ToArray() );
	    }

	    public void OnImport_ExistingImages( GenerationData_Kind kind, List<Texture2D> textures,
	                                         Action<GenerationData_Kind,Dictionary<Texture2D,UDIM_Sector>> onComplete_texturesWithoutOwner,  
	                                         Action<GenerationData_Kind,string> onFail = null){
	        if(isImporting){ onFail?.Invoke(kind,"import images, currently already importing others."); return; }
	        isImporting = true;
	        var texturesAndUdims = new Dictionary<Texture2D, UDIM_Sector>();
	        try{
	            for(int i=0;  i<textures.Count; ++i){
            
	                if(i >= UDIMs_Helper._allKnownUdims.Count){ break; }
            
	                Texture2D tex = textures[i];
	                UDIM_Sector udim_sector = UDIMs_Helper._allKnownUdims[i];
	                texturesAndUdims.Add(tex, udim_sector);
	            }
	        }
	        catch (Exception e){
	            Debug.LogError(e.Message);
	        }
	        isImporting = false;
	        onComplete_texturesWithoutOwner?.Invoke(kind, texturesAndUdims);
	    }


	    void OnImportCustomImage_FileConfirmed( string[] files ){
	        Invoke_on_MainThread(files);
	    }//end()



	    // Loading images from provided filepaths, and finding udims for each one.
	    void Invoke_on_MainThread( string[] files ){
	        isImporting = false;

	        if(files==null || files.Length==0){ //just cancelled
	            _onComplete = null;  
	            _onFail = null;
	            return; 
	        }
	        Dictionary<Texture2D,string> texturesAndFilepath =  Cast_as_Textures(files);
	        if(texturesAndFilepath == null){ return; }

	        Dictionary<Texture2D,UDIM_Sector> texturesAndUdims =  UDIMs_Helper.Determine_UDIMs(texturesAndFilepath);
	        var act = _onComplete;
	        _onComplete = null;
	        _onFail = null;
	        act?.Invoke(_kind, texturesAndUdims);
	    }


	    Dictionary<Texture2D,string> Cast_as_Textures( string[] files ){
        
	        var textures = new Dictionary<Texture2D,string>();

	        for (int i=0; i<files.Length; ++i){
	            string filePath = files[i];
	            if (File.Exists(filePath)==false){ continue; }
	            byte[] fileData = File.ReadAllBytes(filePath);
	            Texture2D tex = new Texture2D(2, 2);
	            // Load the image data into the texture (size will be set automatically)
	            if(tex.LoadImage(fileData) == false){ continue;}
	            textures.Add(tex, filePath);
	            // Apply the texture to a material or use it as needed
	            // For example: GetComponent<Renderer>().material.mainTexture = tex;
	        }
	        if(textures.Count==0){
	            string msg = "Couldn't load any selected textures. Check extensions.";
	            if(_onFail!=null){ 
	                _onFail(_kind, msg); 
	            }else{  
	                Viewport_StatusText.instance.ShowStatusText(msg, false, 4, false);  
	            }
	            _onComplete = null;  _onFail = null;
	            return null;
	        }
	        return textures;
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;
	    }
	}
}//end namespace
