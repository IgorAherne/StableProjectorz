using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace spz {

	public class ResizeRect_by_Text : MonoBehaviour
	{
	    [SerializeField] bool _isPositivePrompt;
	    [SerializeField] int _minLines = 1;
	    [SerializeField] int _maxLines = 6;
	    [SerializeField] float _extraPadding = 10f;
	    [SerializeField] TMP_Text _text;
	    [Space(10)]
	    [SerializeField] RectTransform _ZeroPos_all_childrenOfThis; //needed if our text is part of input field. Null otherwise

	    RectTransform _rectTransform;
	    float _initialHeight;
    
	    public static System.Action<string,bool> Act_onTextTyped { get; set; } =null; //text,isPositive
	    bool _invoking_onTextTyped_now = false;//helps avoid infinite recursion.


	    public void OnMyTextTyped(string txt){
	        _invoking_onTextTyped_now = true;
	        Act_onTextTyped?.Invoke(txt, _isPositivePrompt);
	        _invoking_onTextTyped_now = false;
	    }


	    public void AdjustHeight_manual() => AdjustHeight();



	    void AdjustHeight(){ //invoked every frame from here
	        if(_text==null){ return; }
	        if(_rectTransform==null){ return; }//awake didn't run for us yet.

	        _text.ForceMeshUpdate();
	        var textInfo = _text.textInfo;

	        float lineHeight = textInfo.lineCount > 0 ? textInfo.lineInfo[0].lineHeight : _text.fontSize * _text.lineSpacing;
	        int lineCount = Mathf.Clamp(textInfo.lineCount, _minLines, _maxLines);
	        float newHeight = lineCount * lineHeight + _extraPadding;

	        if (_rectTransform.sizeDelta.y == newHeight){ return; }

	        _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, newHeight);

	        if (_rectTransform.pivot.y > 0.5f){
	            float heightDifference = newHeight - _initialHeight;
	            _rectTransform.anchoredPosition = new Vector2( 
	                _rectTransform.anchoredPosition.x,
	                _rectTransform.anchoredPosition.y - heightDifference * (1 - _rectTransform.pivot.y)
	            );
	        }
	        // Make sure caret and th etext are centered
	        // (when typing, text might snap down by one line which is quite annoying).
	        // This helps to ensure it remains centered:
	        for(int i =0; i<_ZeroPos_all_childrenOfThis.childCount; i++){
	            var rtf = _ZeroPos_all_childrenOfThis.GetChild(i).transform as RectTransform;
	            rtf.anchoredPosition = Vector3.zero;
	        }
	    }


	    void Update(){//adjust height inside Update, not inside callback. Else results 
	        AdjustHeight();//in slight jitter when creating a new line (possible due to order of events)
	    }

	    void Awake(){
	        _rectTransform = (RectTransform)transform;
	        _initialHeight = _rectTransform.rect.height;
	    }

	    void OnEnable(){
	        AdjustHeight();
	    }

	    char OnValidateNewText(string text, int charIndex, char addedChar){
	        if(addedChar=='\t'){  return '\0'; }//skip tab character
	        return addedChar;
	    }
	}
}//end namespace
