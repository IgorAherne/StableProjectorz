using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace spz {

	[Serializable]
	public class SamplerOptions{
	    public string scheduler;
	    public string second_order;
	    public string brownian_noise;
	    public string uses_ensd;
	    public string discard_next_to_last_sigma;
	    public string solver_type;
	    // Add more options fields as needed, matching the keys in the JSON response.
	}


	[Serializable]
	public class Sampler{
	    public string name;
	    public string[] aliases;
	    public SamplerOptions options;
	}


	[Serializable]
	public class SamplersList{
	    public Sampler[] samplers;
	    public static SamplersList CreateFromJSON(string jsonString){
	        // Use class-type information, to support inheritance of objects:
	        var settings = new JsonSerializerSettings{ TypeNameHandling = TypeNameHandling.Auto, };
	        return JsonConvert.DeserializeObject<SamplersList>("{\"samplers\":" + jsonString + "}", settings);
	    }
	}


	// Sends a GET request to the IP with the Stable diffusion server.
	// Obtains a respons wiht a collection of available samplers (Euler a, SD++ Karras, and so on)
	// Populates its ui dropdown once ready.
	public class SD_Samplers : MonoBehaviour{
	    public static SD_Samplers instance { get; private set; } = null;


	    [SerializeField] TMP_Dropdown _samplers_dropdown;

	    private SamplersList _listOfSamplers;
	    public SamplersList listOfSamplers => _listOfSamplers;
	    public Sampler value=> GetSelectedSampler(); //currently chosen in dropdown
    
	    // if we loaded from a save-file, we migth want to select a sampler.
	    // If we are not connected, this sampler won't be in the dropdown.
	    // But we can try to find it as soon a we connect next time;
	    string _prefferedSampler_viaLoad = "";


	    public Sampler GetSelectedSampler(){
	        if (_samplers_dropdown.options.Count == 0){ return null; }// No options in dropdown
	        if (_listOfSamplers == null || _listOfSamplers.samplers == null || _listOfSamplers.samplers.Length == 0){ return null; }// No samplers loaded

	        int selectedIndex = _samplers_dropdown.value;
	        if (selectedIndex < 0 || selectedIndex >= _listOfSamplers.samplers.Length){ return null; }// Out of range protection

	        return _listOfSamplers.samplers[selectedIndex];
	    }


	    public void Save(SD_GenSettingsInput_UI fill_this){
	        fill_this.samplers = new SD_InputSamplers_SL();
	        fill_this.samplers.selectedSampler_name =  value?.name?? "";
	    }

	    public void Load(SD_GenSettingsInput_UI from_this){
	        SD_InputSamplers_SL s = from_this.samplers;
	        int newIndex =  _samplers_dropdown.options.FindIndex( opt => opt.text==s.selectedSampler_name );
	        if(newIndex == -1){
	            _prefferedSampler_viaLoad = s.selectedSampler_name;
	            return;
	        }
	        _prefferedSampler_viaLoad = "";//found, no longer need to search for it.
	        _samplers_dropdown.value = newIndex;
	    }


	    void Awake(){
	        if (instance != null) { DestroyImmediate(this); return; };
	        instance = this;
	    }

	    void Start(){
	        Coroutines_MGR.instance.StartCoroutine( FetchContiniously() );
	    }


	    IEnumerator FetchContiniously(){
        
	        DEBUG_FetchContiniously(0);

	        while (true){
	            if (!Connection_MGR.is_sd_connected){ 
	                yield return new WaitForSeconds(0.211f); 
	                continue; 
	            }
	            Coroutines_MGR.instance.StartCoroutine( GetSamplers_crtn() );
	            dropdown_LoadedSavedSampler_maybe();

	            yield return new WaitForSeconds(4f);
	        }
	    }


	    IEnumerator GetSamplers_crtn(){
	        //Don't send network request to webui if rendering, else it seems to stuck it sometimes.
	        if(StableDiffusion_Hub.instance._generating){ yield break; }

	        DEBUG_GetSamplers(0);
	        UnityWebRequest request = UnityWebRequest.Get(Connection_MGR.A1111_SD_API_URL + "/samplers");
	        yield return request.SendWebRequest();

	        DEBUG_GetSamplers(1);

	        bool isBad =  request.result == UnityWebRequest.Result.ConnectionError;
	             isBad |= request.result == UnityWebRequest.Result.ProtocolError;
	        if (isBad){
	            DEBUG_GetSamplers(2, request.error);
	            yield break;
	        }
	        DEBUG_GetSamplers(3, request.downloadHandler.text + "\n\n");

	        _listOfSamplers = SamplersList.CreateFromJSON(request.downloadHandler.text);

	        DEBUG_GetSamplers(4, _samplers_dropdown.options.Count.ToString());

	        Populate_DropdownModels();

	        #if SP_VERBOSE_SAMPLERS_DEBUG
	        string msgStr = "";
	        _samplers_dropdown.options.ForEach(opt=> msgStr +=opt.text + "    ");
	        DEBUG_GetSamplers(6, msgStr);
	        #endif
	    }

	    void Populate_DropdownModels(){

	        var newOptions =  Array.ConvertAll(_listOfSamplers.samplers, sampler => new TMP_Dropdown.OptionData(sampler.name)).ToList();
	        bool different =  newOptions.Count != _samplers_dropdown.options.Count;

	        for(int i=0; i<newOptions.Count; ++i){
	            different |= _samplers_dropdown.options[i].text != newOptions[i].text;
	            if(different){ break; }
	        }

	        if (different){
	            string previousSelection = _samplers_dropdown.options.Count > 0 ? _samplers_dropdown.options[_samplers_dropdown.value].text : "";
	            DEBUG_GetSamplers(5, previousSelection);
	            _samplers_dropdown.ClearOptions();
	            _samplers_dropdown.AddOptions(newOptions);
	            int newIx   =  Array.FindIndex(_listOfSamplers.samplers, sampler => sampler.name==previousSelection);
	            if(newIx>=0){ _samplers_dropdown.value = newIx; }
	        }

	        _samplers_dropdown.RefreshShownValue();//refresh text and image of currently shown option.
	    }


	    // If we loaded a project file, maybe we couldn't find required model back then.
	    // So, see if we can find it now, and set dropdown to that value if possible.
	    void dropdown_LoadedSavedSampler_maybe(){
	        bool wantLoaded = string.IsNullOrEmpty(_prefferedSampler_viaLoad) == false;
	        if(!wantLoaded){ return; }
	        int samplerIx = _samplers_dropdown.options.FindIndex( opt => opt.text==_prefferedSampler_viaLoad);
	        if(samplerIx>=0){
	            _prefferedSampler_viaLoad = "";//found, no longer need to search for it.
	            _samplers_dropdown.value = samplerIx;
	        }
	    }


	    void DEBUG_FetchContiniously(int KeyIx, string suffix=""){
	        #if SP_VERBOSE_SAMPLERS_DEBUG
	        Dictionary<int, string> dict = new Dictionary<int, string>(){
	            {0, "SD_Samplers::FetchContiniously() started"},
	        };
	        Debug.Log(dict[KeyIx] + suffix);
	        #endif
	    }


	    void DEBUG_GetSamplers(int KeyIx, string suffix=""){
	        #if SP_VERBOSE_SAMPLERS_DEBUG
	        Dictionary<int, string> dict = new Dictionary<int, string>(){
	            {0, "SD_Samplers::GetSamplers() entered, will send webRequest"},
	            {1, "SD_Samplers::GetSamplers() webrequest yielded"},
	            {2, "SD_Samplers::GetSamplers() webrequest Error: " },
	            {3, "SD_Samplers::GetSamplers() webrequest Success. JSON: " },
	            {4, "SD_Samplers::GetSamplers() _samplers_dropdown.options.Count " },
	            {5, "SD_Samplers::GetSamplers() previouSelection: " },
	            {6, "SD_Samplers::GetSamplers() modelIx: "},
	        };
	        Debug.Log(dict[KeyIx] + suffix);
	        #endif
	    }

	}
}//end namespace
