using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace spz {

	//one of cameras that belong to ProjectionCameras.
	//Capable of "shining" a texture onto selected 3d objects.
	[RequireComponent(typeof(Camera))]
	public class ProjectorCamera : MonoBehaviour
	{
	    [SerializeField] Camera _projectionCamera;
    
	    //makes textures that show what texels are visibile to the camera
	    //when in some of its point-of-views (POVs).
	    [SerializeField] ProjCam_HelpTextures_Init _init_helper;
    
	    ProjectorCamera_SL _loadedSL; //temporary, only used during Load().
	    List<ushort> _remembered_meshIds = new List<ushort>();//can be used if meshes are destroyed and swapped-out.

	    //those that were 'selected' when Generation was started, and this projector-camera was born.
	    List<SD_3D_Mesh> _myMeshes = new List<SD_3D_Mesh>();
	    List<Renderer> _myRenderers = new List<Renderer>();

	    // Identifier corresponding to a particular stable-diffusion generation request.
	    // Stable diffusion doesn't know about such GUID, it's only used inside unity so we can 
	    // distinguish between different generation requests.
	    public GenData2D _myGenData { get; private set; } = null;
	    public int numPOV => _myGenData._masking_utils.numPOV;


	    // One of 2d-screen-space images (from a batch, which will be "projected" from camera
	    // onto a surface of objects.  We merely reference it, doesn't belong to us.  MIGHT BE NULL.
	    public IconUI myIconUI { get; private set; } = null;
	    public void Set_IconUI(IconUI myIcon){
	        //unsub from callbacks of previous icon:
	        if(this.myIconUI!=null){ 
	            this.myIconUI.Act_OnSomeBlends_sliders -= OnIcon_Some_Blending_Slider;
	            this.myIconUI.Act_OnHSVC_sliders -= OnIcon_Some_HSVC_Slider; 

	        }
	        this.myIconUI = myIcon;  //subscribe for callbacks of this new icon:
	        this.myIconUI.Act_OnSomeBlends_sliders += OnIcon_Some_Blending_Slider;
	        this.myIconUI.Act_OnHSVC_sliders += OnIcon_Some_HSVC_Slider;
	    }

	    void OnIcon_Some_Blending_Slider(){
	        ModelsHandler_3D.instance.DoForIsolatedMeshes(_myMeshes, doSomething:Render_the_Visibilities);
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();
	    }

	    void OnIcon_Some_HSVC_Slider(HueSatValueContrast hsvc){
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();
	    }


	    public void Init(GenData2D genData){
	        _myGenData = genData;
	        _myMeshes  = ModelsHandler_3D.instance.selectedMeshes.ToList();//ToList makes copy
	        _myRenderers = ModelsHandler_3D.instance.selectedMeshes.Select( m=>m._meshRenderer ).ToList();

	        _projectionCamera.enabled = false;//Important. Keep disabled.  Render() will work + avoids automatic renders.
	        _projectionCamera.depthTextureMode = DepthTextureMode.Depth;

	        ModelsHandler_3D.instance.DoForIsolatedMeshes(_myMeshes, doSomething:Render_the_Visibilities);
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();
	    }


	    //produces image that tells if POV is visible to the camera, or is obstructed by some depth.
	    void Render_the_Visibilities(){
	        GenData_Masks utils = _myGenData._masking_utils;

	        _init_helper.Make_Visibilities_and_Alignments( _myMeshes.ToList(),  myIconUI.projBends(),  OnWillRenderPOV );

	        RenderUdims OnWillRenderPOV(int povIx){  
	            return utils._ObjectUV_visibilityR8G8[povIx];
	        }
	    }

	    public class RenderProj_arg{
	        public RenderUdims renderIntoHere;//texture contains projections from preceeding cameras.
	        public Material materialOnGeometry = null;
	        public int onlySpecificPov = -1;
	        public CameraClearFlags clearFlags = CameraClearFlags.Nothing;
	        public RenderProj_arg( RenderUdims renderIntoHere ){
	            this.renderIntoHere = renderIntoHere;
	        }
	    }


	    public void RenderProj_into( RenderProj_arg a ){

	        var prevParams = new ParamsBeforeRender(_projectionCamera);

	            Set_POV_vars_into_material_maybe( a );

	            _projectionCamera.clearFlags = a.clearFlags;
	            _projectionCamera.allowMSAA = false;//important to disable, otherwise edges of UV chunks remain blurry and affect dillation.

	            _projectionCamera.targetTexture = a.renderIntoHere.texArray;

	            //_projectionCamera.enabled = true;  COMMENTED OUT, KEPT FOR PRECAUTION.  Keep disabled.  Render() will work + avoids automatic renders.

	            // Other meshes are disabled, enable only those wich our camera was associated with.
	            // This will make projection "land" in uvs described by those meshes.
	            // All other meshes are disabled, therefore won't "direct" the projectionfrom this camera into their uv locations.
	            CommandBufferScope cmd = new CommandBufferScope("RenderProjCamera");
	            cmd.RenderIntoTextureArray( _projectionCamera, _myRenderers, a.materialOnGeometry, 
	                                        useClearingColor:false, clearTheDepth:false, Color.clear, 
	                                        a.renderIntoHere.texArray, -1);
	        prevParams.RestoreCam( _projectionCamera );
	    }



	    // Fills camera-placement variables in your material shader.
	    // This makes it ready to render from all 6 custom sides (POVs) at once, via single camera.Render() invocation.
	    void Set_POV_vars_into_material_maybe( RenderProj_arg a ){
	        Material mat = a.materialOnGeometry;
	        if(mat == null){ return; }

	        if(numPOV==1){  
	            CameraPovInfo pov = _myGenData.povInfos.get_Nth_active_pov(0);
	            CameraTools.Set_POV_properties_into_mat( mat, _projectionCamera, pov, ixInShader:0, alterTheCamera:true );
	            return;
	        }
	        //if a multi-view (multi-pov) projection. Its masks are additive, not overlay. Ensure sums to 100%:
	        if(a.onlySpecificPov != -1){
	            CameraPovInfo pov = _myGenData.povInfos.povs[a.onlySpecificPov];
	            CameraTools.Set_POV_properties_into_mat(mat, _projectionCamera, pov, ixInShader:0, alterTheCamera:true);
	        }else { 
	            CameraTools.Set_POVs_properties_into_mat(mat, _projectionCamera, _myGenData.povInfos.povs, alterTheCamera:true);
	        }
	    }


	#region Save / Load
	    public void Save(ProjectorCamera_SL projCamSL){
	        projCamSL.genGUID = _myGenData.total_GUID.ToString();
	        projCamSL.myMeshes_uniqueIds = new List<ushort>();
	        _myMeshes.ForEach( m => projCamSL.myMeshes_uniqueIds.Add(m.unique_id) );
	        // no need to save _myRenderers, those will be recalculated, and are just for convenience.
	    }

	    public void Load(ProjectorCamera_SL projCamSL){
	        _loadedSL = projCamSL;
	    }

	    public void Init_AfterLoadedAll(){
	        _projectionCamera.enabled = false;//Important. Keep disabled.  Render() will work + avoids automatic renders.
	        _projectionCamera.depthTextureMode = DepthTextureMode.Depth;
	        _myGenData = GenData2D_Archive.instance.GenerationGUID_toData( new Guid(_loadedSL.genGUID) );
	        _projectionCamera.aspect = _myGenData.camera_aspect();
	    }


	    public void OnWillImport_3dModel(ModelsHandler_ImporingInfo info){
	        if(info.isKeep_Art2D_Icons == false){
	            _remembered_meshIds.Clear();//only clear if NOT keeping the icons. 
	            return;                     //SEE THE COMMENT AT THE END OF OnImported_3dModel()
	        }
	        _myMeshes.ForEach(m => _remembered_meshIds.Add(m.unique_id));
	        _remembered_meshIds = _remembered_meshIds.Distinct().ToList();
	    }


	    // Allows us to complete the Loading. Mesh is loaded from disk asynchronously,
	    // so we complete only when it's ready.
	    public void OnImported_3dModel(GameObject go){
	        if(_loadedSL == null  &&  _remembered_meshIds.Count==0){ return; }
        
	        List<ushort> mesh_ids = _loadedSL?.myMeshes_uniqueIds?? _remembered_meshIds;

	        _myMeshes  = ModelsHandler_3D.instance.getMeshes_by_uniqueIDs( mesh_ids);
	        _myRenderers = _myMeshes.Select( m=>m._meshRenderer ).ToList();

	        _projectionCamera.enabled = false;//Important. Keep disabled.  Render() will work + avoids automatic renders.
	        _projectionCamera.depthTextureMode = DepthTextureMode.Depth;
	        //Commented out, kept for precation. 
	        //Alignments currently alter the brush masks, so don't do them after the project-file loading:
	        //   Init_AlignmentsTexture();
	        _loadedSL = null;

	        // COMMENTED OUT, KEPT FOR PRECAUTION:  do NOT clear the remembered mesh ids.
	        // Even if user imported non-destructively, user might have imported a wrong model that has fewer meshes.
	        // So maintain these ids in case they decide to import a proper model:
	        //      _remembered_meshIds.Clear();
	    }
	#endregion


	#region init/deinit
	    void Awake(){
	        var MAIN_viewCam = UserCameras_MGR.instance._curr_viewCamera;
	        _projectionCamera.nearClipPlane = MAIN_viewCam.tightNearPlane;//TIGHT, else depth is bad. (Far and Sft sliders give bad result).
	        _projectionCamera.farClipPlane  = MAIN_viewCam.myCamera.farClipPlane;//NOT TIGHT, so taht all bad float imprecision is kept away.

	        //NOTICE: not MAIN camera, but CONTENT camera.
	        //Main camera is rendering into our viewport, and has different aspect than the Content Camera.
	        //Content camera renders what gets sent to StableDiffusion, without wireframe, (especially if wide image is requiested (1024x5012)
	        //so we need to use it.  Content camera does copy its parameters from main (at least in v1.0):
	        var contentCam = UserCameras_MGR.instance._curr_viewCamera.contentCam.myCamera;
	        _projectionCamera.fieldOfView = contentCam.fieldOfView;
	        _projectionCamera.aspect = contentCam.aspect;
	        _projectionCamera.orthographic  = contentCam.orthographic;
	        _projectionCamera.orthographicSize = contentCam.orthographicSize;
	        transform.position = contentCam.transform.position;
	        transform.rotation = contentCam.transform.rotation;
	        //very important. Don't rely on depth buffer of render textures.
	        //camera creates its own depth buffer, which can be accessed from shaders:
	        _projectionCamera.depthTextureMode = DepthTextureMode.Depth;

	        ModelsHandler_3D.Act_onWillLoadModel += OnWillImport_3dModel;
	        ModelsHandler_3D.Act_onImported += OnImported_3dModel;
	        SD_3D_Mesh.Act_OnWillDestroyMesh += OnWillDestroyMesh;
	    }

	    void OnDestroy(){
	        ModelsHandler_3D.Act_onWillLoadModel -= OnWillImport_3dModel;
	        ModelsHandler_3D.Act_onImported -= OnImported_3dModel;
	        SD_3D_Mesh.Act_OnWillDestroyMesh -= OnWillDestroyMesh;
	        if(myIconUI!=null){  
	            myIconUI.Act_OnSomeBlends_sliders -= OnIcon_Some_Blending_Slider;
	            myIconUI.Act_OnHSVC_sliders -=  OnIcon_Some_HSVC_Slider;
	        }
	    }

	    void OnWillDestroyMesh(SD_3D_Mesh mesh){
	        bool didHave = _myMeshes.Remove(mesh);
	        if(didHave){ _myRenderers.Remove(mesh._meshRenderer); }
	    }
	#endregion

	}
}//end namespace
