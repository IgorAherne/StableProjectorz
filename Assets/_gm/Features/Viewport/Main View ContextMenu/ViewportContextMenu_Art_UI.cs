using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// shown under cursor in the large 2D viewport. 
	// Exposes controls for adjusting hue, saturation value of the current projection/background.
	// This is useful so that user doesn't have to reach to the 2d ui List each time.
	// Instead, these sliders will be right under their cursor.
	public class ViewportContextMenu_Art_UI : ViewportContextMenu_UI{
	    [SerializeField] Slider _hueSlider;
	    [SerializeField] Slider _saturationSlider;
	    [SerializeField] Slider _valueSlider;
	    [SerializeField] Slider _contrastSlider;
	    [SerializeField] Button _restoreCameraButton;
	    [SerializeField] Button _seed_button;
	    [SerializeField] Button _save_button;
	    [SerializeField] Button _load_button;

	    //which icon from the list are we "tied to".
	    //We will affect its hue, saturation, value, etc.
	    IconUI _associatedIcon;

	    protected override void Init(IconUI affectThisIcon){
	        _associatedIcon = affectThisIcon;
	        _associatedIcon.Link_ViewportContextMenu_toSelf(this);

	        void setupSlider(Slider slider, float observedTotalVal){
	            slider.SetValueWithoutNotify(observedTotalVal);
	        }
	        HueSatValueContrast hsvc = affectThisIcon.hsvc();
	        setupSlider(_hueSlider, hsvc.hueShift);
	        setupSlider(_saturationSlider, hsvc.saturation);
	        setupSlider(_valueSlider, hsvc.value);
	        setupSlider(_contrastSlider, hsvc.contrast);
	    }


	    protected override void DeInit(){
	        onHueSlider = null;
	        onSaturationSlider = null;
	        onValueSlider = null;
	        onContrastSlider = null;
	        onRestoreCameraButton = null;
	        onSeedButton = null;
	        onSaveButton = null;
	        onLoadButton = null;
	    }

	    public System.Action<float> onHueSlider { get; set; } = null;
	    public System.Action<float> onSaturationSlider { get; set; } = null;
	    public System.Action<float> onValueSlider { get; set; } = null; 
	    public System.Action<float> onContrastSlider { get; set; } = null;
	    public System.Action onRestoreCameraButton { get; set; } = null;
	    public System.Action onSeedButton { get; set; } = null;
	    public System.Action onSaveButton { get; set; } = null;
	    public System.Action onLoadButton { get; set; } = null;
	    public System.Action onClickButton { get; set; } = null;

	    void Start(){
	        _hueSlider.onValueChanged.AddListener( (val)=>onHueSlider?.Invoke(val) );
	        _saturationSlider.onValueChanged.AddListener( (val)=>onSaturationSlider?.Invoke(val) );
	        _valueSlider.onValueChanged.AddListener( (val)=>onValueSlider?.Invoke(val) );
	        _contrastSlider.onValueChanged.AddListener( (val)=>onContrastSlider?.Invoke(val) );
	        _seed_button.onClick.AddListener( ()=>onSeedButton?.Invoke() );
	        _restoreCameraButton.onClick.AddListener( ()=>onRestoreCameraButton?.Invoke() );
	        _save_button.onClick.AddListener( ()=>onSaveButton?.Invoke() );
	        _load_button.onClick.AddListener( ()=>onLoadButton?.Invoke() );
	    }
	}


	// small panel that opens under cursor, when user right clicks in the large 2D viewport. 
	public abstract class ViewportContextMenu_UI : MonoBehaviour{
    
	    [SerializeField] CanvasGroup _canvGrp;
	    [SerializeField] RectTransform _myRectTransf;
	    bool _alreadyHiding = false;
    
	    protected abstract void Init(IconUI affectThisIcon);
	    protected abstract void DeInit();


	    public void ToggleVisibility(bool isShow,  IconUI affectThisIcon,  bool isInstant, System.Action onComplete=null){
	        if (isShow){ 
	            InitAndShow(affectThisIcon);
	        }else{
	            DeinitAndHide(isInstant:isInstant, onComplete);
	        }
	    }

    
	    void Reposition_underCursor(){
	        RectTransform myParentRectTransf =  transform.parent as RectTransform;
	        Vector2 localPoint;
	        // Convert the mouse position to local point in parent RectTransform:
	        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(  myParentRectTransf, Input.mousePosition, null, out localPoint)){
	            Vector2 relativePosition = Rect.PointToNormalized(myParentRectTransf.rect, localPoint);
	            _myRectTransf.anchorMin = _myRectTransf.anchorMax = relativePosition;
	            _myRectTransf.anchoredPosition = Vector2.zero;
	        }
	    }

	    void InitAndShow( IconUI affectThisIcon ){
	        _alreadyHiding = false;
	        gameObject.SetActive(true);
	        Reposition_underCursor();
	        Init(affectThisIcon);
	        StopAllCoroutines();
	        StartCoroutine( ToggleVisibil_crtn(isShow:true, isInstant:false, onComplete:null) );
	    }

	    void DeinitAndHide(bool isInstant, System.Action onComplete){ 
	        if(_alreadyHiding){ return; }
	        _alreadyHiding = true;
	        gameObject.SetActive(true);
	        DeInit(); 
	        StopAllCoroutines();
	        StartCoroutine( ToggleVisibil_crtn(isShow:false, isInstant:isInstant, onComplete) );
	    }

	    IEnumerator ToggleVisibil_crtn(bool isShow, bool isInstant,  System.Action onComplete, float dur=0.12f){
	        if (!isInstant){ 
	            float startOpacity = _canvGrp.alpha;
	            float endOpacity = isShow ? 1 : 0;
	            float startTime = Time.time;
	            while (true){
	                float elapsed01 = (Time.time - startTime)/dur;
	                      elapsed01 = Mathf.Clamp01(elapsed01);
	                 _canvGrp.alpha = Mathf.Lerp(startOpacity, endOpacity, elapsed01);

	                if(elapsed01==1.0){ break; }
	                yield return null;
	            }
	        }
	        gameObject.SetActive(isShow);
	        onComplete?.Invoke();
	    }

	    protected virtual void Awake(){
	        _canvGrp.alpha = 0;
	        gameObject.SetActive(false);
	    }
	}
}//end namespace
