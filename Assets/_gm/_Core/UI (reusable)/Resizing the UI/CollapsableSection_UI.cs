using EnhancedScrollerDemos.NestedScrollers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Can open up and shrink the UI region.
	// When shrinking, it scales down its canvas and makes it transparent.
	// Helps to nest/group several elements together
	public class CollapsableSection_UI : MonoBehaviour{
	    [SerializeField] Vector2 _closed_minAndPreferred_height = new Vector2(50,50);
	    [SerializeField] Vector2 _opened_minAndPreferred_height = new Vector2(480,480); //when this control net is disabled (and the panel is collapsed)
	    [Space(10)]
	    [SerializeField] bool _update_textString = true;
	    [SerializeField] string _closed_headerText;
	    [SerializeField] string _opened_headerText;
	    [SerializeField] Color _headerText_openColor = Color.black;
	    [SerializeField] Color _headerText_closedColor = Color.black;
	    [Space(10)]
	    [SerializeField] RectTransform _headerTransf;
	    [SerializeField] TextMeshProUGUI _mainHeaderText;
	    [SerializeField] List<TextMeshProUGUI> _additional_headerText; //if specified, its color will be faided as well.
	    [Space(10)]
	    [SerializeField] Image _mainHeaderImage_optional;
	    [SerializeField] Color _headerImg_openColor;
	    [SerializeField] Color _headerImg_closedColor;
	    [Space(10)]
	    [SerializeField] Button _headerRibbon_button;
	    [SerializeField] LayoutElement _myLayoutElement;//for this entire unit box (contains header and contents)
	    [SerializeField] GameObject _contents;//hidden when we are collapsed.
	    [SerializeField] CanvasGroup _contentsCanvGroup;
	    [Space(10)]
	    [SerializeField] bool _always_controled_by_other_script = false;
	    [SerializeField] bool _will_start_as_open; //expands or colapses on start, only if not controlled by other script

	    bool _neverClosedNorOpenedYet = true;
	    Coroutine _openClose_crtn = null;
	    public bool _isExpanded { get; private set; }

	    public System.Action onClicked { get; set; }//only if our header was clicked
	    public System.Action<bool> onOpenOrClose { get; set; } //if clicked or some script told us to open/close.

	    public Vector2 opened_minAndPreferred_height => _opened_minAndPreferred_height;

	    public void OpenOrCloseSelf(bool isOpen, float dur=0.2f){
	        if(isOpen == _isExpanded && !_neverClosedNorOpenedYet){ return; }//safely ignores duplicate invocations
	        _neverClosedNorOpenedYet = false;
	        _isExpanded = isOpen;
	        onOpenOrClose?.Invoke(isOpen);
	        //use the manager, our game object might be disabled:
	        if(_openClose_crtn != null){  Coroutines_MGR.instance.StopCoroutine(_openClose_crtn);  }
	        _openClose_crtn =  Coroutines_MGR.instance.StartCoroutine( ExpandShrink_crtn(_isExpanded, dur) );
	    }
   
	    public void OpenCloseSelf(bool isInstant=false){
	        _neverClosedNorOpenedYet = false;
	        _isExpanded = !_isExpanded;
	        onOpenOrClose?.Invoke(_isExpanded);
	        float dur = isInstant ? 0 : 0.2f;
	        //use the manager, our game object might be disabled:
	        if(_openClose_crtn != null){  Coroutines_MGR.instance.StopCoroutine(_openClose_crtn);  }
	        _openClose_crtn =  Coroutines_MGR.instance.StartCoroutine( ExpandShrink_crtn(_isExpanded, dur) );
	    }

	    public void Set_MaxOpenHeight(Vector2 minAndPreferred_height, float dur=0.2f){
	        _opened_minAndPreferred_height = minAndPreferred_height;
	        if(!_isExpanded){ return; }
	        if(_openClose_crtn != null){  Coroutines_MGR.instance.StopCoroutine(_openClose_crtn);  }
	        _openClose_crtn =  Coroutines_MGR.instance.StartCoroutine( ExpandShrink_crtn(_isExpanded, dur) );
	    }
    

	    IEnumerator ExpandShrink_crtn(bool isOpen, float dur=0.2f){
	        yield return null;
	        yield return null;

	        if (_update_textString){
	            _mainHeaderText.text =  isOpen ? _opened_headerText : _closed_headerText;
	        }

	        Vector2 startHeight =  new Vector2(_myLayoutElement.minHeight, _myLayoutElement.preferredHeight);
	        Vector2 endHeight = isOpen? _opened_minAndPreferred_height : _closed_minAndPreferred_height;
	        float startTime  = Time.unscaledTime;

	        _contents.SetActive(true);

	        float contentStartAlpha = _contentsCanvGroup.alpha;
	        float contentEndAlpha = isOpen ? 1.0f : 0.0f;
	        var contentStartScale = _contentsCanvGroup.transform.localScale;
	        var contentEndScale = isOpen? new Vector3(1,1,1) : new Vector3(1,0.001f,1);

	        var headerTxt_startColor = _mainHeaderText.color;
	        var headerTxt_endColor =  isOpen ? _headerText_openColor : _headerText_closedColor;

	        var header_startColor = _mainHeaderImage_optional?.color ?? Color.clear;
	        var header_endColor   = isOpen? _headerImg_openColor  :  _headerImg_closedColor;

	        while (true){
	            float elapsed01 = (Time.unscaledTime - startTime) / dur;
	            elapsed01 = Mathf.Clamp01(elapsed01);
	            if(dur == 0){ elapsed01 = 1.0f; }

	            Vector2 height = Vector2.Lerp(startHeight, endHeight, elapsed01);
	            _myLayoutElement.minHeight      = height.x;
	            _myLayoutElement.preferredHeight = height.y;
	            _contentsCanvGroup.alpha =  Mathf.Lerp(contentStartAlpha, contentEndAlpha, elapsed01);
	            _contentsCanvGroup.transform.localScale = Vector3.Lerp(contentStartScale, contentEndScale, elapsed01);

	            _mainHeaderText.color =  Color.Lerp( headerTxt_startColor,  headerTxt_endColor,  elapsed01);
	            _additional_headerText.ForEach( t => t.color=_mainHeaderText.color );

	            if(_mainHeaderImage_optional != null){ 
	                _mainHeaderImage_optional.color = Color.Lerp(header_startColor,  header_endColor,  elapsed01);
	            }
	            //we might overlap other elements without it:
	            LayoutRebuilder.MarkLayoutForRebuild( transform.parent as RectTransform );

	            if (elapsed01 == 1.0f){ break; }
	            yield return null;
	        }
	        _contents.SetActive(isOpen);
	        GetComponentsInChildren<UI_with_OptimizedUpdates>().ToList().ForEach( u=>u.ManuallyUpdate() );
	    }

	    void Start(){
	        if (!_always_controled_by_other_script){ 
            
	            _headerRibbon_button.onClick.AddListener( ()=>{ 
	                onClicked?.Invoke();  
	                OpenCloseSelf(); 
	            });

	            //Set self as closed if we are to be opened, so that OnOpenCloseSelf flips it to opened:
	            _isExpanded =  _will_start_as_open==false;
	            OpenOrCloseSelf(_will_start_as_open, dur:0.0f);
	        }
	    }//end()
	}
}//end namespace
