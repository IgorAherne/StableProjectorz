using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace spz {

	//shown to the User when the program starts, unless "don't show on startup" is ticked.
	public class WelcomeScreenNovices_MGR : MonoBehaviour{
	    public static WelcomeScreenNovices_MGR instance { get; private set; } = null;

	    [SerializeField] Canvas _canvas;
	    [SerializeField] RectTransform _popupTransform;
	    [SerializeField] Button _joinDiscordButton;
	    [SerializeField] ButtonToggle_UI _dontShowOnStartup;
	    [SerializeField] Button _bg_surface;//outside this panel (behind it). When clicked, we hide this paenl.
	    [SerializeField] Animator _anim;

	    [SerializeField] NonDrawingGraphic _blockAnyClicks_surface;
	    [SerializeField] ScrollRect_AutoScroll _scrollRect_autoscroll;
	    [Space(10)]
	    [SerializeField] List<Button> _videoTutorialButtons;

	    public bool _isShowing { get; private set; } = false;


	    public void Show(float delay=0){
	        _isShowing = true;
	        StartCoroutine( Show_crtn(delay) );
	    }


	    //little button (outside this screen), which opens the help-panel.
	    //will shrink the panel into here when hidden.
	    RectTransform get_helpButton()
	        => EventsBinder.FindComponent<HelpButton_MainViewport>("HelpButton_MainViewport")
	                        .transform as RectTransform;

	    void OnBackgroundClicked(){
	        _isShowing = false;
	        _anim.SetTrigger("hide");
	        StartCoroutine(ShrinkSelf_AndDisable() );
	    }


	    IEnumerator Show_crtn(float delay=0){
	        if(delay>0){  yield return new WaitForSeconds(delay);  }
        
	        _canvas.gameObject.SetActive(true);//entire canvas, which holds panel too.
	        _popupTransform.localPosition = Vector3.zero;
	        _popupTransform.localScale = Vector3.one;
	        _anim.SetTrigger("show");
	        _scrollRect_autoscroll.GetComponent<ScrollRect>().verticalNormalizedPosition = 0.001f;
	        _scrollRect_autoscroll.ScrollToEnd(1.2f, isScrollDown:false);

	        _blockAnyClicks_surface.gameObject.SetActive(true);//to prevent accidental click-away.
	        yield return new WaitForSeconds(0.6f);
	        _blockAnyClicks_surface.gameObject.SetActive(false);
	    }


	    IEnumerator ShrinkSelf_AndDisable(float dur=0.8f){
	        _blockAnyClicks_surface.gameObject.SetActive(true);

	        while (true){//keep waiting until animation enters the Hide state.
	            var stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
	            if(stateInfo.IsName("Hide")){ break; }
	            yield return null; 
	        }
	        Vector3 startPos = _popupTransform.position;
	        Vector3 endPos   = get_helpButton()?.transform.position??  _popupTransform.position;

	        float startTime = Time.unscaledTime;
	        while(true){
	            float elapsed = Time.unscaledTime - startTime;
            
	            var stateInfo  = _anim.GetCurrentAnimatorStateInfo(0);

	            _popupTransform.position = Vector3.Lerp(startPos, endPos, stateInfo.normalizedTime);

	            if( stateInfo.normalizedTime>0.999f || elapsed>3){ break; }
	            yield return null;
	        }
	        _blockAnyClicks_surface.gameObject.SetActive(false);
	        _canvas.gameObject.SetActive(false);//entire canvas, which holds panel too
	    }//end()


    
	    void OnDontShowOnStartupToggle(bool isTicked){
	        string prefsKey = "WelcDontShowOnStartup" + CheckForUpdates_MGR.CURRENT_VERSION_HERE;
	        PlayerPrefs.SetInt(prefsKey, isTicked?1:0);
	    }


	    void OnJoinDiscordButton(){
	        Application.OpenURL("https://discord.gg/aWbnX2qan2");
	    }


	    void Awake(){
	        if (instance != null){  DestroyImmediate(this.gameObject); return; }
	        instance = this;

	        string prefsKey = "WelcDontShowOnStartup" + CheckForUpdates_MGR.CURRENT_VERSION_HERE;
	        int isDontShowPressed = PlayerPrefs.GetInt(prefsKey, defaultValue:0);
	        _dontShowOnStartup.SetValueWithoutNotify( isDontShowPressed==1 );
	        _dontShowOnStartup.onClick += OnDontShowOnStartupToggle;
	        _bg_surface.onClick.AddListener(OnBackgroundClicked);

	        _joinDiscordButton.onClick.AddListener( OnJoinDiscordButton );

	        _videoTutorialButtons.ForEach( but=>but.onClick.AddListener(OnTutorialButton) );
	        _canvas.gameObject.SetActive(false);

	        #if UNITY_EDITOR
	        return; //keeps bothering me during editing
	        #endif

	        bool hidden = DisablePanel_ifDontShowOnStartup();
	        if (!hidden){ Show(delay:2.5f); }
	    }


	    void OnTutorialButton(){
	        Application.OpenURL("https://stableprojectorz.com/lessons-and-videos/");
	    }


	    bool DisablePanel_ifDontShowOnStartup(){
	        string prefsKey = "WelcDontShowOnStartup" + CheckForUpdates_MGR.CURRENT_VERSION_HERE;
	        int isDontShow = PlayerPrefs.GetInt(prefsKey, defaultValue:0);
	        if(isDontShow>0){
	            _canvas.gameObject.SetActive(false);//entire canvas, not just panel
	            return true;
	        }
	        return false;
	    }

	}
}//end namespace
