using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	//cans mothely move the scroll rect to the end, for example, if you apeended new UI icons under it.
	public class ScrollRect_AutoScroll : MonoBehaviour{

	    [SerializeField] ScrollRect _scrollRect;
	    Coroutine _scrollCrtn = null;

	    [SerializeField] float _startFromPos01 = 0;//for very long litsts, maybe start from 0.7 etc.

	    public void ScrollToEnd(float duration, bool isScrollDown){

	        if (!gameObject.activeInHierarchy){//can't launch coroutine.
	            _scrollRect.verticalNormalizedPosition =  isScrollDown ? 0.0001f : 0.9999f;
	            return;
	        }

	        //start from the opposite side:
	        _scrollRect.verticalNormalizedPosition =  isScrollDown? 0.999f : 0.001f;

	        if(_scrollCrtn != null){ StopCoroutine(_scrollCrtn); }
	        _scrollCrtn = StartCoroutine( ScrollToBottom_crtn(duration, isScrollDown) );
	    }

	    IEnumerator ScrollToBottom_crtn(float duration, bool isScrollDown){
	        //perturb a little, and skip frame, then record starting value.
	        //Without this, scroll rect refuses to change verticalNormalizedPosition after it becomes zero.
	        _scrollRect.verticalNormalizedPosition = _startFromPos01;
	        yield return null;
	        _scrollRect.verticalNormalizedPosition -= 0.0001f;
	        yield return null;

	        float startValue = _scrollRect.verticalNormalizedPosition;

	        float endVal = isScrollDown ? 0.0001f : 0.9999f;

	        float startTime = Time.time;
	        while (true){
	            float elapsed01 = Mathf.InverseLerp(startTime, startTime+duration, Time.time );
	                  elapsed01 = Mathf.Clamp01(elapsed01);
	            _scrollRect.verticalNormalizedPosition = Mathf.SmoothStep(startValue, endVal, elapsed01);
	            if(elapsed01 == 1) { break; }
	            yield return null;
	        }
	        _scrollRect.verticalNormalizedPosition = endVal;
	        _scrollCrtn = null;
	    }
	}
}//end namespace
