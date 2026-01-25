using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lavender.Systems;
using System;
using SimpleFileBrowser;


namespace spz {

	public class RestartTheWebui : MonoBehaviour {
	    [SerializeField] protected Button _launchButton;
	    [SerializeField] protected Button _fileButton; //allows to specify path to the file that should be launched
	    [SerializeField] protected string _openFile_os_window_headerMsg;
	    [Space(10)]
	    [SerializeField] protected string _defaultRelativePath = "./stable-diffusion-webui-forge/run_noQuickEdit.lnk";
	    [SerializeField] protected string _playerPrefs_filepathID = "_RestartWebuiFilepath";
	    [SerializeField] protected Animation _anim;

	    bool _isPlayingAttentionAnim = false;

	    string _filepath; // internal variable to hold the correct path
	    public Action OnClicked { get; set; } = null;


	    public void KeepPlaying_attention_anim(bool isKeepPlaying){
	        if (_anim == null) { return; }
	        if (isKeepPlaying == _isPlayingAttentionAnim){ return; }
	        _isPlayingAttentionAnim = isKeepPlaying;
	        if (isKeepPlaying){ 
	            _anim.Play(); 
	        }else {
	            _anim.Stop();
	            _anim.clip.SampleAnimation(gameObject, 0);
	        }
	    }

	    protected string TryFindFileInParentDirectories(string filepath){
	        // If path is absolute and file exists, return it directly
	        if (Path.IsPathRooted(filepath) && File.Exists(filepath)){ return filepath; }

	        // Get the initial directory to start searching from
	        string currentDir = Directory.GetParent(Application.dataPath).FullName;
	        // Get just the filename, regardless of path structure
	        string filename = Path.GetFileName(filepath);

	        while (currentDir != null){
	            string attemptPath = Path.Combine(currentDir, filename);
	            if (File.Exists(attemptPath)){ return attemptPath;}

	            // Move up one directory
	            DirectoryInfo parentDir = Directory.GetParent(currentDir);
	            if (parentDir == null) break;
	            currentDir = parentDir.FullName;
	        }
	        // If we couldn't find the file, return the original path
	        return filepath;
	    }

	    protected virtual void OnStartWebuiButton(){
	        if(string.IsNullOrEmpty(_filepath)){
	            Print_Webui_NotFound();
	            return; 
	        }

	        string full_path = _filepath;
	        //if the path is relative, starts with ./  then we need to make it absolute:
	        if(full_path.Length>0 && full_path[0] == '.'){
	            string exeDirectory = Directory.GetParent(Application.dataPath).FullName;
	            full_path = Path.Combine(exeDirectory, full_path);
	        }
        
	        try{
	            //simplify the path, to make it standardized:
	            full_path = Path.GetFullPath(_filepath);
	        }catch(Exception e){
	            Debug.Log("path is incorrect, please check it again");
	        }
        
	        // Try to find the file recursively in parent directories if it doesn't exist
	        if (!File.Exists(full_path)){
	            full_path = TryFindFileInParentDirectories(full_path);
	        }
	        if (File.Exists(full_path) == false){
	            Print_Webui_NotFound();
	            return; 
	        }
	        full_path = OnWillLaunchWebui_AdjustArgs(full_path);
	        uint pid = StartExternalProcess.Run_Bat_or_Shortcut_or_Command(full_path, isJustFile:true, 
	                                                                       Directory.GetParent(full_path).FullName);
	        if (pid == 0){
	            Debug.LogError("Failed to launch the file. Consider launching StableProjectorz as Admin.");
	            return;
	        }
	        string message = "Webui Restarted.  Always ensure only 1 webui is open, to save VRAM.";
	        Viewport_StatusText.instance.ShowStatusText(message, false, 3, false);
	        OnClicked?.Invoke();
	    }


	    void Print_Webui_NotFound(){
	        string msg = "File not found in the current or parent directories." +
	                        "\nVerify it's correct or launch StableProjectorz as Admin.";
	        Viewport_StatusText.instance.ShowStatusText(msg, false, 3, false);
	    }


	    protected virtual string OnWillLaunchWebui_AdjustArgs(string path){
	        return path; //child classes can append custom args, for example path+"--precision full", or something like that.
	    }

	    protected virtual void OnSpecifyFileButton(){
        
	        FileBrowser.SetFilters(true, new FileBrowser.Filter("Executables", "bat", "exe", "sh"));
	        FileBrowser.SetDefaultFilter("bat");

	        FileBrowser.ShowLoadDialog( (paths) => {
	            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0])){
	                _filepath = Path.GetFullPath(paths[0]);
	                PlayerPrefs.SetString(_playerPrefs_filepathID, _filepath);
	            }
	        }, 
	        null, 
	        FileBrowser.PickMode.Files, false, null, null, _openFile_os_window_headerMsg, "Select");
	    }

	    protected virtual void Start(){
	        _launchButton.onClick.AddListener(OnStartWebuiButton);
	        _fileButton.onClick.AddListener(OnSpecifyFileButton);

	        _filepath = PlayerPrefs.GetString(_playerPrefs_filepathID, _defaultRelativePath);
	        _filepath = _filepath.Length<2048? _filepath : _defaultRelativePath;//helps if glitched
	    }
    

	}
}//end namespace
