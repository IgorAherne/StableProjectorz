using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// Clicking on screen will place this pivot onto surface of the objects
	// Reads the depth texture to determine its placement in the world.
	// Camera can then orbit around this pivot.
	public class CameraOrbit_ClickPivot : MonoBehaviour{
	    public static CameraOrbit_ClickPivot instance { get; private set; } = null;

	    [SerializeField] Renderer _pivotSphereRender;
	    [SerializeField] float _meshFadeAfter = 0.1f;
	    [SerializeField] float _meshFadeDur = 0.5f;
	    [SerializeField] float _clickMoveThresh = 0.002f;
	    [SerializeField] float _clickMaxPressTime = 0.4f;
	    [Space(10)]
	    [SerializeField] AnimationCurve _recenterOnPivot_speedCurve;
	    [SerializeField] float _pivotRecenterSpeed = 1;

	    Vector2 _cursorClick_ViewPos;
	    Vector3 _cameraRecenter_toPos;
	    bool _keepRecentering = false;
	    bool _ignoreBlinkColor_requests;

	    float _timeStartedPress;
	    float _time_wasPressing;

	    public void blinkWithColor(){
	        if(_ignoreBlinkColor_requests){ return; }
	        _time_wasPressing=Time.time;//will cause color to become prominent again in Update.
	    }
    
	    public void ForceInFrontOfCamera(){
	        _keepRecentering = false;
	        Transform camTransf = UserCameras_MGR.instance._curr_viewCamera.myCamera.transform;
	        float distance      = (transform.position - camTransf.position).magnitude;
	        transform.position  = camTransf.position + camTransf.forward*distance;
	        _ignoreBlinkColor_requests = true;//moved camera, conceal coordinate until MMB is clicked again.
	    }                                    //Otherwise coord might appear behind objects / in empty space.
        


	    void Update(){
	        if (KeyMousePenInput.isMMBpressedThisFrame()){
	            _cursorClick_ViewPos = KeyMousePenInput.cursorViewPos01();
	            _timeStartedPress = Time.time;
	            _time_wasPressing = Time.time;
	        }
	        ClickPivot_OntoSurface_maybe();
	        RecenterOntoPivot_maybe();
	        RecolorCoord();
	    }

	    void ClickPivot_OntoSurface_maybe(){
	        if (KeyMousePenInput.isMMBreleasedThisFrame()==false){ return; }
        
	        float mag = (KeyMousePenInput.cursorViewPos01()-_cursorClick_ViewPos).magnitude;
	        if(mag > _clickMoveThresh){ return; }//moved the mouse too much, not a click.
	        if(Time.time - _timeStartedPress > _clickMaxPressTime){ return; }

	        Vector2 viewportPos = MainViewport_UI.instance.cursorMainViewportPos01;
	        View_UserCamera vCam = UserCameras_MGR.instance._curr_viewCamera;
	        Camera camera    = vCam.myCamera;

	        // Get the depth from the depth texture
	        float depth = SampleDepth(viewportPos);

	        if(depth == 1.0){ return; }//keep self as is, Clicked into the infinite distance (not a surface)

	        // Convert viewport position to world position
	        Vector3 worldPos = ViewportToWorldPoint(vCam, viewportPos, depth);

	        // Move the pivot (assuming this script is attached to the pivot object)
	        transform.position = worldPos;
	        _pivotSphereRender.transform.position = worldPos;
	        _time_wasPressing = Time.time;
	        _keepRecentering = true;
	        _ignoreBlinkColor_requests = false;

	        //see where the camera will have to shift in world, so that pivot will appear centered on the screen.
	        // Get the pivot position in camera space:
	        Vector3 pivotInCameraSpace = camera.transform.InverseTransformPoint(transform.position);
	        // Compute the translation needed to center the pivot
	        // Then transform the translation from camera space to world space
	        Vector3 translationInCameraSpace = new Vector3(pivotInCameraSpace.x, pivotInCameraSpace.y, 0);
	        Vector3 translationInWorldSpace  = camera.transform.TransformDirection(translationInCameraSpace);
	        _cameraRecenter_toPos = camera.transform.position + translationInWorldSpace;
	    }


	    //uv is a viewport pos [0,1]
	    float SampleDepth(Vector2 uv){
	        RenderTexture depthTex = UserCameras_MGR.instance.camTextures._viewCamDepthLast_linear_ref;
	        Texture2D tex = new Texture2D(1, 1, TextureFormat.RFloat, false);

	        uv.y =  AreTexturesFlipped_Y() ?  1-uv.y  :  uv.y;
	        RenderTexture originalActive = RenderTexture.active;
	        RenderTexture.active = depthTex;
	        tex.ReadPixels(new Rect(uv.x*depthTex.width, uv.y*depthTex.height, 1, 1), 0, 0);
	        tex.Apply();
	        RenderTexture.active = originalActive;

	        float depth = tex.GetPixel(0, 0).r;
	        Destroy(tex);
	        return depth;
	    }


	    // On DirectX, reading pixels from RenderTexture into Texture2D will be upside down.
	    // https://docs.unity3d.com/2018.4/Documentation/Manual/SL-PlatformDifferences.html
	    bool AreTexturesFlipped_Y(){
	        return false; //after updating to Unity 6000 rendered textures don't seem to be upside-down. Jan 2026.

	        // Create a simple orthographic projection matrix
	        Matrix4x4 projectionMatrix = Matrix4x4.Ortho(0, 1, 0, 1, -1, 1);
	        // Get the GPU projection matrix
	        Matrix4x4 gpuProjectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true);
	        // Check if the y-scale has been flipped
	        return gpuProjectionMatrix[1, 1] < 0;
	    }
     

	    //locate point in frustum between nearPlane and farPlane, by the depth in [0,1] range.
	    Vector3 ViewportToWorldPoint(View_UserCamera vCam, Vector2 viewportPoint, float normalizedDepth){
	        //notice, 0, not nearPlane. because depth texture is between 0 and farClipPlane.
	        float z = Mathf.Lerp(0, vCam.myCamera.farClipPlane, normalizedDepth);
	        Vector3 viewportPointWithDepth = new Vector3(viewportPoint.x, viewportPoint.y, z);
	        // Do through vCam, not vCam.myCamera.  The former will prepare fov.
	        // Else build will use an incorrect FOV, compared to editor:
	        return vCam.ViewportToWorldPoint(viewportPointWithDepth);
	    }

	    void RecenterOntoPivot_maybe(){
	        // Stop recentering if the user is interacting with the camera:
	        bool stop =  KeyMousePenInput.isRMBpressed() || KeyMousePenInput.isLMBpressed() || 
	                     KeyMousePenInput.isMMBpressed();
	             stop |= Input.mouseScrollDelta.y != 0;
	             stop |= Settings_MGR.instance.get_isAlwaysFocusCameraPivot()==false;
             
	        if (stop){ _keepRecentering = false; }
	        // Cameras don't care about center on the click-pivot while in the multiview mode
	        if(MultiView_Ribbon_UI.instance._isEditingMode == false){ _keepRecentering = false; }
	        if(!_keepRecentering){ return; }

	        View_UserCamera viewCam = UserCameras_MGR.instance._curr_viewCamera;
        
	        float elapsed =  Time.time - _time_wasPressing;
	        float speed   = _pivotRecenterSpeed * _recenterOnPivot_speedCurve.Evaluate(elapsed);
	        viewCam.transform.position =  Vector3.Lerp( viewCam.transform.position,
	                                                    _cameraRecenter_toPos, 
	                                                    Time.deltaTime*speed );
	    }

	    void RecolorCoord(){
	        float elapsed01 =  (Time.time - _time_wasPressing - _meshFadeAfter) / _meshFadeDur;
	              elapsed01 =  Mathf.Clamp01(elapsed01);
	        float opacity = 1- elapsed01;
	        Color col = Settings_MGR.instance.get_wireframeColor();
	        col.a = opacity;
	        _pivotSphereRender.material.SetColor("_Color", col);
	        float dist  = (transform.position - UserCameras_MGR.instance._curr_viewCamera.transform.position).magnitude;
	        float scale = Mathf.InverseLerp(0, 3, dist);
	              scale = Mathf.Clamp01(scale) * 0.2f;
	        _pivotSphereRender.transform.localScale = new Vector3(scale, scale, scale);
	    }



	    //allows to temporarily conceal self, to avoid being visible in depth map etc.
	    void OnHideSelf_duringRender(bool isHide) => _pivotSphereRender.enabled= !isHide;

	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; } 
	        instance = this;
	        CameraFocus._Act_onFocused += OnFocused;
	        UserCameras_MGR._Act_WillRender_viewCamDepth_ids += OnHideSelf_duringRender;
	    }

	    void OnFocused(CameraFocus focus, Vector3 boundsCenter){
	        transform.position = boundsCenter;
	        _keepRecentering = false;//important if we were recentering 
	    }

	    void OnDestroy(){
	        CameraFocus._Act_onFocused -= OnFocused;
	        UserCameras_MGR._Act_WillRender_viewCamDepth_ids -= OnHideSelf_duringRender;
	    }
	}
}//end namespace
