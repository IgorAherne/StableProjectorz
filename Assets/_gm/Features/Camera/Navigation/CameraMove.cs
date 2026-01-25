using UnityEngine.InputSystem;
using UnityEngine;

namespace spz {

	//lets user use WASD and look around.
	public class CameraMove : MonoBehaviour{

	    [SerializeField] View_UserCamera _cam;
	    [SerializeField] float _cameraSpeed = 4.0f;
	    [SerializeField] float _rotationSpeed = 10.0f;
	    [SerializeField] AnimationCurve _rotationSpeed_byFov;
	    public float rotationSpeed => _rotationSpeed;

	    static CameraMove _currentMover;

	    private void OnApplicationFocus(bool focus){
	        //important! Feb 2024 user told that after opening File window they couldn't
	        //manipulate camera anymore, even after focusing the stable projectorz
	        #if UNITY_EDITOR
	        return;
	        #endif
	        StopMoveRotate();
	    }


	    void OnUpdate(){
	        View_UserCamera nearestCam = UserCameras_MGR.instance.NearestToCursor();
	        if(nearestCam==_cam){  StartMoveRotate_ifCan(); }
	        if(_currentMover==this){  MoveRotate(); }
	    }


	    void StartMoveRotate_ifCan(){
	        if(_currentMover != null){ return; }
	        bool pressedThisFrame  = KeyMousePenInput.isRMBpressedThisFrame();
	        bool hovering   =  MainViewport_UI.instance.isCursorHoveringMe();
	        bool navAllowed =  DimensionMode_MGR.instance.is_3d_navigation_allowed;
	        if(pressedThisFrame && hovering && navAllowed){
	            _currentMover = this;
	        }
	    }

	    void StopMoveRotate(){
	        _currentMover =  _currentMover==this?  null : _currentMover;
	    }


	    void MoveRotate(){ 
	        if(KeyMousePenInput.isKey_alt_pressed()){ return; }//probably already doing Dolly zoom.
	        if(KeyMousePenInput.isKey_Shift_pressed()){ return; }//possibly resizing the paint-brush via Shift+RightMouseButton
	        if(KeyMousePenInput.isKey_CtrlOrCommand_pressed()){ return; }
        
	        if(!KeyMousePenInput.isRMBpressed()){
	            StopMoveRotate();
	            return; 
	        }
	        Move();
	        Rotate();
	        CameraOrbit_ClickPivot.instance.ForceInFrontOfCamera();
	    }


	    void Rotate(){
	        Vector2 rotationInput = KeyMousePenInput.delta_while_RMBpressed(normalizeByScreenDiagonal: true);

	        float fov = _cam.contentCam.myCamera.fieldOfView;
	        float fovScale01 = _rotationSpeed_byFov.Evaluate(fov);

	        float h = rotationInput.x * _rotationSpeed * fovScale01;
	        float v = rotationInput.y * _rotationSpeed * fovScale01;

	        transform.Rotate(0, h, 0, Space.World); // Rotate around the world's y-axis
	        transform.Rotate(-v, 0, 0); // Rotate around the local x-axis

	        var angles = transform.eulerAngles;
	        angles.z = 0;
	        transform.eulerAngles = angles;
	    }


	    void Move(){
	        // Reading values from Keyboard or another device if needed
	        Vector3 moveInput = new Vector3(
	            Keyboard.current.aKey.isPressed ? -1.0f : Keyboard.current.dKey.isPressed ? 1.0f : 0.0f,
	            Keyboard.current.eKey.isPressed ? 0.8f : Keyboard.current.qKey.isPressed ? -0.8f : 0.0f, // 0.8 because 1 feels too much for up down
	            Keyboard.current.wKey.isPressed ? 1.0f : Keyboard.current.sKey.isPressed ? -1.0f : 0.0f
	        );

	        float fov = _cam.contentCam.myCamera.fieldOfView;
	        float fovRatio = 90f / fov; // Ratio of 90 degrees to current FOV
	        float speedScale = Mathf.Pow(fovRatio, 0.75f); // Cubic root scaling factor

	        Vector3 scale    = _cameraSpeed*Time.deltaTime*Vector3.one;
	                scale.z *= speedScale;

	        moveInput =  new Vector3(moveInput.x*scale.x,  moveInput.y*scale.y,  moveInput.z*scale.z);

	        transform.Translate(moveInput, Space.Self);
	    }

	    void Start(){
	        Update_callbacks_MGR.navigation += OnUpdate;
	    }

	    void OnDestroy(){
	        Update_callbacks_MGR.navigation -= OnUpdate;
	    }

	}
}//end namespace
