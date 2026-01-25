using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using SimpleFileBrowser;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem;
using System.IO;
using System.Linq;


namespace spz {

	public class SaveStatus{//allows for coroutine to keep looping until becomes true.
	    public bool isComplete = false;
	}

	//capable of saving Albedo, AmbientOcclusion textures to disk.
	public class Save_MGR : MonoBehaviour{
	    public static Save_MGR instance { get; private set; } = null;

	    [SerializeField] ProjectSaveLoad_Helper _saveLoad_helper;

	    public bool _isSaving { get; private set; } = false;
	    public bool _isLoading { get; private set; } = false;


	    public void MergeIcons( Action<Dictionary<Texture2D,UDIM_Sector>> onHaveAlbedo,  bool oldIcons_survive=false ){
	        _isSaving = true;

	        _saveLoad_helper.Save_FinalCompositeTexture( OnReady1 );

	        void OnReady1() => StartCoroutine( WaitForRenderAll_crtn(skipAO_blit:true, OnReady2) );
     
	        void OnReady2(){//save + ensure albedo won't be deleted, - we'll keep using it in new generation:
	            Save_Mesh_Textures(OnHaveAlbedo, "", isDilate: false, forbid_albedoDelete: true);
	        }

	        void OnHaveAlbedo( Dictionary<Texture2D,UDIM_Sector> albedoDict ){
	            var mgr = GenData2D_Archive.instance;
	            var uvTex  = mgr.FindAll_GenData_ofKind( GenerationData_Kind.UvTextures_FromFile );
	            var uvBrush= mgr.FindAll_GenData_ofKind( GenerationData_Kind.UvPaintedBrush );
	            var prTex  = mgr.FindAll_GenData_ofKind( GenerationData_Kind.SD_ProjTextures );
	            var allTex = uvTex.Union(prTex).Union(uvBrush);
	            if(oldIcons_survive == false){ 
	                foreach (GenData2D genDat in allTex){  mgr.DisposeGenerationData(genDat.total_GUID);  }
	            }
	            onHaveAlbedo(albedoDict);
	            _isSaving = false;
	        };
	    }


	    public void DoSaveProject(){
	        _isSaving = true;

	        Action<string> onResultMessage =  msg =>Viewport_StatusText.instance.ShowStatusText(msg, false, 6, false);
	        _saveLoad_helper.SaveProject( onReady1, onResultMessage );
        
	        void onReady1(string path) => OnSaveProjTextures_PathChosen(path, isDilate:true, onReady2);

	        void onReady2(){
	            //after saving, Unpress any ctrl, alt etc. Else unity might keep thinking they are still pressed:
	            _isSaving = false;
	            StartCoroutine(ResetCtrlKey_AfterLoadSave());
	        }
	    }

	    public void DoLoadProject(){
	        _isLoading = true;
        
	        // CHANGED: LoadProject is now Async, so we use a callback instead of 'out string'
	        _saveLoad_helper.LoadProject( (resultMessage_) => {
	            Viewport_StatusText.instance.ShowStatusText(resultMessage_, false, 6, false);
	            _isLoading = false;
	            //after loading, Unpress any ctrl, alt etc. Else unity might keep thinking they are still pressed:
	            StartCoroutine( ResetCtrlKey_AfterLoadSave() );
	        });
	    }

	    IEnumerator ResetCtrlKey_AfterLoadSave(){
	        yield return null;
	        yield return null;
	        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
	        InputSystem.QueueStateEvent(Mouse.current, new MouseState());
	        InputSystem.QueueStateEvent(Pen.current, new PenState());
	        Input.ResetInputAxes();//for legacy input system (Input.GetKey etc)
	    }



