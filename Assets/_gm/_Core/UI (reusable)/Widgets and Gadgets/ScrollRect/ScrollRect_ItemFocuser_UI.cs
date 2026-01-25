using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace spz {

	public class ScrollRect_ItemFocuser_UI : MonoBehaviour{
	    public ScrollRect _scrollRect;
	    public float _smoothTime = 0.3f;
		Coroutine _focusAtPointCrtn = null;

		// Doing it from here (instead from ScrolLViewFocusFunctions) allows to launch from 'Coroutines_MGR.instance'.
		// This allows lerping to happen even while the scroll rect is inactive.
		public void FocusOnItemLocalPos(RectTransform childRectTransform, float dur=0.3f, float land_at_pcntInViewport=0.7f){
			Vector2 pos = _scrollRect.CalculateFocusedScrollPosition(childRectTransform, land_at_pcntInViewport);
	        if(_focusAtPointCrtn != null){ Coroutines_MGR.instance.StopCoroutine(_focusAtPointCrtn); }
			_focusAtPointCrtn = Coroutines_MGR.instance.StartCoroutine( _scrollRect.LerpToScrollPositionCoroutine(pos,dur) );
	    }
	}



	//from https://gist.github.com/yasirkula/75ca350fb83ddcc1558d33a8ecf1483f (Feb 2024)
	//MIT license 
	public static class ScrollViewFocusFunctions{
		public static void FocusAtPoint( this ScrollRect scrollView, Vector2 focusPoint, float land_at_pcntInViewport=0.7f ){
			scrollView.normalizedPosition = scrollView.CalculateFocusedScrollPosition( focusPoint, land_at_pcntInViewport );
		}

		public static void FocusOnItem( this ScrollRect scrollView, RectTransform item, float land_at_pcntInViewport=0.7f ){
			scrollView.normalizedPosition = scrollView.CalculateFocusedScrollPosition( item, land_at_pcntInViewport );
		}


		public static Vector2 CalculateFocusedScrollPosition( this ScrollRect scrollView, RectTransform item, float land_at_pcntInViewport=0.7f ){
			Vector2 itemCenterPoint = scrollView.content.InverseTransformPoint( item.transform.TransformPoint( item.rect.center ) );

			Vector2 contentSizeOffset = scrollView.content.rect.size;
			contentSizeOffset.Scale( scrollView.content.pivot );

			return scrollView.CalculateFocusedScrollPosition( itemCenterPoint + contentSizeOffset, 
															  land_at_pcntInViewport );
		}


		public static Vector2 CalculateFocusedScrollPosition( this ScrollRect scrollView, Vector2 focusPoint, float land_at_pcntInViewport ){
			Vector2 contentSize = scrollView.content.rect.size;
			Vector2 viewportSize = ( (RectTransform) scrollView.content.parent ).rect.size;
			Vector2 contentScale = scrollView.content.localScale;

			contentSize.Scale( contentScale );
			focusPoint.Scale( contentScale );

			float vprt = land_at_pcntInViewport;

			Vector2 scrollPosition = scrollView.normalizedPosition;
			if( scrollView.horizontal && contentSize.x > viewportSize.x )
				scrollPosition.x = Mathf.Clamp01( ( focusPoint.x - viewportSize.x * vprt) / ( contentSize.x - viewportSize.x ) );
			if( scrollView.vertical && contentSize.y > viewportSize.y )
				scrollPosition.y = Mathf.Clamp01( ( focusPoint.y - viewportSize.y * vprt) / ( contentSize.y - viewportSize.y ) );

			return scrollPosition;
		}

		public static IEnumerator LerpToScrollPositionCoroutine( this ScrollRect scrollView, Vector2 targetNormalizedPos, float dur ){
			Vector2 initialNormalizedPos = scrollView.normalizedPosition;
			float startTime = Time.time;
			while( true ){
				float elapsed01 = (Time.time - startTime)/dur;
				elapsed01= Mathf.Clamp01(elapsed01);
			
				float t = Mathf.SmoothStep(0, 1, elapsed01);
				scrollView.normalizedPosition = Vector2.LerpUnclamped( initialNormalizedPos, targetNormalizedPos, 1f - (1f-t)*(1f-t) );

	            if(elapsed01 == 1.0f){ break; }
				yield return null;
			}
			scrollView.normalizedPosition = targetNormalizedPos;
		}
	}
}//end namespace
