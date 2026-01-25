using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace spz {

	public enum Gen3D_InputElement_Kind{
	    Uknown,
	    Header,//text header
	    CircleSlider,
	    Toggle,
	    Int_Horiz,
	    Int_Vertical,
	    StrInput,
	    Dropdown,
	    Button,
	    TextPrompt,
	    SingleMultiImageInputs,
	    Supports_Retexture
	}

	// for describing controls that the 'Gen3D_InputPanelBuilder_UI' can dynamically create.
	// They sit inside the UI and allow user to specify the needed parameters.
	public class Gen3D_InputElement_UI : MonoBehaviour {
    
	    [SerializeField] TextMeshProUGUI _visible_name_text;

	    public string code_name { get; private set; } //for example "my_slider"
	    public string visible_name { get; private set; } //for example "My Slider"
	    public Gen3D_InputElement_Kind kind { get; private set; }

	    // Sometimes there is a certain limit of content, before our Gen3D button becomes active.
	    // For example, can represent number of images imported. Or number of characters in text prompt.
	    //   string is operation type
	    //   int is mimunim amount of content needed, to allow this generation.
	    // For example
	    //   {"make_meshes_and_tex", 1},
	    //   {"some_other_operation", 5}, etc.
	    // If operation isn't mentioned, we don't enforce any minimim for it.
	    // For exmaple, "retexture" might not need any ui-input-images, and will instead need other inputs.
	    Dictionary<string,int> _min_amount_toAllowGenerate = new Dictionary<string, int>();


	    //references, for convenience. Get assigned during Init()
	    CircleSlider_Snapping_UI _circle_slider = null;
	    TextMeshProUGUI _circle_slider_numberTxt;
	    int _float_show_n_decimals = 0;//how many numbers to visually show after the dot, in the slider
    
	    IntegerInputField _int_input;
	    Button _int_input_resetButton;//has a dice as its icon, and use if it's a Seed.
	    int _int_input_defaultVal;

	    TMP_InputField _str_input;
	    TMP_Dropdown _dropdown;
	    Button _button;
	    Toggle _toggle = null;
	    Generation3D_Prompt_UI _text_prompt;

	    Gen3D_All_ImageInputs_UI _imageInputs;

	    public void Init(string code_name, string visible_name, Gen3D_InputElement_Kind kind){
	        this.code_name = code_name;
	        this.visible_name = visible_name;
	        this.kind = kind;

	        gameObject.name = code_name;

	        if (_visible_name_text != null){
	            _visible_name_text.text = visible_name;
	        }

	        switch (kind){
	            case Gen3D_InputElement_Kind.Uknown:
	                break;
	            case Gen3D_InputElement_Kind.CircleSlider:
	                _circle_slider = GetComponentInChildren<CircleSlider_Snapping_UI>();
	                _circle_slider_numberTxt = _circle_slider.GetComponentInChildren<TextMeshProUGUI>();
	                break;
	            case Gen3D_InputElement_Kind.Toggle:
	                _toggle = GetComponentInChildren<Toggle>();
	                break;
	            case Gen3D_InputElement_Kind.Int_Vertical:
	                _int_input = GetComponentInChildren<IntegerInputField>();
	                break;
	            case Gen3D_InputElement_Kind.Int_Horiz:
	                _int_input = GetComponentInChildren<IntegerInputField>();
	                _int_input_resetButton = _int_input.GetComponentInChildren<Button>();
	                _int_input_resetButton?.onClick.AddListener( ()=>_int_input.SetValue(_int_input_defaultVal.ToString()) );
	                break;
	            case Gen3D_InputElement_Kind.StrInput:
	                _str_input = GetComponentInChildren<TMP_InputField>();
	                break;
	            case Gen3D_InputElement_Kind.TextPrompt:
	                _text_prompt = GetComponentInChildren<Generation3D_Prompt_UI>();
	                break;
	            case Gen3D_InputElement_Kind.Dropdown:
	                _dropdown = GetComponentInChildren<TMP_Dropdown>();
	                break;
	            case Gen3D_InputElement_Kind.Button:
	                _button = GetComponentInChildren<Button>();
	                break;
	            case Gen3D_InputElement_Kind.SingleMultiImageInputs:
	                _imageInputs = GetComponentInChildren<Gen3D_All_ImageInputs_UI>(includeInactive:true);
	                break;
	            default:
	                break;
	        }
	    }

	    public void SetMinMax_Float_noNotify(float min, float max, float currVal, int show_n_decimals=0){ 
	        _circle_slider.min=min;  
	        _circle_slider.max=max;
	        _circle_slider.defaultVal = currVal;
	        _circle_slider.SetSliderValue(currVal, invokeCallback: false);
	        _float_show_n_decimals = show_n_decimals;
	    }

	    public void SetMinMax_Int_noNotify(int min, int max, int currVal){  
	        _int_input.SetMin(min); 
	        _int_input.SetMax(max);
	        _int_input.SetValueWithoutNotify(currVal.ToString());
	        _int_input_defaultVal = currVal;
	    }
	    public void Set_IntInput_AsSeed(bool isTrue){
	        _int_input_resetButton.gameObject.SetActive(isTrue);
	        RectTransform reset_rtr = _int_input_resetButton.transform as RectTransform;

	        var rtr   = _int_input.transform as RectTransform;
	        var offset = rtr.offsetMin;
	            offset.x = isTrue? reset_rtr.rect.width+3 : 0;
	            rtr.offsetMin = offset;
	    }

	    public void SetTextPropompt_isPositive(bool isPositive){
	        _text_prompt.Set_IsPositive(isPositive);
	    }

	    public void SetText_noNotify(string txt){
	        if(kind == Gen3D_InputElement_Kind.TextPrompt){
	            _text_prompt.PasteText(txt);
	        }else { 
	            _str_input.text = txt;
	        }
	    }

	    public void SetToggleValue_noNotify(bool isOn){
	        _toggle.SetIsOnWithoutNotify(isOn);
	    }

	    public void SetDropDownChoices_noNotify(List<string> choices, int currIx, int dropdown_extraWidth_px=100){
	        _dropdown.ClearOptions();
	        _dropdown.AddOptions(choices);
	        _dropdown.SetValueWithoutNotify(currIx);
	        var rtr = _dropdown.transform.Find("Template") as RectTransform;
	        if(rtr != null){
	            float tinyOffset = -0.5f;//ensures there is a tiny offset from the screen-side, else dropdown glitches.
	            rtr.sizeDelta = new Vector2(dropdown_extraWidth_px+tinyOffset, rtr.sizeDelta.y);  
	        }
	    }

	    public object GetValueData(){
	        object data = null;
	        switch (kind){
	            case Gen3D_InputElement_Kind.CircleSlider:
	                data = _circle_slider.value;
	                break;
        
	            case Gen3D_InputElement_Kind.Toggle:
	                data = _toggle.isOn;
	                break;
        
	            case Gen3D_InputElement_Kind.Int_Horiz:
	            case Gen3D_InputElement_Kind.Int_Vertical:
	                data = _int_input.recentVal;
	                break;
        
	            case Gen3D_InputElement_Kind.StrInput:
	                data = _str_input.text;
	                break;
        
	            case Gen3D_InputElement_Kind.SingleMultiImageInputs:
	                var imgInputs = GetComponentInChildren<Gen3D_All_ImageInputs_UI>();
	                if (imgInputs != null) {
	                    data = imgInputs.get_images_asBase64();
	                }
	                break;
        
	            case Gen3D_InputElement_Kind.Dropdown:
	                data = _dropdown.value;
	                break;
        
	            case Gen3D_InputElement_Kind.TextPrompt:
	                data = _text_prompt.prompt;
	                break;
        
	            case Gen3D_InputElement_Kind.Button:
	            case Gen3D_InputElement_Kind.Uknown:
	            default:
	                break;
	        }
	        return data;
	    }
    

	    public void Set_min_amount_toAllowGenerate(Dictionary<string,int> operationType_to_minAmountNeeded){
	        _min_amount_toAllowGenerate = operationType_to_minAmountNeeded?? new Dictionary<string,int>();
	    }

	    //operation_type is what we intend to do on the server, for example "make_meshes_and_tex", or "retexture".
	    public bool isReady_ForGenerate(string operation_type){
	        bool caresAboutTheOperation = _min_amount_toAllowGenerate.TryGetValue(operation_type, out int minNumber);
	        if (caresAboutTheOperation == false){ return true; }//true - no minimum is enforced.

	        if(kind == Gen3D_InputElement_Kind.SingleMultiImageInputs){
	            return _imageInputs.numImages() >= minNumber; 
	        }
	        if(kind == Gen3D_InputElement_Kind.TextPrompt){ 
	            return _text_prompt.prompt.Length >= minNumber; 
	        }
	        if(kind == Gen3D_InputElement_Kind.StrInput){ 
	            return _str_input.text.Length >= minNumber;
	        }
	        return true;
	    }

	    public bool OnDragAndDropImages(List<string> files, Vector2Int screenCoord){
	        if(kind != Gen3D_InputElement_Kind.SingleMultiImageInputs) { return false; }//files are irrelevant to us.
	        bool consumed = _imageInputs.OnDragAndDropImages(files, screenCoord);
	        return consumed;
	    }

	    void Update(){
	        if (_circle_slider_numberTxt != null){
	            string txt = _circle_slider.value.ToString($"F{_float_show_n_decimals}");
	            int numChars = txt.Length;

	                if(numChars >= 4){ txt = $"<size=87%>{txt}</size>"; }
	           else if(numChars >= 3){ txt = $"<size=90%>{txt}</size>"; }

	            _circle_slider_numberTxt.text = txt;
	        }
	    }//end update

	}
}//end namespace
