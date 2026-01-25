using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class OpenURL_and_Subdirectory : MonoBehaviour{
	    [SerializeField] Button _buttonOptional;
	    [SerializeField] string _url_file_or_page;
	    [SerializeField] string _subdir_rel_webuiDatapath_FORGE;//if using 'Forge' (priority).
	    [SerializeField] string _subdir_rel_webuiDatapath_A1111;//if using Automatic1111 (legacy).
	#if UNITY_EDITOR
	    [SerializeField] string _subdir_relative_to_exe_EDITOR;
	#endif

	    void Start(){
	        if(_buttonOptional != null){ 
	            _buttonOptional.onClick.AddListener( ()=>OpenSubdir_and_theURL("","") );
	        }
	    }

	    public void OpenSubdir_and_theURL(string absFilePath ="", string customURL = ""){
	        #if UNITY_EDITOR && false
	            // In the Unity Editor, use a different subdirectory within the project folder
	            string dir  = _subdir_relative_to_exe_EDITOR.TrimStart('/', '\\');
	                   dir  = Path.Combine(Directory.GetParent(Application.dataPath).FullName, _subdir_relative_to_exe_EDITOR);
	        #else
	            bool hasForgeWebui = SD_SysInfo_MGR.instance.isForgeWebui_detected();
	            string dir =   SD_SysInfo_MGR.instance.sysInfo.DataPath.TrimEnd('/', '\\');
	                   dir +=  hasForgeWebui ? _subdir_rel_webuiDatapath_FORGE : _subdir_rel_webuiDatapath_A1111;
	        #endif

	        dir = dir.Replace('\\', '/'); // Normalize the path

	        string path = absFilePath!=""? absFilePath : dir;
	        string url  = customURL!=""? customURL : _url_file_or_page;
	        // Open the folder on user's computer.
	        string folderPath = Path.GetDirectoryName(path);
				   folderPath = folderPath.Replace('\\', '/'); // Normalize the path
			if (!string.IsNullOrEmpty(folderPath)){
	            Application.OpenURL(folderPath);
	        }
	        // Open the URL:
	        if (!string.IsNullOrEmpty(url)){  Application.OpenURL(url);  }
	    }

	}
}//end namespace
