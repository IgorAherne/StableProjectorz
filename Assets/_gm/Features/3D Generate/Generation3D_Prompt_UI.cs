using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace spz {

	public class Generation3D_Prompt_UI : MonoBehaviour{

	    [SerializeField] protected TMP_InputField _txt_input;
	    [SerializeField] protected SD_PromptWord_WebFind _webFind;
	    [SerializeField] protected List<Toggle> _presetToggles;

	    bool _isPositive_prompt=true;//use Set_IsPositive() to initialize it

	    protected bool _txt_changed_thisFrame;
	    protected List<string> _prompts = new List<string>();

	    protected string _tooltips_hint = "<size=60%>Click: switch here.  CTRL+click: add to current.</size>\n";
	    protected bool _isLoading = false; //are we currently loading from a save-file.
	    int _recentToggle_ix = 0;

	    public string prompt => StripColorTags(_txt_input.text);


	    public void Set_IsPositive(bool isPositive){
	        _isPositive_prompt = isPositive;
	        _txt_changed_thisFrame = true;//will re-color on next LateUpdate.
	    }

	    public void PasteText(string newText){
	        newText = CleanNewlines(StripColorTags(newText));
	        _txt_input.text = SD_Prompt_NounHighlighter.instance.HighlightNouns(newText, true);
	        _txt_changed_thisFrame = true;//will re-color on next LateUpdate.
	    }


	    protected virtual void Awake(){
	        _txt_input.onValueChanged.AddListener((txt) => OnTextChanged(txt));
	        _txt_input.onValidateInput += OnValidateNewText;

	        for (int i=0; i<_presetToggles.Count; ++i) {
	            int i_cpy = i;
	            _presetToggles[i_cpy].onValueChanged.AddListener( (isOn)=>OnPresetToggle(i_cpy,isOn,true) );
	            _prompts.Add("");
	        }
	        _recentToggle_ix = _presetToggles.FindIndex(t=>t.isOn);
	    }

	    void Start(){
	        Settings_MGR._Act_onTextSize += OnChanged_textSize;
	        OnChanged_textSize( Settings_MGR.instance.get_getPromptTextSize() );
	    }

	    void OnChanged_textSize(int textSize){
	        _txt_input.pointSize = textSize;
	    }

	    char OnValidateNewText(string text, int charIndex, char addedChar){
	        if(addedChar=='\t'){  return '\0'; }//skip tab character
	        return addedChar;
	    }

	    //copy the text into the appropriate prompts list.
	    void OnTextChanged(string txt){
	        _txt_changed_thisFrame = true;
	    }


	    void LateUpdate(){
	        if(_txt_changed_thisFrame){
	            string currentNegText = _txt_input.text;
	            _txt_input.SetTextWithoutNotify( CleanNewlines(currentNegText) );
	            ColorText(_isPositive_prompt);
	        }
	        _txt_changed_thisFrame = false;
	        CopyIntoBuffer_maybe();
	        TAB_to_switch_prompts();
	    }

	    void CopyIntoBuffer_maybe(){//in late update.
	        if(EventSystem.current.currentSelectedGameObject != _txt_input.gameObject){ return; }
	        if(KeyMousePenInput.isKey_CtrlOrCommand_pressed()==false){ return; }
	        if(Input.GetKeyDown(KeyCode.C) == false){ return; }
	        TMP_InputFieldExtensions.CopySelectedText(_txt_input, StripColorTags);
	    }

	    void TAB_to_switch_prompts(){
	        // Handle tab switching between prompts
	        if(Input.GetKeyDown(KeyCode.Tab)==false){ return; }

	        //commented-out on 18 jan  2025 - we don't know if there is a sibling prompt yet.
	        // GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

	        //bool isPositive =  currentSelected == _positive_input.gameObject;
	        //bool isNegative =  currentSelected == _negative_input.gameObject;
	        //if (!isPositive && !isNegative){ return; }

	        //if(isPositive && _negative_input!=null){//swap  positive--> negative
	        //    _negative_input.Select();
	        //    _negative_input.ActivateInputField();
	        //    _negative_input.caretPosition = _negative_input.text.Length-1;
	        //}else{
	        //    _positive_input.Select();
	        //    _positive_input.ActivateInputField();
	        //    _positive_input.caretPosition =  _positive_input.text.Length-1;
	        //}
	    }


	    //invoked at the end of the Update
	    void ColorText(bool isPositive){
	        int ixOfActive = _presetToggles.FindIndex(t => t.isOn);

	        string txt = _txt_input.text;
	        string strippedText = StripColorTags(txt);

	        // Store the stripped text (without any tags) in prompts
	        _prompts[ixOfActive] = strippedText;

	        // Apply noun highlighting
	        string highlightedText = SD_Prompt_NounHighlighter.instance.HighlightNouns(strippedText, isPositive);

	        int carPos = _txt_input.caretPosition; // Ignoring any existing rich-text tags.
	        int ix = 0;
	        for(int i=0; i<highlightedText.Length; ++i){
	            char c = highlightedText[i];
	            if(c == '<'){
	                int tagEnd = highlightedText.IndexOf('>', i);
	                if(tagEnd != -1){
	                    string tag = highlightedText.Substring(i, tagEnd - i + 1);
	                    if(tag.StartsWith("<color=") || tag == "</color>"){
	                        i = tagEnd;
	                        continue;
	                    }
	                }
	            }
	            ix++;
	            if(ix == carPos){
	                carPos = i + 1;
	                break;
	            }
	        }
	        // Update the input field text with highlighted version
	        _txt_input.SetTextWithoutNotify(highlightedText);

	        // 'stringPosition': without ignoring any existing rich-text tags (unlike caretPosition)
	        _txt_input.stringPosition = carPos;

	        string msg = _tooltips_hint + (string.IsNullOrEmpty(_prompts[ixOfActive]) ? "." : _prompts[ixOfActive]);
	        msg = TooltipsPrettier(msg);
	        _presetToggles[ixOfActive].GetComponent<CanShowTooltip_UI>()?.set_overrideMessage(msg);
	    }


	    public static string StripColorTags(string text){
	        return Regex.Replace(text, @"<color[^>]*>|</color>", string.Empty);
	    }


	    protected void OnPresetToggle(int ix, bool isOn, bool isPositive){
	        if(!isOn){ return; }

	        _txt_input.Select();//important, otherwise selection changes onto the toggle.

	        _txt_changed_thisFrame = true;//will re-color on next LateUpdate.

	        if (KeyMousePenInput.isKey_CtrlOrCommand_pressed()==false || _isLoading){
	            _txt_input.SetTextWithoutNotify(_prompts[ix]);
	            _recentToggle_ix = ix; 
	            return;
	        }//otherwise, append inside the prompt, where the cursor is:

	        // Make sure the previous toggle remains enabled, despite that we clicked the ix:
	        _presetToggles[_recentToggle_ix].SetIsOnWithoutNotify(true);
	        _presetToggles[ix].SetIsOnWithoutNotify(false);

	        string currText   = StripColorTags(_txt_input.text);
	        int caretPosition = _txt_input.caretPosition;

	        // Insert the prompt text at the current caret position
	        _txt_input.text = currText.Insert(caretPosition, " " + _prompts[ix]);
	        _txt_input.caretPosition = caretPosition + _prompts[ix].Length + 1;
	    }


	    //ensures there are new lines if any line is too long.
	    protected string TooltipsPrettier(string text){
	        string result = "";
	        int lineLength = 0;

	        foreach (string word in text.Split(' ')){
	            if (lineLength + word.Length >= 100){
	                result += "\n";
	                lineLength = 0;
	            }
	            result += word + " ";
	            lineLength += word.Length + 1;
	        }
	        return result;
	    }


	    // Removes repeated newlines and trims whitespace so we don't store or load large blank sections.
	    // Dec 2024: user showed a .spz save file with huge amount of consecutive \r\n  which lagged his project.
	    protected static string CleanNewlines(string text){
	        if (string.IsNullOrEmpty(text)){ return text; }
	        text = Regex.Replace(text, @"[\r\n]+", "\n");// Replace consecutive \r or \n with a single \n
	        return text;
	    }
	}
}//end namespace
