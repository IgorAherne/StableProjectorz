using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;


namespace spz {

	public class ModelsHandler_ImporingInfo{
	    public readonly bool isKeep_Art2D_Icons;//should these exist, or be cleared: projections, uv textures, etc
	    public readonly bool isKeep_ArtBG_Icons;//should these exists or be cleared: background images.
	    public readonly bool isKeep_AO_Icons;//for ambient occlusion
	    public ModelsHandler_ImporingInfo(){}
	    public ModelsHandler_ImporingInfo(bool isKeep_Art2D_Icons, bool isKeep_ArtBG_Icons, bool isKeep_AO_Icons){
	        this.isKeep_Art2D_Icons = isKeep_Art2D_Icons;
	        this.isKeep_ArtBG_Icons = isKeep_ArtBG_Icons;
	        this.isKeep_AO_Icons = isKeep_AO_Icons;
	    }
	}


	// sits in the scene, contains ONE model: _currModelRootGO.
	// The model consists of several 'SD_3D_Mesh' children, so we track them here.

	// NOTICE: there is also a UI part of this class, called 'ModelsHandler_3D_UI'.
	// That one lives in canvas and controls icons of each 3d mesh, etc.
	public class ModelsHandler_3D : MonoBehaviour{
	    public static ModelsHandler_3D instance { get; private set; } = null;

	    [SerializeField] Objs3D_Container o3d;
	    [SerializeField] UDIMs_Helper _udims_helper;
	    [SerializeField] ModelsHandler3D_ImportHelper _importHelper;

	    // created while we are importing, and destroyed at the end.
	    // Allows us to keep remembering what we care about during the importing process.
	    ModelsHandler_ImporingInfo _importingInfo = null;

	    public bool _isImportingModel => _importHelper._isImportingModel;
	    public string _path_recentlyExported => _importHelper._path_recentlyExported;
	    public static Action<ModelsHandler_ImporingInfo> Act_onWillLoadModel { get; set; } = null;
	    public static Action<GameObject> Act_onImported { get; set; } = null;


	    public bool hasModelRootGO => o3d.currModelRootGO != null;
	    public string currModelRootGO_name() => o3d.currModelRootGO_name();
	   //into local space of the model. Useful because we rescaled and moved it on import.
	    public Vector3 currModel_InverseTransformPoint(Vector3 worldPos){
	        if (o3d.currModelRootGO == null){ return worldPos; }
	        return o3d.currModelRootGO.transform.InverseTransformPoint(worldPos);
	    }


	    public IReadOnlyList<UDIM_Sector> _allKnownUdims{
	        get { 
	            var list = UDIMs_Helper._allKnownUdims; 
	            list = list.Count>0?  list  :  new List<UDIM_Sector>(){ new UDIM_Sector(0,0) };
	            return list;
	        }
	    }
	    public IReadOnlyList<UDIM_Sector> _allSelectedUdims => UDIMs_Helper._allSelectedUdims;


	    public IReadOnlyList<SD_3D_Mesh> meshes => o3d.meshes;
	    public IReadOnlyList<SD_3D_Mesh> selectedMeshes => o3d.selectedMeshes;
	    public IReadOnlyList<SD_3D_Mesh> nonSelectedMeshes => o3d.nonSelectedMeshes;


	    public IReadOnlyList<Renderer> renderers => o3d.renderers;
	    public IReadOnlyList<Renderer> selectedRenderers => o3d.selectedRenderers;


	    //only valid inside the scope of our DoForIsolatedMeshes(). Cleared outside of it.
	    public IReadOnlyList<SD_3D_Mesh> isolatedMeshes => o3d.isolatedMeshes;
	    public IReadOnlyList<Renderer> isolatedRenderers => o3d.isolatedRenderers;


	    // Each mesh has an 16-bit integer that it generates during its Awake(). We can find all neeeded meshes, given their ids.
	    public List<SD_3D_Mesh> getMeshes_by_uniqueIDs(List<ushort> unique_ids) 
	        => o3d.getMeshes_by_uniqueIDs(unique_ids);
    
	    public SD_3D_Mesh getMesh_byUniqueID(ushort unique_id){//returns null if not found
	        SD_3D_Mesh m = null;
	        o3d.meshID_to_mesh.TryGetValue(unique_id, out m);
	        return m;
	    }


