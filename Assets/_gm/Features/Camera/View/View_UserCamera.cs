using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace spz {

	// There are several View-userCameras, they allow to view models from different angles simultaniously.
	// This is esseintial for the multi-view projection.
	// View-UserCamera can fly around and look at the 3d object.
	// Other cameras (depth, projection) usually copy its parameters.
	public class View_UserCamera : MonoBehaviour
	{
	    [SerializeField] Camera _camera;
	    [SerializeField] Depth_UserCamera _depthCam;
	    [SerializeField] Normals_UserCamera _normalsCam;
	    [SerializeField] Content_UserCamera _contentCam;
	    [SerializeField] VertexColors_UserCamera _vertexColorsCam;
	    [SerializeField] ViewCamera_FOV _fovMgr;
	    [SerializeField] CameraFocus _camFocus;
	    [SerializeField] CameraMove _cameraMove;
	    [SerializeField] CameraDolly _cameraDolly;
	    [SerializeField] CameraOrbit _cameraOrbit;
	#if UNITY_EDITOR
	    [SerializeField] RawImage _img_manualyUpdated_duringEditor;
	#endif

	    public Camera myCamera => _camera;
	    public Depth_UserCamera depthCam => _depthCam;//child of this View_UserCamera
	    public Normals_UserCamera normalsCam => _normalsCam;
	    public Content_UserCamera contentCam => _contentCam;//child of this View_UserCamera
	    public VertexColors_UserCamera vertexColorsCam => _vertexColorsCam;

	    public ViewCamera_FOV fovMgr => _fovMgr;//contains Field-of-view values of this camera. Helps with FOV adjustments.
	    public CameraMove cameraMove => _cameraMove;
	    public CameraFocus cameraFocus => _camFocus;
	    public CameraOrbit cameraOrbit => _cameraOrbit;
	    public CameraDolly cameraDolly => _cameraDolly;

	    //allows to skip frustum cull, during next render, once.
	    bool _dontFrustumCullThisTime = false;


	    // Calculated by us every frame, if some other camera wants
	    // to "hug" its planes around the currently visible 3d meshes.
	    public float tightNearPlane { get; private set; }
	    public float tightFarPlane { get; private set; }


	    // (0,0) is left corner of viewport, (1,1) top right.
	    public Vector2 _projectionMat_center => _contentCam._projectionMat_center;
	    public void Set_ProjMat_center(Vector2 viewportCoord01) => _contentCam.Set_ProjMat_center(viewportCoord01);



	    public Vector3 ViewportToWorldPoint( Vector3 coord ){
	        _camera.projectionMatrix =  ExpandFov_Match_ContentCamFov( with_ShiftPerspectiveCenter:false );
	        Vector3 worldPoint = _camera.ViewportToWorldPoint(coord);
	        RestoreMatrices_Proj_and_Cull();
	        return worldPoint;
	    }


	    public void RenderImmediate_Arr( RenderTexture renderIntoHere,  bool ignore_nonSelected_meshes,  Material withThisMat,
	                                     bool useClearingColor, Color clearingColor, bool dontFrustumCull=false){

	        Debug.Assert(renderIntoHere.dimension == TextureDimension.Tex2DArray, 
	                     "use RenderImmediate if destination texture is not an array.");
        
	        // Disable frustum culling by setting a very large culling matrix
	        _dontFrustumCullThisTime = dontFrustumCull;

	        var prevParams = new ParamsBeforeRender(_camera);

	            //_projectionCamera.enabled = true;  COMMENTED OUT, KEPT FOR PRECAUTION. Keep disabled. Render() still works + avoids automatic renders.

	            var cmd = new CommandBufferScope("Render to TextureArray");

	            var renderers =  ignore_nonSelected_meshes? ModelsHandler_3D.instance.selectedRenderers
	                                                      : ModelsHandler_3D.instance.renderers;
	            bool clearDepth = useClearingColor;
	            cmd.RenderIntoTextureArray( _camera,  renderers,  withThisMat,  useClearingColor, clearDepth, clearingColor, 
	                                        renderIntoHere,  sliceIndex:-1, OnPreCull, OnPreRender, OnPostRender);
         
	        prevParams.RestoreCam(_camera);// Restore the previous camera state
	    }


    
	    public void RenderImmediate( RenderTexture renderIntoHere,  bool ignore_nonSelected_meshes, 
	                                 CameraClearFlags clearFlags=CameraClearFlags.Nothing,  bool allowMSAA=true,  
	                                 bool dontFrustumCull=false, Shader replacementShader =null){
        
	        RenderTexture tex = renderIntoHere;
	        if(tex!=null){ Debug.Assert(tex.dimension == TextureDimension.Tex2D); }
        
	        var prevParams = new ParamsBeforeRender(_camera);
	            _camera.targetTexture = tex;
	            _camera.clearFlags = clearFlags;

	            int maskAll = LayerMask.GetMask("Geometry", "Default", "Geometry Hidden");
	            _camera.cullingMask = ignore_nonSelected_meshes ? LayerMask.GetMask("Geometry") : maskAll;
	            _camera.allowMSAA   = allowMSAA;
	            //_projectionCamera.enabled = true;  COMMENTED OUT, KEPT FOR PRECAUTION. Keep disabled. Render() still works + avoids automatic renders.

	            // Disable frustum culling by setting a very large culling matrix
	            _dontFrustumCullThisTime = dontFrustumCull;

	            if (replacementShader != null){
	                _camera.RenderWithShader(replacementShader, "");
	            }else { 
	                _camera.Render();
	            }

	        prevParams.RestoreCam(_camera);// Restore the previous camera state
	    }


	    // The order is OnPreCull() --> OnPreRender() --> OnPostRender().
	    // We want to adjust the FOV, but need to do it here because OnPreRender() will be after culling was done.
	    // This could cause objects to disappear if they are close to the edge of the screen.
	    void OnPreCull(){
	      #if UNITY_EDITOR
	        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode == false) { return; }
	      #endif
	        _camera.projectionMatrix = ExpandFov_Match_ContentCamFov( with_ShiftPerspectiveCenter:true );

	        _camera.cullingMatrix =  _dontFrustumCullThisTime?  Matrix4x4.Ortho(-100_000, 100_000, -100_000, 100_000, -100_000, 100_000)
	                                                           : _camera.projectionMatrix * _camera.worldToCameraMatrix;
	        _dontFrustumCullThisTime = false;
	    }


	    void OnPreRender(){
	       #if UNITY_EDITOR
	        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode == false){ return; }
	       #endif
	        if (_camera.clearFlags == CameraClearFlags.Skybox){
	            _camera.clearFlags = CameraClearFlags.SolidColor;
	            _camera.backgroundColor = new Color(0, 0, 0, 0);
	        }
	    }

	    void OnPostRender()
	        => RestoreMatrices_Proj_and_Cull();


	    //returns projection matrix
	    public Matrix4x4 ExpandFov_Match_ContentCamFov( bool with_ShiftPerspectiveCenter ){
	        float mainCameraAspect = _camera.aspect;
	        float contentCameraAspect = _contentCam.cameraAspect;
	        Camera c = _camera;
	        // I have 2 cameras: main (this one), and a second (Content camera).
	        // The main camera has fov 20. And Content camera always has the same fov(it copies).
	        // However,  the viewport might be very wide for the main camera, meaning it's aspect can be 4 to 1. 
	        // Assume the main camera has aspect as 1.
	        // We need to figure out the factor by which to scale  the fov of camera so that it visually "matches" that of the Content camera,
	        // given that Content camera's viewport is fitted inside the main camera's viewport.
	        // Ensures that the field of view (FOV) of your main camera matches that of the Content camera in Unity,
	        // especially when the aspect ratio of the Content camera is greater than main camera's aspect (wider than main camera viewport)
	        // This code snippet adjusts the vertical FOV of the main camera to match the horizontal FOV of the Content camera,
	        // maintaining the correct perspective projection:
	        var prevParams = new ParamsBeforeRender(c);
            
	            //cc aspect can be less, depending on value of Width Height sliders in Input panel.
	            bool cc_aspect_smaller =  contentCameraAspect < mainCameraAspect;
            
	            //could occur during start. Else we would remember this messed up value into '_cameraFovBeforeRender':
	            bool c_fov_inadequate  =  c.fieldOfView < 0.001f || c.fieldOfView > 180;

	            if (cc_aspect_smaller || c_fov_inadequate){ 
	                if(with_ShiftPerspectiveCenter){  ShiftPerspectiveCenter(); }
	                Matrix4x4 prjMat = c.projectionMatrix;
	                prevParams.RestoreCam(c);
	                return prjMat;
	            }
	            // Adjust the main camera's vertical FOV to match the Content camera's horizontal FOV
	            _fovMgr.Remember_TrueFov(c.fieldOfView);
            
	            float contentCameraHorizFOV = 2 * Mathf.Atan(Mathf.Tan(c.fieldOfView * Mathf.Deg2Rad / 2) * contentCameraAspect) * Mathf.Rad2Deg;
	            c.fieldOfView = 2 * Mathf.Atan(Mathf.Tan(contentCameraHorizFOV * Mathf.Deg2Rad / 2) / mainCameraAspect) * Mathf.Rad2Deg;
	            c.projectionMatrix = Matrix4x4.Perspective(c.fieldOfView, c.aspect, c.nearClipPlane, c.farClipPlane);
        
	            if(with_ShiftPerspectiveCenter){  ShiftPerspectiveCenter(); }
	            Matrix4x4 projMat = c.projectionMatrix;

	        prevParams.RestoreCam(c);
	        return projMat;
	    }
     

	    void ShiftPerspectiveCenter(){
	        Vector2 vc_viewportSize = MainViewport_UI.instance.mainViewportRect.rect.size;
	        Vector2 cc_viewportSize = MainViewport_UI.instance.innerViewportRect.rect.size;
	        Vector2 sizeFactor = vc_viewportSize / cc_viewportSize;
	        Vector2 perspCenterShift01 = (_projectionMat_center - Vector2.one * 0.5f) / sizeFactor + Vector2.one * 0.5f;
	        CameraTools.ShiftViewportCenter_ofProjMat(_camera, perspCenterShift01);
	    }


	    void RestoreMatrices_Proj_and_Cull(){
	        #if UNITY_EDITOR
	        if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode == false){ return; }
	       #endif
	        if(_fovMgr._trueCameraFov != -1){
	            _camera.fieldOfView = _fovMgr._trueCameraFov;
	            _camera.ResetCullingMatrix();
	            _camera.ResetProjectionMatrix();
	        }
	    }


	    public void OnUpdateParams(){
        
	        if(ModelsHandler_3D.instance == null){ //scenes are probably still loading.
	            tightNearPlane = _camera.nearClipPlane;
	            tightFarPlane  = _camera.farClipPlane;
	            return; 
	        }
	        //recalculate the tightClipPlanes (2 distances to hug the model)
	        var aroundThese = ModelsHandler_3D.instance.selectedMeshes;
	        CameraTools.TightPlanes_around_meshes( _camera, smallestAllowedNearPlane:0.25f, 
	                                               aroundThese, out float near, out float far);
	        tightNearPlane = near;
	        tightFarPlane = far;
	    }


	#region init
	    public void OnInit(){
	        #if UNITY_EDITOR
	        _camera.enabled = true;//NOTICE: enable, even though we use Render(). 
	        #else                   //if we have all cameras Off, unity Editor-scene-camera affects depthmaps...
	        _camera.enabled = false;
	        #endif
	        _camera.depthTextureMode = DepthTextureMode.Depth;
	        UserCameras_MGR.instance.camTextures._Act_CreatedNewRenderTexture -= OnCreatedNewRenderTexture;
	        UserCameras_MGR.instance.camTextures._Act_CreatedNewRenderTexture += OnCreatedNewRenderTexture;
	        UserCameras_MGR.instance.camTextures._Act_WillDestroy_RenderTex -= OnWillDestroyRenderTexture;
	        UserCameras_MGR.instance.camTextures._Act_WillDestroy_RenderTex += OnWillDestroyRenderTexture;
	        _depthCam.OnInit();
	        _contentCam.OnInit();
	        _fovMgr.Remember_TrueFov( _camera.fieldOfView );
	    }

	    void OnDestroy(){
	        _camera.targetTexture = null;
	        if (UserCameras_MGR.instance != null){
	            UserCameras_MGR.instance.camTextures._Act_CreatedNewRenderTexture -= OnCreatedNewRenderTexture;
	            UserCameras_MGR.instance.camTextures._Act_WillDestroy_RenderTex -= OnWillDestroyRenderTexture;
	        }
	    }

	    void OnCreatedNewRenderTexture(RenderTexture rt,  CameraTexType texType ){
	        if(texType != CameraTexType.ViewUserCamera){ return; }
	        _camera.aspect = rt.width / (float)rt.height;
	    }
    
	    void OnWillDestroyRenderTexture( RenderTexture rt,  CameraTexType texType ){
	        if(texType != CameraTexType.ViewUserCamera){ return; }
	    }
	#endregion
	}
}//end namespace
