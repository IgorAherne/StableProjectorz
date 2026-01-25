using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace spz {

	public class SD_SysInfo_MGR : MonoBehaviour{
	    public static SD_SysInfo_MGR instance { get; private set; } = null;

	    public static string INTERNAL_API_URL => Connection_MGR.A1111_IP_AND_PORT + "/internal";

	    //continiously fetched from the server (every few seconds).
	    //Can tell us the setup that user has, number of control units etc.
	    public SD_SysInfo sysInfo { get; private set; } = new SD_SysInfo();
    
	    public bool isForgeWebui_detected(){
	        bool isFound  = sysInfo?.DataPath?.ToLower().Contains("forge") ?? false;
	             isFound |= sysInfo?.ScriptPath?.ToLower().Contains("forge")?? false;
	        return isFound;
	    }


	    void Awake(){
	        if (instance != null){ DestroyImmediate(this); return; }
	        instance = this; 
	    }

	    void Start(){
	        StartCoroutine( FetchContiniously() );
	    }


	    IEnumerator FetchContiniously(){
	        while (true){
	            if (!Connection_MGR.is_sd_connected){ 
	                yield return new WaitForSeconds(0.25f); 
	                continue; 
	            }
	            yield return StartCoroutine( FetchInfo_crtn() );
	            yield return new WaitForSeconds(5f);
	        }
	    }

	    IEnumerator FetchInfo_crtn(){
	      //models list:
	        bool success = false;
	        Action<bool,string> onResult =  (isSuccess,text) => { 
	            success=isSuccess;
	            this.sysInfo = success? SD_SysInfo.CreateFromJSON(text) 
	                                  : new SD_SysInfo();//error, so just an empty sysInfo.
	        };
	        yield return StartCoroutine( FetchData_crtn(INTERNAL_API_URL + "/sysinfo", onResult) );
	        if (!success){ yield break; }
	    }

	    IEnumerator FetchData_crtn( string url,  Action<bool,string> onResult ){
	        //Don't send network request to webui if rendering, else it seems to stuck it sometimes.
	        if(StableDiffusion_Hub.instance._generating){ yield break; }

	        UnityWebRequest request = UnityWebRequest.Get(url);
	        yield return request.SendWebRequest();

	        bool isBad = request.result == UnityWebRequest.Result.ConnectionError;
	            isBad |= request.result == UnityWebRequest.Result.ProtocolError;
	        if (isBad){
	            onResult?.Invoke(false, "");
	        }else{
	            onResult?.Invoke(true, request.downloadHandler.text);
	        }
	    }
	}


	[Serializable]//returns from stableDiffusion, back into StableProjectorz
	public class SD_SysInfo{
	    public string Platform = "";
	    public string Python = "";
	    public string Version = "";
	    public string Commit = "";
	    [JsonProperty("Script path")]
	    public string ScriptPath = "";
	    [JsonProperty("Data path")]
	    public string DataPath = "";
	    [JsonProperty("Extensions dir")]
	    public string ExtensionsDir = "";
	    public string Checksum = "";
	    public List<string> Commandline;
	    public Config Config = new Config();

	    public static SD_SysInfo CreateFromJSON(string jsonString){
	        // Use class-type information, to support inheritance of objects:
	        var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, };
	        return JsonConvert.DeserializeObject<SD_SysInfo>(jsonString, settings);
	    }
	}


	[Serializable]//returns from stableDiffusion, back into StableProjectorz
	public class Config{
	    public int control_net_unit_count;
	    public int control_net_model_cache_size;
	    public int control_net_max_models_num;
	    public int num_units(){
	        return Mathf.Max(control_net_unit_count, control_net_max_models_num);
	    }
	}
}//end namespace
