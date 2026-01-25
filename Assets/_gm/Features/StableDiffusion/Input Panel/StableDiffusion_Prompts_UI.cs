using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;

namespace spz {

	public class StableDiffusion_Prompts_UI : TextPrompts_UI{
	    public static StableDiffusion_Prompts_UI instance { get; private set; } = null;

	    public void Save( SD_GenSettingsInput_UI fill_this ){
	        // 1) Clean & store the main prompts
	        fill_this.positivePrompt = CleanNewlines( StripColorTags(_positive_input.text) );
	        fill_this.negativePrompt = _negative_input!=null ? CleanNewlines( StripColorTags(_negative_input.text) ) : "";

	        fill_this.positivePromptPresets = new List<string>();
	        fill_this.negativePromptPresets = new List<string>();

	        // 2) Clean & store each preset
	        for(int i=0; i<_positive_prompts.Count; ++i){
	            fill_this.positivePromptPresets.Add( CleanNewlines(_positive_prompts[i]) );
	        }

	        if (_negative_input != null){
	            for(int i=0; i<_negative_prompts.Count; ++i){
	                fill_this.negativePromptPresets.Add( CleanNewlines(_negative_prompts[i]) );
	            }
	        }

	        fill_this.positivePromptPreset_ix = _positive_presetToggles.FindIndex(t => t.isOn);
	        fill_this.negativePromptPreset_ix = _negative_presetToggles.FindIndex(t => t.isOn);
	    }

	    public void Load(SD_GenSettingsInput_UI fromThis){
	        _isLoading = true;

	        // 1) Make sure we clean them here too, in case the .spz had many newlines
	        fromThis.positivePrompt = CleanNewlines(fromThis.positivePrompt);
	        fromThis.negativePrompt = CleanNewlines(fromThis.negativePrompt);
	        for(int i=0; i<fromThis.positivePromptPresets.Count; i++){
	            fromThis.positivePromptPresets[i] = CleanNewlines(fromThis.positivePromptPresets[i]);
	        }
	        for(int i=0; i<fromThis.negativePromptPresets.Count; i++){
	            fromThis.negativePromptPresets[i] = CleanNewlines(fromThis.negativePromptPresets[i]);
	        }

	        _positive_input.text = SD_Prompt_NounHighlighter.instance.HighlightNouns(fromThis.positivePrompt, true);
	        if(_negative_input!=null){
	            _negative_input.text = SD_Prompt_NounHighlighter.instance.HighlightNouns(fromThis.negativePrompt, false);
	        }
	        _posChanged_thisFrame = _negChanged_thisFrame = true;//will re-color on next LateUpdate.

	        _positive_prompts.Clear();
	        fromThis.positivePromptPresets.ForEach( p => _positive_prompts.Add(p) );
	        int notEnoughPositive = fromThis.positivePromptPresets.Count - _positive_prompts.Count;
	        for(int i=0; i<notEnoughPositive; ++i){ _positive_prompts.Add(""); }

	        if (_negative_input != null){ 
	            int notEnoughNegative = fromThis.negativePromptPresets.Count - _negative_prompts.Count;
	            _negative_prompts.Clear();
	            fromThis.negativePromptPresets.ForEach( p => _negative_prompts.Add(p) );
	            for(int i=0; i<notEnoughNegative; ++i){ _negative_prompts.Add(""); }
	        }

	        int pos_ix = Mathf.Min(fromThis.positivePromptPreset_ix, _positive_presetToggles.Count-1);
	        int neg_ix = Mathf.Min(fromThis.negativePromptPreset_ix, _negative_presetToggles.Count-1);
	        LoadPresets(fromThis, pos_ix, neg_ix);

	        //manually invoke callback, in case if the toggle was already on, and wouldn't invoke callback.
	        OnPresetToggle(pos_ix, isOn:true, isPositive:true);
	        OnPresetToggle(neg_ix, isOn:false, isPositive:false);
	        //ensure the tooltips match what's in the presets:
	        LoadTooltips(_positive_presetToggles, _positive_prompts);
	        if(_negative_input != null){  LoadTooltips(_negative_presetToggles, _negative_prompts); }

	        _isLoading = false;
	    }


