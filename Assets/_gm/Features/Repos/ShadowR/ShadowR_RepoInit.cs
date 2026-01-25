using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace spz {

	// Can download the repository of Shadow_R, preparing it for the use
	// Algorithm for reducing/removing the shadows from an image.
	// Shadow_R Repository Initialization
	public class ShadowR_RepoInit : BaseRepoInit
	{
	    [SerializeField] protected TextMeshProUGUI _pkl_warning_text;
    
	    protected override string RepoName => "Shadow_R";

	    protected override DownloadPortion[] GetDownloadInfo(){
	        return new DownloadPortion[]{
	            new DownloadPortion{
	                Mirrors = new string[] {
	                    "https://github.com/IgorAherne/Shadow_R/releases/download/latest/code.zip",
	                },
	                ZipName = "code.zip",
	                ExtractPath = _repoDir,
	                Description = "Code folder"
	            },
	            new DownloadPortion{
	                Mirrors = new string[] {
	                    "https://github.com/IgorAherne/Shadow_R/releases/download/latest/system.zip",
	                },
	                ZipName = "system.zip",
	                ExtractPath = _repoDir,
	                Description = "Python System-Folder"
	            }
	        };
	    }//end()

	    public override bool ShouldDownload(){
	        if (base.ShouldDownload()){ return true; }
        
	        string exeDirectory = Directory.GetParent(Application.dataPath).FullName;
	        string repoPath = Path.Combine(exeDirectory, RepoName);
	        string codePath = Path.Combine(repoPath, "code");
	        string systemPath = Path.Combine(repoPath, "system");

	        int numWeights = Directory.EnumerateFiles(repoPath).Count();
	        if (numWeights < 2){ return true; }

	        if (Directory.Exists(codePath) == false){ return true; }
	        return false;
	    }

	    public override void ShowPanel(){
	        _pkl_warning_text.gameObject.SetActive(true);
	        base.ShowPanel();
	    }

	    protected override void OnDownloadButton(){
	        _pkl_warning_text.gameObject.SetActive(false);
	        base.OnDownloadButton();
	    }
	}


	// Helps to donwload a repository
	public abstract class BaseRepoInit : MonoBehaviour
	{
	    [SerializeField] protected Button _cancel_button;
	    [SerializeField] protected GameObject _wholePanel_go;
	    [Space(10)]
	    [SerializeField] protected Button _downloadButton;
	    [SerializeField] protected RectTransform _progressTransform;

	    //file created after downloaded all the stuff AND after unarchived it:
	    protected const string DOWNLOAD_COMPLETE_FLAG = "download_complete.txt";

	    protected class DownloadPortion{
	        public string[] Mirrors { get; set; }  // Array of URLs to try
	        public string ZipName { get; set; }
	        public string ExtractPath { get; set; }
	        public string Description { get; set; }
	        public int CurrentUrlIndex { get; set; } = 0;  // Track which URL we're trying
	        public int AttemptsForCurrentUrl { get; set; } = 0;  // Track attempts for current URL
	    }

	    protected DownloadPortion[] _downloads;
	    protected string _repoDir;
	    protected bool _isDownloading = false;
	    protected bool _showPanel_alreadyInvoked = false;

	    protected abstract string RepoName { get; }
	    protected abstract DownloadPortion[] GetDownloadInfo();

	    protected virtual void Start(){
	        InitializeDownloadInfo();
	        _downloadButton.onClick.AddListener(OnDownloadButton);
	        _progressTransform.gameObject.SetActive(false);
	        _cancel_button.onClick.AddListener(OnUserCancelled);
	        if (_showPanel_alreadyInvoked == false){
	            HidePanel_StopDownloads();
	        }
	    }

	    protected virtual void InitializeDownloadInfo(){
	        string exeDirectory = Directory.GetParent(Application.dataPath).FullName;
	        _repoDir = Path.Combine(exeDirectory, RepoName);
	        _downloads = GetDownloadInfo();
	    }

	    public virtual bool ShouldDownload(){
	        string exeDirectory = Directory.GetParent(Application.dataPath).FullName;
	        string repoPath = Path.Combine(exeDirectory, RepoName);
	        string flagFilePath = Path.Combine(repoPath, DOWNLOAD_COMPLETE_FLAG);
	        if (Directory.Exists(repoPath) == false){ return true; }
	        if (File.Exists(flagFilePath) == false){ return true; }

	        return false;
	    }

	    public virtual void ShowPanel(){
	        _showPanel_alreadyInvoked = true;
	        _wholePanel_go.SetActive(true);
	        _cancel_button.gameObject.SetActive(true);
	        _downloadButton.gameObject.SetActive(true);
	        _progressTransform.gameObject.SetActive(false);
	    }

	    protected virtual void HidePanel_StopDownloads(){
	        if(!_isDownloading){
	            _wholePanel_go.SetActive(false);
	            return;
	        }//else was downloading:
	        foreach (var download in _downloads){
	            foreach (var url in download.Mirrors){  Download_MGR.instance.CancelDownload(url); }
	        }
	        _isDownloading = false;
	        _wholePanel_go.SetActive(false);
	    }

	    protected virtual void OnUserCancelled(){
	        HidePanel_StopDownloads();
	        CleanupDownloadDirectory();
	    }

	    protected virtual void CleanupDownloadDirectory(){
	        if (Directory.Exists(_repoDir)){
	            try{
	                Directory.Delete(_repoDir, true);
	                Debug.Log($"Cleaned up {RepoName} directory");
	            }
	            catch (System.Exception e){
	                Debug.LogError($"Failed to cleanup directory: {e.Message}");
	            }
	        }
	    }

	    protected virtual void OnDownloadButton(){
	        _downloadButton.gameObject.SetActive(false);
	        _progressTransform.gameObject.SetActive(true);
        
	        CleanupDownloadDirectory();
        
	        _isDownloading = true;
	        StartCoroutine(DownloadAndExtractAll());
	    }


	    protected virtual IEnumerator DownloadAndExtractAll(){
	        // Each file gets an equal slice of progress [0..1].
	        float portionSize = 1f / _downloads.Length;
	        bool isSuccess = true;
    
	        Directory.CreateDirectory(_repoDir);
	        _isDownloading = true;

	        for(int i=0; i < _downloads.Length && isSuccess && _isDownloading; i++){
	            // The start of this download's portion
	            float baseProgress = i * portionSize;
        
	            // Attempt to download+extract
	            var downloadOperation = new DownloadOperation();
	            yield return AttemptDownloadAndExtract(_downloads[i], baseProgress, portionSize, downloadOperation);

	            if(!downloadOperation.Success){
	                Viewport_StatusText.instance.ShowStatusText(downloadOperation.ErrorMessage, false, 8, false);
	                isSuccess = false;
	            }
	        }

	        _isDownloading = false;
	        _progressTransform.gameObject.SetActive(false);
	        _downloadButton.gameObject.SetActive(!isSuccess);

	        if(!isSuccess) yield break;

	        // Mark as done
	        string flagFilePath = Path.Combine(_repoDir, DOWNLOAD_COMPLETE_FLAG);
	        try {
	            File.WriteAllText(flagFilePath, System.DateTime.Now.ToString());
	        }
	        catch(System.Exception e){
	            Debug.LogError($"Failed to create download_complete flag-file: {e.Message}");
	            isSuccess = false;
	            _downloadButton.gameObject.SetActive(true);
	        }
	        HidePanel_StopDownloads();
	    }


	    protected class DownloadOperation {
	        public bool Success = false;
	        public string ErrorMessage ="";
	    }

    
	    // tries different mirrors, until successs:
	    protected IEnumerator AttemptDownloadAndExtract( DownloadPortion downloadInfo,  float baseProgress, 
	                                                     float portionSize,  DownloadOperation operation ){
	        operation.Success = false;
	        bool downloadSuccess = false;
	        string zipPath = Path.Combine(_repoDir, downloadInfo.ZipName);
	        Directory.CreateDirectory(downloadInfo.ExtractPath);
    
	        // Download (80% of this portion)
	        while(!downloadSuccess && downloadInfo.CurrentUrlIndex < downloadInfo.Mirrors.Length && _isDownloading){
	            string currentUrl = downloadInfo.Mirrors[downloadInfo.CurrentUrlIndex];
	            Debug.Log($"Attempting download of {downloadInfo.Description} from {currentUrl} ...");

	            Download_MGR.instance.DownloadFile(
	                currentUrl, zipPath,
	                (progress) => UpdateProgress(baseProgress + progress * portionSize * 0.8f),
	                false
	            );

	            // Wait for finish/cancel
	            while(Download_MGR.instance.IsDownloading(currentUrl) && _isDownloading){
	                yield return null;
	            }

	            if(!_isDownloading) yield break; // user canceled
        
	            if(File.Exists(zipPath)){
	                downloadSuccess = true;
	                break;
	            }

	            downloadInfo.AttemptsForCurrentUrl++;
	            if(downloadInfo.AttemptsForCurrentUrl >= 2){
	                downloadInfo.CurrentUrlIndex++;
	                downloadInfo.AttemptsForCurrentUrl = 0;
	            }
	            yield return new WaitForSeconds(1f);
	        }

	        if(!downloadSuccess){
	            operation.ErrorMessage = $"Failed to download {downloadInfo.Description} from all mirrors";
	            yield break;
	        }

	        // Extraction (remaining 20%)
	        Debug.Log($"Extracting {downloadInfo.Description}...");
	        try {
	            ZipFile.ExtractToDirectory(zipPath, downloadInfo.ExtractPath, true);
	            if(File.Exists(zipPath)) File.Delete(zipPath);
	            UpdateProgress(baseProgress + portionSize); // jump to full portion
	            operation.Success = true;
	        }
	        catch(System.Exception e){
	            Debug.LogError($"Failed to extract {downloadInfo.Description}: {e.Message}");
	            operation.ErrorMessage = e.Message;
	        }
	    }



	    protected virtual void UpdateProgress(float progress){
	        _progressTransform.GetChild(0).localScale = new Vector3(Mathf.Clamp01(progress), 1, 1);
	    }
	}
}//end namespace
