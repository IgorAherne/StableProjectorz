using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace spz {

	public class ControlNetUnit_ThreshSliders : MonoBehaviour{
	    [Space(10)]
	    [SerializeField] ControlNetUnit_UI _myUnit;
	    [SerializeField] CollapsableSection_UI _collapsableSection;
	    [SerializeField] GameObject _sliders_parentGO;
	    [Space(10)]
	    [SerializeField] GameObject _sliderA_sectionGO;//holds slider+header
	    [SerializeField] SliderUI_Snapping _sliderA;
	    [SerializeField] TextMeshProUGUI _sliderA_nameAndVal;
	    [Space(10)]
	    [SerializeField] GameObject _sliderB_sectionGO;//holds slider+header
	    [SerializeField] SliderUI_Snapping _sliderB;
	    [SerializeField] TextMeshProUGUI _sliderB_nameAndVal;

	    // -1 is default BUT Most expect 0.5 (like Depth, Normalbae, or preprocessor that's reference_only)
	    // and -1 doesn't really work well for them.
	    // But some might expect other specific values. This can be seen from Browser, beneath generated image.
	    public float threshold_A => _sliderA_sectionGO.activeInHierarchy? _sliderA.value : 0.5f;
	    public float threshold_B => _sliderB_sectionGO.activeInHierarchy ? _sliderB.value : 0.5f;

	    public string sliderA_currName = ""; //can be different to what's in the label (name without the value)
	    public string sliderB_currName = ""; //can be different to what's in the label (name without the value)

	    Vector2 _csec_minMaxHeightStart;//height vals that the Collapsable Section starts with.


	    public void CopyFromAnother(ControlNetUnit_ThreshSliders other){
	        if(_sliderA.value != other._sliderA.value){ _sliderA.SetSliderValue(_sliderA.value, true); }
	        if(_sliderB.value != other._sliderB.value){ _sliderB.SetSliderValue(_sliderB.value, true); }
	        sliderA_currName = other.sliderA_currName;
	        sliderB_currName = other.sliderB_currName;
	    }

	    public void OnUnitAltered(){
	        string preprocessor = _myUnit.currPreprocessorName().ToLower();
	        string model = _myUnit.currModelName().ToLower();

	        bool a_wasOn = _sliderA_sectionGO.activeInHierarchy;
	        bool b_wasOn = _sliderB_sectionGO.activeInHierarchy;
	        _sliderA_sectionGO.SetActive(false);
	        _sliderB_sectionGO.SetActive(false);

	        if (preprocessor.Contains("reference")){
	            EnableSliders("Style Fidelity", "", 0.5f, 0.5f, new Vector2(0, 1), new Vector2(0, 1));
	        }
	        if (preprocessor.Contains("mlsd")){
	            EnableSliders("MLSD Val Thresh", "MLSD Dist Tresh", 0.1f, 0.1f, new Vector2(0, 2), new Vector2(0.01f, 20));
	        }
	        if (preprocessor.Contains("canny")){
	            EnableSliders("Low Thresh", "High Thresh", 100, 200, new Vector2(0, 256), new Vector2(0, 256));
	        }
	        if (preprocessor.Contains("clip-g")){
	            EnableSliders("Noise Augment", "", 0, -1, new Vector2(0, 1), new Vector2(0, 1));
	        }
	        if (preprocessor.Contains("clip-g (revision)")){
	            EnableSliders("Noise Augment", "", 0, -1, new Vector2(0, 1), new Vector2(0, 1));
	        }
	        if (preprocessor.Contains("tile_colorfix+sharp")){
	            EnableSliders("Sharpness", "", 1, -1, new Vector2(0, 2), new Vector2(0, 2));
	        }
	        if (preprocessor.Contains("threshold")){
	            EnableSliders("Binarize Thresh", "", 127, -1, new Vector2(0, 255), new Vector2(0, 255));
	        }
	        if (preprocessor.Contains("softedge_teed")){
	            EnableSliders("Safe Steps", "", 2, -1, new Vector2(0, 10), new Vector2(0, 10));
	        }
	        if (preprocessor.Contains("scribble_xdog")){
	            EnableSliders("XDoG Threshold", "", 32, -1, new Vector2(1, 64), new Vector2(1, 64));
	        }
	        if (preprocessor.Contains("reference_adain+attn")){
	            EnableSliders("Style Fidelity", "", 0.5f, 0.5f, new Vector2(0, 1), new Vector2(0, 1));
	        }
	        if (preprocessor.Contains("reference_adain")){
	            EnableSliders("Style Fidelity", "", 0.5f, 0.5f, new Vector2(0, 1), new Vector2(0, 1));
	        }
	        if (preprocessor.Contains("recolor_luminance")){
	            EnableSliders("Gamma Correction", "", 1, -1, new Vector2(0.1f, 2), new Vector2(0.1f, 2));
	        }
	        if (preprocessor.Contains("recolor_intensity")){
	            EnableSliders("Gamma Correction", "", 1, -1, new Vector2(0.1f, 2), new Vector2(0.1f, 2));
	        }
	        if (preprocessor.Contains("normal_midas")){
	            EnableSliders("Normals BG Thresh", "", 0.4f, -1, new Vector2(0, 1), new Vector2(0, 1));
	        }
	        if (preprocessor.Contains("mediapipe_face")){
	            EnableSliders("Max Faces", "Min Face Confid", 1, 0.5f, new Vector2(1, 10), new Vector2(0.01f, 1));
	        }
	        if (preprocessor.Contains("depth_leres++") || preprocessor.Contains("depth_leres")){
	            EnableSliders("Remove Near %", "Remove BG %", 0, 0, new Vector2(0, 100), new Vector2(0, 100));
	        }
	        if (preprocessor.Contains("blur_gaussian")){
	            EnableSliders("Sigma", "", 9, -1, new Vector2(0.01f, 64), new Vector2(0.01f, 64));
	        }
	        LayoutRebuilder.MarkLayoutForRebuild(_sliderA.transform.parent as RectTransform);

	        bool a_isOn = _sliderA_sectionGO.activeSelf;
	        bool b_isOn = _sliderB_sectionGO.activeSelf;
	        bool any_on =  a_isOn || b_isOn;

	        _sliders_parentGO.SetActive(any_on);

	        if(a_wasOn != a_isOn  ||  b_wasOn != b_isOn){
	            Vector2 extraHeight = new Vector2(70,70);
	            Vector2 minMaxHeight = _csec_minMaxHeightStart + (any_on? extraHeight : Vector2.zero);
	            _collapsableSection.Set_MaxOpenHeight(minMaxHeight, dur:0.1f);
	        }
	    }

	    void OnValueChanged_SliderA(float val){
	        _sliderA_nameAndVal.text = $"<size=90%>{sliderA_currName}:  {val.ToString("0.00")}</size>";
	    }

	    void OnValueChanged_SliderB(float val){
	        _sliderB_nameAndVal.text = $"<size=90%>{sliderB_currName}:  {val.ToString("0.00")}</size>";
	    }

	    void EnableSliders( string sliderA_name,  string sliderB_name,
	                        float aVal,  float bVal, 
	                        Vector2 a_minMax,  Vector2 b_minMax ){
	        bool enableA = sliderA_name != "";
	        bool enableB = sliderB_name != "";

	        _sliderA_sectionGO.SetActive(enableA);
	        _sliderB_sectionGO.SetActive(enableB);

	        _sliderA_nameAndVal.text = sliderA_currName = sliderA_name;
	        _sliderB_nameAndVal.text = sliderB_currName = sliderB_name;
         
	        if(enableA){ _sliderA.AdjustMinMax(a_minMax, aVal, invokeCallback:true); }
	        if(enableB){ _sliderB.AdjustMinMax(b_minMax, bVal, invokeCallback:true); }
	    }


	    void Awake(){
	        _sliderA.onValueChanged.AddListener( OnValueChanged_SliderA );
	        _sliderB.onValueChanged.AddListener( OnValueChanged_SliderB );

	        //do this in Awake, not in Start(), because light Load project from hard drive:
	        _csec_minMaxHeightStart = _collapsableSection.opened_minAndPreferred_height;
	        _sliders_parentGO.SetActive(false);//starts inactive initially
	    }

	    void OnDestroy(){
	        _sliderA?.onValueChanged?.RemoveAllListeners();
	        _sliderB?.onValueChanged?.RemoveAllListeners();
	    }

    
	}
}//end namespace
