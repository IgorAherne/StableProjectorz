using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace spz {

	public class UserCameras_MGR : MonoBehaviour{
	    public static UserCameras_MGR instance { get; private set; } = null;

	    [SerializeField] UserCameras_MGR_CamTextures _camTextures;
	              public UserCameras_MGR_CamTextures camTextures => _camTextures;
	    [Space(10)]
	    [SerializeField] Transform _noEditMode_enabledGO;
	    [SerializeField] Transform _editMode_disabledGO;//cameras (except the 'current') are parented here when Editing mode is on
	    [Space(10)]
	    [SerializeField] Shader _normalsShader;
	    [SerializeField] Shader _meshIds_and_depth_shader; //for rendering a view-texture, containing ids of materials. Also renders into depth buffer.
	    [SerializeField] Shader _visualizeLastDepth_shader; //for our showLastDepth_DEBUG().
	    [Space(10)]
	    [SerializeField] Material _uvPreview_bg_mat;//draws a background when previewing UV layout
	                                                //a square image with 1 pixel border. When repeated creates a grid for showing UDIM sectors.
	    [Space(10)]
	    [SerializeField] List<View_UserCamera> _viewCameras;
	    [Space(10)]
	    [SerializeField] UserCameras_UV_warp_Helper _uv_warp_helper;
	    [SerializeField] Camera_UV_NavigateHelper _uv_navigate_helper;


	    public static readonly int MAX_NUM_VIEW_CAMERAS = 6;//how many can show at the same time.
	    public View_UserCamera _curr_viewCamera { get; private set; }
	    public int ix_currentViewCam => _viewCameras.IndexOf(_curr_viewCamera);
	    public int numActiveViewCameras() => _viewCameras.Count(c=>c.gameObject.activeSelf);
	    public int ix_specificViewCam(View_UserCamera cam) => _viewCameras.IndexOf(cam);

	    public static Action<int,bool> _Act_OnTogledViewCamera { get; set; } = null;//ix, isOn.
	    public static Action<int> _Act_OnViewCamera_BecameCurrent { get; set; }
	    public static Action<GenData2D> _Act_OnRestoreCameraPlacements { get; set; } = null;
	    public static Action<float> _Act_OnFovChanged { get; set; } = null;
	    public static Action<bool> _Act_WillRender_viewCamDepth_ids { get; set; } = null;//Allows certain decoration elements to hide. true:began, false:done.


	    Vector3 _selectedObj_center_beforeEditMode;
	    List<CameraPovInfo> _camPovInfos_beforeEditingMode = new List<CameraPovInfo>();

	    Material _normals_mat;
	    Material _uv_preview_bg_mat_cpy;

	    //passing ix=-1 will toggle any first camera that's not equal to 'isOn'.
	    public void ToggleViewCamera(int ix, bool isOn, bool doCallback=true){
	        if (ix == -1){//from 0 onwards:
	            ix = _viewCameras.FindIndex(vc=>vc.gameObject.activeSelf!=isOn);
	        }
	        if(_viewCameras[ix].gameObject.activeSelf==isOn){ return; }
	        _viewCameras[ix].gameObject.SetActive(isOn);//entire gameObject, which also affects its move/focus components.
	        if(!isOn  &&  _viewCameras[ix] == _curr_viewCamera){
	            SetCurrViewCamera( _viewCameras.FindIndex( c=>c.gameObject.activeInHierarchy ) );
	        }
	        if(doCallback){ _Act_OnTogledViewCamera?.Invoke(ix,isOn); }
	    }

	    public void DisableAll_but_CurrViewCam(bool doCallbacks=true){
	        for(int i=0;  i<MAX_NUM_VIEW_CAMERAS; ++i){
	            if(i == ix_currentViewCam){ continue; }
	            ToggleViewCamera(i,false, doCallbacks);
	        }
	    }

	    public void EnableExactly_N_ViewCameras(int num){
	        for(int i=0; i<UserCameras_MGR.MAX_NUM_VIEW_CAMERAS; ++i){
	            bool isEnable = i<num;
	            ToggleViewCamera(i, isEnable);
	            if(i==ix_currentViewCam){ continue; }
	        }
	        if(ix_currentViewCam>=num){
	            SetCurrViewCamera(0);
	        }
	    }

    
	    //even though multile ViewCameras might be on, only 1 is "Current".
	    void SetCurrViewCamera(int ix){
	        if(ix<0){ ix = 0; }
	        if(ix == ix_currentViewCam){ return; }
	        _curr_viewCamera = _viewCameras[ix];
	        _Act_OnViewCamera_BecameCurrent?.Invoke(ix);
	    }

    
	    public void FocusViewCamera(int ix)
	        => _viewCameras[ix].cameraFocus.Focus_Selection_maybe(forceTheFocus:true);

    
	    public View_UserCamera NearestToCursor(){
	        var pins = CamerasMGR_PinsZone_UI.instance;
	        //Check if possibly dragging something. If so, don't update currViewCamera for now:
	        int nearestPin = pins.FindNearestPin();
	        if(nearestPin < 0){ return _curr_viewCamera; }
	        return _viewCameras[nearestPin];
	    }


	    // For perspective-shifting of a given camera.
	    // Traditionally, the perspective-center is in the middle of screen (viewport) for any camera.
	    // But this allows you to shift it around the viewport.
	    // That's important because we want each camera to focus on specific area of viewport.
	    public void Set_ProjMatrixCenter_ofCamera(int cameraIx, Vector2 viewportCoord01){
	        _viewCameras[cameraIx].Set_ProjMat_center(viewportCoord01);
	    }

	    // Gives you information about all view-cameras (even if they are inactive).
	    // maxOneActive: if there are several cameras active, we only record first
	    // of them as 'active', rest are recorded as 'inactive'.
	    public List<CameraPovInfo> get_viewCams_PovInfos(bool maxOneActive=false){
	        var infosList = new List<CameraPovInfo>();
	        bool atLeastOneWasActive = false;
	        for(int i=0; i<_viewCameras.Count; ++i){
	            View_UserCamera vcam = _viewCameras[i];
	            bool isActive  =  (maxOneActive && atLeastOneWasActive)?  false : vcam.gameObject.activeInHierarchy;
	            var camGenInfo =  new CameraPovInfo( isActive,  vcam.transform.position,  vcam.transform.rotation, 
	                                                 vcam.fovMgr._trueCameraFov,  vcam._projectionMat_center );
	            infosList.Add(camGenInfo);
	            atLeastOneWasActive |= isActive;
	        }
	        return infosList;
	    }


	    public void Restore_CamerasPlacements( GenData2D genData ){
	        List<CameraPovInfo> povs     = genData.povInfos.povs.ToList();//a copy, just in case.
	        Vector3 selectedObjectCenter = genData._selected3dModel_pos;
	        for(int i=0; i<povs.Count; ++i){
	            CameraPovInfo inf = povs[i];
	            ToggleViewCamera(i, inf.wasEnabled, doCallback:true);
	            if(!inf.wasEnabled){ continue; }

	            View_UserCamera vcam = _viewCameras[i];
	            vcam.cameraFocus.Restore_CameraPlacement(inf, selectedObjectCenter);
	            vcam.fovMgr.Restore_FieldOfView( inf.camera_fov );
	        }
	        _Act_OnRestoreCameraPlacements?.Invoke(genData);
	    }



	    void OnStartEditMode(MultiView_StartEditMode_Args args){
        
	        if (ModelsHandler_3D.instance == null){ return; }//the scenes are still probably loading.

	        _camPovInfos_beforeEditingMode = get_viewCams_PovInfos();
	        _selectedObj_center_beforeEditMode = ModelsHandler_3D.instance.GetTotalBounds_ofSelectedMeshes().center;

	        for(int i=0; i<_viewCameras.Count; ++i){
	            if(_viewCameras[i] == _curr_viewCamera){ continue; }
	            _viewCameras[i].transform.SetParent(_editMode_disabledGO.transform, worldPositionStays:true);
	        }

	        bool canFocus = args.HasFlag(MultiView_StartEditMode_Args.DontFocusTheCamera) == false;
	        if(canFocus){ 
	            _curr_viewCamera.cameraFocus.Focus_Selection_maybe( forceTheFocus:true );
	        }
	    }


	    void OnStopEditMode( MultiView_StopEdit_Args howToStop ){
	        for(int i=0; i<_viewCameras.Count; ++i){
	            _viewCameras[i].transform.SetParent(_noEditMode_enabledGO.transform, worldPositionStays:true);
	        }
        
	        if(howToStop.HasFlag(MultiView_StopEdit_Args.RestoreToPriorPositions)){
	            //revert camera placements, to how they were before Editing began:
	            var dummy_genData = new GenData2D( GenerationData_Kind.TemporaryDummyNoPics, use_many_icons:true,
	                                               _selectedObj_center_beforeEditMode,
	                                               _camPovInfos_beforeEditingMode);
	            Restore_CamerasPlacements( dummy_genData );
	            dummy_genData.Dispose_internal();
	        }
	    }



	    public void StartFOV_compensatedAdjustment(){
	        _viewCameras.ForEach( vc=>vc.fovMgr.Start_offsetCompensated_FOV() );
	    }

	    public void SetFieldOfView_allCameras(float fov){
	        _viewCameras.ForEach(vc => vc.fovMgr.SetFieldOfView(fov,compensateByDistanceOffset:true));
	        _Act_OnFovChanged?.Invoke(fov);
	    }



	#region Update + rendering
	    void OnUpdate_CameraParams(){
	        _viewCameras.ForEach( v=>v.OnUpdateParams() );
	        _viewCameras.ForEach( v=>v.depthCam.OnUpdateParams() );
	        _viewCameras.ForEach( v=>v.contentCam.OnUpdateParams() );
	        _viewCameras.ForEach( v=>v.vertexColorsCam.OnUpdateParams() );
	    }


	    void OnUpdate_ViewCams_depth_ids_Render(){//widescreen, Main-viewport camera's depth. So that brushing works.

	        if(ModelsHandler_3D.instance == null) { return; }
	        if(ClickSelect_Meshes_MGR.instance == null) { return; }//scenes are probably still loading.

	        bool isSeeAll = ClickSelect_Meshes_MGR.instance._isSelectMode;
	        IReadOnlyList<SD_3D_Mesh> showThese = isSeeAll ? ModelsHandler_3D.instance.meshes : ModelsHandler_3D.instance.selectedMeshes;
	        ModelsHandler_3D.instance.DoForIsolatedMeshes(showThese, render);

	        void render(){ 
	            _Act_WillRender_viewCamDepth_ids?.Invoke(true);//true to indicate we began.
	            Vector2 nearFarPlane = Vector2.zero;
	            bool clear = true;//Using this bool because 'i==0' might actually be inactive cam.

	            for (int i=0; i<_viewCameras.Count; ++i){
	                View_UserCamera vcam = _viewCameras[i];
	                if(vcam.gameObject.activeInHierarchy==false){ continue; }

	                //clear both depth and Color. Because we want to clear the ID view-texture,  and the depth buffer.
	                var flags =  clear? CameraClearFlags.Color : CameraClearFlags.Nothing;

	                // NOTICE: don't ignore non-selected meshes. If they are active, it means we see their wireframe.
	                // In that case we might want to click on one of them, to Show them.  So we do need the id on them.
	                // Don't worry, if a mesh is hidden completely (no wireframe is visible), it will de-activate its Renderer entirely.
	                vcam.RenderImmediate( renderIntoHere:camTextures._viewCam_meshIDs_ref,  ignore_nonSelected_meshes:false,  flags,  
	                                      allowMSAA:false,  dontFrustumCull:false,  replacementShader:_meshIds_and_depth_shader);
	                clear = false;
	                nearFarPlane = new Vector2(vcam.myCamera.nearClipPlane, vcam.myCamera.farClipPlane);
	            }
	            _camTextures.OnUpdated_ViewCameraDepth(nearFarPlane);

	            _Act_WillRender_viewCamDepth_ids?.Invoke(false);//false to indicate we finished
	        }//end render
	    }


	    void OnUpdate_SD_DepthCams_render(){//depth that might be sent to StableDiffusion, if we generate (512x512, etc)
	        _camTextures.OnUpdate_SD_ContentDepth_Started(); //clears the depth textures, etc.

	        //single-channel texture, receives [0,1] color.
	        //Notice, depth will get stored separately in unity's native buffers.
	        RenderTexture r32_linear   = _camTextures._SD_depthCam_RT_R32_linear; 
	        RenderTexture r32_contrast = _camTextures._SD_depthCam_RT_R32_contrast;

	        bool clear = true;//Using this bool because 'i==0' might actually be inactive cam.

	        for (int i=0; i<_viewCameras.Count; ++i){
	            View_UserCamera vcam = _viewCameras[i];
	            if(vcam.gameObject.activeInHierarchy==false){ continue; }

	            //the camera will only render "Geometry" layer.
	            var flags =  clear? CameraClearFlags.Depth : CameraClearFlags.Nothing;
	            vcam.depthCam.RenderDepth_of_Objects(r32_linear, r32_contrast, flags);
	            clear = false;
	        }
	        _camTextures.OnDepthUpdate_End();
	    }


	    void OnUpdate_cams_render(){
	        Render_ViewCameras();
	        RenderContentCameras(_camTextures._contentCam_RT_ref,  force_noWireframeMat:true,  CameraClearFlags.Skybox);
	        Render_NormalCameras();
	    }


   
	    void Render_ViewCameras(){ //depth, normals, view
	        UserCameras_MGR_CamTextures ct = _camTextures;//for convenience.
	        TextureTools_SPZ.ClearRenderTexture(ct._viewCam_RT_ref, Color.black);

	        // see if we should make our preview shaders morph into UV representation.
	        // Assign shader variables for this:
	        float warp_into_uv01 = _uv_warp_helper.warp_into_uv01;
	        bool dontFrustumCull = warp_into_uv01 > 0;
	        Shader.SetGlobalFloat("_GLOBAL_WarpIntoUVSpace01", warp_into_uv01);
	        Shader.SetGlobalFloat("_GLOBAL_inv_cameraAspect01", 1.0f/_curr_viewCamera.myCamera.aspect);
	        Shader.SetGlobalVector("_GLOBAL_InspectUV_Navigate", _uv_navigate_helper.vec4_InspectUV_Navigate);

	        //render view and vertex cams:
	        bool clear = true;//Using this bool because 'i==0' might actually be inactive cam.
	        for (int i=0; i<_viewCameras.Count; ++i){
	            View_UserCamera vcam = _viewCameras[i];
	            if(vcam.gameObject.activeInHierarchy==false){ continue; }
            
	            CameraClearFlags flags;
	            flags =  clear? CameraClearFlags.Skybox : CameraClearFlags.Nothing;
	            //the camera will only render "Geometry" layer:
	            vcam.RenderImmediate(ct._viewCam_RT_ref, ignore_nonSelected_meshes:false, flags, 
	                                 allowMSAA:true, dontFrustumCull:dontFrustumCull );

	            vcam.vertexColorsCam.RenderVertexColors(ct._vertexColorsCam_RT_ref, flags);
	            clear = false;
	        }
        
	        // if we are in the UV mode, slap a grid-texture onto the resulting texture, to show UDIM sectors:
	        if (warp_into_uv01 > 0){
	            _uv_preview_bg_mat_cpy.SetFloat("_Visibility", Mathf.Pow(warp_into_uv01,3));
	            TextureTools_SPZ.Blit(null, ct._viewCam_RT_ref, _uv_preview_bg_mat_cpy);
	        }
	    }


	    //Render the "Content Camera" (similar to view_camera, shows colors but at Stable-diffusion resolution, for example, 512x512).
	    public void RenderContentCameras( RenderTexture dest_2d, bool force_noWireframeMat, 
	                                      CameraClearFlags flags, bool allowMSSA=false ){//mssa smoothese edges, but will cause dilation to mess up.
	        Debug.Assert(dest_2d.dimension==TextureDimension.Tex2D, $"expected a usual 2d texture in {nameof(RenderContentCameras)}");

	        TextureTools_SPZ.ClearRenderTexture(_camTextures._contentCam_RT_ref, Color.black); //MODIF is this needed? We render into dest_2d

	        if(force_noWireframeMat){  //no need to disable meshes manually. But maybe ensure 'wireframe' isn't used:
	            Objects_Renderer_MGR.instance?.TemporaryPreventWireframe_onSelected(isPreventWireframe: true);
	        } // ELSE, KEEP PREVIOUS MATERIALS AS IS.

	        //Using this bool because 'i==0' might actually be inactive camera, so first camera might actually be later:
	        bool clear = true;
	        for(int i=0; i<_viewCameras.Count; ++i){
            
	            View_UserCamera vcam = _viewCameras[i];
	            if(vcam.gameObject.activeInHierarchy==false){ continue; }
            
	            flags =  clear? flags : CameraClearFlags.Nothing;
	            vcam.contentCam.OnUpdateParams();
	            vcam.contentCam.RenderWhatsVisible( dest_2d, flags, allowMSSA:allowMSSA );
	            clear = false;
	        }
	    }



	    void Render_NormalCameras(){
	        if(UserCameras_Permissions.normalsCam_keepRendering.isLocked() == false){ return; }

	        UserCameras_MGR_CamTextures ct = _camTextures;//for convenience.
	        TextureTools_SPZ.ClearRenderTexture( ct._normalsCam_RT_ref, new Color(0.5f, 0.5f, 1, 1)); //neutral, pointing outwards (in tangent space)

	        RenderTexture objNormals_tex;
	        Texture bgNormals_tex;
	        Get_ViewNormals_textures(out objNormals_tex, out bgNormals_tex);

	        Equip_NormalsMaterial( objNormals_tex, ModelsHandler_3D.instance._allSelectedUdims );

	        //Using this bool because 'i==0' might actually be inactive camera, so first camera might actually be later:
	        bool clear = true;
	        for(int i=0; i<_viewCameras.Count; ++i){

	            View_UserCamera vcam = _viewCameras[i];
	            if(vcam.gameObject.activeInHierarchy==false){ continue; }

	            if(clear){  SkyboxBackground_MGR.instance.MomentaryOverride_Texture(bgNormals_tex);  }
                
	                var flags =  (clear && bgNormals_tex!=null)?  CameraClearFlags.Skybox : CameraClearFlags.Nothing;
	                vcam.normalsCam.RenderNormals(ct._normalsCam_RT_ref, ignore_nonSelected_meshes:true, flags);
                
	            if(clear){  SkyboxBackground_MGR.instance.EndMomentaryOverride();  }

	            clear = false;
	        }
	    }


	    void Get_ViewNormals_textures(out RenderTexture objNormals_tex_, out Texture bgNormals_tex_){
	        //we'll need to provide UV-texture for normals, to render the normals camera.
	        //Fetch the last one in the generations manager:
	        GenData2D gen_normalsTex = GenData2D_Archive.instance.Find_GenData_ofKind(GenerationData_Kind.UvNormals_FromFile, search_lastToFirst: true);
        
	        Guid texGuid = gen_normalsTex?.textureGuidsOrdered.FirstOrDefault() ?? default;
	        GenData_TextureRef texRef =  gen_normalsTex?.GetTexture_ref(texGuid);
	        objNormals_tex_ = texRef?.texArray?? null;//textureArray, because it contains several slices (for different UDIMs)

	        gen_normalsTex = GenData2D_Archive.instance.Find_GenData_ofKind(GenerationData_Kind.BgNormals_FromFile, search_lastToFirst: true);

	        texGuid = gen_normalsTex?.textureGuidsOrdered.FirstOrDefault() ?? default;
	        texRef = gen_normalsTex?.GetTexture_ref(texGuid);
	        bgNormals_tex_ =  texRef?.tex2D?? null;
	    }

	    void Equip_NormalsMaterial( RenderTexture uv_normalsTex,  IReadOnlyList<UDIM_Sector> udims ){
	        if(uv_normalsTex != null){ 
	            Debug.Assert(uv_normalsTex.dimension == TextureDimension.Tex2DArray,
	                         "expecting a RenderTexture which is a texture-array, so that UDIMs can work.");
	        }
	        _normals_mat.SetTexture("_NormalsTex", uv_normalsTex);
	        RenderUdims.SetNumUdims(udims, _normals_mat);
	        TextureTools_SPZ.SetKeyword_Material(_normals_mat, "NORMALMAP_IS_EMPTY", uv_normalsTex==null);

	        Objects_Renderer_MGR.instance.EquipMaterial_on_Specific(ModelsHandler_3D.instance.selectedMeshes, _normals_mat);
	    }

	#endregion


	#region save load
	    public void Save(StableProjectorz_SL spz){
	    }

	    public void Load(StableProjectorz_SL spz){

	    }

	    public void OnAfter_AllLoaded(){
	        Guid latestGUID   = GenData2D_Archive.instance.latestGeneration_GUID;
	        GenData2D genData = GenData2D_Archive.instance.GenerationGUID_toData(latestGUID);
	        if(genData != null){ 
	            Restore_CamerasPlacements(genData);
	        }
	    }
	#endregion


	#region init
	    void Awake(){
	        if(instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;
	        _viewCameras.ForEach( cam=>cam.OnInit() );
        
	        MultiView_Ribbon_UI.OnStartEditMode += OnStartEditMode;
	        MultiView_Ribbon_UI.OnStop1_EditMode  += OnStopEditMode;

	        _normals_mat = new Material(_normalsShader);
	        _uv_preview_bg_mat_cpy = new Material(_uvPreview_bg_mat);   _uvPreview_bg_mat=null;//to avoid modifying the original.
	    }

	    void Start(){
	        SetCurrViewCamera(0);
	        ToggleViewCamera(0, true);
	        DisableAll_but_CurrViewCam();
	        //these two should always remain Disabled and Enabled. We'll be parenting cameras under them when needed:
	        _editMode_disabledGO.gameObject.SetActive(false);
	        _noEditMode_enabledGO.gameObject.SetActive(true);

	        Action<RenderTexture> act =  (RenderTexture dest) => RenderContentCameras(dest, force_noWireframeMat:true, CameraClearFlags.Skybox);
	        _camTextures.Init( manualRenderNow_contentCamera:act );

	        Update_callbacks_MGR.cameraParams        += OnUpdate_CameraParams;
	        Update_callbacks_MGR.viewCam_depthRender += OnUpdate_ViewCams_depth_ids_Render;
	        Update_callbacks_MGR.content_depthRender += OnUpdate_SD_DepthCams_render;
	        Update_callbacks_MGR.userCams_render     += OnUpdate_cams_render;
	      #if UNITY_EDITOR
	        _showLastDepthMat = new Material(_visualizeLastDepth_shader);
	      #endif
	    }

	    void OnDestroy(){
	        Update_callbacks_MGR.cameraParams        -= OnUpdate_CameraParams;
	        Update_callbacks_MGR.viewCam_depthRender -= OnUpdate_ViewCams_depth_ids_Render;
	        Update_callbacks_MGR.content_depthRender -= OnUpdate_SD_DepthCams_render;
	        Update_callbacks_MGR.userCams_render     -= OnUpdate_cams_render;
	        DestroyImmediate(_normals_mat);
	        DestroyImmediate(_uv_preview_bg_mat_cpy);
	      #if UNITY_EDITOR
	        DestroyImmediate(_showLastDepthMat);
	      #endif
	    }
	    #endregion



	 //any script can invoke UserCameras_MGr.showLastDepth_DEBUG()
	 //to render the most recently observed depth.
	 //You can then look at this texture from unity inspector panel.
	 #region debug the depth
	#if UNITY_EDITOR
	    Material _showLastDepthMat;
	    public RenderTexture _lastDepth;
	    float _latestDepthTime = -999;
    
	    public void showLastDepth_DEBUG(int expectedWidth, int expectedHeight){
	        Debug.Assert(_latestDepthTime < Time.unscaledTime, 
	                      $"you should only invoke{nameof(showLastDepth_DEBUG)} once per frame");
	        _latestDepthTime = Time.unscaledTime;

	        bool create  = _lastDepth == null;
	             create |= _lastDepth!=null  &&  (_lastDepth.width!=expectedWidth || _lastDepth.height!=expectedHeight);
	        if (create){
	            if(_lastDepth!=null){ DestroyImmediate(_lastDepth); }  
	            _lastDepth = new RenderTexture(expectedWidth, expectedHeight, 0, GraphicsFormat.R32_SFloat);
	        }
	        //Use the material to copy the 'LastCameraDepthTexture' into this _lastDepth RT:

	        // NOTICE, we need to set the _NearClip and _FarClip, but we don't know the camera that produced this depth.
	        // But because we are not asking for ENSURE_LINEAR_01_DEPTH, we won't use those anyway.

	        Graphics.Blit(null, _lastDepth, _showLastDepthMat);
	    }
	#endif
	#endregion

	}
}//end namespace
