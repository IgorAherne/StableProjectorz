using System;
using UnityEngine;

namespace spz {

	public class Update_callbacks_MGR : MonoBehaviour{

	    private static Update_callbacks_MGR instance = null;//.instance is only for internal use (to avoid duplicates). All actions are STATIC.
     
	    public static Action navigation { get; set; } = null;
	    public static Action cameraParams { get; set; } = null;//fov, culling etc. But not rendering yet.
	    public static Action viewCam_depthRender { get;set; } = null;//has to be before brushing. Depth that is used for the wide main viewport.
	    public static Action meshClick_mgr { get; set; } = null;
	    public static Action brushing { get; set; } = null;

	    public static Action general_UI { get; set; } = null;

	    public static Action content_depthRender { get; set; } = null;//render scene to make depth, that can be sent to stable diffusion (512x512 etc)
	    public static Action objectsRender { get; set; } = null;//uv-textures, projections, AmbientOcclusion.
	    public static Action userCams_render { get; set; } = null;//final render, to make objects visibile to the user, in Main-View window.
	    public static Action calc_inpaintScreenMask { get; set; } = null;
	    public static Action show_inpaintScreenMask { get; set; } = null;

	    //don't forget there also exists 'EarlyUpdate_callbacks_MGR'.
	    //don't forget there also exists 'LateUpdate_callbacks_MGR'.


	    void Awake(){
	        if(instance != null){  DestroyImmediate(this.gameObject); return; }
	        instance = this;
	    }

	    void Update(){
	        navigation?.Invoke();
	        cameraParams?.Invoke();
	        viewCam_depthRender?.Invoke();
	        meshClick_mgr?.Invoke();
	        brushing?.Invoke();
	        general_UI?.Invoke();
	        EventsBinder.OnUpdate();
	    }

	    void LateUpdate(){
	        content_depthRender?.Invoke();
	        objectsRender?.Invoke();
	        userCams_render?.Invoke();
	        calc_inpaintScreenMask?.Invoke();//after cameras (cams could have rendered screen masks, which the inpaint-panel might show now).
	        show_inpaintScreenMask?.Invoke();
	    }
	}
}//end namespace
