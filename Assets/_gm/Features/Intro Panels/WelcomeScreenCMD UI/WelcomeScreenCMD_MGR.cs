using System.Collections;
using UnityEngine.UI;
using UnityEngine;

namespace spz {

	//shown to the User when the program starts, unless "don't show on startup" is ticked.
	public class WelcomeScreenCMD_MGR : MonoBehaviour{

	    [SerializeField] Canvas _canvas;
	    [SerializeField] RectTransform _popupTransform;
	    [SerializeField] ButtonToggle_UI _dontShowOnStartup;
	    [SerializeField] Button _ok_Button;
	    [SerializeField] Animator _anim;
	    [SerializeField] NonDrawingGraphic _blockAnyClicks_surface;
	    [Space(10)]
	    [SerializeField] Button _howToOpen_button;
	    [SerializeField] CanvasGroup _howToOpen_canvGroup;
	    [SerializeField] Button _howToOpen_backSurface;

	    Coroutine _onHowToOpen_crtn = null;

	    public static bool _isShowing { get; private set; } = false;


	    void Show(float delay=0){
	        StartCoroutine( Show_crtn(delay) );
	    }


	    IEnumerator Show_crtn(float delay=0){
	        _isShowing = true;

	        if (delay>0){  yield return new WaitForSeconds(delay);  }
        
	        _canvas.gameObject.SetActive(true);//entire canvas, which holds panel too.
	        _anim.SetTrigger("show");

	        _blockAnyClicks_surface.gameObject.SetActive(true);//to prevent accidental click-away.
	        yield return new WaitForSeconds(0.6f);
	        _blockAnyClicks_surface.gameObject.SetActive(false);
	    }


	    IEnumerator ShrinkSelf_AndDisable(float dur=0.8f){
	        while (true){//keep waiting until animation enters the Hide state.
	            var stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
	            if(stateInfo.IsName("Hide")){ break; }
	            yield return null; 
	        }

	        float startTime = Time.unscaledTime;
	        while(true){
	            float elapsed = Time.unscaledTime - startTime;
            
	            var stateInfo  = _anim.GetCurrentAnimatorStateInfo(0);
	            if( stateInfo.normalizedTime>0.999f || elapsed>3){ break; }
	            yield return null;
	        }
	        _canvas.gameObject.SetActive(false);//entire canvas, which holds panel too
	        _isShowing = false;
	    }//end()


    
	    void OnDontShowOnStartupToggle(bool isTicked){
	        string prefsKey =  "cmdDontShowOnStartup" + CheckForUpdates_MGR.CURRENT_VERSION_HERE;
	        PlayerPrefs.SetInt(prefsKey, isTicked?1:0);
	    }


	    void Awake(){
	        string prefsKey =  "cmdDontShowOnStartup" + CheckForUpdates_MGR.CURRENT_VERSION_HERE;
	        int isDontShowPressed = PlayerPrefs.GetInt(prefsKey, defaultValue:0);
	        _dontShowOnStartup.SetValueWithoutNotify( isDontShowPressed==1 );
	        _dontShowOnStartup.onClick += OnDontShowOnStartupToggle;

	        _canvas.gameObject.SetActive(false);

	        bool hidden = DisablePanel_ifDontShowOnStartup();
	        if (!hidden){ Show(delay:1.5f); }

	        _ok_Button.onClick.AddListener( OnConfirmButton );
	        _howToOpen_button.onClick.AddListener( OnHowToOpenButton );
	        _howToOpen_backSurface.onClick.AddListener( OnHowToOpen_BackSurfaceClicked );
	    }

    
	    bool DisablePanel_ifDontShowOnStartup(){
	        string prefsKey =  "cmdDontShowOnStartup" + CheckForUpdates_MGR.CURRENT_VERSION_HERE;
	        int isDontShow = PlayerPrefs.GetInt(prefsKey, defaultValue:0);
	        if(isDontShow>0){
	            _canvas.gameObject.SetActive(false);//entire canvas, not just panel
	            return true;
	        }
	        return false;
	    }


	    void OnConfirmButton(){
	        _anim.SetTrigger("hide");
	        StartCoroutine(ShrinkSelf_AndDisable());
	        WelcomeScreenNovices_MGR.instance.Show(delay:0);
	    }

    
	    //if user clicked "CMD didn't open" button - show them panel with instructions.
	    void OnHowToOpenButton(){
	        _howToOpen_canvGroup.gameObject.SetActive(true);
	        _howToOpen_canvGroup.alpha = 0;
	        if(_onHowToOpen_crtn != null){ StopCoroutine(_onHowToOpen_crtn); }
	        _onHowToOpen_crtn = StartCoroutine( OnHowToOpen_fade_crtn(finalAlpha:1) );
	    }

	    void OnHowToOpen_BackSurfaceClicked(){
	        if(_onHowToOpen_crtn != null){ StopCoroutine(_onHowToOpen_crtn); }
	        _onHowToOpen_crtn = StartCoroutine( OnHowToOpen_fade_crtn(finalAlpha:0) );
	    }


	    IEnumerator OnHowToOpen_fade_crtn(float finalAlpha, float dur=0.2f){
	        float startAlpha = _howToOpen_canvGroup.alpha;
	        float startTime = Time.time;
	        while (true){
	            float elapsed01 = (Time.time - startTime) / dur;
	            elapsed01 = Mathf.Clamp01(elapsed01);
	            _howToOpen_canvGroup.alpha = Mathf.Lerp(startAlpha, finalAlpha, elapsed01);
	            if(elapsed01 == 1.0f){ break; }
	            yield return null;
	        }
	        if(finalAlpha == 0){ _howToOpen_canvGroup.gameObject.SetActive(false); }
	        _onHowToOpen_crtn = null;
	    }



	}
}//end namespace
