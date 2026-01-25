using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using SimpleFileBrowser;

namespace spz {

	//assistant-object, which helps our 'ModelsHandler_3D' to bring a 3d model into the project.
	public class ModelsHandler3D_ImportHelper : MonoBehaviour{

	    [SerializeField] UDIMs_Helper _udims_helper;
	    [SerializeField] Objs3D_Container o3d;
	    [SerializeField] ModelsHandler_SaveFBX_Helper _saveFBX_helper;
	    GameObject _latestSuccessRoot = null;

	    //used when loading the project from a save file.
	    ModelsHandler_3D_SL _modelsHandler_SL = null;

	    // We remember raw bytes structure, if user loads mesh from file.
	    // Later on, if we decide to save project, we'll just dump these bytes into needed location.
	    // without having to convert our unity meshes into the needed format.
	    public byte[] _modelBytesCache { get; private set; } = null;
	    public string _modelBytesCache_filename { get; private set; } = "";
	    public bool _isImportingModel { get; private set; } = false;
	    public string _path_recentlyExported { get; private set; } = "";

	    public Action _Act_onStartedImporting{ get; set; } = null;// isSuccess,What.
	    public Action<bool, GameObject> _Act_onImportComplete { get; set; } = null;// isSuccess,What.
    

	    public bool CanImportFile(string filepath){
	        if (!File.Exists(filepath)){
	            Viewport_StatusText.instance.ShowStatusText("3d-model file doesn't exist.", false, 1.5f, false);
	            return false;  
	        }
	        return !_isImportingModel;
	    }


	    public void ImportModel_via_Filepath( string filepath ){

	        Debug.Assert( File.Exists(filepath)  &&  _isImportingModel==false );

	        _isImportingModel = true;
	        _Act_onStartedImporting?.Invoke();
        
	        // We simulate the progress text here since Assimp is fast/blocking in this implementation
	        Viewport_StatusText.instance.ShowStatusText($"Importing {Path.GetFileName(filepath)}...", false, 15, true);

	        // Also, store bytes for later use.
	        // If we decide to save project, we'll just dump them into needed location,
	        // without having to convert unity mesh into needed format.
	        _modelBytesCache = File.ReadAllBytes(filepath);
	        _modelBytesCache_filename = Path.GetFileName(filepath);

	        StartCoroutine(ImportRoutine(filepath));
	    }

	    IEnumerator ImportRoutine(string filepath)
	    {
	        // Using our custom AssimpLoader wrapper (AssimpNetter)
	        AssimpLoader loader = new AssimpLoader();
	        GameObject loadedGo = null;
	        string error = "";

	        yield return null; // Wait one frame to allow UI to update (show status text)

	        try {
	            loadedGo = loader.Load(filepath);
	        } 
	        catch(Exception e) {
	            error = e.Message;
	        }

	        if(loadedGo != null){
	            OnSuccess_AcceptModel(loadedGo);
	        } else {
	            OnError(error);
	        }
	    }


	    void OnError(string errorMsg){
	        string statusMsg = $"Importing failed.\nError: {errorMsg}";
	        Viewport_StatusText.instance.ShowStatusText(statusMsg, false, 15, true);
	        Resources.UnloadUnusedAssets();
	        _isImportingModel = false;
	        _Act_onImportComplete?.Invoke(false, null);
	    }


	    void OnSuccess_AcceptModel( GameObject loadedRoot ){
        
	        Resources.UnloadUnusedAssets();
	        if(loadedRoot == null){  _Act_onImportComplete?.Invoke(false, null); return; }

	        //set to true again even if was already true (method might have been called separately)
	        _isImportingModel = true;
	        _latestSuccessRoot = loadedRoot;

	        bool success = o3d.Init(loadedRoot);
	        if(!success){ _Act_onImportComplete?.Invoke(false, _latestSuccessRoot);  return; }

	        _udims_helper.Init_FindAll_UDIMs( o3d.meshes, (pcnt01)=>OnUDIMsProgress01(pcnt01, loadedRoot) );
	    }


	    void OnUDIMsProgress01(float progress01, GameObject rootObj ){
	        Viewport_StatusText.instance.ShowStatusText($"Scanning UVs. Progress: {progress01}", false, 2.5f, false);
	        if(progress01<1.0){ return; }
        
	        o3d.meshes.ForEach(sm => sm.TryChange_SelectionStatus(true, out bool isSuccess));
        
	        OnImportComplete();
	        CreateImportedTextures(rootObj); //AFTER the onComplete callbacks
	    }


