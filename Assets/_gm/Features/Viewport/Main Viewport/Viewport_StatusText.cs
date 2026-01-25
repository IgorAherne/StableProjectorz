using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace spz {

	public class Viewport_StatusText : MonoBehaviour { 
	    public static Viewport_StatusText instance { get; private set; } = null;

	    [SerializeField] GameObject _selfHide_GO; //main parent, allows us to keep self hidden, regardless of any other visibility.
	    [Space()]
	    [SerializeField] Button _help_button;
	    [SerializeField] Image _textBackground;
	    [SerializeField] GameObject _helpTipsPanel;
	    [SerializeField] CanvasGroup _helpTipsPanel_canvGrp;
	    [Space(10)]
	    [SerializeField] GraphicRaycaster _rayacster; //will raycast towards the help button
	    [SerializeField] Image _progressTotal;
	    [SerializeField] TextMeshProUGUI _statusText;//we can show messages in it
	    [SerializeField] RectTransform _statusText_holder;
	    [Space(10)]
	    [SerializeField] TextMeshProUGUI _stickyMsg_Text;//can keep showing message, even when status text has faded out. For example "Using Auto-Erase!"
	    [Space(10)]
	    [SerializeField] Button _openWelcomeNovice_button;//launches panel with tutorials, etc.
	    [SerializeField] Button _openCheckForUpdates_button;//launches panel which shows currnet version and checks for upgrades.
	    [Space(10)]
	    [SerializeField] Button _3dGenerators_catalogue_button;//opens a panel, showing all repositories that can be downloaded.
	    [SerializeField] Gen3D_Catalogue_UI _3d_gencatalogue;

	    float _progressTotal_targ = 0;
	    float _fadeOutText_after = -9999;
	    float _clickedHelpButton_time = -9999;
	    float _statusText_originalPosY;

	    LocksHashset_OBJ _keepHidden_lock = new LocksHashset_OBJ();
	    public void PreferHidden(object requestor){
	        _selfHide_GO.gameObject.SetActive(false);
	        _keepHidden_lock.Lock(requestor); 
	    }
	    public void PreferVIsible(object originalRequestor) {
	        _keepHidden_lock.Unlock(originalRequestor);
	        _selfHide_GO.SetActive( _keepHidden_lock.isLocked() );
	    }


	    public bool _isHovering { get; private set; } = false;

	    public void ReportProgress(float total01){
	        _progressTotal_targ = total01;
	    }

	    //textIsETA_number: appends eta to existing text, or updates previous substring with new ETA.
	    public void ShowStatusText(string text, bool textIsETA_number, float textVisibleDur, bool progressVisibility){
	        _fadeOutText_after = Time.time + textVisibleDur;

	        _progressTotal.gameObject.SetActive(progressVisibility);

	        text = text.Replace("\\", "\\\\");//otherwise TextMeshPro will glitch visually if we give it C:\TheFolder\SomeOther\

	        if (!textIsETA_number){ 
	            _statusText.text = text;
	            return; 
	        }

	        string etaPrefix = "    ETA: ";
	        _statusText.text = _statusText.text.Contains(etaPrefix) ? 
	                              _statusText.text.Split(new[]{ etaPrefix }, StringSplitOptions.None)[0] + etaPrefix + text
	                            : _statusText.text + etaPrefix + text;
	    }


	    public void ShowStickyMsg(string msg, Color col){
	        _stickyMsg_Text.text = msg;
	        _stickyMsg_Text.color = col;
	    }

	    public void StopStickyMsg(string msg){
	        if(_stickyMsg_Text.text != msg){ return; }
	        _stickyMsg_Text.text = "";
	    }


	    void Awake(){
	        if(instance != null){  DestroyImmediate(this.gameObject); return;  }
	        instance = this;
	        _help_button.onClick.AddListener(OnHelpButton);
	        _openWelcomeNovice_button.onClick.AddListener(OnWelcomeNoviceButton);
	        _openCheckForUpdates_button.onClick.AddListener(OnCheckUpdatesButton);
	        _3dGenerators_catalogue_button.onClick.AddListener(OnOpen3DGenCatalogueButton);
	        ShowStatusText("", false, -999, progressVisibility:false);
	    }

    
	    void Start(){
	        _helpTipsPanel.SetActive(false);

	        _statusText_originalPosY = (_statusText_holder.transform as RectTransform).anchoredPosition.y;

	        // Check last animation play time
	        string lastPlayDateString = PlayerPrefs.GetString("LastAnimationPlayDate", "");
	        DateTime lastPlayDate;
	        bool isDateParsed = DateTime.TryParse(lastPlayDateString, out lastPlayDate);
    
	        DateTime currentDate = DateTime.Now;
	        int daysElasped =  (int)Mathf.Abs((float)(currentDate - lastPlayDate).TotalDays);
	        bool shouldPlayAnimation = !isDateParsed || daysElasped >= 7;

	        // Enable animation based on the conditions
	        _help_button.GetComponent<Animation>().enabled = shouldPlayAnimation;

	        //we must use callbacks in case if we get disabled.
	        //Else, panels which we control might remain enabled and semi-visible (and blocking the view).
	        Update_callbacks_MGR.general_UI += OnUpdate;
	    }
    

	    void OnWelcomeNoviceButton(){
	        _helpTipsPanel.SetActive(false);
	        WelcomeScreenNovices_MGR.instance.Show();
	    }

	    void OnCheckUpdatesButton(){
	        _helpTipsPanel.SetActive(false);
	        CheckForUpdates_MGR.instance.ShowPanel( recheckForUpdates:true );
	    }

	    void OnOpen3DGenCatalogueButton(){
	        _3d_gencatalogue.gameObject.SetActive(true);
	    }

	    void OnHelpButton(){
	        _clickedHelpButton_time = Time.time;
	        PlayerPrefs.SetString("LastAnimationPlayDate", DateTime.Now.ToString());
	        _help_button.GetComponent<Animation>().enabled = false;
	        _helpTipsPanel.SetActive(true);
	        _helpTipsPanel_canvGrp.alpha = 0;//will lerp to 1 during Update
	    }


	    void OnUpdate(){
	        LerpTheProgress();
	        RepositionText_and_BG();
	        FadeTheText_andBG();
	        TipsPanel_visibility();
	    }


	    void LerpTheProgress(){
	        float udt = Time.time*8;
	        _progressTotal.transform.localScale = Vector3.Lerp( _progressTotal.transform.localScale,
	                                                             new Vector3(_progressTotal_targ,1,1),  udt);
	    }

    
	    void RepositionText_and_BG(){
	        float stickyPosY  = _stickyMsg_Text.rectTransform.anchoredPosition.y;
	        float verticalPos = _stickyMsg_Text.text!=""?  _stickyMsg_Text.textInfo.lineInfo[0].lineHeight + stickyPosY         
	                                                      : _statusText_originalPosY;
	        _statusText_holder.anchoredPosition = new Vector3(0,verticalPos);

	        //ensure the text-background remains behind it, and stretched to its size:
	        RectTransform bgTransf = _textBackground.rectTransform;
	        float bg_height = _statusText_holder.rect.height + 5;
	              bg_height += _stickyMsg_Text.text!=""? _stickyMsg_Text.textInfo.lineInfo[0].lineHeight  :  0;

	        bgTransf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bg_height);
	        bgTransf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _statusText.rectTransform.rect.width+50);
	    }


	    void FadeTheText_andBG(){
	        float txtFade01 = Mathf.InverseLerp(_fadeOutText_after, _fadeOutText_after + 0.8f, Time.time);
	        txtFade01 = Mathf.Clamp01(txtFade01);
	        _statusText.alpha = 1.0f - txtFade01;

	        _textBackground.enabled = _statusText.text != "";
	        if (!_textBackground.enabled) { return; }
        
	        Color bgCol = _textBackground.color;
	        bgCol.a = 0.2f * _statusText.alpha;
	        _textBackground.color = bgCol;
	    }


	    void TipsPanel_visibility(){
	        if(Time.time == _clickedHelpButton_time){ return; }//skip this frame, let the panel enable self, its colliders.
	        if(_helpTipsPanel.activeSelf == false){ return; }
	        _helpTipsPanel_canvGrp.alpha = Mathf.Lerp(_helpTipsPanel_canvGrp.alpha, 1, Time.deltaTime*7);

	        bool isHoveringViewport = MainViewport_UI.instance.isCursorHoveringMe();
	        if(isHoveringViewport){ _helpTipsPanel.SetActive(false); }
	    }

   
	}
}//end namespace
