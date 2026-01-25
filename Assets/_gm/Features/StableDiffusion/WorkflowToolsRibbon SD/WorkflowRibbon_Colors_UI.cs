using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace spz {

	public class WorkflowRibbon_Colors_UI : MonoBehaviour, IWorkflowModeToggle{
	    [SerializeField] Toggle _toggle;
	    [SerializeField] Animation _anim;
	    [SerializeField] SlideOut_Widget_UI _options_slideOut;
	    [SerializeField] MouseHoverSensor_UI _options_mouseHover;
	    [Space(10)]
	    [SerializeField] Button _bakeColors_button;//to extract brushed paint into a separate icon.
    
	    public bool isOn => _toggle.isOn;
	    public Action<bool> onValueChanged { get; set; } = null;
	    public Action onBakeColors_button { get; set; } = null;


	    bool _isDoingCallback = false;

	    float _next_hintTime  = 0;
	    int _num_hintsShown = 0;
	    int _hints_spacing = 15;

	    static int _latestHintShown_frame = 0;
	    public static bool didShowHint_thisFrame(){ return _latestHintShown_frame==Time.frameCount;}


	    public void EnableToggle(bool playAttentionAnim =false){
	        _toggle.isOn = true;
	        if(playAttentionAnim){ _anim.Play(); }
	        ShowHint_maybe();
	    }

	    void ShowHint_maybe(){
	        if(Time.time < _next_hintTime){ return; }
	        if(_num_hintsShown > 3){ return; }
	        if(DimensionMode_MGR.instance._dimensionMode != DimensionMode.dim_sd){ return; }
	        string msg = "Color-Inpaint:  GenArt will respect the colors according to the Re-do slider." +
	                     "\nRight click for color pallete.  Alt+Click to sample a color.  1,2,3 etc for Brush Strength.";
	        Viewport_StatusText.instance.ShowStatusText(msg, false, 6, false);
	        _num_hintsShown++;
	        _next_hintTime = Time.time + _hints_spacing*_num_hintsShown;
	        _latestHintShown_frame = Time.frameCount;
	    }

	    void OnValueChanged(bool isOn){
	        if(_isDoingCallback){ return; }//avoid recursion
	        _isDoingCallback = true;
	        onValueChanged?.Invoke(isOn);
	        _isDoingCallback = false;
	    }

	    void OnButton_BakeColors(){
	        onBakeColors_button?.Invoke();
	    }


	    // User toggled a setting to change the arrangement of the UI panels.
	    // We need to adjust the slide-outs, so that they still make sense:
	    void OnSettings_ToolRibbonSwapped(bool isSwapped){
	        if(_options_slideOut.isFlipped() == isSwapped){ return; }
	        _options_slideOut.Flip_if_possible();
	    }


	    void Update(){
	        if(isOn  &&  _options_mouseHover.isHovering){
	            _options_slideOut.Toggle_if_Different(true);
	        }
	        if (!isOn){
	            _options_slideOut.Toggle_if_Different(false);
	        }
	    }

	    void Awake(){
	        _toggle.onValueChanged.AddListener( OnValueChanged );
	        _bakeColors_button.onClick.AddListener( OnButton_BakeColors );
	    }

	    void Start(){
	        Settings_MGR._Act_verticalRibbonsSwapped += OnSettings_ToolRibbonSwapped;
	        OnSettings_ToolRibbonSwapped(Settings_MGR.instance.get_viewport_isSwapVerticalRibbons());
	    }

	    void OnDestroy(){
	        Settings_MGR._Act_verticalRibbonsSwapped -= OnSettings_ToolRibbonSwapped;
	    }
	}
}//end namespace
