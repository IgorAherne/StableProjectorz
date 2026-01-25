using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// shown under cursor in the large 2D viewport. 
	// Exposes controls for adjusting visibility of the current AmbientOcclusion icon in the Arts-list.
	// This is useful so that user doesn't have to reach to the 2d ui List each time.
	// Instead, these sliders will be right under their cursor.
	public class ViewportContextMenu_AO_UI : ViewportContextMenu_UI
	{
	    [SerializeField] Slider _visibilitySlider;
	    [SerializeField] Slider _halfSlider;
	    [SerializeField] Slider _darkSlider;
	    [SerializeField] Slider _midSlider;
	    [SerializeField] Slider _highSlider;
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
	        AmbientOcclusionInfo aoInfo = affectThisIcon.aoInfo();
	        setupSlider(_visibilitySlider, aoInfo.visibility);
	        setupSlider(_halfSlider, aoInfo.pivot);
	        setupSlider(_darkSlider, aoInfo.darkCoeff);
	        setupSlider(_midSlider, aoInfo.midtonesCoeff);
	        setupSlider(_highSlider, aoInfo.highlightsCoeff);
	    }

	    protected override void DeInit(){
	        onDarkSlider = null;
	        onHalfSlider = null;
	        onDarkSlider = null;
	        onMidSlider = null;
	        onHighSlider = null;
	        onSaveButton = null;
	        onLoadButton = null;
	    }

	    public System.Action<float> onVisibilitySlider { get; set; } = null;
	    public System.Action<float> onHalfSlider { get; set; } = null;
	    public System.Action<float> onDarkSlider { get; set; } = null;
	    public System.Action<float> onMidSlider { get; set; } = null;
	    public System.Action<float> onHighSlider { get; set; } = null;
	    public System.Action onSaveButton { get; set; } = null;
	    public System.Action onLoadButton { get; set; } = null;

	    void Start(){
	        _visibilitySlider.onValueChanged.AddListener( (val)=> onVisibilitySlider?.Invoke(val) );
	        _halfSlider.onValueChanged.AddListener( (val)=>onHalfSlider?.Invoke(val) );
	        _darkSlider.onValueChanged.AddListener( (val)=>onDarkSlider?.Invoke(val) );
	        _midSlider.onValueChanged.AddListener( (val)=>onMidSlider?.Invoke(val) );
	        _highSlider.onValueChanged.AddListener( (val)=>onHighSlider?.Invoke(val) );
	        _save_button.onClick.AddListener( ()=>onSaveButton?.Invoke() );
	        _load_button.onClick.AddListener( ()=>onLoadButton?.Invoke() );
	    }
	}
}//end namespace