	    // While this function is working, the 'isolatedMeshes' and 'isolatedRenderers' list become active.
	    // And are allowed to be can be accessed by anyone
	    public void DoForIsolatedMeshes( IReadOnlyList<SD_3D_Mesh> isolateAndEnable, Action doSomething )
	        => o3d.DoForIsolatedMeshes(isolateAndEnable, doSomething);

	    public void DoForAllMeshes_EvenIfHidden( Action doSomething )
	        => o3d.DoForAllMeshes_EvenIfHidden(doSomething);


	    //box that encapsulates all mesh renderers.
	    public Bounds GetTotalBounds_ofSelectedMeshes() => o3d.GetTotalBounds_ofSelectedMeshes();



	    //keep_art_icons: preserve projections, textures etc. Useful if user wants to merely update the mesh.
	    public void ImportModel_via_Filepath( string filepath,  bool keep_art_icons=false ){
	        if(!_importHelper.CanImportFile(filepath)){ return; }

	        {// NOTICE: we are doing it REGARDLESS of whether we keep the icons or whether we import distructively:
	         // this will help us if we swap out models.
	            ushort smallest_id = o3d.meshes.Count==0? (ushort)1 : o3d.meshes.Min(m=>m.unique_id);
	            SD_3D_Mesh_UniqueIDMaker.Reset_toExactNextID(smallest_id);
	        }
        
	        _importingInfo = new ModelsHandler_ImporingInfo(isKeep_Art2D_Icons: keep_art_icons, 
	                                                        isKeep_ArtBG_Icons: keep_art_icons, 
	                                                        isKeep_AO_Icons: keep_art_icons);
	        _importHelper.ImportModel_via_Filepath(filepath);
	    }


	    void OnStartedImporting(){
	        _importingInfo = _importingInfo??new ModelsHandler_ImporingInfo();//could be null if Loading a project
	        Act_onWillLoadModel?.Invoke(_importingInfo);                      //In that case, just default stuff.
	        Remove_CurrentModel();
	    }

	    void OnImportModel_Done(bool isSuccess, GameObject loadedRoot){
	        _importingInfo = null;
	        if(!isSuccess){ return; }
         
	        o3d.meshes.ForEach(sm => sm.TryChange_SelectionStatus(true, out bool isSuccessOut, isDeselectOthers:false));

	        //MODIF don't have a black texture array...
	        //int numUdims = _allObservedUdims.Count;
	        //var blackList = new List<Texture>(numUdims);
	        //for(int i=0; i<numUdims; ++i){  blackList.Add(Texture2DArray..blackTexture);  }
	        //var ru = new RenderUdims(0, blackList, _allObservedUdims, texturesBelongToMe:false);
	        //ShowFinalMat_on_ALL( ru );

	        Act_onImported?.Invoke(o3d.currModelRootGO);
	    }


	    void Remove_CurrentModel(){ //the model along with all of its meshes.
	        if(o3d.currModelRootGO == null){ return; }

	        var meshesCopy = o3d.meshes.ToList(); //.ToList() makes copy
	        o3d.meshes.Clear();
	        o3d.meshID_to_mesh.Clear();
	        o3d.renderers.Clear();
	        o3d.selectedMeshes.Clear();
	        o3d.nonSelectedMeshes.Clear();
	        o3d.selectedRenderers.Clear();
	        _udims_helper.Recalc_selected_UDIMS();

	        foreach (SD_3D_Mesh m in meshesCopy){  m?.DestroySelf();  }
	        Destroy(o3d.currModelRootGO); //not DestroyImmediate()
	        o3d.currModelRootGO = null;
	    }

	    void OnSelected_Mesh( SD_3D_Mesh mesh ){
	        if(o3d.selectedMeshes.Contains(mesh)){ return; }
	        o3d.selectedMeshes.Add(mesh);
	        o3d.selectedRenderers.Add(mesh._meshRenderer);
	        o3d.nonSelectedMeshes.Remove(mesh);
	        _udims_helper.Add_to_selected_UDIMS(mesh);
	    }

