using System.Collections;
using System.Collections.Generic;
using System.Web;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace spz {

	public class SD_PromptWord_WebFind : MonoBehaviour{

	    [SerializeField] Button _button;
	    [SerializeField] Image _image;
	    [SerializeField] Sprite _inactiveSprite;
	    [SerializeField] Sprite _activeSprite;
	    [Space(10)]
	    [SerializeField] TMP_InputField _myPrompt;

	    string _latestSelected_text = "";

	    void OnButtonClicked(){
	        if(_latestSelected_text == ""){ return; }
	        string encodedQuery = _latestSelected_text.Replace(" ", "+");
	        string url = $"https://www.google.com/search?q={encodedQuery}&tbm=isch";
	        Application.OpenURL(url);
	    }

	    void Update(){
	        string highlighted = GetHighlightedText();
	        _image.sprite =  highlighted!=""? _activeSprite : _inactiveSprite;

	        // Only update the latest-selected text, if highlighted isn't "".
	        // UI is polled at different framerate than Update. 
	        // Therefore wa always want to "remember" the latest-selected-text just in case:
	        _latestSelected_text =  highlighted!="" ? highlighted : _latestSelected_text;
	    }

	    string GetHighlightedText(){
	        if(!_myPrompt.isFocused){ return ""; }
	        int selectionStart = _myPrompt.selectionStringAnchorPosition;
	        int selectionEnd   = _myPrompt.selectionStringFocusPosition;

	        // Ensure selectionStart is always the lower index
	        if (selectionStart > selectionEnd){
	            int temp = selectionStart;
	            selectionStart = selectionEnd; 
	            selectionEnd = temp;
	        }
	        // Check if there's any text selected
	        if (selectionStart == selectionEnd){ return ""; }
        
	        string txt = _myPrompt.text.Substring(selectionStart, selectionEnd-selectionStart);
	        return StableDiffusion_Prompts_UI.StripColorTags( txt );
	    }


	    void Awake(){
	        _button.onClick.AddListener( OnButtonClicked );
	        _image.sprite = _inactiveSprite;
	    }

	}
}//end namespace
