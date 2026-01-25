using System;
using System.IO;
using UnityEngine;
using Lavender.Systems;

namespace spz {

	public class LaunchWebUIBatFile : MonoBehaviour{
	    public static LaunchWebUIBatFile instance { get; private set; } = null;
    

	    string GetWebuiFilePath( bool printStatusText_ifNotFound = false){
	        string exeDirectory = Directory.GetParent(Application.dataPath).FullName;
	        string filePath = Path.Combine(exeDirectory, "stable-diffusion-webui-forge", "run_noQuickEdit.lnk");

	        #if UNITY_EDITOR
	        filePath = @"C:\_myDrive\repos\sd\forge\run.bat";
	        #endif

	        if(File.Exists(filePath)){
	            Debug.Log($"Webui file found, launching it automatically: {filePath}");
	            return filePath;
	        }
	        string msg = $"Webui file not found, can't launch it automatically. User will have to launch their own. Tried the: {filePath}";
	        if (printStatusText_ifNotFound){
	            Viewport_StatusText.instance.ShowStatusText(msg, textIsETA_number: false, 10, false);
	        }
	        Debug.Log(msg);
	        return "";
	    }


	    public void LaunchWebui_Manually( bool printStatusText_ifNotFound = false){
	        string filePath = GetWebuiFilePath(printStatusText_ifNotFound);
	        if(filePath==""){ return; }

	        try{
	            uint pid = StartExternalProcess.Run_Bat_or_Shortcut_or_Command(filePath, isJustFile:true, Directory.GetParent(filePath).FullName);
	            if (pid != 0){
	                Debug.Log($"Process launched successfully with PID: {pid}");
	            }else{
	                Debug.LogError("Failed to launch process.");
	            }
	        }
	        catch (Exception e){
	            Debug.LogError($"Error launching process: {e.Message}");
	        }
	    }


	    void Start(){
	        #if UNITY_EDITOR
	        return; //else keeps bothering me
	        #endif
	        bool printStatusText_ifNotFound = true;
	        LaunchWebui_Manually(printStatusText_ifNotFound);
	    }

	    void Awake(){
	        if (instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	    }
	}
}//end namespace
