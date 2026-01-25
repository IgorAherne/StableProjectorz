using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser; 
using System.Collections;

namespace spz {

	//panel that contains icons of subMeshes, for 3d-models that we've imported.
	public class ModelsHandler_3D_UI : MonoBehaviour {
	    public static ModelsHandler_3D_UI instance { get; private set; }

	    [SerializeField] Button _loadModel_button;
	    [Space(10)]
	    [SerializeField] SlideOut_Widget_UI _import_slideOut;
	    [SerializeField] Button _import_button;
	    [SerializeField] Button _import_andKeepIcons_button;

	    [SerializeField] RectTransform _contentParent; //will be parenting icons for submeshes here
	    [Space(10)]
	    [SerializeField] SD_subMesh_IconUI _subMesh_uiElem_PREFAB;
	    [SerializeField] Button _selectAll_button;
	    [SerializeField] Button _deleteAllNonSelected_button;
	    [SerializeField] ButtonToggle_UI _showVertexColors_toggle;

	    // Icons of all submeshes in our imported 3d-models.
	    // NOTICE: all submeshes, from ALL 3d models are dumped into this list.
	    // But, you can obtain all submeshes given a 3D model, see  allSubmeshes_of_3dModel().
	    List<SD_subMesh_IconUI> _icons = new List<SD_subMesh_IconUI>();

	    //for example, if we are adding/removing the icons in a loop.
	    //We can keep this as false, and then do ResizeGroup at the end, not to waste performance.
	    bool _invoke_resizeGroupEvent = true;
    
	    public bool _useWireframe_onSelected => LeftRibbon_UI.instance.isShowWireframe_onSelected;
	    public bool _showVertexColors_on3d => _showVertexColors_toggle.isPressed;

	    bool _is_importAndKeepIcons = false;//can be manually temporarily set to true while importing a model.


	    public void OnDragAndDrop_3D_File(string file){
	        ConfirmPopup_UI.instance.Show("Import the 3D object?  Make sure to save\nyour work, there is no <b>ctrl+z</b>.", onYes, onNo:null);
	        void onYes() => ModelsHandler_3D.instance.ImportModel_via_Filepath(file);
	    }

    
	    void OnImportModel_button(){
	        _is_importAndKeepIcons = false;
        
	        FileBrowser.SetFilters(true, new FileBrowser.Filter("Models", "obj", "fbx", "glb"));
	        FileBrowser.SetDefaultFilter("obj");
        
	        FileBrowser.ShowLoadDialog( (paths) => {
	            OnImportModel_FileConfirmed(paths);
	        },
	        null, // Cancel callback
	        FileBrowser.PickMode.Files, false, null, null, "Open 3D Model", "Load");
	    }


	    void OnImportModel_andKeepIcons_button(){
	        _is_importAndKeepIcons = true;
        
	        FileBrowser.SetFilters(true, new FileBrowser.Filter("Models", "obj", "fbx", "glb"));
	        FileBrowser.SetDefaultFilter("obj");
        
	        FileBrowser.ShowLoadDialog( (paths) => {
	            OnImportModel_FileConfirmed(paths);
	        },
	        null, // Cancel callback
	        FileBrowser.PickMode.Files, false, null, null, "Open 3D Model", "Load");
	    }


	    void OnImportModel_FileConfirmed(string[] files){
	        invokeOnMainThread();

	        void invokeOnMainThread(){
	            if(files == null || files.Length == 0){ return; }
	            // files[0] gives the path string directly
	            ModelsHandler_3D.instance.ImportModel_via_Filepath( files[0],  keep_art_icons:_is_importAndKeepIcons);
	        }
	    }


	    void OnModelsHandler_ImportDone(GameObject go){
	        _invoke_resizeGroupEvent = false;
	            IReadOnlyList<SD_3D_Mesh> meshes = ModelsHandler_3D.instance.meshes;
	            meshes.ForEach(m => AddMeshesIcon(m));
	        _invoke_resizeGroupEvent = true;
	        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentParent);

	        _is_importAndKeepIcons = false;
	    }


	    void OnWillDestroy_Icon(SD_subMesh_IconUI which){
	        _icons.Remove(which);
	        if(_invoke_resizeGroupEvent){  LayoutRebuilder.ForceRebuildLayoutImmediate(_contentParent);  }
	    }