	    void LoadPresets(SD_GenSettingsInput_UI fromThis, int pos_ix, int neg_ix){
	        _positive_presetToggles[pos_ix].isOn = true;
	        _negative_presetToggles[neg_ix].isOn = true;
	        // Manually set remaining toggles as off if we are Inactive right now,
	        // and ToggleGroup won't update them yet.
	        for (int i=0; i<_positive_presetToggles.Count-1; ++i){
	            _positive_presetToggles[i].isOn = i==pos_ix;
	        }
	        if(_negative_input != null){ 
	            for (int i=0; i<_negative_presetToggles.Count-1; ++i){
	                _negative_presetToggles[i].isOn = i==pos_ix;
	            }
	        }
	    }

	    void LoadTooltips(List<Toggle> toggles, List<string> prompts){
	        for(int i=0; i<toggles.Count; ++i){
	            string msg =  _tooltips_hint + (string.IsNullOrEmpty(prompts[i])?"." : prompts[i]);
	            msg = TooltipsPrettier(msg);
	            toggles[i].GetComponent<CanShowTooltip_UI>().set_overrideMessage(msg);
	        }
	    }

	    protected override void Awake(){
	        if (instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	        base.Awake();
	    }
	}


	public class TextPrompts_UI : MonoBehaviour{

	    [SerializeField] protected TMP_InputField _positive_input;
	    [SerializeField] protected TMP_InputField _negative_input;
	    [SerializeField] protected SD_PromptWord_WebFind _webFind_positive;
	    [SerializeField] protected SD_PromptWord_WebFind _webFind_negative;
	    [SerializeField] protected List<Toggle> _positive_presetToggles;
	    [SerializeField] protected List<Toggle> _negative_presetToggles;
    
	    protected string _tooltips_hint = "<size=60%>Click: switch here.  CTRL+click: add to current.</size>\n";
	    protected bool _isLoading = false; //are we currently loading from a save-file.
	    int _recentPositiveToggle_ix = 0;
	    int _recentNegativeToggle_ix = 0;

	    protected bool _posChanged_thisFrame;
	    protected bool _negChanged_thisFrame;

	    protected List<string> _positive_prompts = new List<string>();
	    protected List<string> _negative_prompts = new List<string>();

	    public string positivePrompt => StripColorTags(_positive_input.text);
	    public string negativePrompt => StripColorTags(_negative_input.text);

	    public Action<string,bool> Act_onTextTyped { get; set; } = null;//message,isPositive
	    bool _invoking_onTextTyped_now = false;//helps avoid infinite recursion.
    

	    protected virtual void Awake(){
	        MouseWorkbench_Prompt.Act_onTextTyped += OnViewportContextPromptChanged;

	        _positive_input.onValueChanged.AddListener((txt) => OnTextChanged(txt, true));
	        _positive_input.onValidateInput += OnValidateNewText;

	        if (_negative_input != null){ 
	            _negative_input.onValueChanged.AddListener((txt) => OnTextChanged(txt, false));
	            _negative_input.onValidateInput += OnValidateNewText;
	        }

	        for (int i=0; i<_positive_presetToggles.Count; ++i) {
	            int i_cpy = i;
	            _positive_presetToggles[i_cpy].onValueChanged.AddListener( (isOn)=>OnPresetToggle(i_cpy,isOn,true) );
	            _positive_prompts.Add("");
	        }
	        for(int i=0; i<_negative_presetToggles.Count; ++i){
	            if(_negative_input==null){ break; }
	            int i_cpy = i;
	            _negative_presetToggles[i_cpy].onValueChanged.AddListener( (isOn)=>OnPresetToggle(i_cpy,isOn,false) );
	            _negative_prompts.Add("");
	        }

	        _recentPositiveToggle_ix = _positive_presetToggles.FindIndex(t=>t.isOn);
	        _recentNegativeToggle_ix = _negative_presetToggles.FindIndex(t=>t.isOn);
	    }

