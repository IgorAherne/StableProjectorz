using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

namespace spz {

	public class Tootlips_UI_MGR : MonoBehaviour
	{
	    public static Tootlips_UI_MGR instance { get; private set; } = null;

	    [SerializeField] CanvasScaler _canvScaler_ofParent;
	    [SerializeField] RectTransform _tooltip_rectTransf;
	    [SerializeField] CanvasGroup _tooltip_canvGroup;
	    [SerializeField] TextMeshProUGUI _tooltipInvisible_text;
	    [SerializeField] TextMeshProUGUI _tooltipVisible_text;
	    [SerializeField] float _tooltipOffset = 1.0f;

	    CanShowTooltip_UI _currentRequestor = null;
	    float _time_requested_showTooltip = -9999;


	    public void ShowTooltipFor( CanShowTooltip_UI forThis){

	        if(forThis!=_currentRequestor){//sometimes OnPointerExit isn't invoked, so ensure manually:
	            _currentRequestor?.OnPointerExit( new PointerEventData(EventSystem.current) );
	        }

	        //check if focused, to avoid distracting user if not:
	        if(Settings_MGR.instance.get_isAllowTooltips()==false || Application.isFocused==false){
	            _currentRequestor = null;
	            return; 
	        }

	        _currentRequestor = forThis;
        
	        _tooltip_rectTransf.gameObject.SetActive(true);
	        _tooltip_canvGroup.alpha = 0;//will be shown during update. (gives time to resize, etc)
	        _time_requested_showTooltip = Time.time;

	        _tooltipInvisible_text.text = forThis.tooltipText;
	        _tooltipVisible_text.text = forThis.tooltipText;
	    }
    



	    void Update(){
	        bool timeIsGreater =  Time.time > _time_requested_showTooltip;
	        if(_currentRequestor!=null && timeIsGreater){//don't reveal if it's still same frame - give it time to update size.
	            _tooltip_canvGroup.alpha = Mathf.Lerp(_tooltip_canvGroup.alpha, 1, Time.deltaTime*7);
	            PositionTooltip();
	        }
	        if(_currentRequestor == null){ return; }
	        if(_currentRequestor.isHovered && KeyMousePenInput.isLMBpressed()==false){ return; }
	        //else not hovered any more:
	        _tooltipInvisible_text.text = " ";
	        _tooltipVisible_text.text = " ";
	        _tooltip_canvGroup.alpha = 0;
	        _tooltip_rectTransf.gameObject.SetActive(false);
	        _currentRequestor = null;
	    }




	    void PositionTooltip(){
	        // Adjust for canvas scaling
	        float SF = calcScaleFactor();
	        Vector2 min = Vector2.zero;
	        Vector2 max = new Vector2(Screen.width, Screen.height);
	        Vector2 toolSize = _tooltip_rectTransf.rect.size * SF;
	        float offset = _tooltipOffset * SF;

	        // Adjust the tooltip position with offset and scaling
	        Vector3 position = new Vector3((Input.mousePosition.x + toolSize.x/2 + offset),
	                                       (Input.mousePosition.y + toolSize.y/2 + offset),
	                                       0f);
	        // Clamp it to the screen size so it doesn't go outside
	        _tooltip_rectTransf.position = new Vector3( Mathf.Clamp(position.x,  min.x + toolSize.x/2,  max.x-toolSize.x/2),
	                                                    Mathf.Clamp(position.y,  min.y + toolSize.y/2,  max.y-toolSize.y/2),
	                                                    _tooltip_rectTransf.position.z );
	    }

	    float calcScaleFactor(){
	        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
	        Vector2 refRes = _canvScaler_ofParent.referenceResolution;
	        float kLogBase = 2;
	        // We take the log of the relative width and height before taking the average.
	        // Then we transform it back in the original space.
	        // the reason to transform in and out of logarithmic space is to have better behavior.
	        // If one axis has twice resolution and the other has half, it should even out if widthOrHeight value is at 0.5.
	        // In normal space the average would be (0.5 + 2) / 2 = 1.25
	        // In logarithmic space the average is (-1 + 1) / 2 = 0
	        float logWidth = Mathf.Log(screenSize.x / refRes.x, kLogBase);
	        float logHeight = Mathf.Log(screenSize.y / refRes.y, kLogBase);
	        float logWeightedAverage = Mathf.Lerp(logWidth, logHeight, _canvScaler_ofParent.matchWidthOrHeight);
	        float scaleFactor = Mathf.Pow(kLogBase, logWeightedAverage);
	        return scaleFactor;
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;
	        _tooltip_canvGroup.gameObject.SetActive(false);
	        _tooltip_canvGroup.alpha = 0;
	    }

	}
}//end namespace