	    void OnImportComplete(){
	        if(_modelsHandler_SL!=null){  ModelLoaded_complete_ProjectLoad(); }

	        string msg  = "Model loaded successfuly. Press 'F' to focus on it.";
	        float dur = 3f;
	        if (o3d.scaleWasTooLarge_duringImport){
	            msg += " <b>\nBut its scale/units are massive ..or you have distant objects/polygons," +
	                   " inside the FBX.\nIf any camera/rendering issues, or Depth is white" +
	                   " - resize in your 3d software before exporting</b>";
	            dur = 16;
	        }

	        int numUdims = UDIMs_Helper._allKnownUdims.Count;
	        if (numUdims > 1){
	            msg += $"\nUV outside [0,1] range, so using them as {numUdims} UDIMs." +
	                   $"\n<b>Careful:  {numUdims} udims = {numUdims} projectors every Gen Art.  (more VRAM, lower FPS)</b>";
	            dur = 9;
	        }

	        Viewport_StatusText.instance.ShowStatusText(msg, false, dur, false);
	        _isImportingModel = false;
	        _Act_onImportComplete?.Invoke(true, _latestSuccessRoot);
	    }


	    void CreateImportedTextures(GameObject rootObj){
	        var diffuse = new List<Texture2D>();
	        var normal = new List<Texture2D>();
	        var scannedTextures = new HashSet<Texture>(); // Prevent duplicates

	        // Scan all renderers in the imported object
	        Renderer[] renderers = rootObj.GetComponentsInChildren<Renderer>(true);

	        foreach(var r in renderers)
	        {
	            foreach(var mat in r.sharedMaterials)
	            {
	                if(mat == null) continue;

	                // Check Standard Unity Shader properties (AssimpLoader maps to these)
	                if(mat.HasProperty("_MainTex"))
	                {
	                    Texture t = mat.mainTexture;
	                    if(t != null && t is Texture2D t2d && !scannedTextures.Contains(t))
	                    {
	                        diffuse.Add(t2d);
	                        scannedTextures.Add(t);
	                    }
	                }

	                if(mat.HasProperty("_BumpMap"))
	                {
	                    Texture t = mat.GetTexture("_BumpMap");
	                    if(t != null && t is Texture2D t2d && !scannedTextures.Contains(t))
	                    {
	                        normal.Add(t2d);
	                        scannedTextures.Add(t);
	                    }
	                }
	            }
	        }

	        if(diffuse.Count > 0){
	            GenData2D_Maker.make_ImportedCustomImages(GenerationData_Kind.UvTextures_FromFile, diffuse);
	        }
	        if(normal.Count > 0){
	            GenData2D_Maker.make_ImportedCustomImages(GenerationData_Kind.UvNormals_FromFile, normal);
	        }
	    }

    
	    void ModelLoaded_complete_ProjectLoad(){

	        SD_3D_Mesh_UniqueIDMaker.OnLoad_ResetIds();
	        o3d.meshID_to_mesh.Clear();
	        o3d.nonSelectedMeshes.Clear();
        
	        for(int i=0; i<o3d.meshes.Count; ++i){
	            SD_3D_Mesh_SL sl = _modelsHandler_SL.meshes[i];
	            o3d.meshes[i].Load( sl );
	            o3d.meshID_to_mesh.Add( sl.unique_id, o3d.meshes[i] );//we cleared above, so re-add with a new ID.
	        }

	        HashSet<ushort> selectedMeshesId = new HashSet<ushort>(_modelsHandler_SL.selectedMeshes );
	        for(int i=0; i<o3d.meshes.Count; ++i){
	            bool isSelect =  selectedMeshesId.Contains( o3d.meshes[i].unique_id );
	            bool success;
	            //invokes callbacks etc:
	            o3d.meshes[i].TryChange_SelectionStatus( isSELECT:isSelect, out success,  
	                                                     isDeselectOthers:false, preventDeselect_ifLast:true);
	            if(!isSelect){ o3d.nonSelectedMeshes.Add(o3d.meshes[i]);  }
	        }
	        _modelsHandler_SL = null;
	    }


