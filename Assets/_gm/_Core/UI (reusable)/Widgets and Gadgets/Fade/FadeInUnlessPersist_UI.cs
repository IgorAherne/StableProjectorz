using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class FadeInUnlessPersist_UI : MonoBehaviour
	{
	    [SerializeField] Image _img;
	    [SerializeField] CanvasGroup _canvasGrp;
	    [SerializeField] float _FadeInSpeed = 0.5f;
	    [SerializeField] float _fadeOutSpeed = 2f;
	    [Space(10)]
	    [SerializeField] float _minAlpha = 0;
	    [SerializeField] float _maxAlpha = 1;
	    bool _fadeOutThisFrame = false;
	    bool _keepFadingOut = false;

	    public void FadeOutThisFrame( bool forceMinAlphaNow=false,  bool forceMaxAlphaNow=false ){
	        gameObject.SetActive(true);
	        _fadeOutThisFrame = true;
	        if(forceMinAlphaNow){ ForceAlpha(_minAlpha); }
	        if(forceMaxAlphaNow){ ForceAlpha(_maxAlpha); }
	    }
    

	    public void KeepFadingOut(bool allow, bool forceMinAlphaNow=false, bool forceMaxAlphaNow=false){
	        this.enabled = true;//so that at least one LateUpdate is ran.
	        _keepFadingOut = allow;
	        if(forceMinAlphaNow){ ForceAlpha(_minAlpha); }
	        if(forceMaxAlphaNow){ ForceAlpha(_maxAlpha); }
	    }


	    //will be invoked until alpha becomes zero.
	    void LateUpdate(){
	        if (_fadeOutThisFrame || _keepFadingOut){
	            FadeOutImage(true);
	            FadeOutCanvasGrp(true);
	            return;
	        }
	        FadeOutImage(false);
	        FadeOutCanvasGrp(false);
	    }//end()


	    void FadeOutImage(bool isNegative){
	        if(_img == null){ return; }
	        Color col = _img.color;

	        if (isNegative){
	            _fadeOutThisFrame = false;
	            col.a -= Time.deltaTime*_fadeOutSpeed;
	            col.a = Mathf.Max(col.a, _minAlpha);//so it doesn't go above starting value.
	            _img.color = col;
	            return;
	        }
	        col.a += Time.deltaTime*_FadeInSpeed;
	        col.a  = Mathf.Min(col.a, _maxAlpha);
	        _img.color = col;
	        //so LateUpdate() will temporarily stop being invoked:
	        if(col.a==_maxAlpha){  this.enabled=false;  }
	    }


	    void FadeOutCanvasGrp(bool isNegative){
	        if(_canvasGrp == null){ return; }
	        float alpha = _canvasGrp.alpha;

	        if (isNegative){
	            _fadeOutThisFrame = false;
	            alpha -= Time.deltaTime*_fadeOutSpeed;
	            alpha = Mathf.Max(alpha, _minAlpha);//so it doesn't go above starting value.
	            _canvasGrp.alpha = alpha;
	            return;
	        }
	        alpha += Time.deltaTime*_FadeInSpeed;
	        alpha  = Mathf.Min(alpha, _maxAlpha);
	        _canvasGrp.alpha = alpha;
	        //so LateUpdate() will temporarily stop being invoked:
	        if(_canvasGrp.alpha==_maxAlpha){  this.enabled=false;  }
	    }

	    void ForceAlpha(float alpha){
	        if (_img != null){
	            Color col = _img.color;
	            col.a = alpha;
	            _img.color = col;
	        }
	        if (_canvasGrp != null){
	            _canvasGrp.alpha = alpha;
	        }
	    }

	}
}//end namespace
