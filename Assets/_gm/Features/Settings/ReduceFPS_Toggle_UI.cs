using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Helps control how often we request StableDiffusion to report progress and eta remaining time.
	// Useful when we are generating things.
	public class ReduceFPS_Toggle_UI : MonoBehaviour{

	    [SerializeField] CanvasGroup _myCanvGrp;
	    [SerializeField] TextMeshProUGUI _progressReqFreq_text;
	    [Space(10)]
	    [SerializeField] Toggle _throttleFPS;//user can tick this to reduce fps during generation.
	    [SerializeField] Animation _throttleFPS_anim;
    
	    int _playAnim_rnd = 1;
	    public bool throttleFPS_whenGenerating => _throttleFPS.isOn;


	    void OnStartedGenerate(){
	        gameObject.SetActive(true);
	        StopAllCoroutines();
	        StartCoroutine(FadeCrtn(0.3f, finalVisibility:1.0f));

	        if(!_throttleFPS.isOn  &&  Random.Range(0,_playAnim_rnd)==0){  
	            _throttleFPS_anim.Play();
	            _playAnim_rnd++;//makes it more rare, to be less distracting.
	        }
	    }


	    void OnStoppedGenerate(bool cancelled){
	        gameObject.SetActive(true);
	        StopAllCoroutines();
	        StartCoroutine(FadeCrtn(0.3f, finalVisibility:0.0f));
	    }
    

	    IEnumerator FadeCrtn(float dur, float finalVisibility){
	        _myCanvGrp.alpha = 1-finalVisibility;
	        float startTime = Time.unscaledTime;
	        float fromAlpha = _myCanvGrp.alpha;
	        while(true){
	            float elapsed01 =  (Time.unscaledTime - startTime) / dur;
	                  elapsed01 = Mathf.Clamp01(elapsed01);
	            _myCanvGrp.alpha = Mathf.Lerp(fromAlpha, finalVisibility, elapsed01);
	            if(elapsed01 == 1.0f){ break; }
	            yield return null;
	        }
	        if (finalVisibility == 0){ gameObject.SetActive(false); }
	    }


	    void Awake(){
	        EventsBinder.Bind_Clickable_to_event( nameof(ReduceFPS_Toggle_UI), this );
	    }

	    void Start(){
	        gameObject.SetActive(false);
	        GenerateButtons_UI._Act_OnGenerate_started += OnStartedGenerate;
	        GenerateButtons_UI._Act_OnGenerate_finished += OnStoppedGenerate;
	    }
	}
}//end namespace