	    void Start(){
	        Settings_MGR._Act_onTextSize += OnChanged_textSize;
	        OnChanged_textSize( Settings_MGR.instance.get_getPromptTextSize() );
	    }

	    void OnChanged_textSize(int textSize){
	        _positive_input.pointSize = textSize;
	        if(_negative_input != null){ 
	            _negative_input.pointSize = textSize;
	        }
	    }

	    char OnValidateNewText(string text, int charIndex, char addedChar){
	        if(addedChar=='\t'){  return '\0'; }//skip tab character
	        return addedChar;
	    }

	    void OnViewportContextPromptChanged(string txt, bool isPositive){
	        if(_invoking_onTextTyped_now){ return; }
	        //user adjusted contents of Prompt inside MainViewport context menu.
	        //make sure we copy its value to self.
	        if (isPositive){
	            _positive_input.text = txt;
	        }else{ 
	            if(_negative_input!=null){  _negative_input.text = txt;  }
	        }
	    }

	    //copy the text into the appropriate prompts list.
	    void OnTextChanged(string txt, bool isPositive){
	        if (isPositive) { _posChanged_thisFrame = true; }
	        if (!isPositive){
	            _negChanged_thisFrame = true; 
	        }
	    }


	    void LateUpdate(){
	        if(_posChanged_thisFrame){
	            string currentNegText = _positive_input.text;
	            _positive_input.SetTextWithoutNotify( CleanNewlines(currentNegText) );
	            ColorText(isPositive:true);
	        }
	        if(_negChanged_thisFrame && _negative_input!=null){ 
	            string currentNegText = _negative_input.text;
	            _negative_input.SetTextWithoutNotify( CleanNewlines(currentNegText) );
	            ColorText(isPositive:false);
	        }
	        _posChanged_thisFrame = false;
	        _negChanged_thisFrame = false;
	        CopyIntoBuffer_maybe();
	        TAB_to_switch_prompts();
	    }

	    void CopyIntoBuffer_maybe(){//in late update.
	        TMP_InputField input = null;
	        if(EventSystem.current.currentSelectedGameObject == _positive_input.gameObject){  input = _positive_input; }
	        if(_negative_input != null){ 
	            if(EventSystem.current.currentSelectedGameObject == _negative_input.gameObject){  input = _negative_input; }
	        }
	        if(input!=null  &&  KeyMousePenInput.isKey_CtrlOrCommand_pressed() && Input.GetKeyDown(KeyCode.C)){
	            TMP_InputFieldExtensions.CopySelectedText(input, StripColorTags);
	        }
	    }

	    void TAB_to_switch_prompts(){
	        // Handle tab switching between prompts
	        if(Input.GetKeyDown(KeyCode.Tab)==false){ 
	            return; }

	        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

	        bool isPositive =  currentSelected == _positive_input.gameObject;
	        bool isNegative =  currentSelected == _negative_input.gameObject;
	        if (!isPositive && !isNegative){ return; }

	        if(isPositive && _negative_input!=null){//swap  positive--> negative
	            _negative_input.Select();
	            _negative_input.ActivateInputField();
	            _negative_input.caretPosition = _negative_input.text.Length-1;
	        }else{
	            _positive_input.Select();
	            _positive_input.ActivateInputField();
	            _positive_input.caretPosition =  _positive_input.text.Length-1;
	        }
	    }

