using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;


namespace spz {

	//silently checks several web addresses for version.
	//if one of them mentions version that's greater than stored here, opens a popup, inviting the user to update
	public class CheckForUpdates_MGR : MonoBehaviour{
	    public static CheckForUpdates_MGR instance { get; private set; } = null;

	    public static readonly string CURRENT_VERSION_HERE = SP_Version.currVersion;
	    public static readonly string PASTEBIN_VERSION_URL = "https://pastebin.com/raw/DP5JQ5Ja";
	    public static readonly string WEBSITE_VERSION_URL  = "https://stableprojectorz.com/latest-version";
	    public static readonly string WEBSITE_DOWNLOAD_URL = "https://stableprojectorz.com"; //where to download the new exe.

	    public static readonly string SKIP_UPDATE_FLAG = "--skip-updates-check";
	    public static readonly string CONFIG_FILENAME = "spz.config";

	    [SerializeField] VersionPopupPanel_UI _versionPopup;

	    Coroutine _checkForUpdates_crtn = null;

	    public bool isShowing => _versionPopup.isShowing; 


	    public void ShowPanel(bool recheckForUpdates){
	        if (recheckForUpdates){
	            _versionPopup.ShowPanel( VersionPopupPanel_UI.VersionDecision.Checking );
	            if(_checkForUpdates_crtn!=null){ StopCoroutine(_checkForUpdates_crtn);  }
	            _checkForUpdates_crtn = StartCoroutine( checkVersions_crtn() );
	        }
	        else{
	            _versionPopup.ShowPanel( VersionPopupPanel_UI.VersionDecision.AlreadyHaveLatest );
	        }
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;

	        _versionPopup.gameObject.SetActive(false);

	        if (isSkipUpdateChecks()){
	            Debug.Log("Update check skipped due to spz.config file setting");
	            return;
	        }
	        //look for updates, WITHOUT showing the panel. 
	        if(_checkForUpdates_crtn!=null){ StopCoroutine(_checkForUpdates_crtn);  }
	        _checkForUpdates_crtn = StartCoroutine( checkVersions_crtn() );
	    }


	    bool isSkipUpdateChecks(){
	        try {
	            string exeDir = Directory.GetParent(Application.dataPath).FullName;
	            string configPath = Path.Combine(exeDir, CONFIG_FILENAME);
	            if (!File.Exists(configPath)){
	                return false;
	            }
	            // Read all lines and check if any line contains the skip flag (case insensitive)
	            string[] configLines = File.ReadAllLines(configPath);
	            foreach(string line in configLines) {
	                if (line.Trim().Equals(SKIP_UPDATE_FLAG, StringComparison.OrdinalIgnoreCase)) {
	                    return true;
	                }
	            }
	        }catch (Exception ex){
	            Debug.LogError($"Error reading config file: {ex.Message}");
	        }
	        return false;
	    }


    
	    IEnumerator checkVersions_crtn(){

	        UnityWebRequest pastebinWWW = UnityWebRequest.Get(PASTEBIN_VERSION_URL);
	        UnityWebRequest websiteWWW = UnityWebRequest.Get(WEBSITE_VERSION_URL);

	        // Send both web requests simultaneously
	        var pastebinRequest = pastebinWWW.SendWebRequest();
	        var websiteRequest = websiteWWW.SendWebRequest();

	        // Wait for both requests to complete
	        yield return new WaitUntil(() => pastebinRequest.isDone && websiteRequest.isDone);

	        string latestVersion = CURRENT_VERSION_HERE; // Start with the current version
	        string description = "";
	        bool foundNewerVersion = ProcessWebResponse(pastebinWWW, ref latestVersion, ref description);// Process Pastebin response
	            foundNewerVersion |= ProcessWebResponse(websiteWWW, ref latestVersion, ref description);// Process website response

	        bool panelShowing = _versionPopup.isShowing;
        
	        if (!foundNewerVersion){
	            if(panelShowing){ 
	                _versionPopup.UpdateVersionDecision(VersionPopupPanel_UI.VersionDecision.AlreadyHaveLatest, latestVersion, description); 
	            }
	            _checkForUpdates_crtn = null;
	            yield break; 
	        }
	        //A newer version is available:

	        if (panelShowing){
	            _versionPopup.UpdateVersionDecision(VersionPopupPanel_UI.VersionDecision.CanDownloadNewer, latestVersion, description);
	        }else { 
	            _versionPopup.ShowPanel( VersionPopupPanel_UI.VersionDecision.CanDownloadNewer, latestVersion, description );
	        }

	        _checkForUpdates_crtn = null;
	    }


	    bool ProcessWebResponse(UnityWebRequest www, ref string latestVersion_, ref string description_){
        
	        if (www.result != UnityWebRequest.Result.Success){ return false; }

	        string responseText = www.downloadHandler.text;
	        int newlineIndex = responseText.IndexOf("\n");

	        if (newlineIndex == -1){ return false; }// No newline found

	        string fetchedVersionStr = responseText.Substring(0, newlineIndex).Trim();
	        if (isFetchedNewer(latestVersion_, fetchedVersionStr)==false){ return false; }

	        latestVersion_ = fetchedVersionStr;
	        description_   = responseText.Substring(newlineIndex+1).Trim();
	        return true;
	    }


	    bool isFetchedNewer(string currentVersionStr, string fetchedVersionStr){
	        if (Version.TryParse(currentVersionStr, out var currentVersion) &&
	            Version.TryParse(fetchedVersionStr, out var fetchedVersion)){
	            if (fetchedVersion > currentVersion){  return true;  }//fetched is newer, user can update.
	            else{ return false; }//current (in this tool) is newest.
	        }else{
	            //error parsing. Newer considered current
	            return false;
	        }
	    }

	}
}//end namespace
