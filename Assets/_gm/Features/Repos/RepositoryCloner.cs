using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

namespace spz {

	public class RepositoryCloner : MonoBehaviour{

	    public static RepositoryCloner instance { get; set; } = null;

	    public bool RepoExists(string dest_relativeDir){
	        string path_abs = Path.Combine(GetExeDirectory(), dest_relativeDir);
	        return Directory.Exists(path_abs);
	    }

	    public void Download(string zipUrl, string dest_relativeDir,
	                         Action<float> onProgress01, Action<bool> onDone){

	        string path_abs = Path.Combine(GetExeDirectory(), dest_relativeDir);
	        if (RepoExists(path_abs)){
	            Debug.Log($"repo is already installed at {path_abs}, skipping download");
	            onDone?.Invoke(true);
	            return;
	        }
	        StartCoroutine(DownloadAndExtractCoroutine(zipUrl, path_abs, onProgress01, onDone));
	    }


	    IEnumerator DownloadAndExtractCoroutine(string zipUrl, string destPath, Action<float> onProgress, Action<bool> onDone)
	    {
	        string zipPath = Path.Combine(destPath, "temp.zip");
        
	        yield return DownloadFileCoroutine(zipUrl, zipPath, onProgress);
        
	        if (!File.Exists(zipPath)){
	            Debug.LogError($"Failed to download: {zipUrl}");
	            onDone?.Invoke(false);
	            yield break;
	        }

	        try{
	            ExtractZip(zipPath, destPath);
	            File.Delete(zipPath);
	            onProgress?.Invoke(1f);
	            onDone?.Invoke(true);
	        }
	        catch (Exception ex){
	            Debug.LogError($"Extraction failed: {ex.Message}");
	            onDone?.Invoke(false);
	        }
	    }


	    IEnumerator DownloadFileCoroutine(string url, string filePath, Action<float> onProgress){
	        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
	        Download_MGR.instance.DownloadFile(url, filePath, onProgress, false);
	        while (Download_MGR.instance.IsDownloading(url)) yield return null;
	    }

    
	    void ExtractZip(string zipPath, string destPath){

	        ZipFile.ExtractToDirectory(zipPath, destPath);
	        var directories = Directory.GetDirectories(destPath);
        
	        if(directories.Length == 0){ throw new Exception("no directories found after unzipping."); }

	        // If there are more than 1 directories, we leave the structure as is
	        if(directories.Length > 1){ return; }
	        // else, 1 sub directory.
	        // Move the contents out of it into the destination instead:

	        string singleDir = directories[0];

	        //enumerates the immediate children(files and directories) of the specified directory:
	        foreach (var path in Directory.EnumerateFileSystemEntries(singleDir)){
	            var newPath = Path.Combine(destPath, Path.GetFileName(path));
	            if(Directory.Exists(path)){  Directory.Move(path, newPath); }
	            else File.Move(path, newPath);
	        }
	        Directory.Delete(singleDir);
	    }


	    static string GetExeDirectory(){
	        string path;
	        #if UNITY_EDITOR
	            path = Directory.GetParent(Application.dataPath).FullName;
	        #else
	            path = Path.Combine(Application.dataPath, "..");
	        #endif
	        return path;
	    }

	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	    }
	}
}//end namespace
