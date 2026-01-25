using System.Collections.Generic;
using UnityEngine;

namespace spz {

	public class ProjectorCameras_MGR : MonoBehaviour {
	    public static ProjectorCameras_MGR instance { get; private set; }

	    [SerializeField] ProjectorCameras_RenderHelper _renderHelper;
	    [SerializeField] ProjectorCamera _projCamera_PREFAB;

	    //all cameras that cast projection. Order doesn't matter.
	    List<ProjectorCamera> _projCameras = new List<ProjectorCamera>();
	    //we'll draw checkers, projected by this camera, on top of all other projections,
	    //if there is a camera to be temporarily "highlighted". Helps to see us where exactly it will shine.
	    ProjectorCamera _highlight_projCam = null;


	    public void HighlightProjCamera(ProjectorCamera projCam) =>  _highlight_projCam = projCam;//pass 'null' to stop highlighting.

	    public int projCameraIx(ProjectorCamera pc) => _projCameras?.IndexOf(pc)?? -1;
	    public ProjectorCamera ix_toProjCam(int ix) => (ix<0 || ix>=_projCameras.Count)? null : _projCameras[ix];
	    public int num_projCameras => _projCameras.Count;


	    //we will draw black to white colors instead of projections, to show which projection is render when.
	    public bool _showOrderOfProjections{
	        get => _renderHelper._showOrderOfProjections;
	        set => _renderHelper._showOrderOfProjections =value;
	    }

	    //for example, when user scales entire 3d model (all meshes).
	    //We will scale projection cameras too, so that they remain applied to the 3d model.
	    public void ChangeScaleAll(float newGlobalScale){
	        transform.localScale = Vector3.one*newGlobalScale;
	    }

	    public void RenderProjCamera( ProjectorCamera pcam, int pcamIx, RenderUdims intoHereRT){
	        _renderHelper.RenderProjCamera(pcam, intoHereRT, pcamIx,  isHighlight:false);
	    }

	    //can show checker-pattern that's drawn on top of all other projections.
	    //Useful to show where some selected projectorCamera is shining, even if its overlapped by some other projection
	    public void HighlightProjCamera_maybe(RenderUdims intoHereRT){
	        if (_highlight_projCam == null){ return; }
	        _renderHelper.RenderProjCamera(_highlight_projCam, intoHereRT, -1,  isHighlight:true);
	    }

    
	    // each such a camera will eventually have its own 2D art.
	    // The camera will be able to project this art, and can be repositioned wherever.
	    public ProjectorCamera Spawn_ProjCamera(){
	        ProjectorCamera projCam = Instantiate(_projCamera_PREFAB, transform);
	        _projCameras.Add(projCam);
	        return projCam;
	        //NOTICE:  projCam.Init() has to be invoked, but a bit later (by its Generation_Data)
	    }

	    public void Destroy_ProjCamera(ProjectorCamera cam){
	        if (cam == null){ return; }
	        _projCameras.Remove(cam);

	        if(_highlight_projCam == cam){ _highlight_projCam=null; }
	        DestroyImmediate(cam.gameObject);
	    }
    
    
	    void Select_Specific_ProjCamera(ProjectorCamera projCam, bool notify_IconsListUI){
	        if(notify_IconsListUI){
	            IconUI icon =  projCam.myIconUI;
	            GenerationData_Kind kind =  icon._genData.kind;
	            IconUI.Act_OnSomeIconClicked?.Invoke(icon, kind);
	        }
	    }

	    void OnSomeIconUI_selected(IconUI someIcon, GenerationData_Kind kind){
	        if(someIcon==null){return;}
	        GenData2D genData = someIcon._genData;
	        if(genData == null){ return; }
	        if(genData._projCamera == null){ return; }
	        genData._projCamera.Set_IconUI(someIcon);
	        Select_Specific_ProjCamera( genData._projCamera, notify_IconsListUI:false );
	    }

	    void OnCameraPlacements_Restored(GenData2D genData){
	        ProjectorCamera projCam = genData._projCamera;
	        if(projCam == null){ return; }//could be some user's UV texture, etc.
	        Select_Specific_ProjCamera(projCam, notify_IconsListUI:true);
	    }


	#region save/load  init/deinit
	    public void Save(StableProjectorz_SL spz){
	        spz.projectorCameras = new ProjectorCameras_SL();
	        spz.projectorCameras.projCameras = new List<ProjectorCamera_SL>();

	        foreach (var pcam in _projCameras){
	            var projCamSL = new ProjectorCamera_SL();
	            pcam.Save(projCamSL);
	            spz.projectorCameras.projCameras.Add(projCamSL);
	        }
	    }

	    public void Load( StableProjectorz_SL spz ){
        
	        _projCameras.ForEach(pc=>DestroyImmediate(pc));
	        _projCameras.Clear();

	        foreach(var projCamSL in spz.projectorCameras.projCameras){
	             ProjectorCamera projCam = Instantiate(_projCamera_PREFAB, transform);
	            _projCameras.Add(projCam);
	            projCam.Load(projCamSL);
	        }
	    }

	    public void OnAfterLoadedAll(){
	        foreach(var pcam in _projCameras){
	            pcam.Init_AfterLoadedAll();
	        }
	    }


	    void Awake(){
	        if(instance != null){  DestroyImmediate(this.gameObject); return;  }
	        instance = this;
	        IconUI.Act_OnSomeIconClicked += OnSomeIconUI_selected;
	        UserCameras_MGR._Act_OnRestoreCameraPlacements += OnCameraPlacements_Restored;
	    }


	    void OnDestroy(){
	        IconUI.Act_OnSomeIconClicked -= OnSomeIconUI_selected;
	        UserCameras_MGR._Act_OnRestoreCameraPlacements -= OnCameraPlacements_Restored;
	    }
	    #endregion

	}
}//end namespace
