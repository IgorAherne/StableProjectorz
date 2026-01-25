using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// belongs to some IconUI element.
	// contains buttons for Hiding and Solo.
	// Those just enable the ui surfaces, stretched on top of the whole IconUI element.
	public class IconUI_HideSolo_Buttons : MonoBehaviour
	{
	    [SerializeField] IconUI _icon;
	    [SerializeField] Vector2Int _widthHeight;
	    [SerializeField] Vector2 _anchorPcnt;
	    [SerializeField] RectTransform _rt;
	    [SerializeField] Toggle _hideToggle;
	    [SerializeField] Toggle _soloToggle;
	    [Space(10)]
	    [SerializeField] GameObject _hidingSurface;//used when this icon is 'hidden' (user might want to temporarily exclude it from rendering)
	    [SerializeField] GameObject _soloSurface; //used when we have to be concealed because some other icon wants to be shown 'solo' (on its own)

	    public Action<bool> onHide_click { get; set; }
	    public Action<bool> onSolo_click { get; set; }

	    public bool is_HideOrSoloCover_showing => _hidingSurface.activeSelf || _soloSurface.activeSelf;
	    public MouseClickSensor_UI hidingSurfaceButton => _hidingSurface.GetComponent<MouseClickSensor_UI>();
	    public MouseClickSensor_UI soloSurfaceButton => _soloSurface.GetComponent<MouseClickSensor_UI>();


	    public void OnMyIcon_HideOrSolo(bool isIconHidden, bool isIconSoloSelf,  bool otherGroupRemains_asSolo){
	        _hidingSurface.SetActive( isIconHidden );
	        _soloSurface.SetActive( otherGroupRemains_asSolo );
	        bool anyCoverShown   = _hidingSurface.activeSelf || _soloSurface.activeSelf;
	    }

	    public void SetWithoutNotify(bool isRemainSolo, bool isRemainHidden){
	        _hideToggle.SetIsOnWithoutNotify(isRemainHidden);
	        _soloToggle.SetIsOnWithoutNotify(isRemainSolo);
	    }

	    public void OnAfterInstantiated(){
	        _hidingSurface.SetActive(false);
	        _soloSurface.SetActive(false);
	    }

	    // these buttons were hard to manage. Their size would collapse to (0,0) after Load.
	    // Therefore, doing it here.
	    void Update(){
	        _rt.anchorMin = _rt.anchorMax = _anchorPcnt;
	        _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _widthHeight.x);
	        _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _widthHeight.y);
	        bool keepVisible = _icon.isShowing_artContextMenu();
	            keepVisible |= _hideToggle.isOn;
	            keepVisible |= _soloToggle.isOn;
	        _rt.gameObject.SetActive(keepVisible);
	    }

	    void Awake(){
	        _hideToggle.onValueChanged.AddListener( isOn => onHide_click?.Invoke(isOn) );
	        _soloToggle.onValueChanged.AddListener( isOn => onSolo_click?.Invoke(isOn) );
	    }

	}
}//end namespace