	    public void Export3D_with_textures(){
	        _isSaving = true;
        
	        // let the user pick path and export the 3d model:
	        ModelsHandler_3D.instance.ExportModel();
	        string path_exported3D = ModelsHandler_3D.instance._path_recentlyExported;
	        //remove .obj or fbx, because images will use default .png:
	        path_exported3D =  Path.ChangeExtension(path_exported3D, null);

	        //save the final composite texture:
	        _saveLoad_helper.Save_FinalCompositeTexture( OnReady1 );

	        void OnReady1() => StartCoroutine( WaitForRenderAll_crtn(skipAO_blit: true, OnReady2) );

	        void OnReady2() => Save_Mesh_Textures( onHaveAlbedo:null, path_exported3D, isDilate: true, 
	                                               forbid_albedoDelete:false,  onComplete:OnComplete);
	        void OnComplete()=>_isSaving=false;
	    }


	    public void Save2DArt_ExactPath(Texture2D saveMe, string pathAbs, bool destroyTex){
	        _isSaving = true;
	        TextureTools_SPZ.EncodeAndSaveTexture(saveMe, pathAbs);
	        if(destroyTex){  DestroyImmediate(saveMe);  }
	        _isSaving = false;
	    }


	    public void Save2DArt( Dictionary<Texture2D,UDIM_Sector> saveMe, bool destroyTexs){
	        _isSaving = true;
	        string defaultName = "Tex_StableProjectorz";
	        GetBasePathForTextures(defaultName, OnReady);
        
	        void OnReady(string file){
	            OnBasePathForTextures_Chosen(file, saveMe, destroyTexs);
	            _isSaving=false;
	        }
	    }

	    public void SaveViewTextures(){ //save whatever the camera is observing (view,depth,normals,etc)
	        _isSaving = true;
	        string defaultName = "Tex_StableProjectorz";
	        GetBasePathForTextures(defaultName, onComplete:(path) => OnSaveViewTextures_PathChosen(path,OnReady));
	        void OnReady() =>_isSaving=false;
	    }


	    //dilation allows to "spread" the texture outwards from uv-chunks. Helps to avoid seams.
	    public void SaveProjectionTextures(bool isDilate){
	        _isSaving = true;
	        string defaultName = "Tex_StableProjectorz";
	        GetBasePathForTextures( defaultName, onComplete:(path)=>OnSaveProjTextures_PathChosen(path,isDilate,OnReady) );
        
	        void OnReady()=> _isSaving = false;
	    }

    
	    void GetBasePathForTextures( string defaultName,  Action<string> onComplete ){
	        // CHANGED: Using SimpleFileBrowser Async pattern.
	        // NOTE: SimpleFileBrowser automatically handles the "Overwrite?" popup, so ConfirmPopup_UI logic is removed.
        
	        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", "png", "jpg", "tga"));
	        FileBrowser.SetDefaultFilter("png");

	        FileBrowser.ShowSaveDialog( (paths) => {
	            if(paths.Length > 0){
	                onComplete(paths[0]);
	            }
	        },
	        () => {
	             onComplete(null);
	        },
	        FileBrowser.PickMode.Files, false, null, defaultName, "Save Resulting Model Texture", "Save");
	    }
    

	    void OnSaveViewTextures_PathChosen( string basePath, Action onComplete ){
	        if(string.IsNullOrEmpty(basePath)){ return; }
        
	        StartCoroutine( WaitForRenderAll_crtn(skipAO_blit:false, onReady) );

	        void onReady(){ 
	            Save_ViewTextures(basePath);
	            onComplete?.Invoke();
	        }
	    }


	    void OnSaveProjTextures_PathChosen( string basePath, bool isDilate, Action onComplete ){
	        if(string.IsNullOrEmpty(basePath)){ return; }
        
	        StartCoroutine( WaitForRenderAll_crtn(skipAO_blit:true, onReady) );
        
	        void onReady() => Save_Mesh_Textures(null, basePath, isDilate, forbid_albedoDelete:false, onComplete);
	    }

    
	    void OnBasePathForTextures_Chosen( string filepath,  Dictionary<Texture2D,UDIM_Sector> saveMe, bool destroyTexs ){
	        if (string.IsNullOrEmpty(filepath)){ return; }

	        filepath = MakeUniquePath(filepath,suffix:"");
	        EncodeAndSaveTextures(saveMe, filepath);

	        Viewport_StatusText.instance.ShowStatusText("Saved to "+ filepath.Replace("\\", "\\\\"), 
	                                                    false, 10, progressVisibility: false);
	        if(destroyTexs){  
	            foreach(var kvp in saveMe){ DestroyImmediate(kvp.Key); }
	        }
	    }
    

