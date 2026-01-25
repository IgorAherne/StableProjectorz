using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace spz {

	public class SkyboxBackground_Rect_UI : MonoBehaviour
	{
	    [SerializeField] RawImage_with_aspect _uiImage_withAspect;//will assign background texture to be displayed in here.
	    [SerializeField] AspectRatioFitter _uiImage_cullingParent;//will assign background texture to be displayed in here.
	    public RawImage_with_aspect uiImage_withAspect => _uiImage_withAspect;


	    void Awake(){
	        EventsBinder.Bind_Clickable_to_event( nameof(SkyboxBackground_Rect_UI), this);
	    }

	    void Start(){
	        if (SkyboxBackground_MGR.instance != null){
	            SkyboxBackground_MGR.instance.InitOther(this);
	        }
	    }

	    void Update(){
	        _uiImage_cullingParent.aspectRatio = UserCameras_MGR.instance?._curr_viewCamera.contentCam.cameraAspect ?? 1.0f;
	    }
	}
}//end namespace
