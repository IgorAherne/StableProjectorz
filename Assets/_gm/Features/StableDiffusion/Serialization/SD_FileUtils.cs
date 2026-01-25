using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace spz {

	public static class SD_FileUtils
	{

	    public static void CleanDirectory(string directoryPath){
	        if(!Directory.Exists(directoryPath)){
	            Directory.CreateDirectory(directoryPath);
	            Debug.Log($"Created directory: {directoryPath}");
	            return;
	        }
	        //otherwise, dir already exisits, remove its contents:
	        try{
	            foreach (string file in Directory.GetFiles(directoryPath)){  File.Delete(file); }
	            Debug.Log($"Cleaned directory: {directoryPath}");
	        }
	        catch (Exception e){
	            Debug.LogError($"Error cleaning directory {directoryPath}: {e.Message}");
	        }
	    }

	    public static int CountFiles_withExtensions(string directoryPath, params string[] extensions){
	        if (!Directory.Exists(directoryPath)){ 
	            return 0; 
	        }try{
	            return extensions.SelectMany(ext => Directory.GetFiles(directoryPath, $"*.{ext.TrimStart('*', '.')}"))
	                             .Count();
	        }catch (Exception e){
	            Debug.LogError($"Error counting files in directory {directoryPath}: {e.Message}");
	            return 0;
	        }
	    }

	    public static bool IsFileBusy(string filename){
	        try{
	            using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None)){
	                return inputStream.Length <= 0;
	            }
	        }catch (Exception){ return true; }
	    }

	    public static bool IsAllFilesReady(string directoryPath, params string[] extensions){
	        if (!Directory.Exists(directoryPath)){ 
	            return true; //return true to avoid getting stuck waiting for non-existing dir.
	        }
	        try {
	            var files = new List<string>(); 
	            for(int i=0; i< extensions.Length; ++i){
	                string ext = extensions[i];
	                var ext_files = Directory.GetFiles(directoryPath, $"*.{ext.TrimStart('*', '.')}");
	                files.AddRange(ext_files);
	            }
	            return files.All(file => !IsFileBusy(file));
	        }catch (Exception){
	            return false;
	        }
	    }

	}
}//end namespace