	    public void SaveCachedMesh_toFile(string pathWithExten=null){
	        if(_modelBytesCache == null){ return; }
        
	        // Define local callback to handle saving (replaces previous flow with confirmation popup)
	        void onComplete(string path){
	            _path_recentlyExported = path;
	            if(string.IsNullOrEmpty(path)){ return; }
	            Directory.CreateDirectory( Path.GetDirectoryName(path) ); //ensure dir exists.
	            File.WriteAllBytes(path, _modelBytesCache);
	            Viewport_StatusText.instance.ShowStatusText("Exported the mesh to\n"+path, false, 5, false);
	        }

	        if(string.IsNullOrEmpty(pathWithExten)){//allow user to select directory manually
	            string fname = Path.GetFileNameWithoutExtension(_modelBytesCache_filename);
	            string exten = Path.GetExtension(_modelBytesCache_filename).TrimStart('.'); // SimpleFileBrowser needs "obj", not ".obj"

	            FileBrowser.SetFilters(true, new FileBrowser.Filter("3D Model", exten));
	            FileBrowser.SetDefaultFilter(exten);

	            FileBrowser.ShowSaveDialog((paths) => {
	                if(paths.Length > 0) onComplete(paths[0]);
	            }, null, FileBrowser.PickMode.Files, false, null, fname, "Save Mesh", "Save");
	        }
	        else{
	            // Path provided directly (e.g. from script or known location), check existing handled by OS or caller logic mostly
	            // But here we just save directly as requested.
	            onComplete(pathWithExten);
	        }
	    }


	    public void SaveDefaultDoor_toFile(string pathWithExten=null){
        
	        void PerformSave(string path){
	             path = Path.ChangeExtension(path, "fbx");
	            _saveFBX_helper.SaveModels(path, o3d.currModelRootGO);
	        }

	        if(string.IsNullOrEmpty(pathWithExten)){//allow user to select directory manually
	            FileBrowser.SetFilters(true, new FileBrowser.Filter("FBX", "fbx"));
	            FileBrowser.SetDefaultFilter("fbx");
            
	            FileBrowser.ShowSaveDialog((paths) => {
	                if(paths.Length > 0) PerformSave(paths[0]);
	            }, null, FileBrowser.PickMode.Files, false, null, "StableProjectorz_door", "Save Door", "Save");
	        }
	        else{
	            PerformSave(pathWithExten);
	        }
	    }


	    public void Save( StableProjectorz_SL spz ){
	        string fp_relativeToDataDir;

	        if (_modelBytesCache != null){
	            fp_relativeToDataDir = _modelBytesCache_filename; 
	            string fp = Path.Combine(spz.filepath_dataDir, fp_relativeToDataDir);
	            SaveCachedMesh_toFile(fp);
	        }else{
	            fp_relativeToDataDir = o3d.currModelRootGO.name + ".fbx";
	            string fp = Path.Combine(spz.filepath_dataDir, fp_relativeToDataDir);
	            _saveFBX_helper.SaveModels(fp, o3d.currModelRootGO);
	        }
	        spz.modelsHandler3D.currModelRootGO = fp_relativeToDataDir;
	    }


	    public void Load(ModelsHandler_3D_SL sl, string dataDir){
	        _modelsHandler_SL = sl;
	        string fp = Path.Combine(dataDir, sl.currModelRootGO);
	        ImportModel_via_Filepath( fp );
	    }


	    void Start(){
	        // Check for child objects (default door logic)
	        GameObject root = transform.childCount>0 ? transform.GetChild(0).gameObject : null;
	        if(root != null)
	        {
	            OnSuccess_AcceptModel(root);
	        }
	        // NOTE: Configuration options that were previously here are now handled by AssimpLoader.
        
	        // 1. "Our inpaint-brush shader needs very smooth model, which has no sharp creases.
	        //    It allows it to fade out the brushing near borders. So 180 and generate."
	        //    -> This is now handled inside AssimpLoader using PostProcessSteps.GenerateSmoothNormals.
        
	        // 2. "ImportNormals = true; ImportTangents = true;"
	        //    -> AssimpLoader uses PostProcessSteps.CalculateTangentSpace.

	        // 3. "ImportBlendShapes = false;// Morpher/BlendShapes glitch the importer"
	        //    -> AssimpLoader logic currently ignores BlendShapes, replicating this behavior.
	    }
	}
}//end namespace
