using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

namespace spz {

	public enum DimensionMode{
	    dim_uv, // texture-coordinate inspection
	    dim_sd, // stable diffusion texturing
	    dim_gen_3d, // 3d generation (Trellis, etc)
	}

	public class DimensionMode_MGR : MonoBehaviour{
	    public static DimensionMode_MGR instance { get; private set; } = null;

	    [SerializeField] Animation _mainChoice_anim;
	    [SerializeField] MouseHoverSensor_UI _mainChoiceHoverSurf;
	    [SerializeField] TextMeshProUGUI _mainChoice_text;
	    [Space(10)]
	    [SerializeField] Animator _choicesPanel_anim;
	    [SerializeField] RectTransform _choicesPanel_rectTransf;
	    [Space(10)]
	    [SerializeField] float _choiceHover_AnimSpeed = 15;
	    [Space(10)]
	    [SerializeField] Button _3d_choice_button;
	    [SerializeField] MouseHoverSensor_UI _3d_choice_sensor;
	    [Space(10)]
	    [FormerlySerializedAs("_2d_choice_button")][SerializeField] Button _sd_choice_button;
	    [SerializeField] MouseHoverSensor_UI _2d_choice_sensor;
	    [Space(10)]
	    [SerializeField] Button _uv_choice_button;
	    [SerializeField] MouseHoverSensor_UI _uv_choice_sensor;
	    [Space(10)]
	    [SerializeField] Button _bg_choice_button;
	    [SerializeField] MouseHoverSensor_UI _bg_choice_sensor;
	    [Space(10)]
	    [SerializeField] Color _inactiveColor = new Color(0.59f, 0.54f, 0.63f, 1);
	    [SerializeField] Color _activeColor = Color.white;

	    Vector3 _choice_originalScale;

	    bool _ishowingChoicePanel;

	    Coroutine _showHidePanel_crtn = null;

	    public static Action<DimensionMode> _Act_OnDimensionChanged { get; set; } = null;


	    public DimensionMode _dimensionMode { get; private set; } = DimensionMode.dim_sd;

	    //true if camera is around to fly around the 3D scene, or false if should remain at the same location.
	    public bool is_3d_navigation_allowed => _dimensionMode != DimensionMode.dim_uv;


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;
	        var sensor = _3d_choice_button.GetComponentInParent<MouseHoverSensor_UI>();
	        sensor.onSurfaceEnter += p=>OnSurfaceEnter(_3d_choice_button, p);
	        sensor.onSurfaceExit += p=>OnSurfaceExit(_3d_choice_button, p);
	        _3d_choice_button.onClick.AddListener( ()=>OnButtonPressed(_3d_choice_button) );

	        sensor = _sd_choice_button.GetComponentInParent<MouseHoverSensor_UI>();
	        sensor.onSurfaceEnter += p=>OnSurfaceEnter(_sd_choice_button, p);
	        sensor.onSurfaceExit += p=>OnSurfaceExit(_sd_choice_button, p);
	        _sd_choice_button.onClick.AddListener( ()=>OnButtonPressed(_sd_choice_button) );

	        sensor = _uv_choice_button.GetComponentInParent<MouseHoverSensor_UI>();
	        sensor.onSurfaceEnter += p=>OnSurfaceEnter(_uv_choice_button, p);
	        sensor.onSurfaceExit += p=>OnSurfaceExit(_uv_choice_button, p);
	        _uv_choice_button.onClick.AddListener( ()=>OnButtonPressed(_uv_choice_button) );

	        sensor = _bg_choice_button.GetComponentInParent<MouseHoverSensor_UI>(includeInactive:true);
	        sensor.onSurfaceEnter += p=>OnSurfaceEnter(_bg_choice_button, p);
	        sensor.onSurfaceExit += p=>OnSurfaceExit(_bg_choice_button, p);
	        _bg_choice_button.onClick.AddListener( ()=>OnButtonPressed(_bg_choice_button) );
        
