using System;
using System.Collections.Generic;
using UnityEngine;


namespace spz {

	static class SD_3D_Mesh_UniqueIDMaker {
	    static ushort _nextId = 0;
	    public static ushort nextId(){
	        _nextId =  (_nextId+1 > ushort.MaxValue)? (ushort)0 : (ushort)(_nextId+1);
	        return _nextId;
	    }

	    public static ushort Reset_toExactNextID(ushort next_id) 
	        => _nextId = (ushort)(next_id-1);//subtract 1, because in nextId() we add 1 before returning.

	    public static void OnLoad_ResetIds() => _nextId = 0;
    
	    public static void OnLoad_SetNextUniqueID(ushort next_id){ 
	        _nextId =  next_id>_nextId ? next_id : _nextId; 
	    }

	    public static ushort DecodeID_fromColor(Color color)
	        => (ushort)(Mathf.RoundToInt(color.r * 255) |
	                   (Mathf.RoundToInt(color.g * 255) << 8) |
	                   (Mathf.RoundToInt(color.b * 255) << 16));

	    public static Color EncodeID_intoColor(ushort id)
	        => new Color((id & 0xFF) / 255f,
	                     ((id >> 8) & 0xFF) / 255f,
	                      1,  1);
	}


	// 'ModelsHandler_3D' will automatically attached to 3D meshes, after its loaded from disk.
	// This onto all child objects that contain MeshRenderers.
	// This script knows about meshrenderer, meshFilter, collider:
	public class SD_3D_Mesh : MonoBehaviour{
	    bool _destroyed = false;

	    MaterialPropertyBlock _matPropertyBlock;
	    public Renderer _meshRenderer { get; private set; }//either MeshRenderer or SkinnedMeshRenderer
	    public Mesh _sharedMesh { get; private set; }//could be from MeshFilter or SkinnedMeshRenderer
	    public MeshCollider _meshCollider { get; private set; }

	    public ushort unique_id{ get; private set; }//assigned during Awake
	    public List<UDIM_Sector> _udimSectors { get; private set; } = new List<UDIM_Sector>();
	    public void InitUDIMs(List<UDIM_Sector> sectors) => _udimSectors = sectors;

	    public Bounds bounds => _meshRenderer.bounds;
	    public bool _isSelected { get; private set; } = false;
	    public bool _isVisible => _meshRenderer.enabled;
	    public static Action<SD_3D_Mesh> Act_OnMeshSelected { get; set; } = null; //bool means "isDeselectOthers".
	    public static Action<SD_3D_Mesh> Act_OnMeshDeselected { get; set; } = null;
	    public static Action<SD_3D_Mesh> Act_OnWillDestroyMesh { get; set; } = null;

	    static Action<SD_3D_Mesh> _Act_onDeselectAll = null; //argument contains mesh that must remain enabled
	    static Action _Act_OnSelectAll   = null;


	    public void ToggleRender(bool isOn) => _meshRenderer.enabled = isOn;


	    List<Material> _matsList;//to avoid re-creating it several times a frame.
	    public void EquipMaterial( Material matBelongsToSomeone ){
	        // Sometimes meshes have several materials applied (polygon-groups).
	        // Make sure to point them to the material, else surface will appear invisible.
	        // NOTICE: we must assign entire list at once, unity will ignore our [i] attempts.
	        int len   =  _meshRenderer.sharedMaterials.Length;
	        _matsList =  _matsList?? new List<Material>();
	        if(_matsList.Count != len){ 
	            for(int i=0; i<len; i++){  _matsList.Add(matBelongsToSomeone); } 
	        }
	        for(int i=0; i<len; i++){  _matsList[i] = matBelongsToSomeone; } 

	        _meshRenderer.SetSharedMaterials(_matsList);
	        _meshRenderer.SetPropertyBlock(_matPropertyBlock);
	    }


	    public static void SelectAll()  => _Act_OnSelectAll?.Invoke();
	    public static void DeselectAll() => _Act_onDeselectAll?.Invoke(null);

	    void OnSelectAll(){
	        TryChange_SelectionStatus(true,  out bool isSuccess,  isDeselectOthers:false);
	    }

	    void OnDeselectAll(SD_3D_Mesh keepThisOneSelected){
	        if(this == keepThisOneSelected){ return; }
	        TryChange_SelectionStatus( false,  out bool isSuccess,  isDeselectOthers:false, 
	                                   preventDeselect_ifLast:false );
	    }


