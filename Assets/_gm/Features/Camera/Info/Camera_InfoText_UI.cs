using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace spz {

	// Component sitting on the uppor portion of the Main View window.
	// Can display info about the camera parameters
	public class Camera_InfoText_UI : MonoBehaviour
	{
	    [SerializeField] TextMeshProUGUI _cam_infoText_pos;
	    [SerializeField] TextMeshProUGUI _cam_infoText_rot;
	    [SerializeField] TextMeshProUGUI _cam_infoText_fov;

	    private void Awake(){
	        EventsBinder.Bind_Clickable_to_event( nameof(Camera_InfoText_UI), this );
	    }

	    void Update(){
	        bool isShowInfoText = Settings_MGR.instance.get_isShow_CameraInfoText();
	        _cam_infoText_pos.transform.parent.gameObject.SetActive( isShowInfoText );
	        if(!isShowInfoText){ return; }

	        Content_UserCamera myContentCam = EventsBinder.FindComponent<Content_UserCamera>(nameof(Content_UserCamera));
	        if(myContentCam == null){ return; } //the scenes are probably still loading

	        // 3d models get scaled down and shifted during import, so that they fit into predefined volume.
	        // Therefore, we need to compensate for it, in our position, but not rotation:
	        Vector3 pos = ModelsHandler_3D.instance.currModel_InverseTransformPoint(myContentCam.transform.position );
	        Vector3 rot = myContentCam.transform.eulerAngles;
	        float fov = myContentCam.myCamera.fieldOfView;

	        _cam_infoText_pos.text =  $"<b>pos</b> ({pos.x.ToString("0.000")},  {pos.y.ToString("0.000")},  {pos.z.ToString("0.000")})";
	        _cam_infoText_rot.text =  $"<b>rot</b> ({rot.x.ToString("0.000")},  {rot.y.ToString("0.000")},  {rot.z.ToString("0.000")})";
	        _cam_infoText_fov.text =  $"<b>fov</b> ({fov.ToString("0.00")})";
	    }
	}
}//end namespace
