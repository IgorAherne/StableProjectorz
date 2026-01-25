using UnityEngine;


namespace spz {

	//allows to fly in circles around the nearest selected 3D mesh.
	public class CameraOrbit : MonoBehaviour{

	    [SerializeField] View_UserCamera _myViewCam;
	    [SerializeField] float _orbitSpeed = 300;
	    [SerializeField] AnimationCurve _recenterOnPivot_speedCurve;
	    [SerializeField] float _pivotRecenterSpeed = 1;

	    Transform _tempPivot;
	    public bool _isOrbiting => _theCurrentlyOrbiting == this;

	    public static CameraOrbit _theCurrentlyOrbiting { get; private set; } = null;//there can be several cameras (with our script).
	    float _clickStartTime;


	    void OnApplicationFocus(bool focus){
	        //important! Feb 2024 user told that after opening File window they couldn't
	        //manipulate camera anymore, even after focusing the stable projectorz
	        StopOrbit_ifWas();
	    }


	    void OnUpdate(){
	        View_UserCamera nearestCam = UserCameras_MGR.instance.NearestToCursor();
        
	        if(nearestCam==_myViewCam){  StartOrbit_maybe(); }

	        if(_theCurrentlyOrbiting==this){  Orbit_Selection_maybe(); }
	    }


	    void StartOrbit_maybe(){
	        bool pressedThisFrame  =  KeyMousePenInput.isLMBpressedThisFrame();
	        bool hovering_mainView =  MainViewport_UI.instance.isCursorHoveringMe();
	        bool navAllowed = DimensionMode_MGR.instance.is_3d_navigation_allowed;

	        if(!pressedThisFrame || !hovering_mainView || !navAllowed) { return; }
	        _theCurrentlyOrbiting = this;
	        _clickStartTime = Time.time;
	    }

    
	    void Orbit_Selection_maybe(){
	        if(ModelsHandler_3D.instance == null) { return; } //scene is probably still loading

	        bool hasALT   = KeyMousePenInput.isKey_alt_pressed();
	        bool hasCtrl  = KeyMousePenInput.isKey_CtrlOrCommand_pressed();

	        bool stopNow  =  _theCurrentlyOrbiting!=this || !hasALT|| !KeyMousePenInput.isLMBpressed();
	             stopNow |=  KeyMousePenInput.isMMBpressed() || KeyMousePenInput.isRMBpressed();
       
	        if(stopNow){ StopOrbit_ifWas(); return; }

	        Bounds bounds = ModelsHandler_3D.instance.GetTotalBounds_ofSelectedMeshes();

	        if (hasCtrl){
	            SnapCameraDirection(ref bounds);
	        } else {
	            UsualOrbit(ref bounds);
	        }
	    }


	    void StopOrbit_ifWas(){
	        _theCurrentlyOrbiting =  _theCurrentlyOrbiting==this?  null : _theCurrentlyOrbiting;
	    }
    

	    //allows to face the camera towards the 6 sides of the world. 
	    void SnapCameraDirection(ref Bounds bounds){
	        Vector3 closestDirection = nearest45DegreeDir(transform.forward);
	        //see if we picked vertical axis
	        bool isUp =  Mathf.Approximately(closestDirection.y,1);
	        bool isDown =  Mathf.Approximately(closestDirection.y,-1);
	        Vector3 localUp = Vector3.up;
	        if(isUp || isDown){ localUp = nearest45DegreeDir(transform.up); }

	        _tempPivot.SetParent(transform.parent, worldPositionStays:true);
	        _tempPivot.position = bounds.center;
	        _tempPivot.LookAt(transform.position, transform.up);

	        transform.SetParent(_tempPivot, worldPositionStays:true);
	        _tempPivot.LookAt(_tempPivot.position-closestDirection, localUp);

	        transform.SetParent(_tempPivot.parent, worldPositionStays: true);
	    }


	    void UsualOrbit(ref Bounds bounds){
	        Vector2 inputDelta = KeyMousePenInput.delta_while_LMBpressed(normalizeByScreenDiagonal: true);
	        float speed = 2.5f * _orbitSpeed;

	        float inputX = inputDelta.x * speed;
	        float inputY = -inputDelta.y * speed;

	        //for multiview cameras only spin around bounds center. Else, around the orbit-pivot.
	        Vector3 coord = MultiView_Ribbon_UI.instance._isEditingMode ?
	                                           CameraOrbit_ClickPivot.instance.transform.position
	                                         : bounds.center;
	        transform.RotateAround(coord, Vector3.up, inputX);
	        transform.RotateAround(coord, transform.right, inputY);
	    }


	    Vector3 nearest45DegreeDir(Vector3 toThis){
	        //imagine that we need to look towards a center of a cube. Defining directions for:
	        // sides of the cube (6 faces),
	        // looking through 12 segments,
	        // and looking diagonally through 8 vertices.
	        Vector3[] directions = {
	            Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
	            Vector3.up, Vector3.down,
	            (Vector3.forward + Vector3.right).normalized,
	            (Vector3.forward + Vector3.left).normalized,
	            (Vector3.back + Vector3.right).normalized,
	            (Vector3.back + Vector3.left).normalized,
	            (Vector3.up + Vector3.forward).normalized,
	            (Vector3.up + Vector3.back).normalized,
	            (Vector3.up + Vector3.left).normalized,
	            (Vector3.up + Vector3.right).normalized,
	            (Vector3.down + Vector3.forward).normalized,
	            (Vector3.down + Vector3.back).normalized,
	            (Vector3.down + Vector3.left).normalized,
	            (Vector3.down + Vector3.right).normalized,
	            (Vector3.forward + Vector3.right + Vector3.up).normalized,
	            (Vector3.forward + Vector3.left + Vector3.up).normalized,
	            (Vector3.back + Vector3.right + Vector3.up).normalized,
	            (Vector3.back + Vector3.left + Vector3.up).normalized,
	            (Vector3.forward + Vector3.right + Vector3.down).normalized,
	            (Vector3.forward + Vector3.left + Vector3.down).normalized,
	            (Vector3.back + Vector3.right + Vector3.down).normalized,
	            (Vector3.back + Vector3.left + Vector3.down).normalized
	        };
	        float largestDot = float.MinValue;
	        Vector3 closestDirection = Vector3.forward;
	        foreach (Vector3 direction in directions){
	            float dot = Vector3.Dot(toThis, direction);
	            if (dot > largestDot){
	                closestDirection = direction;
	                largestDot = dot;
	            }
	        }
	        return closestDirection;
	    }


	    void Awake(){
	        _tempPivot = new GameObject("CameraOrbit SnappingTempParent").transform;
	    }

	    void Start(){
	        Update_callbacks_MGR.navigation += OnUpdate;
	    }

	    void OnDestroy(){
	        Update_callbacks_MGR.navigation -= OnUpdate;
	    }

	}
}//end namespace
