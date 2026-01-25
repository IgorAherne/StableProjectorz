using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.IO.Compression;
using UnityEngine.Networking;
using System;

namespace spz {

	//represents an entry inside the catalogue list.
	//Describes one of the 3d generators which can be downloaded by the user.
	//For example, Trellis, Dora, etc.
	public class Gen3D_CatalogueRow_UI : MonoBehaviour {

	    [SerializeField] Button _download_button;
	    [SerializeField] Button _showInExplorer_button;
	    [SerializeField] Button _moreInfo_button;//visit the website to learn more.
	    [Space(10)]
	    [SerializeField] RawImage _rawImg;
	    [SerializeField] TextMeshProUGUI _headerText;
	    [SerializeField] TextMeshProUGUI _descriptionText;
	    [SerializeField] GameObject _nonCommercial_badge;//enabled to signal that this entry is for demo only.
	    [Space(10)]
	    [SerializeField] GameObject _progressBar;
	    [SerializeField] Transform _progressBar_stretchMe;
	    [SerializeField] TextMeshProUGUI _onReady_text;

	    Gen3D_Catalogue_UI _wholeCataloguePanel;

	    Coroutine _onDownloadRepo_crtn = null;
	    Coroutine _loadThumbnail_crtn;

	    DownloadInfo _download_info;
	    string _moreInfo_url;


	    [Serializable]
	    public class DownloadInfo {
	        public string[] mirrors { get; set; } 
	        public string zip_name { get; set; }  
	        public string extract_path { get; set; }
	        public string description { get; set; } 
	        public int CurrentUrlIndex { get; set; } = 0;  
	        public int AttemptsForCurrentUrl { get; set; } = 0;  
	    }

	    public class CatalogueRow_initArg {
	        public List<string> imageUrls;
	        public string title;
	        public string description;
	        public DownloadInfo download_info;
	        public string info_url;
	        public bool isNonCommercial;
	        public string onReadyMessage = "Downloaded. Double-click its 'run-fp16.bat' file";
	    }


	    public void Init(Gen3D_Catalogue_UI wholeCataloguePanel, CatalogueRow_initArg arg) {
	        _wholeCataloguePanel = wholeCataloguePanel;

	        _headerText.text = arg.title;
	        _descriptionText.text = arg.description;
	        _download_info = arg.download_info;
	        _moreInfo_url = arg.info_url;
	        _nonCommercial_badge.SetActive(arg.isNonCommercial);
	        _onReady_text.text = arg.onReadyMessage;

	        // Start loading the thumbnail
	        if (_loadThumbnail_crtn != null) { StopCoroutine(_loadThumbnail_crtn); }
	        _loadThumbnail_crtn = StartCoroutine(LoadThumbnail_crtn(arg.imageUrls.ToArray()));
	    }


	    void OnButton_Download(){
	        if (_onDownloadRepo_crtn != null) { StopCoroutine(_onDownloadRepo_crtn); }
	        _onDownloadRepo_crtn = StartCoroutine(OnDownload_crtn());
	    }

	    void OnButton_MoreInfo() {
	        Application.OpenURL(_moreInfo_url);
	    }


	    bool warn_if_directoryExists(){
	        if (!Directory.Exists(_download_info.extract_path)) { return false; }

	        string[] topLevelItems = Directory.GetFileSystemEntries(_download_info.extract_path);
	        if (topLevelItems.Length == 0) { return false; }

	        string msg = $"Destination folder not empty. First, please delete the folder:\n{_download_info.extract_path}";
	        _wholeCataloguePanel.statusText.ShowStatusText(msg, 8);
	        return true;
	    }

	    bool create_dir() {
	        try {
	            Directory.CreateDirectory(_download_info.extract_path);
	            return true;
	        } catch (System.Exception e) {
	            _wholeCataloguePanel.statusText.ShowStatusText($"Failed to create directory: {e.Message}", 8);
	            _progressBar.SetActive(false);
	            return false;
	        }
	    }

	    IEnumerator OnDownload_crtn() {
	        if(_download_info == null){ 
	            _onDownloadRepo_crtn = null; yield break; }
        
	        if(warn_if_directoryExists()){ 
	            _onDownloadRepo_crtn = null; yield break; }

	        if(!create_dir()){
	            _onDownloadRepo_crtn = null; yield break; }

	        _download_button.gameObject.SetActive(false);
	        _progressBar.SetActive(true);
	        _progressBar_stretchMe.localScale = new Vector3(0.0001f, 1, 1);

	        string zipPath = Path.Combine(_download_info.extract_path, _download_info.zip_name);
	        // Download attempt
	        bool downloadSuccess = false;
	        while (!downloadSuccess && _download_info.CurrentUrlIndex < _download_info.mirrors.Length) {
	            string currentUrl = _download_info.mirrors[_download_info.CurrentUrlIndex];

	            Download_MGR.instance.DownloadFile(
	                currentUrl,
	                zipPath,
	                (progress) => _progressBar_stretchMe.localScale = new Vector3(progress * 0.8f, 1, 1),
	                false
	            );
	            while (Download_MGR.instance.IsDownloading(currentUrl)) {
	                yield return null;
	            }
	            if (File.Exists(zipPath)) {
	                downloadSuccess = true;
	                break;
	            }
	            _download_info.AttemptsForCurrentUrl++;
	            if (_download_info.AttemptsForCurrentUrl >= 2) {
	                _download_info.CurrentUrlIndex++;
	                _download_info.AttemptsForCurrentUrl = 0;
	            }
	            yield return new WaitForSeconds(1f);
	        }//end while

	        if (!downloadSuccess) {
	            _wholeCataloguePanel.statusText.ShowStatusText($"Failed to download {_download_info.description} - Check Connection / VPN", 8);
	            _download_button.gameObject.SetActive(true);
	            _download_button.interactable = true;//allow to re-press it.
	            _progressBar.SetActive(false);
	            _onDownloadRepo_crtn = null;
	            yield break;
	        }
	        ExtractZip(zipPath);
	        _onDownloadRepo_crtn = null;
	    }


