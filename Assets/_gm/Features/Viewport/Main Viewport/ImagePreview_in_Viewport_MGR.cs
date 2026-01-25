using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Able to briefly show a 2D image inside the viewport.
	// Icons usually use it, so we can inspect the texture contents easier.
	// Fades out (and eventualy disables) as soon as we stop asking it to ShowImage() every frame.
	public class ImagePreview_in_Viewport_MGR : MonoBehaviour{
	    public static ImagePreview_in_Viewport_MGR instance { get; private set; }

	    [SerializeField] RawImage _rawImg;
	    [SerializeField] CanvasGroup _canvGroup;
	    [SerializeField] float _FadeSpeed = 5;
	    [SerializeField] AspectRatioFitter _aspectFitter;

	    float _showImage_requestTime;

	    public void ShowImage(Texture tex){
	        _showImage_requestTime = Time.time;
	        _canvGroup.gameObject.SetActive(true);
	        tex = tex ?? Texture2D.linearGrayTexture;
	        _rawImg.texture = tex;
	        _rawImg.color = Color.white;
	        _aspectFitter.aspectRatio = tex.width/(float)tex.height;
	    }

	    public void LateUpdate(){
	        float fadeDir =  _showImage_requestTime == Time.time? _FadeSpeed : -_FadeSpeed;
        
	        if(fadeDir > 0){ _canvGroup.gameObject.SetActive(true); }
        
	        _canvGroup.alpha += Time.deltaTime * fadeDir;
	        _canvGroup.alpha  = Mathf.Clamp01(_canvGroup.alpha);
        
	        if(_canvGroup.alpha == 0){
	            _canvGroup.gameObject.SetActive(false);
	        }
	    }

	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;

	        _canvGroup.alpha = 0; //begin hidden.
	    }
	}
}//end namespace
