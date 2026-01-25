using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace spz {

	// For StableDiffusion-texturing mode.
	// Helps the 'BrushRibbon_UI' component.
	// Knows whether we will be adding (positive) or erasing (negative) color with the brush.
	// Only deals with and represents the small button that shows the direction.
	public class SD_BrushRibbon_UI_Direction : BrushRibbon_UI_Direction{
	    [SerializeField] WorkflowRibbon_UI _rib;
	    [SerializeField] BrushRibbon_UI_Colors _colors;
    
	    void OnUpdateDirection_Mode( WorkflowRibbon_CurrMode currMode ){
	        //for convenience. We erase masks of 3d objects usually, and for inpaint - usually add.
	        switch (currMode){
	            case WorkflowRibbon_CurrMode.ProjectionsMasking:
	                //Check if this change in mode was due to user previewing the projection.
	                //If so, keep the direction as is, to avoid frustration:
	                if(Keyboard.current.rKey.isPressed){ return; }
	                SetDirection_Toggle(false);
	                break;
	            case WorkflowRibbon_CurrMode.Inpaint_Color: SetDirection_Toggle(true); break;
	            case WorkflowRibbon_CurrMode.Inpaint_NoColor: SetDirection_Toggle(true); break;
	            case WorkflowRibbon_CurrMode.TotalObject: SetDirection_Toggle(true); break;
	            case WorkflowRibbon_CurrMode.WhereEmpty: SetDirection_Toggle(true); break;
	            default: break;
	        }
	    }

	    protected void OnStartedEditMode_MultiView( MultiView_StartEditMode_Args args ){
	        //if our icon is for multi-view, change to white brush. 
	        //Because rather than erase, we usually want to "increase" the POV's appearance,
	        //to dominate it over its sibling POVs:
	        IconUI icon = Art2D_IconsUI_List.instance._mainSelectedIcon;
	        if (icon != null){
	            bool isMultiPOV =  icon._genData.povInfos.numEnabled > 1;
	            if(isMultiPOV){  SetDirection_Toggle(true);  }
	        }
	    }

	    void OnUpdateDirection_Toggle(Toggle toggle, bool isOn){
	        if(!isOn){ return; } //toggles are in a mutually-exclusive group, so care only if ON.
	        // Don't allow if we are currently dragging on the screen, painting.
	        // Brush stroke must remain of the same color until the mouse button is released.
	        if(Projections_MaskPainter.instance._isPainting){ return; }
	        if(Inpaint_MaskPainter.instance._isPainting){ return; }

	        bool isPositive =  toggle == _brushAdd_Toggle;
	        Cursor_UI.instance.SetCursorColor( isPositive? Color.white : Color.black );
	    }


	    void OnBrushStrokeEnd(){
	        base._anim.Play(); //little bouncing animation, so that user can see that they are painting negatively or positively.
	    }


	    protected override void Awake(){
	        base.Awake();
	        MultiView_Ribbon_UI.OnStartEditMode += OnStartedEditMode_MultiView;
        
	        if(_colors != null){ 
	            _colors._onBrushColorUpdated += (Color col)=>SetDirection_Toggle(true);
	        }
	        if(_rib!=null){
	            WorkflowRibbon_UI._Act_OnModeChanged += OnUpdateDirection_Mode;
	        }
	        Projections_MaskPainter.Act_OnPaintStrokeEnd += OnBrushStrokeEnd;
	        Inpaint_MaskPainter.Act_OnPaintStrokeEnd    += OnBrushStrokeEnd;

	        _brushAdd_Toggle.onValueChanged.AddListener( (isOn)=>OnUpdateDirection_Toggle(_brushAdd_Toggle, isOn) );
	        _brushErase_Toggle.onValueChanged.AddListener( (isOn)=>OnUpdateDirection_Toggle(_brushErase_Toggle, isOn) );
	    }//void Awake()

    
	    protected override void Start(){
	        base.Start();
	        // Usually at the start, users do single-projection, which it makes sense to erase.
	        // Notice, we also have 'OnStartedEditMode_MultiView', where we can force brush color as white :)
	        _brushAdd_Toggle.SetIsOnWithoutNotify(true);
	        _brushErase_Toggle.SetIsOnWithoutNotify(false);
	        SetDirection_Toggle(false);
	    }
	}



	public class BrushRibbon_UI_Direction : MonoBehaviour{
	    [Space(10)]
	    [SerializeField] protected Toggle _brushErase_Toggle;//add or subtract (erase)
	    [SerializeField] protected Toggle _brushAdd_Toggle;
	    [SerializeField] protected Animation _anim;
    
	    public bool isPositive => _brushAdd_Toggle.isOn;

	    protected void SetDirection_Toggle(bool isPositive_dir){
	        if (isPositive_dir){ _brushAdd_Toggle.isOn = true; }
	        if(!isPositive_dir){ _brushErase_Toggle.isOn = true; }//toggle group will disable rest.
	    }

	    protected virtual void Update(){
	        // COMMENTED OUT, KEPT FOR PRECAUTION. Allow user to do it from anywhere, without hovering the viewport:
	        //    if(MainViewport_UI.instance.isCursorHoveringMe() == false){ return; }
	        if(KeyMousePenInput.isSomeInputFieldActive()){ 
	            return; 
	        }//maybe typing text, etc

	        bool hasCTRL = KeyMousePenInput.isKey_CtrlOrCommand_pressed();
	        if (!hasCTRL){
	            if(Input.GetKeyDown(KeyCode.X)){  SetDirection_Toggle(!isPositive);  }
	        }
	    }
	    protected virtual void Awake(){}
	    protected virtual void Start(){ }
	}
}//end namespace