	    public void TryChange_SelectionStatus(bool isSELECT, out bool isSuccess_, bool isDeselectOthers=true,  
	                                          bool preventDeselect_ifLast=true){
	        // Assign a layer here, because awake might not be the best place.
	        // During import we might wait some time until select is invoked, 
	        // So we wanted the model to remain hidden until this select was called:
	        gameObject.layer =  isSELECT? LayerMask.NameToLayer("Geometry") : LayerMask.NameToLayer("Geometry Hidden");
	        isSuccess_ = true;
	        _isSelected = isSELECT;

	        if (isSELECT){
	            ToggleRender(true);
	            Act_OnMeshSelected?.Invoke(this);
	            if(isDeselectOthers){ _Act_onDeselectAll?.Invoke(this); }//specify 'this' to sure we remain selected.
	        }
	        else{//deselect:
	            bool noneSelected =  ModelsHandler_3D.instance.selectedMeshes.Count <= 1;
	            if (noneSelected && preventDeselect_ifLast){
	                isSuccess_=false;  return;//there has to be at least 1 mesh selected.
	            }
	            ToggleRender(false);
	            Act_OnMeshDeselected?.Invoke(this);
	        }
	    }

    
	    public void NearestFurthest_BoundBoxCoords( Vector3 measureFromHere,  Vector3 measureFromOpposite, 
	                                                ref float smallestDist,  ref float greatestDist ){
	        Vector3 closest  = _meshCollider.ClosestPoint(measureFromHere);
	        Vector3 furthest = _meshCollider.ClosestPoint(measureFromOpposite);

	        float distNear = Vector3.Distance(closest, measureFromHere);
	        float distFar = Vector3.Distance(furthest, measureFromHere);//notice, from camera again.

	        // notice, with offset. This adds extra buffer zone equal to width of bounding box.
	        // If model is not in front of camera, camera's nearest plane clips through model with its side.
	        // That's because the side of the nearest plane is further than its center.
	        // This buffer zone prevents this from ever happening.
	        float bb_halfSize = Mathf.Max( _meshCollider.bounds.extents.x,  _meshCollider.bounds.extents.y );
	              bb_halfSize = Mathf.Max( bb_halfSize,  _meshCollider.bounds.extents.z );
	        distNear -= bb_halfSize;

	        smallestDist = Mathf.Min(smallestDist, distNear); //from camera.
	        greatestDist = Mathf.Max(greatestDist, distFar);
	    }


	    void OnWillDestroyIcon( SD_subMesh_IconUI icon ){
	        if(icon.myMesh != this){ return; }
	        DestroySelf();
	    }

    
	    public void Save(SD_3D_Mesh_SL meshSL){
	        meshSL.unique_id = unique_id;
	        meshSL.udimSectors = _udimSectors;
	    }

	    public void Load(SD_3D_Mesh_SL meshSL){
	        unique_id = meshSL.unique_id;
	        _udimSectors = meshSL.udimSectors;
	        SD_3D_Mesh_UniqueIDMaker.OnLoad_SetNextUniqueID( meshSL.unique_id );
	    }


	    public void DestroySelf(){
	        if (_destroyed){ return; }
	        _destroyed = true;
	        Act_OnWillDestroyMesh?.Invoke(this);
	        Destroy(this.gameObject); //not DestroyImmediate.
	    }

	    void OnDestroy(){
	        _destroyed = true;
	        SD_subMesh_IconUI.Act_OnWillDestroy_Icon -= OnWillDestroyIcon;
	        SD_3D_Mesh._Act_OnSelectAll -= OnSelectAll;
	        SD_3D_Mesh._Act_onDeselectAll -= OnDeselectAll;
	    }

	    void Awake(){
	        unique_id =  SD_3D_Mesh_UniqueIDMaker.nextId();

	        _matPropertyBlock = new MaterialPropertyBlock();
	        //will be used in clicking:
	        Color id_as_color = SD_3D_Mesh_UniqueIDMaker.EncodeID_intoColor(unique_id);
	        _matPropertyBlock.SetVector("_UniqueMeshID", id_as_color);

	        _meshRenderer = gameObject.GetComponent<Renderer>();//either MeshRenderer or SkinnedMeshRenderer

	        var mf  = gameObject.GetComponent<MeshFilter>();
	        var smr = gameObject.GetComponent<SkinnedMeshRenderer>();
	        if(mf!=null){ _sharedMesh = mf.sharedMesh; }
	        else if(smr!=null){ _sharedMesh = smr.sharedMesh; }

	        // manually assign sharedMesh to the collider, because it might not be automatically
	        // populated if have SkinnedMeshRenderer:
	        _meshCollider = gameObject.AddComponent<MeshCollider>();
	        _meshCollider.sharedMesh = _sharedMesh;

	        //needed for our nearestFurthest_inCollider(). 
	        _meshCollider.convex = true;//Concave don't work with collider.ClosestPoint().
	        SD_subMesh_IconUI.Act_OnWillDestroy_Icon += OnWillDestroyIcon;
        
	        SD_3D_Mesh._Act_OnSelectAll += OnSelectAll;
	        SD_3D_Mesh._Act_onDeselectAll += OnDeselectAll;
	    }

	}
}//end namespace
