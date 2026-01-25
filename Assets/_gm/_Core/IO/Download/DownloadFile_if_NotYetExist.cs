using System.IO;
using UnityEngine;

namespace spz {

	public class DownloadFile_if_NotYetExist : MonoBehaviour{
	    [SerializeField] string _fileUrl = "https://huggingface.co/lllyasviel/ControlNet-v1-1/resolve/main/control_v11f1p_sd15_depth.pth?download=true";
	    [SerializeField] string _subdirectory_rel_webuiDatapath_FORGE = "/models/ControlNet/";//If using 'Forge' (priority) Replace with your subdirectory name. 
	    [SerializeField] string _subdirectory_relExe_A1111 = "/extensions/sd-webui-controlnet/models/"; //If using Automatic1111 (legacy) Replace with your subdirectory name
	    [Header("if empty, will find+use the filename inside url")]
	    [SerializeField] string _fileName_withExten = "";

	    public void DownloadFile( string fileUrl="",  string absFilepath_withExten = "",  System.Action<float>onProgress = null){
	      #if UNITY_EDITOR && false
	            // In the Unity Editor, use a different subdirectory within the project folder
	            string dir = Path.Combine( Directory.GetParent(Application.dataPath).FullName, "TestDownloadsIntoHere");
	                   dir = Path.Combine(dir, _fileName_withExten).Replace('\\', '/'); // Normalize the path
	      #else
	        bool hasForgeWebui = SD_SysInfo_MGR.instance.isForgeWebui_detected();
	        string dir  =  SD_SysInfo_MGR.instance.sysInfo.DataPath.TrimEnd('/', '\\');
	               dir +=  hasForgeWebui ?  _subdirectory_rel_webuiDatapath_FORGE : _subdirectory_relExe_A1111;

	               dir = Path.Combine(dir, _fileName_withExten).Replace('\\', '/'); // Normalize the path
	      #endif
	        fileUrl =  fileUrl!=""?  fileUrl : _fileUrl;
	        absFilepath_withExten = absFilepath_withExten != ""? absFilepath_withExten : dir;
	        Download_MGR.instance.DownloadFile(fileUrl, absFilepath_withExten, onProgress);
	    }


	    void Awake(){
	        if (_fileName_withExten == "" && _fileUrl!=""){
	            _fileName_withExten = GetFileNameFromUrl(_fileUrl);
	        }
	    }


	    string GetFileNameFromUrl(string url){ 
	        var uri = new System.Uri(url);
	        string filename = Path.GetFileName(uri.LocalPath);
	        return filename;
	    }

	}
}//end namespace
