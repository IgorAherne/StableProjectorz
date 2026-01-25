using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Controls buttons for brushing Additively (+) or Negatively (-).
	// Specifically for Generate-3D, erasing backgrounds etc.
	// Because the StableDiffusion-texturing has its own direction buttons.
	public class Gen3D_BrushRibbon_UI_Direction : BrushRibbon_UI_Direction{
    
	    public void Show(){
	        _anim.gameObject.SetActive(true);

	        if (Cursor_UI.instance != null){
	            Cursor_UI.instance.SetCursorColor(_brushAdd_Toggle.isOn ? Color.white : Color.black );
	        }
	    }

	    public void Hide(){
	        _anim.gameObject.SetActive(false);
	    }

	    void OnBrushStrokeEnd(){
	        base._anim.Play(); //little bouncing animation, so that user can see that they are painting negatively or positively.
	    }

	    void OnUpdateDirection_Toggle(Toggle toggle, bool isOn){
	        if(!isOn){ return; } //toggles are in a mutually-exclusive group, so care only if ON.
	        // Don't allow if we are currently dragging on the screen, painting.
	        // Brush stroke must remain of the same color until the mouse button is released.
	        if(Background_Painter.instance._isPainting){ return; }

	        bool isPositive =  toggle == _brushAdd_Toggle;
	        Cursor_UI.instance.SetCursorColor( isPositive? Color.white : Color.black );
	    }


	    protected override void Awake(){
	        base.Awake();
	        _brushErase_Toggle.gameObject.SetActive(true);//begin as erasing (more convenient)

	        Background_Painter.Act_OnPaintStrokeEnd += OnBrushStrokeEnd;

	        _brushAdd_Toggle.onValueChanged.AddListener((isOn) => OnUpdateDirection_Toggle(_brushAdd_Toggle, isOn));
	        _brushErase_Toggle.onValueChanged.AddListener((isOn) => OnUpdateDirection_Toggle(_brushErase_Toggle, isOn));
	    }

	    protected override void Start()
	    {
	        base.Start(); 
	        Cursor_UI.instance.SetCursorColor(_brushAdd_Toggle.isOn? Color.white : Color.black );
	    }
	}
}//end namespace
