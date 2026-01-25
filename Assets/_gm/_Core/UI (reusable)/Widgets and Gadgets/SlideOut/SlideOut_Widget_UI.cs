using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;


namespace spz {

	public class SlideOut_Widget_UI : MonoBehaviour{
	    [SerializeField] bool _startHidden = true;
	    [SerializeField] bool _isHoriz = true;
	    [SerializeField] bool _isPositiveDir = true;
	    [Space(10)]
	    [SerializeField] float _slideDur = 0.2f;
	    [SerializeField] GameObject _turnOnOff;
	    [SerializeField] RectTransform _slideMe;
	    [Space(10)]
	    [SerializeField] RectTransform _hoverSensorSurface_optional; //when mouse leaves this area, we hide self. It's on our tiny canvas.
	    [SerializeField] List<RectTransform> _externalHoverSurfaces_optional; //if mouse hovers those, we keep showing self (even if its from other canvas)
	    [SerializeField] bool _dontHide_ifLeftMousePressed;//helps prevent hiding panel if we are still dragging our sliders.
	    [Space(10)]
	    [SerializeField] bool _flip_ifPartially_out_of_screen = false;

	    public bool isShowing => _state==State.Showing || _state==State.Shown;

	    //if you set this to true, panel will ignore hover/not hover checks (and won't auto-hide) until you set it to false.
	    public bool _dontAutoHide { get; set; } = false;

	    enum State { Showing, Shown, Hiding, Hidden, }
	    State _state;

	    public bool isPositiveDir => _isPositiveDir;
	    public bool isHoriz => _isHoriz;


	    public void Toggle_if_Different(bool isShow, float slideDuration=-1){
	        if(gameObject.activeInHierarchy == false){ return; }
	        if(isShow == isShowing){ return; }
	        if(KeyMousePenInput.isLMBpressed() || KeyMousePenInput.isRMBpressed()){ return; }//might be dragging something/navigating.

	        _state = isShow? State.Showing : State.Hiding;

	        if(_hoverSensorSurface_optional!=null){ _hoverSensorSurface_optional.gameObject.SetActive(isShow); }
	        _turnOnOff.SetActive(true);
        
	        slideDuration =  slideDuration<0? _slideDur : slideDuration;
	        StopAllCoroutines();
	        StartCoroutine( Slide_crtn(isShow, slideDuration) );
	    }


	    // For example if UI was re-arranged, and the panel should open
	    // in an opposite direction from now on.
	    public void Flip_if_possible(){
	        var flipper = GetComponent<SlideOut_WidgetFlipper_UI>();
	        if(flipper == null){ return; }
	        flipper.Flip();

	        if(_flip_ifPartially_out_of_screen) { 
	            Flip_if_OutsideScreen(flipper);
	        }
	    }


	    // Check if any corner is outside screen space. If yes, Flips itself to avoid being outside.
	    void Flip_if_OutsideScreen(SlideOut_WidgetFlipper_UI flipper){
	        if(flipper == null){ return; }
	        RectTransform rect = transform as RectTransform;
	        Vector3[] corners = new Vector3[4];
	        rect.GetWorldCorners(corners);

	        bool outside = corners.Any(corner => {
	            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, corner);
	            return screenPoint.x < 0 || screenPoint.x > Screen.width ||
	                    screenPoint.y < 0 || screenPoint.y > Screen.height;
	        });
	        // Check if any corner is outside screen space
	        if (outside){
	            flipper.Flip(); // Flip again (undoing) if outside the screen
	        }
	    }

	    public bool isFlipped(){
	        var flipper = GetComponent<SlideOut_WidgetFlipper_UI>();
	        if(flipper == null){ return false; }
	        return flipper.isFlipped;
	    }


	    IEnumerator Slide_crtn(bool isShow, float slideDur){
	        float dimensionSize = _isHoriz?_slideMe.rect.width : _slideMe.rect.height;
	        float startOffset = isShow? -dimensionSize : 0;
	        float endOffset = isShow? 0 : -dimensionSize;

	        startOffset *= _isPositiveDir? 1.0f : -1;
	        endOffset   *= _isPositiveDir? 1.0f : -1;

	        Vector3 startPos = _isHoriz? new Vector3(startOffset, 0, 0) : new Vector3(0,startOffset,0);
	        Vector3 endPos   = _isHoriz? new Vector3(endOffset, 0, 0) : new Vector3(0,endOffset,0);

	        float startTime = Time.time;
	        while (true){
	            float elapsed01 = (Time.time-startTime)/slideDur;
	                  elapsed01 = Mathf.Clamp01(elapsed01);
	                  elapsed01 = slideDur==0? 1 : elapsed01;

	            _slideMe.transform.localPosition =  Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0,1,elapsed01) );

	            if(elapsed01 == 1.0f){ break; }
	            yield return null;
	        }
	        _turnOnOff.SetActive(isShow);
	        _state =  isShow? State.Shown : State.Hidden;
	    }
    


	    int _numUpdatesRan=0;
	    void Update(){
	        if(_numUpdatesRan == 1 && _flip_ifPartially_out_of_screen){ 
	            //doing it in Update, not in Start. (layout isn't reliable during the first frame)
	            Flip_if_OutsideScreen( GetComponent<SlideOut_WidgetFlipper_UI>() ); 
	        }
	        HideIfNotHovering();
	        _numUpdatesRan++;
	    }


	    void HideIfNotHovering(){
	        bool myContains = true;
	        bool otherContains = false;
	        Vector2 cursorPos = KeyMousePenInput.cursorScreenPos();

	        if(_dontHide_ifLeftMousePressed && KeyMousePenInput.isLMBpressed()){ return; }//might be still dragging our sliders.

	        if(_hoverSensorSurface_optional != null){
	            myContains =  RectTransformUtility.RectangleContainsScreenPoint(_hoverSensorSurface_optional, cursorPos);
	        }
	        if(_externalHoverSurfaces_optional.Count>0){
	            foreach(var surf in _externalHoverSurfaces_optional){ 
	                otherContains =  RectTransformUtility.RectangleContainsScreenPoint(surf, cursorPos);
	                if(otherContains){ break; }
	            }
	        }

	        if(myContains || otherContains){ return; }
	        if(_state==State.Hiding || _state==State.Hidden){ return; }
	        if(_dontAutoHide){ return; }
	        Toggle_if_Different(false);
	    }


	    void Start(){
	        Toggle_if_Different( isShow: _startHidden==false,  slideDuration:0 );
	    }

	}
}//end namespace
