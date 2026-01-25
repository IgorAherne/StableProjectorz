using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Text.RegularExpressions;

namespace spz {

	public class MouseWorkbench_Prompt : MonoBehaviour
	{
	    [SerializeField] bool _isPositivePrompt;
	    [SerializeField] ResizeRect_by_Text _resizeByText;
	    [SerializeField] TMP_InputField _inputField;
	    RectTransform _rectTransform;
	    float _initialHeight;
    
	    public static System.Action<string,bool> Act_onTextTyped { get; set; } = null;
	    bool _invoking_onTextTyped_now = false;

	    void Awake(){
	        _rectTransform = (RectTransform)transform;
	        _initialHeight = _rectTransform.rect.height;
	        _inputField.onValueChanged.AddListener(OnMyTextTyped);
	        _inputField.onValidateInput += OnValidateNewText;
	    }

	    void OnEnable(){
	        if(_isPositivePrompt){ _inputField.text  = StableDiffusion_Prompts_UI.instance?.positivePrompt ?? ""; }
	        if(!_isPositivePrompt){ _inputField.text = StableDiffusion_Prompts_UI.instance?.negativePrompt ?? ""; }
	        if(_resizeByText != null){//can be null during game start
	            _resizeByText.AdjustHeight_manual();
	        }
	        ColorText();
	    }

	    char OnValidateNewText(string text, int charIndex, char addedChar){
	        if(addedChar == '\t') { return '\0'; }
	        return addedChar;
	    }

	    public void OnMyTextTyped(string txt){
	        ColorText();
	        _invoking_onTextTyped_now = true;
	        Act_onTextTyped?.Invoke(txt, _isPositivePrompt);
	        _invoking_onTextTyped_now = false;
	    }

	    void ColorText(){
	        if(SD_Prompt_NounHighlighter.instance==null){ return; } //we are in Awake(), will hide soon.
	        string txt = _inputField.text;
	        string strippedText = StripColorTags(txt);

	        // Apply noun highlighting
	        string highlightedText = SD_Prompt_NounHighlighter.instance.HighlightNouns(strippedText, _isPositivePrompt);

	        int carPos = _inputField.caretPosition;//ignoring any existing rich-text tags.
	        int ix = 0;
	        for (int i=0; i < highlightedText.Length; ++i)
	        {
	            char c = highlightedText[i];
	            if(c == '<'){ i = highlightedText.IndexOf('>', i); continue; }
	            if(i == -1 ){ carPos = i; break; }
	            if(c == '>'){ continue; }
	            ix++;
	            if(ix == carPos){ carPos = i+1; break; }
	        }

	        // Update the input field text with highlighted version
	        _inputField.SetTextWithoutNotify(highlightedText);

	        //'stringPosition': without ignoring any existing rich-text tags (unlike caretPosition)
	        _inputField.stringPosition = carPos;
	    }

	    string StripColorTags(string text){
	        return Regex.Replace(text, @"<color[^>]*>|</color>", string.Empty);
	    }
	}
}//end namespace
