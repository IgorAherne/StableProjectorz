using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class InpaintPanel_AH_ButtonColor_UI : MonoBehaviour
	{
	    [SerializeField] CollapsableSection_UI _collapsable;
	    [SerializeField] Color _colorClosed;
	    [SerializeField] Color _colorOpen;
	    [SerializeField] float _lerpSpeed = 6;
	    [SerializeField] Image _image;

	    float _lerp01 = 0;

	    public void Update(){
	        float dir = _collapsable._isExpanded? 1 : -1;
	        _lerp01  += dir * _lerpSpeed * Time.deltaTime;
	        _lerp01   = Mathf.Clamp01(_lerp01);
	        _image.color = Color.Lerp(_colorClosed, _colorOpen, _lerp01);
	    }
	}
}//end namespace
