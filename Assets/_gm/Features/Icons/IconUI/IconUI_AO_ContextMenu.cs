using UnityEngine;
using UnityEngine.UI;
using System;

namespace spz {

	[Serializable]
	public struct AmbientOcclusionInfo{
	    public float visibility;
	    public float darkCoeff;//how visible are shadows.
	    public float midtonesCoeff;//how visible are the in-between values.
	    public float highlightsCoeff;//how pronounced are highlights.
	    public float pivot; //specifies where midtones are
	}


	// Opens little menu inside a 2D icon which sits in some Art ui list.
	// Controls several sliders etc. Sliders for adjusting AmbientOcclsion,
	// which is a way of shading the object.
	public class IconUI_AO_ContextMenu : MonoBehaviour{

	    [SerializeField] IconUI _myIcon; 
	    [Space(10)] 
	    [SerializeField] SliderUI_Snapping _slider_visibility;
	    [SerializeField] SliderUI_Snapping _slider_pivot;
	    [SerializeField] SliderUI_Snapping _slider_darks;
	    [SerializeField] SliderUI_Snapping _slider_midtones;
	    [SerializeField] SliderUI_Snapping _slider_highlights;
	    [SerializeField] DoubleClickButton_UI _delete_button;
	    [SerializeField] Button _load_button;
	    [SerializeField] Button _save_button;
	    bool _alreadyShown = false;
	    bool _StartInvoked = false;
	    public Action OnDeleteButton { get; set; }
	    public Action OnSaveButton { get; set; }
	    public Action OnLoadButton { get; set; }

	    public void ShowOrHide(bool isShow){
	        if (_StartInvoked || isShow){  
	            gameObject.SetActive(isShow);
	            _alreadyShown = true;
	        }
	    }

	    public AmbientOcclusionInfo aoInfo
	        => new AmbientOcclusionInfo{ visibility= _slider_visibility.value,
	                                     darkCoeff = _slider_darks.value,
	                                     midtonesCoeff = _slider_midtones.value,
	                                     highlightsCoeff = _slider_highlights.value,
	                                     pivot = _slider_pivot.value, };

	    public void ForceChange_slider_visibility(float val)=> _slider_visibility.SetSliderValue(val,true);
	    public void ForceChange_slider_half(float val) => _slider_pivot.SetSliderValue(val,true);
	    public void ForceChange_slider_dark(float val) => _slider_darks.SetSliderValue(val,true);
	    public void ForceChange_slider_mid(float val)  => _slider_midtones.SetSliderValue(val,true);
	    public void ForceChange_slider_high(float val) => _slider_highlights.SetSliderValue(val,true);
	    public void ForceClick_SaveButton() => _save_button.onClick.Invoke();
	    public void ForceClick_LoadButton() => _load_button.onClick.Invoke();

	    public void Set_AOInfo( AmbientOcclusionInfo inf ){
	        _slider_visibility.SetSliderValue(inf.visibility,true);
	        _slider_darks.SetSliderValue(inf.darkCoeff,true);
	        _slider_midtones.SetSliderValue(inf.midtonesCoeff,true);
	        _slider_highlights.SetSliderValue(inf.highlightsCoeff,true);
	        _slider_pivot.SetSliderValue(inf.pivot,true);
	    }

	    void Awake(){
	        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
	    }

	    void Start(){
	        _delete_button.onConfirmedClick +=  ()=>OnDeleteButton?.Invoke();
	        _save_button.onClick.AddListener( ()=>OnSaveButton?.Invoke() );
	        _load_button.onClick.AddListener( ()=>OnLoadButton?.Invoke() );
	        if(!_alreadyShown){ gameObject.SetActive(false);}
	        _StartInvoked = true;

	        _slider_visibility.onValueChanged.AddListener( OnAnySliderValChanged );
	        _slider_pivot.onValueChanged.AddListener( OnAnySliderValChanged );
	        _slider_darks.onValueChanged.AddListener( OnAnySliderValChanged );
	        _slider_midtones.onValueChanged.AddListener( OnAnySliderValChanged );
	        _slider_highlights.onValueChanged.AddListener( OnAnySliderValChanged );
	        _delete_button.onConfirmedClick +=  ()=>OnAnySliderValChanged(0.0f);
	    }

	    void OnAnySliderValChanged(float val) => Objects_Renderer_MGR.instance.ReRenderAll_soon();
	}
}//end namespace