	    string MakeUniquePath(string basePath, string suffix){
	        if (string.IsNullOrEmpty(basePath)){ return ""; }

	        string dir = Path.GetDirectoryName(basePath);
	        string filenameWithoutExtension = Path.GetFileNameWithoutExtension(basePath);
	        string extension = Path.GetExtension(basePath);

	        //make it unique:
	        string baseFilename = $"{filenameWithoutExtension}{suffix}";
	        return Path.Combine(dir, baseFilename + extension);
	    }


	    //ask all user-cameras (and projections) to re-render. Wait few frames until all is complete.
	    IEnumerator WaitForRenderAll_crtn(bool skipAO_blit, Action onReady){
	        UserCameras_Permissions.Force_KeepRenderingCameras(true);

	        Objects_Renderer_MGR.instance.ReRenderAll_soon();
	        Objects_Renderer_MGR.instance._skip_AO_blit = skipAO_blit;
	        for(int i=0; i<3; ++i){ yield return null; }
	        Objects_Renderer_MGR.instance._skip_AO_blit = false;
        
	        UserCameras_Permissions.Force_KeepRenderingCameras(false);
	        onReady();
	    }


	    void Save_ViewTextures(string basePath){
	        string pathContent = MakeUniquePath(basePath, "_Content");
	        string pathDepth   = MakeUniquePath(basePath, "_Depth");
	        string pathNormals = MakeUniquePath(basePath, "_Normals");
	        string pathVertex  = MakeUniquePath(basePath, "_VertCols");

	        Texture2D content = UserCameras_MGR.instance.camTextures.GetDisposable_ContentCamTexture();
	        Texture2D depth  = UserCameras_MGR.instance.camTextures.GetDisposable_DepthTexture();
	        Texture2D normals = UserCameras_MGR.instance.camTextures.GetDisposable_NormalsTexture();
	        Texture2D vertCols = UserCameras_MGR.instance.camTextures.GetDisposable_VertexColorsTexture();
        
	        encodeSaveDestroy(content, pathContent);
	        encodeSaveDestroy(depth, pathDepth);
	        encodeSaveDestroy(normals, pathNormals);
	        encodeSaveDestroy(vertCols, pathVertex);

	        void encodeSaveDestroy(Texture2D tex, string path){
	            if(tex == null){ return; }
	            TextureTools_SPZ.EncodeAndSaveTexture(tex, path);
	            DestroyImmediate(tex);
	        }
	        Viewport_StatusText.instance.ShowStatusText("Saved to "+ basePath.Replace("\\", "\\\\"), 
	                                                     false, 10, progressVisibility:false);
	    }


	    void Save_Mesh_Textures( Action<Dictionary<Texture2D,UDIM_Sector>> onHaveAlbedo=null,  
	                            string save_to_basePath="",  bool isDilate=false,
	                            bool forbid_albedoDelete = false,  Action onComplete=null){
	        Dictionary<Texture2D,UDIM_Sector> albedo;
	        Dictionary<Texture2D,UDIM_Sector> ao;
	        bool albedo_destroyWhenDone;
	        bool ao_destroyWhenDone;
	        Get_ProjectionsDict(isDilate, out albedo, out ao, out albedo_destroyWhenDone, out ao_destroyWhenDone);
	        albedo_destroyWhenDone =  forbid_albedoDelete?false : albedo_destroyWhenDone;

	        if(albedo==null && ao==null){
	            onHaveAlbedo?.Invoke(null);
	            onComplete?.Invoke();
	            return;
	        }
	        onHaveAlbedo?.Invoke(albedo);

	        string pathAlbedo=null, pathAO=null;

	        if( save_to_basePath!=""){
	            pathAlbedo = MakeUniquePath(save_to_basePath, "");
	            pathAO = MakeUniquePath(save_to_basePath, "_AO");
	            EncodeAndSaveTextures(albedo, pathAlbedo);
	            EncodeAndSaveTextures(ao, pathAO);
	            Viewport_StatusText.instance.ShowStatusText("Saved to "+ pathAlbedo.Replace("\\", "\\\\"), 
	                                                         false, 10, progressVisibility:false);
	        }
	        //cleanup:
	        if(albedo_destroyWhenDone){ foreach(var kvp in albedo){Texture.DestroyImmediate(kvp.Key);}  }
	        if(ao_destroyWhenDone){     foreach(var kvp in ao){Texture.DestroyImmediate(kvp.Key);}   }
	        onComplete?.Invoke();
	    }



