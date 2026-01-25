using UnityEngine;
using System.Collections;

namespace spz {

	[RequireComponent(typeof(CanvasGroup))]
	public class FadeInOut_UI : MonoBehaviour
	{
	    [SerializeField] float _fadeDuration = 0.2f;
	    [SerializeField] CanvasGroup _canvasGroup;
	    [Space(10)]
	    [SerializeField] bool _disableGO_if_Hidden = true;
	    [SerializeField] bool _startEnabled_GO = true;

	    bool _faded_atLeastOnce = false; 

	    Coroutine _fadeCoroutine;
	    float _targetAlpha;


	    public void FadeIn(){
	        if(_targetAlpha > 0){ return; }
	        Fade(1);
	    }

	    public void FadeOut(){
	        if(_targetAlpha == 0){ return; }
	        Fade(0);
	    } 

	    void Fade(float targetAlpha){
	        _faded_atLeastOnce = true;
	        gameObject.SetActive(true);

	        if (_fadeCoroutine != null){ StopCoroutine(_fadeCoroutine); }
	        _targetAlpha = targetAlpha;

	        if (!gameObject.activeInHierarchy){
	            SetFinalState(targetAlpha);
	            return;
	        }
	        _fadeCoroutine = StartCoroutine(FadeCoroutine());
	    }


	    IEnumerator FadeCoroutine(){
	        float startAlpha = _canvasGroup.alpha;

	        float startTime = Time.time;
	        while (true){
	            float elapsed01 = (Time.time - startTime)/_fadeDuration;
	                  elapsed01 = Mathf.Clamp01(elapsed01);
	            if(elapsed01 == 1){ break; }

	            _canvasGroup.alpha = Mathf.Lerp(startAlpha, _targetAlpha, elapsed01);
	            yield return null;
	        }
	        SetFinalState(_targetAlpha);
	    }


	    void SetFinalState(float alpha){
	        _canvasGroup.alpha = alpha;
	        _canvasGroup.interactable = alpha > 0f;
	        _fadeCoroutine = null;
	        if(_disableGO_if_Hidden){ gameObject.SetActive(alpha>0); }
	    }


	    void OnDisable(){
	        if(_fadeCoroutine == null){ return;}
	        StopCoroutine(_fadeCoroutine);
	        SetFinalState(_targetAlpha);
	    }

	    void Awake(){
	        if(!_faded_atLeastOnce){//checks if desired state is already different.
	            gameObject.SetActive(_startEnabled_GO); 
	        }
	    }
	}
}//end namespace
