using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace spz {

	public class ControlNetUnit_DownloadHelper : MonoBehaviour{
	              public ControlNetUnit_UI myUnit => _myUnit;
	    [SerializeField] ControlNetUnit_UI _myUnit;
	    [SerializeField] CanvasGroup _contentsCanvGroup;
	    [Space(10)]
	    [SerializeField] TMP_Dropdown _modelsDropdown;
	    [SerializeField] SlideOut_Widget_UI _getMore_slideOut;
	    [Space(10)]
	    [SerializeField] Button _download_mandatoryDepthModel; //only shown if there are no models (happens after install).
	    [SerializeField] RectTransform _mandatDepthModel_progress; //we stretch it to show how much was downloaded.
	    [SerializeField] DownloadFile_if_NotYetExist _downloadModel_ifNotExist;//file that will perform actual downloading.
	    [SerializeField] GameObject _downloaded_mandatDepthModel_go;//small UI element, contains a message "please restart StableProjectorz".
   
	    public static bool isSomeUnit_downloadingModels { get; private set; } = false;
	    public static Action<ControlNetUnit_DownloadHelper> _onSomeUnit_startedDownloadModel { get; set; } = null;
	    public static Action<ControlNetUnit_DownloadHelper, float> _onSomeUnit_downloadModelPcnt { get; set; } = null;
	    public static Action<ControlNetUnit_DownloadHelper> _onSomeUnit_stoppedDownloadModel { get; set; } = null;


	    public void OnRefreshInfoComplete(bool isNeedDownloadMandatoryModel ){
	        bool someDownloading = ControlNetUnit_DownloadHelper.isSomeUnit_downloadingModels;
	        _download_mandatoryDepthModel.gameObject.SetActive( isNeedDownloadMandatoryModel && !someDownloading);
	        _mandatDepthModel_progress.parent.gameObject.SetActive( isNeedDownloadMandatoryModel && someDownloading);
	    }

	     // The big large button that's shown instead of the models-dropdown if it's empty.
	    // Happens after installing the StableProjectors, when there are no control-net models initially.
	    void OnDownload_MandatoryDepthModel_button(){
	        if(ControlNetUnit_DownloadHelper.isSomeUnit_downloadingModels){ return; }
	        ControlNetUnit_DownloadHelper.isSomeUnit_downloadingModels = true;//will prevent other controlnet units from downloading.

	        _onSomeUnit_startedDownloadModel.Invoke(this);
	        _downloadModel_ifNotExist.DownloadFile("", "", onProgress);

	        void onProgress(float pcnt01){
	            _onSomeUnit_downloadModelPcnt.Invoke(this, pcnt01);
	            if(pcnt01<1.0f){ return;}
	            _onSomeUnit_stoppedDownloadModel?.Invoke(this);
	            ControlNetUnit_DownloadHelper.isSomeUnit_downloadingModels = false;
	        }
	    }

	    void OnSomeUnit_StartDownloadModel(ControlNetUnit_DownloadHelper who){
	        _download_mandatoryDepthModel.gameObject.SetActive(false);//keep button hidden.
	        _mandatDepthModel_progress.parent.gameObject.SetActive(true);//ensure progress is shown (instead of button).
	        _contentsCanvGroup.interactable = false;
	    }

	    void OnSomeUnit_StopDownloadModel(ControlNetUnit_DownloadHelper who){
	        _download_mandatoryDepthModel.gameObject.SetActive(false);
	        _mandatDepthModel_progress.parent.gameObject.SetActive(false);
	        _contentsCanvGroup.interactable = true;
	        _downloaded_mandatDepthModel_go.SetActive(true);//tells user to restart StableProjectorz, to refresh controlnets.
	    }

	    void OnSomeUnit_DownloadModelPcnt(ControlNetUnit_DownloadHelper who, float progress01){
	        _mandatDepthModel_progress.transform.localScale = new Vector3(progress01, 1, 1);
	    }

    
	    void Update(){
	        _getMore_slideOut._dontAutoHide = true;
	        _getMore_slideOut.Toggle_if_Different(_modelsDropdown.IsExpanded);
	    }


	    void Awake(){
	        _download_mandatoryDepthModel.onClick.AddListener( OnDownload_MandatoryDepthModel_button );
	        _onSomeUnit_startedDownloadModel += OnSomeUnit_StartDownloadModel;
	        _onSomeUnit_stoppedDownloadModel += OnSomeUnit_StopDownloadModel;
	        _onSomeUnit_downloadModelPcnt  += OnSomeUnit_DownloadModelPcnt;
	    }

	    void OnDestroy(){
	        _onSomeUnit_startedDownloadModel -= OnSomeUnit_StartDownloadModel;
	        _onSomeUnit_stoppedDownloadModel -= OnSomeUnit_StopDownloadModel;
	        _onSomeUnit_downloadModelPcnt -= OnSomeUnit_DownloadModelPcnt;
	    }
	}
}//end namespace
