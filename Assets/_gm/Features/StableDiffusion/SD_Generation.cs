using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace spz {

	public class SD_Generation : MonoBehaviour{
	    [SerializeField] SD_Samplers _samplers;
	    [SerializeField] SliderUI_Snapping _sampleSteps_slider;
	    [SerializeField] SliderUI_Snapping _width_slider;
	    [SerializeField] SliderUI_Snapping _height_slider;
	    [SerializeField] SliderUI_Snapping _batch_count_slider;
	    [SerializeField] SliderUI_Snapping _batch_size_slider;
	    [SerializeField] SliderUI_Snapping _CFG_scale_slider;
	    [SerializeField] IntegerInputField _Seed_intField;
	    [Space(10)]
	    [SerializeField] Button _generate_button;

	    void Start(){
	        _generate_button.onClick.AddListener( OnGenerateButton );
	    }

	    void OnGenerateButton(){
	    }
	}
}//end namespace
