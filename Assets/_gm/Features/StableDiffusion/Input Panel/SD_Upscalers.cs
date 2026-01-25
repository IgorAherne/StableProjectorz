using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.Linq;


namespace spz {

	[JsonConverter(typeof(SDUpscalerItemConverter))]
	public class SDUpscalerItem
	{
	    public string name;
	    public string model_name;
	    public string model_path;
	    public string model_url;
	    public float scale;
	}

	[Serializable]
	public class SDUpscalerList
	{
	    public SDUpscalerItem[] upscalers;
	    public static SDUpscalerList CreateFromJSON(string jsonString)
	    {
	        // Use class-type information, to support inheritance of objects:
	        var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, };
	        return JsonConvert.DeserializeObject<SDUpscalerList>("{\"upscalers\":" + jsonString + "}", settings);
	    }
	}



	public class SD_Upscalers : MonoBehaviour
	{
	    public static SD_Upscalers instance { get; private set; } = null;

	    public bool hasValidUpscaler => HasValidUpscaler();
	    public string selectedUpscaler_name => GetSelectedUpscaler_name();


	    public static Action OnGenUpscaleVisible_ButtonX2 { get; set; } = null;
	    public static Action OnGenUpscaleVisible_ButtonX4 { get; set; } = null;


	    bool _isFetchingUpscalers = false;
	    bool _upscalersListObtained = false;

	    // New logic state to replace direct UI access
	    List<string> _availableUpscalers = new List<string>();
	    string _currentSelectedName = "";
	    bool? _lastInteractableState = null;

	    //if we loaded from a save-file, we migth want to select a model.
	    //If we are not connected, this model won't be in the dropdown.
	    //But we can try to find it as soon a we connect next time;
	    string _preferedModelName_viaLoad = "";


	    public void Save(SD_GenSettingsInput_UI settingsSL)
	    {
	        settingsSL.neural_upscaler = new SD_NeuralUpscaler_SL();
	        settingsSL.neural_upscaler.selectedUpscaler_name = selectedUpscaler_name;
	    }

	    public void Load(SD_GenSettingsInput_UI from_this)
	    {
	        SD_NeuralUpscaler_SL sl = from_this.neural_upscaler;
	        if (sl == null) { return; }

	        // Logic change: we can't check dropdown index immediately here, 
	        // as data might not be fetched yet. We store the preference.
	        _preferedModelName_viaLoad = sl.selectedUpscaler_name;
	    }

	    public void PlayAttentionAnim()
	    {
	        StaticEvents.Invoke("SD_Upscalers:PlayAttentionAnim");
	    }


	    void Awake()
	    {
	        if (instance != null) { DestroyImmediate(this); return; }
	        instance = this;
	    }

	    void Start()
	    {
	        StaticEvents.SubscribeUnique<int>("SD_Upscalers_UI", OnDropdown_EntryPicked);

	        // _upscalersDropdown.onValueChanged.AddListener(OnDropdown_EntryPicked); // to be deleted
	        Coroutines_MGR.instance.StartCoroutine(FetchContiniously_crtn());
	        SD_Options_Fetcher.Act_onWillSendOptions_AmmendPlz += OnWillSendOptions_AmmendPlz;

	        // Listen for UI button presses via StaticEvents
	        StaticEvents.SubscribeUnique("SD_Upscalers_UI:OnUpscaleX2", () => OnGenUpscaleVisible_ButtonX2?.Invoke());
	        StaticEvents.SubscribeUnique("SD_Upscalers_UI:OnUpscaleX4", () => OnGenUpscaleVisible_ButtonX4?.Invoke());
	    }

	    void Update()
	    {
	        MaybeActivate_Upscale_buttons();
	    }


	    // We don't set the .interactable of the buttons themselves, because we want
	    // to still capture their press, if user attempts to click them during "inactive".
	    void MaybeActivate_Upscale_buttons()
	    {
	        bool can_genArt;
	        bool can_genBG;
	        StableDiffusion_Hub.instance.isCanGenerate(out can_genArt, out can_genBG);

	        bool interactable = can_genArt || can_genBG;

	        // Logic change: Invoke event only if state changes
	        if (_lastInteractableState != interactable)
	        {
	            _lastInteractableState = interactable;
	            StaticEvents.Invoke<bool>("SD_Upscalers:SetButtonsInteractable", interactable);
	        }
	    }


	    IEnumerator FetchContiniously_crtn()
	    {
	        while (true)
	        {
	            if (!Connection_MGR.is_sd_connected)
	            {
	                yield return new WaitForSeconds(0.211f);
	                continue;
	            }

	            if (!_isFetchingUpscalers)
	            {
	                Coroutines_MGR.instance.StartCoroutine(GetUpscalers_crtn());
	            }

	            dropdown_LoadedSavedModel_maybe();
	            yield return new WaitForSeconds(3f);
	        }
	    }


	    IEnumerator GetUpscalers_crtn()
	    {
	        //Don't send network request to webui if rendering, else it seems to stuck it sometimes.
	        if (StableDiffusion_Hub.instance._generating) { yield break; }

	        _isFetchingUpscalers = true;
	        UnityWebRequest request = UnityWebRequest.Get(Connection_MGR.A1111_SD_API_URL + "/upscalers");
	        yield return request.SendWebRequest();

	        bool isBad = request.result == UnityWebRequest.Result.ConnectionError;
	        isBad |= request.result == UnityWebRequest.Result.ProtocolError;
	        if (isBad)
	        {
	            Debug.LogError("Error: " + request.error);
	        }
	        else
	        {
	            _upscalersListObtained = true;
	            SDUpscalerList listOfUpscalers = SDUpscalerList.CreateFromJSON(request.downloadHandler.text);
	            Populate_List_And_InformUI(listOfUpscalers);
	        }
	        _isFetchingUpscalers = false;
	    }


	    // Logic change: Renamed from Populate_Dropdown
	    void Populate_List_And_InformUI(SDUpscalerList listOfUpscalers)
	    {
	        var newNames = Array.ConvertAll(listOfUpscalers.upscalers, upscaler => upscaler.name).ToList();
	        if (!newNames.Exists(name => name.ToLower() == "none")) { newNames.Insert(0, "None"); }

	        bool different = newNames.Count != _availableUpscalers.Count;
	        if (!different)
	        {
	            for (int i = 0; i < newNames.Count; ++i)
	            {
	                different |= _availableUpscalers[i] != newNames[i];
	                if (different) { break; }
	            }
	        }

	        if (different)
	        {
	            _availableUpscalers = newNames;
	            // Inform UI about the new list
	            StaticEvents.Invoke<List<string>>("SD_Upscalers:ListUpdated", _availableUpscalers);

	            // Re-validate selection
	            if (!string.IsNullOrEmpty(_currentSelectedName) && !_availableUpscalers.Contains(_currentSelectedName))
	            {
	                _currentSelectedName = ""; // reset if no longer exists
	            }
	        }
	    }


	    // If we loaded a project file, maybe we couldn't find required model back then.
	    // So, see if we can find it now, and set dropdown to that value if possible.
	    void dropdown_LoadedSavedModel_maybe()
	    {
	        bool wantsLoaded = string.IsNullOrEmpty(_preferedModelName_viaLoad) == false;
	        if (!wantsLoaded) { return; }

	        int modelIndex = FindIndex_inList(_preferedModelName_viaLoad);
	        if (modelIndex >= 0)
	        {
	            _currentSelectedName = _preferedModelName_viaLoad;

	            // Inform UI to visually select this item
	            StaticEvents.Invoke<string>("SD_Upscalers:SetSelectedByName", _currentSelectedName);

	            _preferedModelName_viaLoad = "";//found, no longer need to search for it.
	        }
	    }

	    int FindIndex_inList(string activeModel_name)
	    {
	        if (_availableUpscalers.Count == 0) { return -1; }
	        return _availableUpscalers.FindIndex(name => name == activeModel_name);
	    }


	    string GetSelectedUpscaler_name()
	    {
	        if (string.IsNullOrEmpty(_currentSelectedName)) { return ""; }
	        string chosen = _currentSelectedName;
	        if (chosen.ToLower().Contains("not connected")) { return ""; }
	        return chosen.ToLower().Contains("none") ? "" : chosen;//because sometimes None or none conflict or don't get recognized.
	    }

	    public bool HasValidUpscaler()
	    {
	        if (string.IsNullOrEmpty(_currentSelectedName)) { return false; }
	        string chosen = _currentSelectedName;
	        if (chosen.ToLower().Contains("not connected")) { return false; }
	        return chosen.ToLower().Contains("none") == false;//because sometimes None or none conflict or don't get recognized.
	    }


	    // Called via StaticEvents from UI
	    void OnDropdown_EntryPicked(int ix)
	    {
	        if (ix < 0 || ix >= _availableUpscalers.Count) { return; }
	        _currentSelectedName = _availableUpscalers[ix];

	        // We will specify upscaler during txt2img gen request.
	        // But for img2img we need to explicitly set the upscaler.

	        //never feeding upscaler into options. User will always manually upscale images.  Nov 2024
	        //   SD_Options_Fetcher.instance.SubmitOptions_Asap();
	    }


	    void OnWillSendOptions_AmmendPlz(SD_OptionsPacket opt)
	    {
	        return;//never feeding upscaler into options. User will always manually upscale images. Nov 2024
	        if (_upscalersListObtained == false) { return; }
	        string selectedUpscaler_name = GetSelectedUpscaler_name();
	        opt.upscaler_for_img2img = selectedUpscaler_name;
	    }
	}
}//end namespace
