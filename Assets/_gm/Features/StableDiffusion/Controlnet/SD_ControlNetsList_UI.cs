using System;
using System.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace spz {

	public class SD_ControlNetsList_UI : MonoBehaviour{
	    public static SD_ControlNetsList_UI instance { get; private set; } = null;

	    [SerializeField] Transform _controlUnits_parent;
	    [SerializeField] ControlNetUnit_UI _controlNet_PREFAB;
	    [SerializeField] List<ControlNetUnit_UI> _controlNet_units;

	    Coroutines_MGR crtnMgr => Coroutines_MGR.instance;


	    public static string API_URL => Connection_MGR.A1111_IP_AND_PORT + "/controlnet";

	    //will be fetched from network, via API json:
	    public CTRLnets_ModelList _models { get; private set; }  = new CTRLnets_ModelList();
	    public CTRLnets_PreprocessorsList _preprocessors_list { get; private set; }  = new CTRLnets_PreprocessorsList();
	    public ControlTypesResponse _net_types { get; private set; }  = new ControlTypesResponse(); //not used at the moment.

	    public int numTotalUnitsExisting() => _controlNet_units.Count;
	    public int numActiveUnits() => _controlNet_units.Count(u => u.isActivated);
	    public bool Has_Active_Inpainting_CTRLUnit() =>  _controlNet_units.Any( u=> u.isActivated && u.isForInpaint() );

	    public bool Has_Depth_CTRLUnit(bool onlyActive, bool only_if_validModel){
	        foreach(var u in _controlNet_units){
	            if(!u.isForDepth()){ continue; }
	            if(onlyActive && !u.isActivated){ continue; }
	            if(only_if_validModel && u.is_currModel_none){ continue; }
	            return true;
	        }
	        return false;
	    }

	    public bool Has_Normals_CTRLUnit(bool onlyActive, bool only_if_validModel){
	        foreach(var u in _controlNet_units){
	            if(!u.isForNormals()){ continue; }
	            if(onlyActive && !u.isActivated){ continue; }
	            if(only_if_validModel && u.is_currModel_none){ continue; }
	            return true;
	        }
	        return false;
	    }

	    public int Num_Active_Reference_CTRLUnit() => _controlNet_units.Count( u=>u.isActivated && u.isReferencePreprocessor() );

	    public List<string> curentModels_of_DepthOrNormal_units(){
	        var names = new List<string>();
	        for(int i=0; i<_controlNet_units.Count; ++i){
	            var unit = _controlNet_units[i];
	            if(!unit.isActivated){ continue; }
            
	            bool isForDepth = _controlNet_units[i].isForDepth();
	            bool isForNorms = _controlNet_units[i].isForNormals();
	            if(!isForDepth && !isForNorms){ continue; }

	            string n = _controlNet_units[i].currModelName();
	            if(string.IsNullOrEmpty(n)){ continue; }
	            names.Add(n);
	        }
	        return names;
	    }


	    // Provides a summary of the current settings for All ControlNet units,
	    // so that we can send a Generate request to stable diffusion.
	    // We can use what's already in 'intermediates' arg, or actually add stuff to it.
	    // NOTICE: some unit might refuse to participate (if some conditions are not met). 
	    // If so, the array inside the args will be shorter.
	    public ControlNet_NetworkArgs GetArgs_forGenerationRequest( SD_GenRequestArgs_byproducts intermediates ){

	        var args_ofValid_units = new List<ControlNetUnit_NetworkArgs>();

	        int numUnits = _controlNet_units.Count;
	        for(int i=0; i<numUnits; ++i){
	            ControlNetUnit_NetworkArgs arg = _controlNet_units[i].GetArgs_forGenerationRequest(intermediates);
	            if(arg!=null){ args_ofValid_units.Add(arg); }
	        }
	        ControlNet_NetworkArgs cnArgs = new ControlNet_NetworkArgs{
	            args = args_ofValid_units.ToArray(),
	        };
	        return cnArgs;
	    }


	    public void DoForEvery_CtrlUnit( Action<ControlNetUnit_UI,int> act_unitAndIndex ){
	        for(int i=0; i<_controlNet_units.Count; ++i){
	            ControlNetUnit_UI unit = _controlNet_units[i];
	            act_unitAndIndex( unit, i);
	        }
	    }

	    public void Save(StableProjectorz_SL spz){
	        spz.controlNetUnits_panel = new ControlNetUnits_Panel_SL();
	        spz.controlNetUnits_panel.ctrl_units = new List<ControlNetUnit_SL>();

	        for(int i=0; i<_controlNet_units.Count; ++i){
	            var unit_sl = new ControlNetUnit_SL();
	            _controlNet_units[i].Save(i, unit_sl, spz.filepath_dataDir);
	            spz.controlNetUnits_panel.ctrl_units.Add(unit_sl);
	        }
	    }

	    public void Load(StableProjectorz_SL spz){
	        //remove any old unit:
	        EnsureExact_num_CTRLnets(0, instantDestroy_excess:true);
	        //load new units:
	        List<ControlNetUnit_SL> unitsSL = spz.controlNetUnits_panel.ctrl_units;
	        EnsureExact_num_CTRLnets( unitsSL.Count, instantDestroy_excess:true );

	        for (int i=0; i<unitsSL.Count; ++i){
	             _controlNet_units[i].Load(unitsSL[i], spz.filepath_dataDir);
	        }
	    }


	    void Awake(){
	        if (instance != null){ DestroyImmediate(this);return; }
	        instance = this;
	        crtnMgr.StartCoroutine( FetchContiniously() );
	    }


	    IEnumerator FetchContiniously(){
        
	        DEBUG_FetchContiniously(0);

	        while (true){
	            if (!Connection_MGR.is_sd_connected){ 
	                yield return new WaitForSeconds(0.25f); 
	                continue; 
	            }
	            DEBUG_FetchContiniously(1);
            
	            yield return crtnMgr.StartCoroutine( Fetch_WebuiInfo_crtn() );
	            yield return new WaitForSeconds(3f);

	            DEBUG_FetchContiniously(2);
	        }
	    }

	    IEnumerator Fetch_WebuiInfo_crtn(){
	        DEBUG_FetchInfo(0);

	        DEBUG_FetchInfo(1);
	      //models list:
	        bool success = false;
	        System.Action<bool,string> onResult =  (isSuccess,text) => { 
	            success=isSuccess;
	            _models = CTRLnets_ModelList.CreateFromJSON(text);
	        };
	        yield return crtnMgr.StartCoroutine(FetchData_crtn(API_URL+"/model_list?update=true", onResult));
	        if (!success){ yield break; }

	        DEBUG_FetchInfo(2);
	        //modules list:
	        success = false;
	        onResult =  (isSuccess,text) => { 
	            success=isSuccess;
	            _preprocessors_list = CTRLnets_PreprocessorsList.CreateFromJSON(text);
	        };
	        yield return crtnMgr.StartCoroutine(FetchData_crtn(API_URL+"/module_list?alias_names=false", onResult));
	        if (!success){ yield break; }

	        DEBUG_FetchInfo(3);

	        //control net types:
	        success = false;
	        onResult =  (isSuccess,text) => { 
	            success=isSuccess;
	            _net_types = ControlTypesResponse.CreateFromJSON(text);
	        }; 
	        yield return crtnMgr.StartCoroutine(FetchData_crtn(API_URL+"/control_types", onResult));
	        //COMMENTED OUT KEPT FOR PRECAUTION:
	        //Some people had 404 for /control_types  (these are just presets of model+preprocessor, bulletpoints).
	        //But I'm not relying on them, so don't break and continue as if nothing happened:
	        //   if (!success){ yield break; }

	        DEBUG_FetchInfo(4);

	        int num_ctrlnetUnits = 0;
	        System.Action<int> on_set_numUnits = (int num)=>{ num_ctrlnetUnits=num; };
	        yield return crtnMgr.StartCoroutine( FetchData_numCtrlUnits(on_set_numUnits) );
	        if(num_ctrlnetUnits==0){ yield break; }//wasn't able to get the number.

	        EnsureExact_num_CTRLnets( num_ctrlnetUnits, instantDestroy_excess:true );

	        DEBUG_FetchInfo(8, _controlNet_units.Count.ToString());
	        _controlNet_units.ForEach(u => u.OnRefresh_WebuiInfo_Complete());
	    }


	    // Coroutine to handle the web request
	    IEnumerator FetchData_crtn( string url,  Action<bool,string> onResult ){
	        //Don't send network request to webui if rendering, else it seems to stuck it sometimes.
	        if(StableDiffusion_Hub.instance._generating){ yield break; }

	        DEBUG_FetchData(0);
	        UnityWebRequest request = UnityWebRequest.Get(url);
	        yield return request.SendWebRequest();

	        bool isBad = request.result == UnityWebRequest.Result.ConnectionError;
	            isBad |= request.result == UnityWebRequest.Result.ProtocolError;
	        if (isBad){
	            DEBUG_FetchData(1, request.error);
	            onResult?.Invoke(false, "");
	        }else{
	            DEBUG_FetchData(2, request.downloadHandler.text + "\n\n");
	            onResult?.Invoke(true, request.downloadHandler.text);
	        }
	    }


	    // Attempts to get parameter 'number of control units', from webui.
	    // Tries to get using legacy api (automatic1111),
	    // and if failed, fetches from new Forge webui instead (we started using it since March 2024)
	    IEnumerator FetchData_numCtrlUnits( System.Action<int> on_set_numUnits ){

	        DEBUG_FetchInfo(5);
	        //settings:
	        bool legacy_A1111webui_success = false;

	        System.Action<bool,string> onResult =  (isSuccess,text) => { 
	            legacy_A1111webui_success=isSuccess;
	            if (!isSuccess){ return; }
	            CTRLnets_Settings settings = CTRLnets_Settings.CreateFromJSON(text);
	            on_set_numUnits( settings.num_units() );
	        };

	        yield return crtnMgr.StartCoroutine(FetchData_crtn(API_URL+"/settings", onResult));
	        if (legacy_A1111webui_success){
	            DEBUG_FetchInfo(6, "success");
	            yield break; 
	        }//user is still using legacy Automatic1111.
        
	        //use new info, via forge:
	        int num = SD_SysInfo_MGR.instance.sysInfo.Config.num_units();
	        on_set_numUnits( num );
	        DEBUG_FetchInfo(7, "success");
	    }


	    void EnsureExact_num_CTRLnets(int wantedNum, bool instantDestroy_excess){
	        DEBUG_EnsureCount(0);
	        int count = _controlNet_units.Count;
	        int excess = count - wantedNum;
	        if(excess==0){
	            DEBUG_EnsureCount(1);
	            return; //all good.
	        }

	        bool hadExcess = destroyExcess(count, excess, instantDestroy_excess);
	        if(hadExcess){ return; }

	        DEBUG_EnsureCount(3, Mathf.Abs(excess).ToString() + " adding new ones" );

	        for(int i=0; i<Mathf.Abs(excess); ++i){
	            var unit = Instantiate(_controlNet_PREFAB, _controlUnits_parent);
	            _controlNet_units.Add(unit);
	        }
	    }


	    bool destroyExcess(int count, int excess, bool instantDestroy_excess){
	        if(excess<=0){ return false;}
	        //too many:
	        DEBUG_EnsureCount(2, excess.ToString()+ " removing redundant ones");
	        for (int i=0; i<excess; ++i){ 
	            if(instantDestroy_excess){
	                DestroyImmediate(_controlNet_units[count-i-1].gameObject); 
	            }else{
	                Destroy(_controlNet_units[count-i-1].gameObject);
	            }
	        }
	        _controlNet_units.RemoveRange(count-excess, excess);
	        return true;
	    }


    
	    void DEBUG_FetchContiniously(int KeyIx, string suffix=""){
	        #if SP_VERBOSE_CTRLNET_DEBUG
	        Dictionary<int, string> dict = new Dictionary<int, string>(){
	            {0, "SD_ControlNetsList_UI::FetchContiniously() CTRLNets List FetchContiniously entered"},
	            {1, "SD_ControlNetsList_UI::FetchContiniously() CTRLNets List starting the RefreshInfo_fromNet_crnt"},
	            {2, "\n\n\n\n\n\n\n\n\n\n" },
	        };
	        Debug.Log(dict[KeyIx] + suffix);
	        #endif
	    }


	    void DEBUG_FetchInfo(int KeyIx, string suffix=""){
	        #if SP_VERBOSE_CTRLNET_DEBUG
	        Dictionary<int, string> dict = new Dictionary<int, string>(){
	            {0, "\n\n--SD_ControlNetsList_UI::FetchInfo() CTRLNetsList entered RefreshInfo_fromNet_crnt()\n\n"},
	            {1, "\n\n--SD_ControlNetsList_UI::FetchInfo() CTRLNetsList going to fetch models"},
	            {2, "\n\n--SD_ControlNetsList_UI::FetchInfo() CTRLNetsList going to fetch modules" },
	            {3, "\n\n--SD_ControlNetsList_UI::FetchInfo() CTRLNetsList going to fetch control types" },
	            {4, "\n\n--SD_ControlNetsList_UI::FetchInfo() CTRLNetsList going to fetch settings" },
	            {5, "\n\n--SD_ControlNetsList_UI::FetchInfo() FetchData_numCtrlUnits() started."  },
	            {6, "\n\n--SD_ControlNetsList_UI::FetchInfo() FetchData_numCtrlUnits() legacy a1111 queried: "  },
	            {7, "\n\n--SD_ControlNetsList_UI::FetchInfo() FetchData_numCtrlUnits() Forge queried. "  },
	            {8, "\n\n--SD_ControlNetsList_UI::FetchInfo() _controlNet_units.Count: "  },
	        };
	        Debug.Log(dict[KeyIx] + suffix);
	        #endif
	    }


	    void DEBUG_FetchData(int KeyIx, string suffix=""){
	        #if SP_VERBOSE_CTRLNET_DEBUG
	        Dictionary<int, string> dict = new Dictionary<int, string>(){
	            {0, "------SD_ControlNetsList_UI::FetchData() CTRLNET going to fetch info"},
	            {1, "------SD_ControlNetsList_UI::FetchData() Error: "},
	            {2, "------SD_ControlNetsList_UI::FetchData() CTRLNET obtained info:\n\n" },
	        };
	        Debug.Log(dict[KeyIx] + suffix);
	        #endif
	    }


	    void DEBUG_EnsureCount(int KeyIx, string suffix=""){
	        #if SP_VERBOSE_CTRLNET_DEBUG
	        Dictionary<int, string> dict = new Dictionary<int, string>(){
	            {0, "------SD_ControlNetsList_UI::EnsureCount() EnsureExact_num_CTRLnets entered"},
	            {1, "------SD_ControlNetsList_UI::EnsureCount() excess 0, all good"},
	            {2, $"------SD_ControlNetsList_UI::EnsureCount() too many "},
	            {3, $"------SD_ControlNetsList_UI::EnsureCount() too few "},
	        };
	        Debug.Log(dict[KeyIx] + suffix);
	        #endif
	    }
	}




	//response to  GET /model_list?update=true

	    [Serializable]
	    public class CTRLnets_ModelList{
	        public string[] model_list;
	        public static CTRLnets_ModelList CreateFromJSON(string jsonString){
	            // Use class-type information, to support inheritance of objects:
	            var settings = new JsonSerializerSettings{ TypeNameHandling = TypeNameHandling.Auto, };
	            return JsonConvert.DeserializeObject<CTRLnets_ModelList>(jsonString, settings);
	        }
	    }


	//response to GET /module_list?alias_names=false

	    [System.Serializable]
	    public class CTRLnets_SliderDetail{
	        public string name;
	        public float value;
	        public float min;
	        public float max;
	        public float step;
	    }

	    [System.Serializable]
	    public class CTRLnets_ModuleDetail{
	        public bool model_free;
	        public CTRLnets_SliderDetail[] sliders;
	    }

	    [System.Serializable]
	    public class CTRLnets_PreprocessorsList{
	        public string[] module_list;
	        public Dictionary<string, CTRLnets_ModuleDetail> module_detail;
	        public static CTRLnets_PreprocessorsList CreateFromJSON(string jsonString){
	            // Use class-type information, to support inheritance of objects:
	            var settings = new JsonSerializerSettings{ TypeNameHandling = TypeNameHandling.Auto, };
	            return JsonConvert.DeserializeObject<CTRLnets_PreprocessorsList>(jsonString, settings);
	        }
	    }


	//response to AFTER GET /control_types

	    public class ControlTypeDetails
	    {
	        [JsonProperty("module_list")]
	        public string[] module_list;
	        [JsonProperty("model_list")]
	        public string[] model_list;
	        [JsonProperty("default_option")]
	        public string default_option;
	        [JsonProperty("default_model")]
	        public string default_model;
	    }

	    public class ControlTypesResponse
	    {
	        [JsonProperty("control_types")]
	        public Dictionary<string, ControlTypeDetails> control_types;
	        public static ControlTypesResponse CreateFromJSON(string jsonString){
	            try{
	                // Use class-type information, to support inheritance of objects:
	                var settings = new JsonSerializerSettings{ TypeNameHandling = TypeNameHandling.Auto, };
	                return JsonConvert.DeserializeObject<ControlTypesResponse>(jsonString, settings);
	            }
	            catch (Exception e){ // Catching a more general exception
	                UnityEngine.Debug.LogError("Exception during JSON deserialization: " + e.Message);
	                return null;
	            }
	        }
	    }


	//response to GET /settings
	    [System.Serializable]
	    public class CTRLnets_Settings{
	        public int control_net_unit_count; //how many actual NET UNITS, not the model types.
	        public int control_net_max_models_num; //older variant of the count, some users had it.
	        public int num_units(){ 
	            return Mathf.Max(control_net_unit_count, control_net_max_models_num); 
	        }

	        public static CTRLnets_Settings CreateFromJSON(string jsonString){
	            // Use class-type information, to support inheritance of objects:
	            var settings = new JsonSerializerSettings{ TypeNameHandling = TypeNameHandling.Auto, };
	            return JsonConvert.DeserializeObject<CTRLnets_Settings>(jsonString, settings);
	        }
	    }
}//end namespace
