using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace spz {

	//can present the user info about the current stable diffusion.
	//And can show info to the user, inviting to update this program when a newer version is available.
	public class VersionPopupPanel_UI : MonoBehaviour{

	    [SerializeField] RectTransform _popupTransform;

	    [SerializeField] TextMeshProUGUI _wholePanel_headerText;
	    [SerializeField] TextMeshProUGUI _versionText;
	    [SerializeField] TextMeshProUGUI _featuresText;
	    [SerializeField] TextMeshProUGUI _descriptionText;
	    [SerializeField] TextMeshProUGUI _checkingText;
	    [SerializeField] Button _openURL_button;
	    [SerializeField] Animator _anim;
	    [Space(10)]
	    [SerializeField] RectTransform _helpButton; //will shrink this panel into here when hidden.
	    [SerializeField] ScrollRect_AutoScroll _scrollRect_autoscroll;
	    [SerializeField] Button _bg_surface;
	    [SerializeField] NonDrawingGraphic _blockAnyClicks_surface;

	    Coroutine _showProgressText_crtn;
	    public enum VersionDecision{ Checking,  AlreadyHaveLatest, CanDownloadNewer, }
	    VersionDecision _decision;
    
	    string _description_optional = ""; //if new description was fetch from the internet
	    string _newVersion_optional = "";

	    public RectTransform helpButtonRectTransf => _helpButton;
	    public bool isShowing => gameObject.activeSelf;


	    public void ShowPanel( VersionDecision decision, string newVersionOptional="", string descriptionOptional="" ){
	        if(gameObject.activeSelf){ return; }//already showing.
	        gameObject.SetActive(true);
	        StartCoroutine( Show_crtn() );

	        _decision = decision;
	        _newVersion_optional = newVersionOptional;
	        _description_optional = descriptionOptional;
	        SetInfo_AfterDecision();

	        if ( decision==VersionDecision.Checking ){
	            if(_showProgressText_crtn!=null){  StopCoroutine(_showProgressText_crtn);  }
	            _showProgressText_crtn =  StartCoroutine( ShowProgressText_crtn() );
	        }
	    }


	    public void UpdateVersionDecision(VersionDecision decision, string newVersionOptional, string descriptionOptional = ""){
	        _decision = decision;//if a coroutine is running, it will soon notice a change in this variable.
	        _newVersion_optional = newVersionOptional;
	        _description_optional = descriptionOptional;
	    }


	    IEnumerator Show_crtn(){
	        _popupTransform.localPosition = Vector3.zero;
	        _popupTransform.localScale    = Vector3.one;
	        _anim.SetTrigger("show");
	        _scrollRect_autoscroll.GetComponent<ScrollRect>().verticalNormalizedPosition = 0.001f;
	        _scrollRect_autoscroll.ScrollToEnd(1.2f, isScrollDown:false);

	        _blockAnyClicks_surface.gameObject.SetActive(true);//to prevent accidental click-away.
	        yield return new WaitForSeconds(0.6f);
	        _blockAnyClicks_surface.gameObject.SetActive(false);
	    }


	    IEnumerator ShowProgressText_crtn(){

	        float step = 0.5f;
	        float startTime = Time.time;

	        bool isBreak(){
	            float elapsed = Time.time-startTime;
	            if(elapsed < 2.51f){ return false; }
	            return _decision != VersionDecision.Checking;
	        }

	        while (true){
	            _checkingText.text = "CHECKING FOR UPDATES";
	            yield return new WaitForSeconds(step);
	            if(isBreak()){ break; }

	            _checkingText.text = "CHECKING FOR UPDATES.";
	            yield return new WaitForSeconds(step);
	            if(isBreak()){ break; }

	            _checkingText.text = "CHECKING FOR UPDATES..";
	            yield return new WaitForSeconds(step);
	            if(isBreak()){ break; }

	            _checkingText.text = "CHECKING FOR UPDATES...";
	            yield return new WaitForSeconds(step);
	            if(isBreak()){ break; }
	        }

	        SetInfo_AfterDecision();

	        _showProgressText_crtn = null;
	    }


	    void SetInfo_AfterDecision(){
	        if (_decision == VersionDecision.AlreadyHaveLatest || _decision==VersionDecision.Checking){ 
	            _openURL_button.gameObject.SetActive(false);
	            _checkingText.gameObject.SetActive(true);
	            _wholePanel_headerText.text = "STABLE PROJECTORZ";
	            _featuresText.text = "CURRENT FEATURES:";
	            _versionText.text =  $"(VERSION {CheckForUpdates_MGR.CURRENT_VERSION_HERE})";
	            _checkingText.text = "You are up-to-date :)";
	        }
	        else { 
	            _openURL_button.gameObject.SetActive(true);
	            _checkingText.gameObject.SetActive(false);
	            _wholePanel_headerText.text = "UPDATE AVAILABLE";
	            _versionText.text =  $"(VERSION {_newVersion_optional})";
	            _featuresText.text = "NEW FEATURES:";
	            if(_description_optional != ""){
	                _description_optional = _description_optional.Replace("\\u2022", "\u2022");
	                _descriptionText.text = _description_optional;
	            }
	        }
	    }



	    void OnBackgroundClicked(){
	        _anim.SetTrigger("hide");
	        StartCoroutine(ShrinkSelf_AndDisable() );
	        if (_showProgressText_crtn != null){  StopCoroutine(ShowProgressText_crtn());  }
	    }


	    IEnumerator ShrinkSelf_AndDisable(float dur=0.8f){
	        _blockAnyClicks_surface.gameObject.SetActive(true);

	        while (true){//keep waiting until animation enters the Hide state.
	            var stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
	            if(stateInfo.IsName("Hide")){ break; }
	            yield return null; 
	        }

	        Vector3 fromPos = _popupTransform.transform.position;
	        float startTime = Time.unscaledTime;
	        while(true){
	            float elapsed = Time.unscaledTime - startTime;
            
	            var stateInfo  = _anim.GetCurrentAnimatorStateInfo(0);
	            _popupTransform.position = Vector3.Lerp(fromPos, _helpButton.transform.position, stateInfo.normalizedTime);

	            if( stateInfo.normalizedTime>0.999f || elapsed>3){ break; }
	            yield return null;
	        }
	        _blockAnyClicks_surface.gameObject.SetActive(false);
	        gameObject.SetActive(false);//this entire object, with canvas (not just panel)
	    }//end()


	    void OnOpenURL_button(){
	        Application.OpenURL( CheckForUpdates_MGR.WEBSITE_DOWNLOAD_URL );
	    }

	    void Awake(){
	        _bg_surface.onClick.AddListener( OnBackgroundClicked );
	        _openURL_button.onClick.AddListener( OnOpenURL_button );
	    }

	}
}//end namespace
