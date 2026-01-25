using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace spz {

	//helps to pan and zoom a UV-space representation (when Dimension_MGR has 'UV' selected instead of 2D/3D)
	public class Camera_UV_NavigateHelper : MonoBehaviour{

	    [SerializeField] float _zoomSpeed = 1;
	    [SerializeField] float _panSpeed = 4;
	    [SerializeField] float _restoreSpeed = 5;//undoes Move and Zoom when user no longer wants to look at uv.
	    [SerializeField] float _focusDuration = 1;

	    const float _defaultZoom = 1.2f;

	    Vector2 _move;
	    float _zoom_out = _defaultZoom;
	    Coroutine _focusCrtn = null;

	    bool _isPanning = false;
	    bool _isZooming = false;

    
	    //cab be plugged into shaders, for looking around in the UV-representation-plane.
	    public Vector4 vec4_InspectUV_Navigate => new Vector4(_move.x, _move.y, _zoom_out, _zoom_out);


	    void Update(){
	        if(restoreDefaults_maybe()){ return; }//not looking at uvs
	        HandlePanningState();
	        HandleZoomingState();
	        FocusMaybe();
	    }


	    bool restoreDefaults_maybe(){
	        if(DimensionMode_MGR.instance._dimensionMode == DimensionMode.dim_uv){ return false; }
	        _move     = Vector2.Lerp(_move,  Vector2.zero,  Time.deltaTime*_restoreSpeed);
	        _zoom_out = Mathf.Lerp(_zoom_out,  _defaultZoom,  Time.deltaTime*_restoreSpeed);
	        return true; 
	    }


	    void FocusMaybe(){
	        if(MainViewport_UI.instance.isCursorHoveringMe() == false){ return; }
	        if(KeyMousePenInput.isSomeInputFieldActive()) { return; }//maybe typing a prompt
	        if(Input.GetKeyDown(KeyCode.F)){
	            if(_focusCrtn!=null){  StopCoroutine(_focusCrtn); }
	            _focusCrtn = StartCoroutine(Focus_crtn());
	        }
	    }

	    IEnumerator Focus_crtn(){
	        float startTime = Time.time;
	        while(true){
	            float elapsed01  =  (Time.time-startTime) / _focusDuration;
	            elapsed01 = Mathf.Clamp01(elapsed01);
	            _move     = Vector2.Lerp(_move,  Vector2.zero,  elapsed01);
	            _zoom_out = Mathf.Lerp(_zoom_out,  _defaultZoom,  elapsed01);
	            if(elapsed01 == 1){ break; }
	            yield return null;
	        }
	        _focusCrtn = null;
	    }


	    //for panning, requires viewport-hover to begin. Continues even if outside, until button release.
	    void HandlePanningState() {
	        bool isMMBPressed = KeyMousePenInput.isMMBpressed();
	        bool pressedThisFrame = KeyMousePenInput.isMMBpressedThisFrame();
	        bool hovering = MainViewport_UI.instance.isCursorHoveringMe();

	        // Start panning
	        if(pressedThisFrame && hovering) {
	            _isPanning = true;
	        }
	        // Stop panning
	        else if(!isMMBPressed) {
	            _isPanning = false;
	        }

	        // Execute pan if we're in panning state
	        if(_isPanning) {
	            Pan();
	        }
	    }


	    //for zooming, requires viewport-hover to begin. Continues even if outside, until button release.
	    void HandleZoomingState(){
	        bool hasCtrl = KeyMousePenInput.isKey_CtrlOrCommand_pressed();
	        float mouseScroll = hasCtrl ? 0 : Mouse.current.scroll.ReadValue().y;
	        bool hasMouseScroll = mouseScroll != 0 && MainViewport_UI.instance.isCursorHoveringMe();
        
	        bool isRMBPressed = KeyMousePenInput.isRMBpressed();
	        bool pressedThisFrame = KeyMousePenInput.isRMBpressedThisFrame();
	        bool hovering = MainViewport_UI.instance.isCursorHoveringMe();
	        bool hasAlt = KeyMousePenInput.isKey_alt_pressed();

	        // Start zooming
	        if((pressedThisFrame && hovering && hasAlt) || hasMouseScroll) {
	            _isZooming = true;
	        }
	        // Stop zooming
	        else if(!isRMBPressed || !hasAlt) {
	            _isZooming = false;
	        }

	        // Execute zoom if we're in zooming state or have mouse scroll
	        if(_isZooming || hasMouseScroll) {
	            Zoom();
	        }
	    }


	    void Pan(){
	        float aspectRatio = UserCameras_MGR.instance._curr_viewCamera.myCamera.aspect;
	        Vector2 delta = KeyMousePenInput.delta_while_MMBpressed();

	        // Balance both axes based on aspect ratio
	        if (aspectRatio > 1){
	            // Wide screen - normalize both axes relative to height
	            delta.x /= aspectRatio;
	            // delta.y stays as is since height is our reference
	        }else{
	            // Tall screen - normalize both axes relative to width
	            delta.x *= 1;  // width is our reference
	            delta.y *= aspectRatio;
	        }

	        delta.y *= -1;  // Maintain your Y-inversion
	        _move += delta * _panSpeed * _zoom_out;//faster when zoomed out.
	    }


	    void Zoom(){
	        Vector2 delta = KeyMousePenInput.delta_while_RMBpressed();
	        float mouseMovementMagnitude;
	        float zoomDirection;
	        float mouseScroll = Mouse.current.scroll.ReadValue().y;

	        // Use mouse wheel if there's scroll input, else use delta from right mouse button
	        if (mouseScroll != 0){
	            mouseMovementMagnitude = Mathf.Abs(mouseScroll);
	            zoomDirection = mouseScroll >= 0 ? -1 : 1;
	            zoomDirection /= 30.0f;//important! scaling down, but only for the scroll wheel.
	        } else {
	            //users asked to invert vertical zoom jul 2024, so outputing delta.x and -delta.y
	            float predominantAxisValue = Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? delta.x : -delta.y;
	            predominantAxisValue *= -1;
	            mouseMovementMagnitude = Mathf.Abs(predominantAxisValue);
	            mouseMovementMagnitude *= KeyMousePenInput.isKey_alt_pressed() ? 1 : 0;
            
	            zoomDirection = predominantAxisValue >= 0 ? 1 : -1;
	        }
	        float val = mouseMovementMagnitude * zoomDirection * _zoomSpeed;
	        _zoom_out *= (1+val);
	    }
	}
}//end namespace
