using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace spz {

	// represents a panel, to show what 3D generators are available for a download.
	// For example Trellis, Dora, etc. Shows them as rows inside a scrollable list.
	public class Gen3D_Catalogue_UI : MonoBehaviour {

	    public static Gen3D_Catalogue_UI instance { get; private set; } = null;

	    [SerializeField] GameObject _whole_panel_GO;

	    [SerializeField] Transform _rows_parent;
	    [SerializeField] Gen3D_CatalogueRow_UI _row_PREFAB;
	    [SerializeField] Transform _moreToCome_text;
	    [Space(10)]
	    [SerializeField] Button _close_button;
	    [SerializeField] Button _close_surface;
	    [SerializeField] Button _discord_button;

	    public Gen3D_Catalogue_StatusText_UI statusText => _statusText;
	    [SerializeField] Gen3D_Catalogue_StatusText_UI _statusText;

	    // Where can we obtain a file that contains a list of generators.
	    // We keep several links, in case one of them is unavailable.
	    static readonly List<string> _catalogue_urls = new List<string>() {
	        "https://raw.githubusercontent.com/IgorAherne/stable-projectorz-gen3d-list/main/avail-generators.txt",
	        "https://sourceforge.net/p/stable-projectorz-gen3d-list/code/ci/main/tree/avail-generators.txt?format=raw",
	    };

	    Coroutine _fetchCatalogue_crtn;

	    [System.Serializable]
	    class CatalogueData {
	        public List<GeneratorEntry> generators;
	    }

	    [System.Serializable]
	    class GeneratorEntry {
	        public string title;
	        public string description;
	        public List<string> images;
	        public string info_url;
	        public Gen3D_CatalogueRow_UI.DownloadInfo download;
	        public bool nonCommercial;
	        public string onReadyMessage = "Downloaded. Double-click its 'run-fp16.bat' file";
	    }


	    void ClearExistingRows(){// Remove all existing catalogue entries
	        for (int i = _rows_parent.childCount - 1; i >= 0; i--) {
	            Transform child = _rows_parent.GetChild(i);
	            if(child == _moreToCome_text){ continue; }//always allow this text to exist.
	            Destroy(child.gameObject);
	        }
	    }

	    IEnumerator FetchCatalogue_crtn()
	    {
	        ClearExistingRows();

	        // Try each URL until we get a successful response
	        for (int urlIndex = 0; urlIndex < _catalogue_urls.Count; urlIndex++)
	        {
	            string currentUrl = _catalogue_urls[urlIndex];

	            using (UnityWebRequest www = UnityWebRequest.Get(currentUrl))
	            {
	                yield return www.SendWebRequest();
	                if (www.result != UnityWebRequest.Result.Success) { continue; }

	                try{
	                    // Use Newtonsoft.Json instead of JsonUtility
	                    CatalogueData data = JsonConvert.DeserializeObject<CatalogueData>(www.downloadHandler.text);
	                    if (data == null || data.generators == null) { continue; }

	                    foreach (var entry in data.generators) { SpawnRow(entry); }
	                    _fetchCatalogue_crtn = null;
	                    yield break; // Successfully loaded and processed
	                }
	                catch (System.Exception e){
	                    #if UNITY_EDITOR
	                    Debug.LogError($"Failed to parse catalogue JSON from {currentUrl}: {e.Message}");
	                    #endif
	                }
	            }
	        }
	        string msg = "Failed to fetch the catalogue from all URLs. Check the internet connection";
	        _statusText.ShowStatusText(msg, 3);
	        _fetchCatalogue_crtn = null;
	    } 

	    void SpawnRow(GeneratorEntry entry) {
	        var row = GameObject.Instantiate(_row_PREFAB, _rows_parent);
	        var arg = new Gen3D_CatalogueRow_UI.CatalogueRow_initArg {
	            imageUrls = entry.images,
	            title = entry.title,
	            description = entry.description,
	            download_info = entry.download,
	            info_url = entry.info_url,
	            isNonCommercial = entry.nonCommercial,
	            onReadyMessage = entry.onReadyMessage,
	        };
	        row.Init(this, arg);
	        _moreToCome_text.SetAsLastSibling();
	    }

	    public void Show(){ 
	        _whole_panel_GO.SetActive(true);
	        if(_fetchCatalogue_crtn == null){ 
	            _fetchCatalogue_crtn = StartCoroutine(FetchCatalogue_crtn());
	        }
	    }

	    public void Hide(){ 
	        if (_fetchCatalogue_crtn != null){
	            StopCoroutine(_fetchCatalogue_crtn);
	            _fetchCatalogue_crtn = null;
	        }
	        _whole_panel_GO.SetActive(false);
	    }

	    void OnJoinDiscordButton(){
	        Application.OpenURL("https://discord.gg/aWbnX2qan2");
	    }

	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	        _close_button.onClick.AddListener( ()=>Hide() );
	        _close_surface.onClick.AddListener( ()=>Hide() );
	        _discord_button.onClick.AddListener( OnJoinDiscordButton );
	    }

	    void Start(){
	        Hide(); //begin disabled.
	    }
	}
}//end namespace
