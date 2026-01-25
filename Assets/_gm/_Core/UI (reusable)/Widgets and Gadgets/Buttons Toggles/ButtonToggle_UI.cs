using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace spz {

	public class ButtonToggle_UI : MonoBehaviour
	{
	    [SerializeField] Button _button;
	    [SerializeField] Image _icon;
	    [SerializeField] Sprite _pressedIcon;
	    [SerializeField] Sprite _unpressedIcon;
	    [FormerlySerializedAs("_beginPressed")][SerializeField] bool _isPressed = false;
	    [Space(10)]
	    [SerializeField] List<Image> _childIcons_toTint = new List<Image>();//will darken those when pressed
	    [SerializeField] float _tintVal_when_pressed = 0.7f;
	    [SerializeField] float _tintVal_when_unpressed = 1.0f;
    
	    public bool isPressed => _isPressed;

	    public System.Action<bool> onClick { get; set; } = null;


	    void Start(){
	        _button.onClick.AddListener( ()=>OnButton(!_isPressed) );
	        UpdateSprites();
	    }

	    void UpdateSprites(){
	        if(_icon == null){ return; }
	        _icon.sprite = _isPressed? _pressedIcon : _unpressedIcon;
	        foreach(Image img in _childIcons_toTint){
	            float h, s, v;
	            Color.RGBToHSV(img.color, out h, out s, out v);
	            v = _isPressed ? _tintVal_when_pressed : _tintVal_when_unpressed;
	            img.color = Color.HSVToRGB(h, s, v);
	        }
	    }

	    void OnButton(bool isPressed){
	        _isPressed = isPressed;
	        UpdateSprites();
	        onClick?.Invoke(isPressed);
	    }

	    public void ForceSameValueAs(bool isPressed){
	        if(_isPressed==isPressed){ return; }
	        OnButton(isPressed);
	    }

	    public void SetValueWithoutNotify(bool isPressed){
	        _isPressed = isPressed;
	        UpdateSprites();
	    }
	}
}//end namespace
