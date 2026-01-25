using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Can notify subscribers when some geometry is clicked.
	// Uses a special ID texture for that. (texture has size of viewport, contains ids of objects).
	public class ClickSelect_Meshes_MGR : MonoBehaviour{
	    public static ClickSelect_Meshes_MGR instance { get; private set; } = null;

    
	    bool _manuallyEnabled = false; //when user clicked on the toggle.

	    public bool _isSelectMode { get; private set; } = false;
	    public static Action<SD_3D_Mesh> _Act_OnClickedMesh { get; set; } = null;


	    ClickSelectMeshes_Toggle_UI get_selectMode_toggle()
	        => EventsBinder.FindComponent<ClickSelectMeshes_Toggle_UI>( nameof(ClickSelectMeshes_Toggle_UI) );


	    void OnUpdate(){
	        bool isSelectMode, allowClicks;
	        allow_or_not(out isSelectMode, out allowClicks);
        
	        // Only update UI if the state actually changed, to avoid spamming the component
	        if (_isSelectMode != isSelectMode){
	            _isSelectMode = isSelectMode;
	            if (!_manuallyEnabled){
	                get_selectMode_toggle()?.SetIsOnWithoutNotify(_isSelectMode);
	            }
	        }

	        if(!allowClicks){ return; }
	        Click_maybe();
	    }


	    void allow_or_not(out bool isSelectMode_, out bool isAllowClicks_){
	        isSelectMode_  = true;
	        isAllowClicks_ = true;

	        // maybe disallow ctrl, but only if not pressing Ctrl+A or Ctrl+D, which we need to take care of.
	        bool allow_ctrl  = Settings_MGR.instance.get_ignoreCtrl_if_clickSelectingMeshes()==false
	                            || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D);
        
	        //allow if manually enabled because we want to orbit around.
	        isSelectMode_  =  !KeyMousePenInput.isKey_alt_pressed();
	        isSelectMode_ &=  !KeyMousePenInput.isMMBpressed();
	        isSelectMode_ &=   KeyMousePenInput.isKey_CtrlOrCommand_pressed() && allow_ctrl;
	        isSelectMode_ |=  _manuallyEnabled;

	        isAllowClicks_ = !KeyMousePenInput.isKey_alt_pressed();
	        isAllowClicks_ &= isSelectMode_;
	    }


	    void Click_maybe(){
	        if(KeyMousePenInput.isLMBpressedThisFrame()){ 
	            Vector2 viewportPos = MainViewport_UI.instance.cursorMainViewportPos01;
	            View_UserCamera vCam = UserCameras_MGR.instance._curr_viewCamera;
	            Camera camera    = vCam.myCamera;
	            //get the mesh-id that was encoded in a pixel of id-view-texture:
	            ushort id = SampleMeshId(viewportPos);
	            SD_3D_Mesh mesh = ModelsHandler_3D.instance.getMesh_byUniqueID(id);
	            if(mesh == null){ return; }
	            bool wasSelected = mesh._isSelected;
	            bool isSuccess = false;
	            mesh.TryChange_SelectionStatus(isSELECT: !wasSelected, out isSuccess, 
	                                           isDeselectOthers:false, preventDeselect_ifLast:false );
	            get_selectMode_toggle()?.PlayAnim();
	        }

	        bool any_inputField = KeyMousePenInput.isSomeInputFieldActive();

	        if (Input.GetKeyDown(KeyCode.A) && !any_inputField){
	            IReadOnlyList<SD_3D_Mesh> meshes = ModelsHandler_3D.instance.meshes;
	            for(int i=0; i<meshes.Count; ++i){
	                SD_3D_Mesh m = meshes[i];
	                if(m._isSelected){ continue; }
	                SD_3D_Mesh.SelectAll();
	            }
	            string msg = "All objects Selected. CTRL+Click, or CTRL+D to deselect all.";
	            Viewport_StatusText.instance.ShowStatusText(msg,false, 5, false);
	            get_selectMode_toggle()?.PlayAnim();
	        }

	        if(Input.GetKeyDown(KeyCode.D) && !any_inputField){
	            IReadOnlyList<SD_3D_Mesh> meshes = ModelsHandler_3D.instance.meshes;
	            for(int i=0; i<meshes.Count; ++i){
	                SD_3D_Mesh m = meshes[i];
	                if(!m._isSelected){ continue; }
	                SD_3D_Mesh.DeselectAll();
	            }
	            string msg = "All objects Deselected. CTRL+Click, or CTRL+A to select all.";
	            Viewport_StatusText.instance.ShowStatusText(msg,false, 5, false);
	            get_selectMode_toggle()?.PlayAnim();
	        }
	    }


	    //uv is a viewport pos [0,1]
	    ushort SampleMeshId(Vector2 uv){
	        RenderTexture id_tex = UserCameras_MGR.instance.camTextures._viewCam_meshIDs_ref;
	        Texture2D tex = new Texture2D(1, 1, TextureFormat.RG16, false);

	        uv.y =  AreTexturesFlipped_Y() ?  1-uv.y  :  uv.y;
	        RenderTexture originalActive = RenderTexture.active;
	        RenderTexture.active = id_tex;
	        Rect pixelRect =  new Rect(uv.x*id_tex.width, uv.y*id_tex.height, 1, 1);
	        tex.ReadPixels(pixelRect, 0, 0);
	        tex.Apply();
	        RenderTexture.active = originalActive;

	        Color col = tex.GetPixel(0, 0);
	        ushort meshId = SD_3D_Mesh_UniqueIDMaker.DecodeID_fromColor(col);
	        Destroy(tex);
	        return meshId;
	    }


	    bool AreTexturesFlipped_Y(){
	        return false; //after updating to Unity 6000 rendered textures don't seem to be upside-down. Jan 2026.

	        // Create a simple orthographic projection matrix
	        Matrix4x4 projectionMatrix = Matrix4x4.Ortho(0, 1, 0, 1, -1, 1);
	        // Get the GPU projection matrix
	        Matrix4x4 gpuProjectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true);
	        // Check if the y-scale has been flipped
	        return gpuProjectionMatrix[1, 1] < 0;
	    }


	    void OnToggled_SelectMode(bool isOn){
	        _manuallyEnabled = isOn;
	        string msg = "Show/Hide meshes.  You can just Ctrl+click them to do it easier :)\nAlso, Ctrl+A to select all, or Ctrl+D to deselect all.\n";
	        Viewport_StatusText.instance.ShowStatusText(msg, false, 4, false);
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;

	        StaticEvents.SubscribeAppend<bool>(nameof(ClickSelectMeshes_Toggle_UI)+"_toggle", OnToggled_SelectMode);
	    }

	    void Start(){
	        Update_callbacks_MGR.meshClick_mgr += OnUpdate;
	    }

	    void OnDestroy(){
	        Update_callbacks_MGR.meshClick_mgr -= OnUpdate;
	        StaticEvents.Unsubscribe<bool>(nameof(ClickSelectMeshes_Toggle_UI) + "_toggle", OnToggled_SelectMode);
	    }//end()
	}
}//end namespace
