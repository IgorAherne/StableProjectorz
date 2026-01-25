using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace spz {

	using static System.Net.WebRequestMethods;

	// This will download & extract 'rembg-stable-projectorz' into a local folder. 
	public class Rembg_RepoInit : BaseRepoInit{
	    // The name of the folder we want to place this into (relative to exe folder).
	    protected override string RepoName => "rembg-stable-projectorz";

	    // Where to download from, how to name the zip, etc.
	    protected override DownloadPortion[] GetDownloadInfo(){
	        return new DownloadPortion[]{
	            new DownloadPortion{
	                Mirrors = new string[]{
	                    // Primary mirror
	                    "https://github.com/IgorAherne/rembg-stable-projectorz/releases/download/latest/rembg-stable-projectorz.zip",
	                    // Optional second mirror:
	                    "https://sourceforge.net/projects/rembg-stable-projectorz/files/rembg-stable-projectorz.zip/download",
	                },
	                ZipName = "rembg-stable-projectorz.zip",
	                ExtractPath = _repoDir,    // We defined this path in BaseRepoInit
	                Description = "Rembg-based stable projectorz code"
	            }, 
	        };
	    }//end()
	}
}//end namespace
