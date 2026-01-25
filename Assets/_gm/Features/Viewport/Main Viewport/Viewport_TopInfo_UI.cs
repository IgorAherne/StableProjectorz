using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

namespace spz {

	public class Viewport_TopInfo_UI : MonoBehaviour {
	    public static Viewport_TopInfo_UI instance{ get; private set; } = null;

	    [SerializeField] TextMeshProUGUI _currProj_cam_text;
	    [SerializeField] TextMeshProUGUI _curr_3D_Obj_text;

	    public void ShowTextProjCamera(int cameraIx, string generationGUID, int curr_artIx_ofCamera){
	        _currProj_cam_text.text = "Camera " +cameraIx + "\t\t"+ generationGUID + "\t\tart " + curr_artIx_ofCamera;
	    }

	    public void UpdateInfo(){
	        StringBuilder sb = new StringBuilder();
	        // Append the model name:
	        string wholeModelName = ModelsHandler_3D.instance.currModelRootGO_name();
	        sb.AppendLine(wholeModelName);
	        // Append the names of all meshes, with indentation:
	        IReadOnlyList<SD_3D_Mesh> selected = ModelsHandler_3D.instance.selectedMeshes;
	        selected.ForEach( m=>sb.AppendLine("\t" + m.name) );

	        _curr_3D_Obj_text.text = sb.ToString();
	    }


	    void Awake(){
	        if(instance != null){  DestroyImmediate(this.gameObject); return;  }
	        instance = this;
	        _currProj_cam_text.text = "";
	    }

	}
}//end namespace
