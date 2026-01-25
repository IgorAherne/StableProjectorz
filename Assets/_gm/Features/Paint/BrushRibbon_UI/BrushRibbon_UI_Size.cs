using TMPro;
using UnityEngine;

namespace spz {

	// Helps the 'BrushRibbon_UI' component. Knows "how large the brush is".
	// Only deals with and represents the small circular slider that controls the brush size.
	public class BrushRibbon_UI_Size : MonoBehaviour{

	    [SerializeField] CircleSlider_Snapping_UI _maskBrushSize_slider;
	    [SerializeField] TextMeshProUGUI _brushSize_text;

	    float _bracket_BrushSize_nextTime = -99999;
	    public float brushSize01 => _maskBrushSize_slider.value;

	    public void SetBrushSize(float s) => _maskBrushSize_slider.SetSliderValue(s, true);


	    void OnBrushSize_sliderPressed()
	        => Viewport_StatusText.instance.ShowStatusText("Shift + RightMouseDrag to change size easier :)", false, 2, false);

	    void OnBrushSize_slider(float size)
	        => _brushSize_text.text = Mathf.RoundToInt(size * 100).ToString();


	    void Update(){
	        if(MainViewport_UI.instance.isCursorHoveringMe() == false){ return; }

	        float currVal = _maskBrushSize_slider.value;
	        float brushIncrement = 0.01f;
	        if(Input.GetKeyDown(KeyCode.LeftBracket)){
	            _maskBrushSize_slider.SetSliderValue( currVal-brushIncrement, invokeCallback:true);
	            _bracket_BrushSize_nextTime = Time.time + 0.2f;
	        }
	        if(Input.GetKeyDown(KeyCode.RightBracket)){ 
	            _maskBrushSize_slider.SetSliderValue( currVal+brushIncrement, invokeCallback:true);
	            _bracket_BrushSize_nextTime = Time.time + 0.2f;
	        }
	        if(Input.GetKey(KeyCode.LeftBracket) && Time.time>=_bracket_BrushSize_nextTime){
	            _maskBrushSize_slider.SetSliderValue( currVal-brushIncrement*3, invokeCallback:true);
	            _bracket_BrushSize_nextTime = Time.time+0.03f;
	        }
	        if(Input.GetKey(KeyCode.RightBracket) && Time.time>=_bracket_BrushSize_nextTime){
	            _maskBrushSize_slider.SetSliderValue( currVal+brushIncrement*3, invokeCallback:true);
	            _bracket_BrushSize_nextTime = Time.time+0.03f;
	        }
	    }

	    void Awake(){
	        _maskBrushSize_slider.onValueChanged.AddListener(OnBrushSize_slider);
	        _maskBrushSize_slider.onPressedDown.AddListener(OnBrushSize_sliderPressed);
	    }

	    void Start(){ //Update the text, else would remain unchanged until brush-resize:
	        OnBrushSize_slider(_maskBrushSize_slider.value);
	    }

	    public void Save(BrushRibbon_UI_SL trSL){
	        trSL.maskBrush_size01 = _maskBrushSize_slider.value;
	    }

	    public void Load(BrushRibbon_UI_SL trSL){
	        _maskBrushSize_slider.SetSliderValue(trSL.maskBrush_size01, invokeCallback: true);
	    }
	}
}//end namespace