	    void OnDeselected_Mesh(SD_3D_Mesh mesh){
	        o3d.selectedMeshes.Remove(mesh);
	        o3d.selectedRenderers.Remove(mesh._meshRenderer);
	        if(o3d.nonSelectedMeshes.Contains(mesh)==false){
	            o3d.nonSelectedMeshes.Add(mesh);
	        }//to avoid duplicates (sometimes there are duplicate invocations)
	        _udims_helper.Recalc_selected_UDIMS();
	    }


	    //a mesh of the model. Other meshes will keep existing.
	    void OnWillDestroy_Mesh(SD_3D_Mesh mesh){
	        o3d.meshes.Remove(mesh);
	        o3d.meshID_to_mesh.Remove(mesh.unique_id);
	        o3d.renderers.Remove(mesh._meshRenderer);
	        o3d.selectedMeshes.Remove(mesh);
	        o3d.nonSelectedMeshes.Remove(mesh);
	        o3d.selectedRenderers.Remove(mesh._meshRenderer);
	        if(o3d.meshes.Count==0){ Remove_CurrentModel(); }
	        _udims_helper.Recalc_selected_UDIMS(); //MODIF already happens in OnDeselected_Mesh()?
	    }


	    public void ExportModel(){
	        if (_importHelper._modelBytesCache != null){ 
	            _importHelper.SaveCachedMesh_toFile();
	        }else { 
	            _importHelper.SaveDefaultDoor_toFile();
	        }
	    }


	    public byte[] Get_3dModel_asBytes(out string mesh_extension_){
	        if (_importHelper._modelBytesCache != null){
	            mesh_extension_ = Path.GetExtension( _importHelper._modelBytesCache_filename );
	            return _importHelper._modelBytesCache.ToArray();//to make a copy
	        }else {
	            //dump our default door into file, and read the bytes from there:
	            string path = Directory.GetParent(Application.dataPath).FullName;
	                   path = Path.Combine(path, "door_temp.fbx");
	            _importHelper.SaveDefaultDoor_toFile(path);
	            mesh_extension_ = "fbx";
	            byte[] bytes = File.ReadAllBytes(path);
	            return bytes;
	        }
	    }


	    public void Save( StableProjectorz_SL spz ){
	        spz.modelsHandler3D = new ModelsHandler_3D_SL();
        
	        spz.modelsHandler3D.currModelRoot_scaleAfterImport = o3d.currModelRoot_scaleAfterImport;

	        spz.modelsHandler3D.selectedMeshes = new List<ushort>();
	        foreach (SD_3D_Mesh m in o3d.selectedMeshes){
	            spz.modelsHandler3D.selectedMeshes.Add( m.unique_id );
	        }
	        spz.modelsHandler3D.meshes = new List<SD_3D_Mesh_SL>();
	        for(int i=0; i<o3d.meshes.Count; ++i){
	            var meshSL = new SD_3D_Mesh_SL();
	            o3d.meshes[i].Save( meshSL );
	            spz.modelsHandler3D.meshes.Add(meshSL);
	        }
	        _importHelper.Save(spz);
	    }


	    public void Load( StableProjectorz_SL sl ){
	        _importHelper.Load(sl.modelsHandler3D, sl.filepath_dataDir); //before we start importing from filepath!
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;
	        SD_3D_Mesh.Act_OnWillDestroyMesh += OnWillDestroy_Mesh;
	        SD_3D_Mesh.Act_OnMeshSelected   += OnSelected_Mesh;
	        SD_3D_Mesh.Act_OnMeshDeselected += OnDeselected_Mesh;
	        _importHelper._Act_onStartedImporting += OnStartedImporting;
	        _importHelper._Act_onImportComplete += OnImportModel_Done;
	    }


	    void OnDestroy(){
	        Remove_CurrentModel();
	        SD_3D_Mesh.Act_OnWillDestroyMesh -= OnWillDestroy_Mesh;
	        SD_3D_Mesh.Act_OnMeshSelected   -= OnSelected_Mesh;
	        SD_3D_Mesh.Act_OnMeshDeselected -= OnDeselected_Mesh;

	        if(_importHelper != null){ 
	            _importHelper._Act_onStartedImporting -= OnStartedImporting;
	            _importHelper._Act_onImportComplete -= OnImportModel_Done;
	        }
	    }

	}
}//end namespace
