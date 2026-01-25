using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace spz {

	// Helper class of the 'ModelsHandler_3D'.
	// Contains all SD_3D_Mesh objects currently in the scene, and allows to iterate them.
	public class Objs3D_Container : MonoBehaviour{
	    //There can be only 1 model, but it can consist of several meshes:
	    public float currModelRoot_scaleAfterImport { get; set; }//<--for example, 0.001
	    public GameObject currModelRootGO { get; set; } = null;

	    public List<SD_3D_Mesh> meshes { get; set; } = new List<SD_3D_Mesh>();
	    public List<Renderer> renderers { get; set; } = new List<Renderer>();

	    public List<SD_3D_Mesh> selectedMeshes { get; set; } = new List<SD_3D_Mesh>();
	    public List<Renderer> selectedRenderers { get; set; } = new List<Renderer>();
	    public List<SD_3D_Mesh> nonSelectedMeshes { get; set; } = new List<SD_3D_Mesh>();

	    // valid only for the duration of our DoForIsolatedMeshes(). Empty when outside of it.
	    public IReadOnlyList<SD_3D_Mesh> isolatedMeshes { get; set; } = new List<SD_3D_Mesh>();
	    public IReadOnlyList<Renderer> isolatedRenderers { get; set; } = new List<Renderer>();


	    public Dictionary<ushort, SD_3D_Mesh> meshID_to_mesh = new Dictionary<ushort, SD_3D_Mesh>();

	    public bool scaleWasTooLarge_duringImport{get; private set;} = false;

	    public string currModelRootGO_name() => currModelRootGO?.name ?? "";

	    // Each mesh has an 16-bit integer that it generates during its Awake().
	    // We can find all neeeded meshes, given their ids.
	    public List<SD_3D_Mesh> getMeshes_by_uniqueIDs( List<ushort> unique_ids ){
	        var found = new List<SD_3D_Mesh>();
	        for(int i=0; i<unique_ids.Count; ++i){
	            ushort id = unique_ids[i];
	            SD_3D_Mesh mesh = null;
	            meshID_to_mesh.TryGetValue(id, out mesh);
	            if(mesh == null){ continue; }
	            found.Add(mesh);
	        }
	        return found;
	    }


	    // While this function is working, the 'isolatedMeshes' and 'isolatedRenderers' list become active.
	    // And are allowed to be can be accessed by anyone
	    public void DoForIsolatedMeshes( IReadOnlyList<SD_3D_Mesh> isolateAndEnable,  Action doSomething ){
	        //only show requred meshes, hide the rest:
	        var wasEnabled = new List<bool>();
	        for(int i=0; i<meshes.Count; ++i){  
	            wasEnabled.Add(meshes[i]._isVisible);
	            meshes[i].ToggleRender(false); 
	        }
	        foreach(var m in isolateAndEnable){ 
	            m.ToggleRender(true); }

	        isolatedMeshes    = isolateAndEnable;
	        isolatedRenderers = isolateAndEnable.Select(m=>m._meshRenderer).ToList();

	        doSomething();//do user instruction

	        // NOTICE: new list, NOT clear. (might have been pointing to someone's list)
	        isolatedMeshes    = new List<SD_3D_Mesh>();
	        isolatedRenderers = new List<Renderer>();

	        //show meshes as was originally:
	        for(int i=0; i<meshes.Count; ++i){  
	            wasEnabled.Add(meshes[i]._isVisible);
	            meshes[i].ToggleRender(wasEnabled[i]); 
	        }
	    }

	    public void DoForAllMeshes_EvenIfHidden( Action doSomething ){
	        var wasEnabled = new List<bool>();
	        for(int i=0; i< meshes.Count; ++i){  
	            wasEnabled.Add(meshes[i]._isVisible);
	            meshes[i].ToggleRender(true); 
	        }
	        doSomething();//do user instruction

	        //show meshes as was originally:
	        for(int i=0; i< meshes.Count; ++i){  
	            wasEnabled.Add(meshes[i]._isVisible);
	            meshes[i].ToggleRender(wasEnabled[i]); 
	        }
	    }


	    // please don't change to much, to avoid depth-precision issues with projections or painting.
	    // Remember that we were tyring to fit the model into small volume when  ModelsHandler3D_ImportHelper.AcceptModel()
	    // via doing RescaleModel_fitIntoVolume().
	    public void ChangeScaleEntireModel(float new_globalScale){
	        if(currModelRootGO == null){ return; }
	        currModelRootGO.transform.localScale =  Vector3.one*new_globalScale*currModelRoot_scaleAfterImport;
	    }

	    //this will prevent issues with depth-testing (when applying projections, painting, etc etc).
	    void RescaleModel_fitIntoVolume(){
	        currModelRootGO.transform.rotation = Quaternion.identity;
	        currModelRootGO.transform.localScale = Vector3.one;//important, before calculating the bounds. Else their sizes would be affected.
	        currModelRootGO.transform.position = Vector3.zero;

	        Renderer[] renderer =  currModelRootGO.GetComponentsInChildren<Renderer>(); //MeshRenderer or SkinnedMeshRenderer
	        if(renderer.Length == 0){ return; }

	        Bounds totalBounds = renderer[0].bounds;
	        for(int i=1; i<renderer.Length; ++i){
	            totalBounds.Encapsulate(renderer[i].bounds);
	        }
	        //excessively large meshes might not scale correctly. Might warn user later.
	        //This might be helpful if user included some "distant light", etc into the FBX, which will mess up the auto-depth.
	        float maxDimension = Mathf.Max(totalBounds.size.x, totalBounds.size.y, totalBounds.size.z);
	        scaleWasTooLarge_duringImport = maxDimension>1001; 

	        float scaleFactor = 3.0f/maxDimension;
	        currModelRoot_scaleAfterImport = scaleFactor;

	        currModelRootGO.transform.localScale =  Vector3.one*scaleFactor;
	        currModelRootGO.transform.position -= totalBounds.center*scaleFactor;
	    }


	    //box that encapsulates all mesh renderers.
	    public Bounds GetTotalBounds_ofSelectedMeshes(){
	        if (selectedMeshes.Count == 0){ return new Bounds(); }

	        Bounds bounds = selectedMeshes[0].bounds;
	        for (int i=1; i<selectedMeshes.Count; ++i){
	            bounds.Encapsulate(selectedMeshes[i].bounds);
	        }
	        return bounds;
	    }


	    public bool Init(GameObject newRootGO){
	        currModelRootGO = newRootGO;

	        if(newRootGO== null){
	            Viewport_StatusText.instance.ShowStatusText("Problem loading a 3d-model. Looks like it's empty.", false, 2.5f, false);
	            return false; 
	        }
	        currModelRootGO.transform.SetParent(transform);
	        RescaleModel_fitIntoVolume();
	        Init_MeshesFromCurrGO();
	        return true;
	    }
    

	    void Init_MeshesFromCurrGO(){
	        Debug.Assert(meshes.Count==0, "meshes should have been despawned + cleared before my Init");

	        Renderer[] renderComponents = currModelRootGO.GetComponentsInChildren<Renderer>();//both MeshRenderer and SkinnedMeshRenderer
        
	        for(int i=0; i<renderComponents.Length; ++i){
	            var sdMesh = renderComponents[i].gameObject.AddComponent<SD_3D_Mesh>();
	            this.meshes.Add(sdMesh);
	            this.meshID_to_mesh.Add(sdMesh.unique_id, sdMesh);
	            this.renderers.Add( renderComponents[i] );
	        }
	    }
	}
}//end namespace