	    void OnWillDestroyMesh( SD_3D_Mesh mesh ){
	        int ix = _icons.FindIndex( i=>i.myMesh==mesh );
	        if (ix < 0){ return; }
	        _icons.RemoveAt(ix);
	    }



	    void OnSelectAll_button(){
	        //select meshes, which will send events, making our icons light up as well:
	        bool isSuccess;
	        IReadOnlyList<SD_3D_Mesh> meshes = ModelsHandler_3D.instance.meshes; 
	        meshes.ForEach(m=>m.TryChange_SelectionStatus(isSELECT:true, out isSuccess, isDeselectOthers:false));
	    }


	    void OnDeleteAllNonSelected_button(){

	        ConfirmPopup_UI.instance.Show("Remove <b>All Non-Selected</b>?\nThere is no CTRL+Z yet.", onYes, null);
	        void onYes(){
	            if (ModelsHandler_3D.instance._isImportingModel){
	                Viewport_StatusText.instance.ShowStatusText("Can't delete the 3D mesh - we are still importing another 3d model from file.", false, 4, false);
	                return;
	            }

	            _invoke_resizeGroupEvent = false;
	                List<SD_subMesh_IconUI> iconsToRemove =  new List<SD_subMesh_IconUI>();
	                //in reverse order, to avoid list mem reallocations:
	                for(int i=_icons.Count-1; i>=0; --i){
	                    SD_subMesh_IconUI icon = _icons[i];
	                    if (icon.myMesh._isSelected){ continue; }
	                    iconsToRemove.Add(icon);
	                    //_icons.RemoveAt(i);
	                    icon.DestroySelf();
	                }
	            _invoke_resizeGroupEvent = true;
	            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentParent);
	        }
	    }


	    void AddMeshesIcon(SD_3D_Mesh mesh){
	        SD_subMesh_IconUI icon =  GameObject.Instantiate(_subMesh_uiElem_PREFAB, _contentParent);
	        icon.Init(mesh);
	        _icons.Add(icon);
	        if(_invoke_resizeGroupEvent){  LayoutRebuilder.ForceRebuildLayoutImmediate(_contentParent);  }
	    }

	    void OnImportButtonHover(bool isStoppedHover){
	        if(isStoppedHover){ return; }
	        _import_slideOut.Toggle_if_Different(true);
	    }

	#region init
	    void Awake(){
	        if(instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;

	        var entries = _contentParent.GetComponentsInChildren<SD_subMesh_IconUI>(includeInactive: true).ToList();
	        for(int i=0; i<entries.Count; ++i){ 
	            entries[i].DestroySelf(sendEvents:false); 
	        }
	        SD_subMesh_IconUI.Act_OnWillDestroy_Icon += OnWillDestroy_Icon;
	        ModelsHandler_3D.Act_onImported += OnModelsHandler_ImportDone;
	        SD_3D_Mesh.Act_OnWillDestroyMesh += OnWillDestroyMesh;

	        _loadModel_button.onClick.AddListener( OnImportModel_button );
	        _import_button.onClick.AddListener( OnImportModel_button );
	        _import_andKeepIcons_button.onClick.AddListener( OnImportModel_andKeepIcons_button );

	        _selectAll_button.onClick.AddListener( OnSelectAll_button );
	        _deleteAllNonSelected_button.onClick.AddListener( OnDeleteAllNonSelected_button );

	        _loadModel_button.GetComponent<MouseHoverSensor_UI>().onSurfaceEnter += (cursor)=>OnImportButtonHover(isStoppedHover:false);
	        _loadModel_button.GetComponent<MouseHoverSensor_UI>().onSurfaceExit  += (cursor)=>OnImportButtonHover(isStoppedHover:true);
	    }


	    void OnDestroy(){
	        SD_subMesh_IconUI.Act_OnWillDestroy_Icon -= OnWillDestroy_Icon;
	        ModelsHandler_3D.Act_onImported -= OnModelsHandler_ImportDone; 
	    }

	    public void Save(StableProjectorz_SL spz){
	        spz.modelsHandler3D_UI = new ModelsHandler_3D_UI_SL();
	        spz.modelsHandler3D_UI.is_show_VertexColors = _showVertexColors_on3d;
	    }

	    public void Load(StableProjectorz_SL spz){
	        if(spz.modelsHandler3D_UI == null){ return; }
	        _showVertexColors_toggle.ForceSameValueAs( spz.modelsHandler3D_UI.is_show_VertexColors );
	    }

	#endregion
	}
}//end namespace