	    void Get_ProjectionsDict( bool isDilate, out Dictionary<Texture2D,UDIM_Sector> albedo_,  
	                                             out Dictionary<Texture2D,UDIM_Sector> ambientOcclusion_,
	                                             out bool albedo_destroyWhenDone_,  out bool ao_destroyWhenDone_){

	        RenderUdims albedo = Objects_Renderer_MGR.instance.accumulationTextures_ref();

	        //Dilate (spread out) the texture around the uv-chunks/islands. This hides seams between them.
	        //check because maybe user doesn't want dilation (maybe they want to see uv islands:
	        if (isDilate){
	            int numDilationIters = Mathf.Max(albedo.width, albedo.height) / 16;  //for exmaple  2048 --> 128 pixels dilated.
	            var dilationArg = new DilationArg(albedo.texArray, numDilationIters, DilateByChannel.A, null);
	            dilationArg.bordersWiderBlur = true;
	            dilationArg.isRunInstantly = true;
	            TextureDilation_MGR.instance.Dillate(dilationArg);
	        }
	        //NOTICE: Convert albedo to texture AFTER dilate. Because dilate works while it's in tex-array form.
	        List<Texture2D> tex2D_list = TextureTools_SPZ.TextureArray_to_Texture2DList(albedo.texArray);
	        albedo_ = new Dictionary<Texture2D, UDIM_Sector>();
	        for(int i=0; i<tex2D_list.Count; ++i){  albedo_.Add(tex2D_list[i], albedo.udims_sectors[i]);  }
	        albedo_destroyWhenDone_ = true;

	        GenData2D ao_genData =  GenData2D_Archive.instance.Find_GenData_ofKind(GenerationData_Kind.AmbientOcclusion, search_lastToFirst:true);
	        IconUI ao_iconUI   = ao_genData==null? null : Art2D_IconsUI_List.instance.GetIcon_of_GenerationGroup(ao_genData.total_GUID, 0);
	        ambientOcclusion_  = AmbientOcclusion_Baker.instance.getDisposable_AO_texture( ao_iconUI, out ao_destroyWhenDone_ );
	    }


	    void EncodeAndSaveTextures( Dictionary<Texture2D,UDIM_Sector> textures,  string path, 
	                                bool skipUdimSuffix_if_1_texture = true ){
	        string pathBeforeExten = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
	        string exten = Path.GetExtension(path);

	        bool canUseIx    = textures.Count>1;
	        bool canUseUdims = textures.Count>1 || !skipUdimSuffix_if_1_texture;
	        int ix = 0;//will use index if udim sectors are empty, and if more than one texture.

	        foreach(var kvp in textures){
	            Texture2D tex  = kvp.Key;
	            UDIM_Sector val = kvp.Value;
	            string suffix = "";
	            if(val.isNonDefault && canUseUdims){ suffix =  "_" + val.ToString(); }
	            else if (canUseIx){  suffix  =  " " + ix; }
	            TextureTools_SPZ.EncodeAndSaveTexture(tex, pathBeforeExten+suffix+exten);
	            ++ix;
	        }
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	    }
	    void Start(){
	        ExportSave_UI_MGR.OnSaveProject_Button += DoSaveProject;
	        ExportSave_UI_MGR.OnLoadProject_Button += DoLoadProject;
	        ExportSave_UI_MGR.OnExport3D_Button += Export3D_with_textures;
	    }
	    void Update(){
	        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S)){  DoSaveProject();  }
	        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L)){  DoLoadProject();  }
	    }

	}
}//end namespace
