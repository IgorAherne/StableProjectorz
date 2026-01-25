using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
namespace spz {

	#endif

	// Knows of current version, updates the ProjectSettings each time we recompile.
	// offers update_versionAndDate_inText() which updates the texts with the verison everywhere
	// where it mentions the special keword. That way we don't have to manually edit each text.
	public class SP_Version : MonoBehaviour{
	    public static SP_Version instance { get; private set; } = null;
	    public static readonly string currVersion = "2.4.5";
	    public static readonly string currDate = "January 2025";
	    public static readonly string versionKeyword = "<sp_version>";
	    public static readonly string versionAndDateKeyword = "<sp_version_and_date>";

	    public string currentVersion => currVersion;

    
	    public string update_versionAndDate_inText(string with_tags, GameObject go){
	        string version_and_date = currVersion + " (" + currDate + ")";

	        int ix = with_tags.IndexOf(versionAndDateKeyword);
	        if (ix < 0){
	            Debug.LogError($"{go.name} is ivoked the {nameof(update_versionAndDate_inText)}, but it doesn't mention {versionAndDateKeyword} to swap out with a version number.");
	            return with_tags;
	        }
	        return with_tags.Substring(0, ix)  + version_and_date + with_tags.Substring(ix + versionAndDateKeyword.Length);
	    }

    
	    // Searches for the special keyword in text, and swaps it out for for our version string.
	    // This saves us time, so we don't have to manually edit each text.
	    public string update_version_inText(string with_tags, GameObject go){
	        int ix = with_tags.IndexOf(versionKeyword);
	        if (ix < 0){
	            Debug.LogError($"{go.name} invoked {nameof(update_version_inText)}, but it doesn't mention {versionKeyword} to swap out with a version number.");
	            return with_tags;
	        }
	        return with_tags.Substring(0, ix)  +  currVersion  +  with_tags.Substring(ix + versionKeyword.Length);
	    }

    
	    void Awake(){
	        if (instance != null) { DestroyImmediate(instance); return; }
	        instance = this;
	    }


	#if UNITY_EDITOR
	    [InitializeOnLoadMethod]
	    private static void OnAfterAssemblyReload(){
	        //touching these settings when exiting/entering gamplay can cause crash:
	        if(EditorApplication.isPlayingOrWillChangePlaymode){ return; }
	        AssemblyReloadEvents.afterAssemblyReload += OnScriptsRecompiled;
	    }
     
	    private static void OnScriptsRecompiled(){
	        // Update the project settings with the current version
	        PlayerSettings.productName = "Stable Projectorz " + currVersion;
	        PlayerSettings.bundleVersion = currVersion;
	        PlayerSettings.Android.bundleVersionCode = GetAndroidBundleVersionCode(currVersion);
	        PlayerSettings.iOS.buildNumber = currVersion;
	        AssetDatabase.SaveAssets();
	    }

	    private static int GetAndroidBundleVersionCode(string version){
	        // Implement your logic to convert the ven stringrsion string to an integer bundle version code for Android
	        // For example, you can split the versio by '.' and combine the parts as an integer
	        string[] parts = version.Split('.');
	        return int.Parse(parts[0]) * 10000 + int.Parse(parts[1]) * 100 + int.Parse(parts[2]);
	    }
	#endif

	}
	//will compare to pastebin and website's, to see if it mentions newer.
}//end namespace