	    //invoked at the end of the Update
	    void ColorText(bool isPositive){
	        TMP_InputField inputField = isPositive ? _positive_input : _negative_input;
	        if(inputField == null){ return; }//for example, might not be using a negative ui prompt at alll

	        List<Toggle> toggles = isPositive ? _positive_presetToggles : _negative_presetToggles;
	        List<string> prompts = isPositive ? _positive_prompts : _negative_prompts;
	        int ixOfActive = toggles.FindIndex(t => t.isOn);

	        string txt = inputField.text;
	        string strippedText = StripColorTags(txt);

	        // Store the stripped text (without any tags) in prompts
	        prompts[ixOfActive] = strippedText;

	        // Apply noun highlighting
	        string highlightedText = SD_Prompt_NounHighlighter.instance.HighlightNouns(strippedText, isPositive);

	        int carPos = inputField.caretPosition; // Ignoring any existing rich-text tags.
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
	        inputField.SetTextWithoutNotify(highlightedText);

	        // 'stringPosition': without ignoring any existing rich-text tags (unlike caretPosition)
	        inputField.stringPosition = carPos;

	        string msg = _tooltips_hint + (string.IsNullOrEmpty(prompts[ixOfActive]) ? "." : prompts[ixOfActive]);
	        msg = TooltipsPrettier(msg);
	        toggles[ixOfActive].GetComponent<CanShowTooltip_UI>()?.set_overrideMessage(msg);

	        _invoking_onTextTyped_now = true;
	        Act_onTextTyped?.Invoke(prompts[ixOfActive], isPositive);
	        _invoking_onTextTyped_now = false;
	    }

	    public static string StripColorTags(string text){
	        return Regex.Replace(text, @"<color[^>]*>|</color>", string.Empty);
	    }

	    protected void OnPresetToggle(int ix, bool isOn, bool isPositive){
	        if(!isOn){ return; }

	        TMP_InputField input = isPositive ? _positive_input : _negative_input;
	        List<string> prompts = isPositive ? _positive_prompts : _negative_prompts;
	        List<Toggle> toggles = isPositive ? _positive_presetToggles : _negative_presetToggles;
	        int recentIx = isPositive ? _recentPositiveToggle_ix : _recentNegativeToggle_ix;

	        input.Select();//important, otherwise selection changes onto the toggle.

	        _posChanged_thisFrame = _negChanged_thisFrame = true;//will re-color on next LateUpdate.

	        if (KeyMousePenInput.isKey_CtrlOrCommand_pressed()==false || _isLoading){
	            input.SetTextWithoutNotify(prompts[ix]);
	            if(isPositive){ _recentPositiveToggle_ix = ix; }
	            else{ _recentNegativeToggle_ix = ix; }
	            return;
	        }//otherwise, append inside the prompt, where the cursor is:

	        // Make sure the previous toggle remains enabled, despite that we clicked the ix:
	        toggles[recentIx].SetIsOnWithoutNotify(true);
	        toggles[ix].SetIsOnWithoutNotify(false);

	        string currText   = StripColorTags(input.text);
	        int caretPosition = input.caretPosition;

	        // Insert the prompt text at the current caret position
	        input.text = currText.Insert(caretPosition, " " + prompts[ix]);
	        input.caretPosition = caretPosition + prompts[ix].Length + 1;
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


	public static class TMP_InputFieldExtensions{
	    public static void CopySelectedText(TMP_InputField inputField, System.Func<string,string> stripColorTagsFunc){
	        if (inputField == null) return;

	        // These are already the "string" (plain text) positions, ignoring rich text.
	        int startPos = inputField.selectionStringAnchorPosition;
	        int endPos   = inputField.selectionStringFocusPosition;

	        // Put them in ascending order.
	        if (startPos > endPos){
	            int temp = startPos;
	            startPos = endPos;
	            endPos   = temp;
	        }

	        // Clamp them to the valid range
	        startPos = Mathf.Clamp(startPos, 0, inputField.text.Length);
	        endPos   = Mathf.Clamp(endPos, 0, inputField.text.Length);

	        string toCopy;
	        if (startPos < endPos){
	            // The user has a highlighted selection.
	            string rawSelection = inputField.text.Substring(startPos, endPos - startPos);
	            toCopy = stripColorTagsFunc(rawSelection);
	        }
	        else{
	            // No highlighted substring; optionally copy entire text or do nothing.
	            toCopy = stripColorTagsFunc(inputField.text);
	        }

	        // Copy result to system clipboard
	        // Option 1: Directly set system clipboard in Unity:
	        GUIUtility.systemCopyBuffer = toCopy;
	    }
	}
}//end namespace
