using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace spz {

	// For example {2,0} means that this sector covers:
	//  [2,3] in x (U)
	//  [0,1] in y (V)
	[Serializable]
	public struct UDIM_Sector{
	    public bool isNonDefault;
	    public int x; //0-based
	    public int y; //0-based
	    public UDIM_Sector(int x, int y){
	        this.x=x;  this.y=y;  isNonDefault=true;
	    }
	    public UDIM_Sector(int udim_1001_andSoOn){
	        x = (int)((udim_1001_andSoOn-1001)%10);
	        y = (int)((udim_1001_andSoOn-1001)/10);
	        isNonDefault = true;
	    }
	    public int ToInt() => 1001 + x + 10*y;
	    public override string ToString() => isNonDefault?  ToInt().ToString() : "";
	    public Vector4 shader_limits()=>new Vector4(x,y,x+1,y+1);
    
	    public static int SortComparer(UDIM_Sector a, UDIM_Sector b){//used in sorting functions
	        int compareY = a.y.CompareTo(b.y);
	        return compareY!=0? compareY : a.x.CompareTo(b.x);
	    }
	}



	// Helper class of 'ModelsHandler_3D'.
	// Looks at uvs of all the current 3D models, and determines which UDIMs it covers.
	// Assigns each 'SD_3D_Mesh' all the udims that mesh covers.
	// Remembers all the unique udims from all meshes (into hashset).
	//
	// Udim is just an interval on the UV-space, for example:  0 to 1,  1 to 2 etc.
	public class UDIMs_Helper : MonoBehaviour {

	    public static readonly int MAX_NUM_UDIMS = 24;
    
	    int _maxConcurrentMeshes = 8;
	    int _runningTaskCount = 0; // Counter for currently running tasks
	    HashSet<UDIM_Sector> _allKnownUdims_HS = new HashSet<UDIM_Sector>();

	    // Sorted.
	    // Keeps track of all observed UDIMs (even though we also initialize each SD_3D_Mesh).
	    // Kept static so any class can access it (and our non-static methods are only
	    // for ModelsHandler_3d owner to invoke)
	    public static List<UDIM_Sector> _allKnownUdims{ get; private set; } 
	        = new List<UDIM_Sector>{ new UDIM_Sector(0,0) };

	    //re-calculated when mesh selection changes
	    public static List<UDIM_Sector> _allSelectedUdims { get; private set; } 
	        = new List<UDIM_Sector>(){ new UDIM_Sector(0,0) };

	    public static  HashSet<UDIM_Sector> _allSelectedUdims_HS { get; private set; } 
	        = new HashSet<UDIM_Sector>();
    
     
	    // Each SD_3D_Mesh will be initialized with its UDIMs, 
	    // And once all completed, invokes 'progress01' with 1.0
	    public void Init_FindAll_UDIMs( List<SD_3D_Mesh> meshes3d, Action<float> progress01 ){
	        _allKnownUdims_HS.Clear();
	        _allKnownUdims.Clear();
	        _allSelectedUdims.Clear();
	        _allSelectedUdims_HS.Clear();
	        string msg = "Importing, scanning UVs...";
	        float dur = 10;
	        Viewport_StatusText.instance.ShowStatusText(msg, false, dur, false);
	        StopAllCoroutines();
	        StartCoroutine(ManageTaskExecution(meshes3d, progress01));
	    }


	    public void Recalc_selected_UDIMS(){
	        _allSelectedUdims = new List<UDIM_Sector>(_allSelectedUdims);
	        _allSelectedUdims.Sort( UDIM_Sector.SortComparer ); //Important, else {1011,1003} can happen. Aug 2024
	    }


	    public void Add_to_selected_UDIMS( SD_3D_Mesh mesh_addToSelected ){
	        bool addedSome = false;
	        foreach(var sector in mesh_addToSelected._udimSectors){
	            addedSome |= _allSelectedUdims_HS.Add( sector );
	        }
	        if(!addedSome){ return; }//all the udim sectors were already known.
	        _allSelectedUdims = new List<UDIM_Sector>( _allSelectedUdims_HS );
	        _allSelectedUdims.Sort( UDIM_Sector.SortComparer ); //Important, else {1003,1002} can happen. Sept 2024
	    }


	    // Launches several concurrent tasks, and keeps doing so until all meshes were processed.
	    // Doesn't launch a task per mesh, because might hit memory limit
	    IEnumerator ManageTaskExecution(List<SD_3D_Mesh> meshes3d,  Action<float> progress01 ){
	        for (int i=0; i<meshes3d.Count; ++i){
	            // Wait for a frame if the maximum concurrent tasks limit is reached:
	            while (_runningTaskCount >= _maxConcurrentMeshes){ yield return null; } 
	            float pcntDone = Mathf.Min(i/(float)meshes3d.Count, 0.999f);
	            progress01.Invoke(pcntDone);

	            Interlocked.Increment(ref _runningTaskCount);
	            LaunchTask(meshes3d[i]);
	        }
	        while(_runningTaskCount>0){ yield return null; }//Wait for all tasks to complete

	        //create global collection of udims, sorted by y and x:
	        _allKnownUdims = new List<UDIM_Sector>(_allKnownUdims_HS);
	        _allKnownUdims.Sort( UDIM_Sector.SortComparer );

	        if (_allKnownUdims.Count == 0){
	            // sometimes models have no UVs. Oct 2024 version 2.0.2
	            // Important to have at least 1, to avoid RenderTexture.volumeDepth==0 glitches
	            _allKnownUdims.Add(new UDIM_Sector(0, 0));
	            _allKnownUdims_HS.Add(new UDIM_Sector(0, 0));
	        }
	        progress01.Invoke(1.0f); //1.0 to signal that all is done
	    }

	    void LaunchTask(SD_3D_Mesh mesh3d){ // Begins a task for the mesh3d.
	        //access all unity properties now, won't be allowed to access them inside Task:
	        Mesh sharedMesh = mesh3d._sharedMesh;
	        List<Vector2> uvs = new List<Vector2>(sharedMesh.vertexCount);
	        sharedMesh.GetUVs(0, uvs);

	        var task =  Task.Run(() => { return CountUDIMsForMesh(uvs); });

	        task.ContinueWith(t => {
	            mesh3d.InitUDIMs(t.Result);
	            foreach (UDIM_Sector udim in t.Result){
	                _allKnownUdims_HS.Add(udim);
	            }
	            Interlocked.Decrement(ref _runningTaskCount);
	        }, TaskScheduler.FromCurrentSynchronizationContext());
	    }

	    List<UDIM_Sector> CountUDIMsForMesh(List<Vector2> uvs){
	        var udimSet = new HashSet<UDIM_Sector>();
	        foreach (Vector2 uv in uvs){
	            // NOTICE: users had uvs as 1.0 and 1.0 and expected it to still be in first usual udim.
	            // Therefore always Ceil, then subtract 1. Don't Floor.
	            int udimX = Mathf.Max(0, Mathf.CeilToInt(uv.x)-1);
	            int udimY = Mathf.Max(0, Mathf.CeilToInt(uv.y)-1);
	            var udim  = new UDIM_Sector(udimX, udimY);
	            udimSet.Add(udim);
	        }
	        return new List<UDIM_Sector>(udimSet);
	    }


	    //to be invoked by the 'ModelsHandler_3D.cs'
	    public static Dictionary<Texture2D,UDIM_Sector> Determine_UDIMs( Dictionary<Texture2D,string> texturesAndFilepaths ){
        
	        var dict = new Dictionary<Texture2D, UDIM_Sector>();
        
	        if(texturesAndFilepaths.Count == 1){ 
	            return texturesAndFilepaths.ToDictionary( kvp=>kvp.Key,  kvp=>new UDIM_Sector(0,0) );
	        }
	        bool ok = true;
	        foreach(var kvp in texturesAndFilepaths){
	            UDIM_Sector sector;
	            ok &= Extract_UDIM_fromPath(kvp.Value, out sector);
	            dict.Add( kvp.Key, sector );
	        }

	        if (!ok){//some udim couldn't be determined. Fill with default udims.
	            int ix = 1001;
	            var keys = dict.Keys.ToList();
	            foreach (var key in keys){
	                dict[key] = new UDIM_Sector(ix);
	                ix++;
	            }
	        }
	        return dict;
	    }

        
	    static bool Extract_UDIM_fromPath( string fullPath,  out UDIM_Sector sector_){
	        string fileName = Path.GetFileNameWithoutExtension(fullPath);

	        for(int i=fileName.Length-4; i>=0; i--){
	            if(!char.IsDigit(fileName[i+0])){ continue; }
	            if(!char.IsDigit(fileName[i+1])){ continue; }
	            if(!char.IsDigit(fileName[i+2])){ continue; }
	            if(!char.IsDigit(fileName[i+3])){ continue; }
	            int potentialUdim = int.Parse(fileName.Substring(i, 4));
	            if(potentialUdim >= 1000 && potentialUdim <= 2000){
	                sector_ = new UDIM_Sector(potentialUdim);
	                return true;
	            }
	        }//else, no valid udim was deduced:
	        sector_ = new UDIM_Sector(0,0);
	        return false;
	    }


	}
}//end namespace
