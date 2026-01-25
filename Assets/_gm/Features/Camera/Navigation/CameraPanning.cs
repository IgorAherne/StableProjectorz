using UnityEngine;

namespace spz {

	public class CameraPanning : MonoBehaviour{

	    [SerializeField] float _cameraPan_Speed = 4;
	    [SerializeField] View_UserCamera _myViewCam;
	    [SerializeField] AnimationCurve _panSpeed01_viaAspect;

	    //there can be several camerase (with our script).
	    public static CameraPanning _theCurrentlyPanning { get; private set; } = null;

	    public static float _haveBeenPanningFor = 0; //0 if not panning, else keeps increasing every frame.

	    void OnUpdate(){
	        StartMoveRotate_ifCan();
	        MoveRotate();
	    }

	     void StartMoveRotate_ifCan(){
	        bool pressedThisFrame  = KeyMousePenInput.isMMBpressedThisFrame();
	        bool hovering =  MainViewport_UI.instance.isCursorHoveringMe();
        
	        //we are only allowed to pan only if in the editing mode. (There will be only 1 camera during edit).
	        bool isEditing_inViewport =  MultiView_Ribbon_UI.instance._isEditingMode;
	        if(!isEditing_inViewport){ return; }

	        if(UserCameras_MGR.instance._curr_viewCamera != _myViewCam){ return; }//some other camera is used during the edit.

	        if(_theCurrentlyPanning != null){ return; }

	        if(DimensionMode_MGR.instance.is_3d_navigation_allowed == false){ return; }

	        if(pressedThisFrame && hovering){ 
	            _theCurrentlyPanning = this;
	            _haveBeenPanningFor = 0;
	        }
	    }


	    void MoveRotate(){
	        if(_theCurrentlyPanning!=this){ return; }
	        // COMMENTED OUT, KEPT FOR PRECAUTION. 
	        // Users have specifically mentioned Alt+MMB for panning.
	        // if (KeyMousePenInput.isKey_alt_pressed()){
	        //   _theCurrentlyPanning=null; return; }//doing something else.

	        if(KeyMousePenInput.isKey_CtrlOrCommand_pressed()){ 
	            _theCurrentlyPanning=null; _haveBeenPanningFor=0; return; }//doing something else.

	        if(KeyMousePenInput.isMMBpressed()==false){  
	            _theCurrentlyPanning=null; _haveBeenPanningFor=0; return; }
	        Pan();
	        _haveBeenPanningFor += Time.deltaTime;
	    }


	    void Pan(){
	        Vector3 centeOfMeshes = ModelsHandler_3D.instance.GetTotalBounds_ofSelectedMeshes().center;
	        float distToMeshes = (transform.position - centeOfMeshes).magnitude;

	        // Reading values from Keyboard or another device if needed. -1* to invert it (for dragging)
	        Vector2 delta = -1 * KeyMousePenInput.delta_while_MMBpressed();

	        float fov = _myViewCam.contentCam.myCamera.fieldOfView;
	        float aspectRatio = _myViewCam.contentCam.cameraAspect;

	        // Calculate the FOV scaling factor
	        float fovRatio = fov / 90f; // Ratio of current FOV to 90 degrees
	        float fovScale = Mathf.Pow(2f, fovRatio) - 1f; // Exponential scaling factor

	        // Combine the FOV and aspect ratio scaling factors
	        float combinedScale = fovScale * _panSpeed01_viaAspect.Evaluate(aspectRatio);

	        Vector3 moveInput = new Vector3(delta.x, delta.y, 0);
	        moveInput *= _cameraPan_Speed * distToMeshes * combinedScale;

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
