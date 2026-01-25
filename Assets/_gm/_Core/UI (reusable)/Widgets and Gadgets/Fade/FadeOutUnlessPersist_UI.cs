using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Will try to decrease the opacity unless you keep bumping it up every frame.
	// If opacity reaches zero, game object is deactivated
	public class FadeOutUnlessPersist_UI : MonoBehaviour
	{
	    [Header("Either Image or CanvasGroup:")]
	    [SerializeField] Image _img;
	    [SerializeField] CanvasGroup _canvasGrp;
	    [SerializeField] float _fadeOutSpeed = 0.5f;
	    [SerializeField] float _fadeInSpeed = 2f;
	    [SerializeField] bool _disableGO_whenAlphZero = true;
	    [Space(10)]
	    [SerializeField] bool _begin_invisible = false;
	    [Space(10)]
	    [SerializeField] float _minAlpha = 0;
	    [SerializeField] float _maxAlpha = 1;
	    bool _overrideMin_0 = false; //until true, smallest allowed value is temporarily 0.

	    bool _fadeInThisFrame = false;
	    bool _keepFadingIn = false;


	    public void FadeInThisFrame(bool forceMinAlphaNow=false,  bool forceMaxAlphaNow=false){
	        this.enabled = true;
	        if (_disableGO_whenAlphZero){//only enable if we were actually allowed to disable. Else someone else might want to keep GO disabled.
	            gameObject.SetActive(true);
	        }
	        _fadeInThisFrame = true;
	        if(forceMinAlphaNow){ ForceAlpha(_minAlpha); }
	        if(forceMaxAlphaNow){ ForceAlpha(_maxAlpha); }
	    }
    

	    public void KeepFadingIn(bool allow, bool forceMinAlphaNow=false,  bool forceMaxAlphaNow=false, bool forceMin0=false){
	        this.enabled = true;

	        if (_disableGO_whenAlphZero){//only enable if we were actually allowed to disable. Else someone else might want to keep GO disabled.
	            gameObject.SetActive(true);//so that at least one LateUpdate is ran.
	        }
	        _keepFadingIn = allow;
	        _overrideMin_0 = forceMin0;
	        if(forceMinAlphaNow){ ForceAlpha(_overrideMin_0?0:_minAlpha); }
	        if(forceMaxAlphaNow){ ForceAlpha(_maxAlpha ); }
	    }


	    //will be invoked until alpha becomes zero.
	    void LateUpdate(){
	        if (_fadeInThisFrame || _keepFadingIn){
	            FadeInImage(true);
	            FadeInCanvasGrp(true);
	            return;
	        }
	        FadeInImage(false);
	        FadeInCanvasGrp(false);
	    }//end()


	    void FadeInImage(bool isPositive){
	        if(_img == null){ return; }
	        Color col = _img.color;

	        if (isPositive){
	            _fadeInThisFrame = false;
	            col.a += Time.deltaTime*_fadeInSpeed;
	            col.a = Mathf.Min(col.a,_maxAlpha);
	            _img.color = col;
	            return;
	        }
	        float min = _overrideMin_0?0:_minAlpha;
	        col.a -= Time.deltaTime*_fadeOutSpeed;
	        col.a = Mathf.Max(col.a, min);
	        _img.color = col;
	        //so LateUpdate() will temporarily stop being invoked:
	        if(col.a==min){  
	            if(_disableGO_whenAlphZero){  gameObject.SetActive(false);  }
	            else{ this.enabled=false; }
	        }
	    }


	    void FadeInCanvasGrp(bool isPositive){
	        if(_canvasGrp == null){ return; }
	        float alpha = _canvasGrp.alpha;

	        if (isPositive){
	            _fadeInThisFrame = false;
	            alpha += Time.deltaTime*_fadeInSpeed;
	            alpha = Mathf.Min(alpha, _maxAlpha);
	            _canvasGrp.alpha = alpha;
	            return;
	        }
	        float min = _overrideMin_0?0:_minAlpha;
	        alpha -= Time.deltaTime*_fadeOutSpeed;
	        alpha = Mathf.Max(alpha, min);
	        _canvasGrp.alpha = alpha;
	        //so LateUpdate() will temporarily stop being invoked:
	        if(_canvasGrp.alpha==min){ 
	            if(_disableGO_whenAlphZero){  gameObject.SetActive(false);  }
	            else{ this.enabled=false; }
	        }
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

	    void Start(){
	        if (_begin_invisible){
	            ForceAlpha(0);
	            LateUpdate();
	        }
	    }
	}
}//end namespace