	    void ExtractZip(string zipPath){
	        try {
	            ZipFile.ExtractToDirectory(zipPath, _download_info.extract_path, true);
	            if (File.Exists(zipPath)) File.Delete(zipPath);
	            _progressBar_stretchMe.localScale = new Vector3(1, 1, 1);
	            _progressBar.SetActive(false);
	            _download_button.gameObject.SetActive(true);
	            _download_button.interactable = false;//all is good, downloaded. Keep non-interactible
	        }
	        catch (System.Exception e) {
	            string msg = $"Failed to extract {_download_info.description}:\n{e.Message}";
	            _wholeCataloguePanel.statusText.ShowStatusText(msg, 8);
	            _progressBar.SetActive(false);
	            _download_button.gameObject.SetActive(true);
	            _download_button.interactable = true;//true, allow to re-press it.
	        }
	    }


	    IEnumerator LoadThumbnail_crtn(string[] imageUrls) {
	        for (int i=0; i < imageUrls.Length; i++) {
	            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrls[i])) {
	                yield return www.SendWebRequest();

	                if (www.result == UnityWebRequest.Result.Success) {
	                    var texture = DownloadHandlerTexture.GetContent(www);
	                    _rawImg.texture = texture;
	                    _loadThumbnail_crtn = null;
	                    yield break;  // Successfully loaded an image, stop trying
	                }
	                //Continue to next URL if this one fails
	            }
	        }
	        _loadThumbnail_crtn = null;
	        Debug.LogError("Failed to load image from all provided URLs");
	    }


	    void UpdateButtonStates(){
	        if (_download_info == null) { return; }

	        bool isDownloading = _onDownloadRepo_crtn != null;
	        bool hasExistingFiles = false;

	        if (Directory.Exists(_download_info.extract_path)) {
	            string[] topLevelItems = Directory.GetFileSystemEntries(_download_info.extract_path);
	            hasExistingFiles = topLevelItems.Length > 0;
	        }

	        // Update download button
	        bool shouldShowDownloadButton = !isDownloading && !hasExistingFiles;
	        if (_download_button.gameObject.activeSelf != shouldShowDownloadButton) {
	            _download_button.gameObject.SetActive(shouldShowDownloadButton);
	            _download_button.interactable = shouldShowDownloadButton;
	        }

	        // Update explorer button
	        bool shouldShowExplorerButton = hasExistingFiles && !isDownloading;
	        if (_showInExplorer_button.gameObject.activeSelf != shouldShowExplorerButton){
	            _showInExplorer_button.gameObject.SetActive(shouldShowExplorerButton);
	            _showInExplorer_button.interactable = shouldShowExplorerButton;
	            _onReady_text.gameObject.SetActive(shouldShowExplorerButton);
	        }
	    }


	    void OnButton_ShowInExplorer(){
	        if(_download_info == null){ return; }
	        // Using the provided OpenSubdir_and_theURL functionality
	        // We pass the extract path as the absolute file path and empty string as URL
	        string path = Path.Combine( Directory.GetParent(Application.dataPath).FullName, 
	                                    _download_info.extract_path );
	         // Open the folder on user's computer.
	        path = path.Replace('\\', '/'); // Normalize the path
			if (!string.IsNullOrEmpty(path)){
	            Application.OpenURL(path);
	        }
	    }


	    int _numUpdatesSinceAwake = 0;
	    void Update(){
	        _numUpdatesSinceAwake++;
	        if(_numUpdatesSinceAwake < 3){ 
	             // Force both rect transform and canvas updates, to ensure thumb's aspect ratio fitter resizes.
	            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
	        }
	        UpdateButtonStates();
	    }

	    void Awake() {
	        _download_button.onClick.AddListener(OnButton_Download);
	        _moreInfo_button.onClick.AddListener(OnButton_MoreInfo);
	        _showInExplorer_button.onClick.AddListener(OnButton_ShowInExplorer);
	        _progressBar.SetActive(false);
	    }

	    void OnDisable() {
	        if (_onDownloadRepo_crtn != null){
	            //show in both, in case if the panel is closed:
	            _wholeCataloguePanel.statusText.ShowStatusText($"Download Cancelled", 8);
	            Viewport_StatusText.instance.ShowStatusText($"Download Cancelled", false, 8, false);

	            StopCoroutine(_onDownloadRepo_crtn);
	            _onDownloadRepo_crtn = null;
	        }
	        if (_loadThumbnail_crtn != null) {
	            StopCoroutine(_loadThumbnail_crtn);
	            _loadThumbnail_crtn = null;
	        }
	    }
	}
}//end namespace
