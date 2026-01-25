using UnityEngine;
using UnityEngine.UI;


namespace spz {

	public class OptimizedAspectRatioFitter : UI_with_OptimizedUpdates{

	    [SerializeField]
	    AspectRatioFitter.AspectMode _aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
    
	    [SerializeField]
	    float _aspectRatio = 1.0f;
	    public float aspectRatio => _aspectRatio;

	    [SerializeField] RectTransform _rt;

    
	    public void ChangeAspect(float newAspect){
	        _aspectRatio = newAspect;
	        base.ManuallyUpdate();
	    }


	    protected override void OnUpdate(){
	        #if UNITY_EDITOR
	        if(_rt == null){  return;  }
	        #endif
	        if(_aspectMode == AspectRatioFitter.AspectMode.WidthControlsHeight){
	            _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _rt.rect.width/_aspectRatio);
	            return;
	        }
	        _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _rt.rect.height*_aspectRatio);
	    }


	#if UNITY_EDITOR
	    protected override void Reset(){//allows for an easy drag-and-drop swap in editor.
	        AspectRatioFitter asp = GetComponent<AspectRatioFitter>();
	        if(asp==null){return;}
	        _aspectMode = asp.aspectMode;
	        _aspectRatio = asp.aspectRatio;

	        _rt = transform as RectTransform;

	        DestroyImmediate(asp);
	    }
	#endif
	}
}//end namespace