	        _choice_originalScale = _3d_choice_button.transform.parent.localScale;
	        _choicesPanel_anim.SetBool("ShowPanel", false);
	    }

	    void Start(){
	        _Act_OnDimensionChanged?.Invoke(_dimensionMode);
	    }


	    void OnButtonPressed(Button but){
	        var img = but.GetComponent<Image>();

	        _3d_choice_button.GetComponent<Image>().color = _inactiveColor;
	        _sd_choice_button.GetComponent<Image>().color = _inactiveColor;
	        _uv_choice_button.GetComponent<Image>().color = _inactiveColor;
	        _bg_choice_button.GetComponent<Image>().color = _inactiveColor;

	        string msg = "";
	        if(but == _3d_choice_button){ 
	            _dimensionMode = DimensionMode.dim_gen_3d; _mainChoice_text.text = "3D";
	            msg = "3d Generation Mode";
	        }
	        if(but == _sd_choice_button){ 
	            _dimensionMode = DimensionMode.dim_sd; _mainChoice_text.text = "SD";
	            msg = "Stable Diffusion Texturing Mode";
	        } //t for 'textures'
	        if(but == _uv_choice_button){ 
	            _dimensionMode = DimensionMode.dim_uv; _mainChoice_text.text = "UV";
	            msg = "Inspect Texture Coords Mode"; //don't explain. Self evident and avoids distraction.
	        }
	        if (string.IsNullOrEmpty(msg) == false){
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 3, false);
	        }
	        _mainChoice_anim.Play();
	        img.color = Color.white;
	        _Act_OnDimensionChanged?.Invoke(_dimensionMode);
	    }


	    void Update(){
	        if (_ishowingChoicePanel){
	            Vector2 mousePos  = KeyMousePenInput.cursorScreenPos();
	            bool panelHovered = RectTransformUtility.RectangleContainsScreenPoint(_choicesPanel_rectTransf, mousePos);
	            if(_mainChoiceHoverSurf.isHovering==false && !panelHovered){
	                _ishowingChoicePanel = false;
	                _choicesPanel_anim.SetBool("ShowPanel", false);
	                _showHidePanel_crtn = StartCoroutine(ShowHidePanel_crtn(hide: false));
	            }
	            ScaleChoice_ifHovered(_3d_choice_button.transform.parent, _3d_choice_sensor);
	            ScaleChoice_ifHovered(_sd_choice_button.transform.parent, _2d_choice_sensor);
	            ScaleChoice_ifHovered(_uv_choice_button.transform.parent, _uv_choice_sensor);
	            ScaleChoice_ifHovered(_bg_choice_button.transform.parent, _bg_choice_sensor);
	        }
	        else{//not showing, check if should show:
	            if(_mainChoiceHoverSurf.isHovering){
	                if(_showHidePanel_crtn!=null){ StopCoroutine(_showHidePanel_crtn); }
	                _showHidePanel_crtn  = StartCoroutine(ShowHidePanel_crtn(hide:false));
	                _ishowingChoicePanel = true;
	                _choicesPanel_anim.SetBool("ShowPanel", true);
	            }
	        }
	    }


	    IEnumerator ShowHidePanel_crtn(bool hide){
	        _choicesPanel_rectTransf.gameObject.SetActive(true);
	        yield return new WaitForSeconds(0.4f);
	        if (hide){
	            _choicesPanel_rectTransf.gameObject.SetActive(false);
	        }
	        _showHidePanel_crtn = null;
	    }


	    void ScaleChoice_ifHovered(Transform transf, MouseHoverSensor_UI sensor){
	        Vector3 targScale = sensor.isHovering ? _choice_originalScale*1.25f : _choice_originalScale;
	        float factor = Time.deltaTime * _choiceHover_AnimSpeed;
	        transf.localScale =  Vector3.Lerp(transf.localScale, targScale, factor);
	    }

	    void OnSurfaceEnter(Button but, PointerEventData p){

	    }

	    void OnSurfaceExit(Button but, PointerEventData p){

	    }
	}
}//end namespace
