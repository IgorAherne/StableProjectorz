using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public enum WorkflowRibbon_CurrMode{
	    ProjectionsMasking,
	    Inpaint_Color,
	    Inpaint_NoColor,
	    TotalObject,
	    WhereEmpty,
	    AntiShade,//not used at the moment
	}

	public enum InpaintingFill{ Fill=0, Original=1, LatentNoise=2, LatentNothing=3, }


	public class WorkflowRibbon_UI : MonoBehaviour {
	    public static WorkflowRibbon_UI instance { get; private set; } = null;

	    [SerializeField] RectTransform _ribbonHoverZone;
	    [SerializeField] RectTransform _ribbonNoHoverZone;
	    [Space(10)]
	    [SerializeField] GameObject _holderGO_turnMeOnOff;
	    [SerializeField] WorkflowRibbon_ProjMask_UI _projMasking;
	    [SerializeField] WorkflowRibbon_Colors_UI _coloring;
	    [SerializeField] WorkflowRibbon_NoColor_UI _colorless;
	    [SerializeField] WorkflowRibbon_EntireObject_UI _entireObj;
	    [SerializeField] WorkflowRibbon_WhereEmpty_UI _WhereEmpty_UI;
	    [SerializeField] WorkflowRibbon_AntiShade_UI _AntiShade_UI;

	    bool _skipShortcutHint = false;
	    int _shortcutHint_numShown = 0;

	    public static Action<WorkflowRibbon_CurrMode> _Act_OnModeChanged { get; set; } = null;
	    public static Action Act_onBakeColors_button { get; set; } = null;

	    public bool isHoveredByCursor { get; private set; } = false;
	    public bool isPressedByCursor { get; private set; }//maybe no longer hovered, but still dragging one of our sliders.

	    public bool has_brushed_mask()
	        =>Inpaint_MaskPainter.instance.isPaintMaskEmpty==false;

	    public bool has_background_mask() 
	        => ArtBG_IconsUI_List.instance.hasBackground(considerGradientColors:true);


	    // If there is a background (always generates whole silhuette) or brushed.
	    // if we are in the workflow mode which always generates mask on its own.
	    public bool has_auto_mask(){
	        switch (currentMode()){
	            case WorkflowRibbon_CurrMode.WhereEmpty:
	            case WorkflowRibbon_CurrMode.TotalObject:
	                return true;
	            default:
	                return false;
	        }
	    }

	    public bool allowed_to_showBrushMask(){
	        switch (currentMode()){
	            case WorkflowRibbon_CurrMode.Inpaint_Color:
	            case WorkflowRibbon_CurrMode.Inpaint_NoColor:
	                return true;
	            default:
	                return false;
	        }
	    }

	    public bool isMode_using_img2img(){
	        switch (currentMode()){
	            case WorkflowRibbon_CurrMode.ProjectionsMasking: return false;
	            default: return true;
	        }
	    }

	    public bool is_allow_SoftInpaint(){
	        switch (currentMode()){
	            case WorkflowRibbon_CurrMode.ProjectionsMasking: //for example, dealing with backgrounds etc.
	            case WorkflowRibbon_CurrMode.WhereEmpty:
	            case WorkflowRibbon_CurrMode.AntiShade:
	                return false;
	            default:
	                return true;
	        }
	    }

	    public InpaintingFill Get_InpaintFill(){
	        // If we are sending entire EMPTY silhuette, use LatentNothing (for WhereEmpty or Backgrounds etc).
	        // For such empty silhuettes, LatentNoise gives bad quality - Sept 2024.
	        // For such empty silhuettes, Original doesn't work too. Even at 100% denoise it still looks under mask,
	        // and gives dark results.
	        // Even if we add noise here to the ViewTex, around silhuette and use Original, - LatentNothing still wins.
	        // Soft inpaint makes stuff pale, so we will use usual inpaint.  'Nothing' looks better than Original with usual Inpaint.
	        switch (currentMode()){
	            case WorkflowRibbon_CurrMode.ProjectionsMasking:
	                return InpaintingFill.LatentNothing; //for example, dealing with backgrounds etc.

	            case WorkflowRibbon_CurrMode.Inpaint_Color:
	                return InpaintingFill.Original;
                
	            case WorkflowRibbon_CurrMode.Inpaint_NoColor:
	                return InpaintingFill.Original;

	            case WorkflowRibbon_CurrMode.TotalObject:
	                return InpaintingFill.Original;

	            case WorkflowRibbon_CurrMode.WhereEmpty:
	                return InpaintingFill.LatentNothing;

	            case WorkflowRibbon_CurrMode.AntiShade:
	                return InpaintingFill.LatentNothing;
                
	            default:
	                return InpaintingFill.LatentNothing;
	        }
	    }


	    public WorkflowRibbon_CurrMode currentMode(){
	        if(_projMasking.isOn){ return WorkflowRibbon_CurrMode.ProjectionsMasking;  }
	        if(_coloring.isOn){    return WorkflowRibbon_CurrMode.Inpaint_Color;  }
	        if(_colorless.isOn){   return WorkflowRibbon_CurrMode.Inpaint_NoColor;  }
	        if(_entireObj.isOn){   return WorkflowRibbon_CurrMode.TotalObject;  }
	        if(_WhereEmpty_UI.isOn){ return WorkflowRibbon_CurrMode.WhereEmpty; }
	        if(_AntiShade_UI.isOn){  return WorkflowRibbon_CurrMode.AntiShade; }
	        return WorkflowRibbon_CurrMode.ProjectionsMasking;
	    }


	    bool _isSettingCurrentMode = false;
	    public void Set_CurrentMode(WorkflowRibbon_CurrMode mode, bool playAttentionAnim=false){
	        if(_isSettingCurrentMode){ return; }//avoid recursion
	        _isSettingCurrentMode = true;

	        IWorkflowModeToggle toggle = null;
	        switch (mode){
	            case WorkflowRibbon_CurrMode.ProjectionsMasking: toggle = _projMasking; break;
	            case WorkflowRibbon_CurrMode.Inpaint_Color:    toggle = _coloring; break;
	            case WorkflowRibbon_CurrMode.Inpaint_NoColor: toggle = _colorless; break;
	            case WorkflowRibbon_CurrMode.TotalObject:  toggle = _entireObj; break;
	            case WorkflowRibbon_CurrMode.WhereEmpty: toggle = _WhereEmpty_UI; break;
	            case WorkflowRibbon_CurrMode.AntiShade: toggle = _AntiShade_UI; break;
	            default: break;
	        }
	        toggle.EnableToggle(playAttentionAnim);

	        _Act_OnModeChanged?.Invoke(mode);
	        _isSettingCurrentMode = false;
	    }


    
	    void OnToggle_ValueChanged(IWorkflowModeToggle tog){
	        Set_CurrentMode( GetMode_from_Toggle(tog), playAttentionAnim:false );
	        if (WorkflowRibbon_ProjMask_UI.didShowHint_thisFrame()){ return; }
	        if (WorkflowRibbon_Colors_UI.didShowHint_thisFrame()){ return; }
	        if (WorkflowRibbon_NoColor_UI.didShowHint_thisFrame()){ return; }//to avoid showing our own hint. (theirs is more important and rare).
	        if (DimensionMode_MGR.instance._dimensionMode != DimensionMode.dim_sd){ return; }

	        if (_skipShortcutHint){ return; }

	        if(_shortcutHint_numShown < 4){
	            _shortcutHint_numShown++;
	            string modifier_key =  Settings_MGR.instance.get_useCtrlScroll_for_WorkflowMode_swaps() ? "Ctrl" : "Shift";
	            Viewport_StatusText.instance.ShowStatusText($"{modifier_key} + Mouse Scroll Wheel to change mode easier :)", false, 2, false);
	        }
	    }

	    WorkflowRibbon_CurrMode GetMode_from_Toggle( IWorkflowModeToggle tog ){
	        if(ReferenceEquals(tog,_projMasking)){ return WorkflowRibbon_CurrMode.ProjectionsMasking;  }
	        if(ReferenceEquals(tog,_coloring)){   return WorkflowRibbon_CurrMode.Inpaint_Color;  }
	        if(ReferenceEquals(tog,_colorless)){   return WorkflowRibbon_CurrMode.Inpaint_NoColor;  }
	        if(ReferenceEquals(tog,_entireObj)){   return WorkflowRibbon_CurrMode.TotalObject;  }
	        if(ReferenceEquals(tog,_WhereEmpty_UI)){ return WorkflowRibbon_CurrMode.WhereEmpty; }
	        if(ReferenceEquals(tog,_AntiShade_UI)){ return WorkflowRibbon_CurrMode.AntiShade; }
	        return WorkflowRibbon_CurrMode.ProjectionsMasking;
	    }

	    IWorkflowModeToggle Get_Toggle_of_currentMode(){
	        switch (currentMode()){
	            case WorkflowRibbon_CurrMode.ProjectionsMasking: return _projMasking; break;
	            case WorkflowRibbon_CurrMode.Inpaint_Color: return _coloring; break;
	            case WorkflowRibbon_CurrMode.Inpaint_NoColor: return _colorless; break;
	            case WorkflowRibbon_CurrMode.TotalObject: return _entireObj; break;
	            case WorkflowRibbon_CurrMode.WhereEmpty: return _WhereEmpty_UI; break;
	            case WorkflowRibbon_CurrMode.AntiShade: return _AntiShade_UI; break;
	            default: Debug.Assert(false, $"unknown mode in {nameof(Get_Toggle_of_currentMode)}"); break;
	        }
	        return _projMasking;
	    }

  
	    void OnBrushStrokeEnd(){
	        //Play animation, so user won't be confused if they were trying to erase/add:
	        Animation anim = (Get_Toggle_of_currentMode() as Component).GetComponent<Animation>();
	        if(anim==null){ return; }
	        anim.Play();
	    }


	    void EarlyUpdate(){
	        Check_if_Hovered();
	        Scroll_to_ChangeMode_maybe();
	    }

	    void Check_if_Hovered(){
        
	        isHoveredByCursor  =  RectTransformUtility.RectangleContainsScreenPoint( _ribbonHoverZone, KeyMousePenInput.cursorScreenPos());
	        isHoveredByCursor &= !RectTransformUtility.RectangleContainsScreenPoint(_ribbonNoHoverZone, KeyMousePenInput.cursorScreenPos());

	        if (isHoveredByCursor && KeyMousePenInput.isLMBpressedThisFrame()){
	            isPressedByCursor = true;
	        }
	        if(KeyMousePenInput.isLMBpressed()==false){
	            isPressedByCursor = false;
	        }
	    }


	    void Scroll_to_ChangeMode_maybe(){
	        //we either Shift+Scroll or Ctrl+Scroll, depends on the preferences:
	        bool use_ctrl = Settings_MGR.instance.get_useCtrlScroll_for_WorkflowMode_swaps();
	        if (use_ctrl){ 
	            if(KeyMousePenInput.isKey_CtrlOrCommand_pressed() == false){ return; }
	        }else { 
	            if(KeyMousePenInput.isKey_Shift_pressed() == false){ return; }
	        }

	        if(Input.mouseScrollDelta.y == 0){ return; }

	        RectTransform curr = (Get_Toggle_of_currentMode() as Component).transform as RectTransform;
	        int num = curr.parent.childCount;

	        int nextIx = curr.GetSiblingIndex();

	        while (true){//keep reducing/increasing the index until we get to the child that has the toggle:
	            if(Input.mouseScrollDelta.y < 0){
	                nextIx++;
	                if(nextIx >= num){ nextIx = 0; }
	            }else{
	                nextIx--;
	                if(nextIx < 0){ nextIx = num-1; }
	            }
	            var tog = curr.parent.GetChild(nextIx).GetComponent<IWorkflowModeToggle>();
	            if (tog == null){ continue; } //some children (bg or frame) aren't toggles, skip them.

	            _skipShortcutHint = true;
	            tog.EnableToggle();
	            _skipShortcutHint = false;
	            break;
	            //toggleGroup will untoggle the old one.
	        }
	    }


	    void Awake(){
	        if (instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	        Projections_MaskPainter.Act_OnPaintStrokeEnd += OnBrushStrokeEnd;
	        Inpaint_MaskPainter.Act_OnPaintStrokeEnd += OnBrushStrokeEnd;

	        _projMasking.onValueChanged+= isOn=>{if(isOn){ OnToggle_ValueChanged(_projMasking); }};
	        _coloring.onValueChanged   += isOn=>{if(isOn){ OnToggle_ValueChanged(_coloring); }};
	        _colorless.onValueChanged  += isOn=>{if(isOn){ OnToggle_ValueChanged(_colorless); }};
	        _entireObj.onValueChanged  += isOn=>{if(isOn){ OnToggle_ValueChanged(_entireObj);} };
	        _WhereEmpty_UI.onValueChanged += isOn=>{ if(isOn){ OnToggle_ValueChanged(_WhereEmpty_UI); }};
	        _AntiShade_UI.onValueChanged += isOn=>{ if(isOn){ OnToggle_ValueChanged(_AntiShade_UI); }};
        
	        _coloring.onBakeColors_button += ()=> Act_onBakeColors_button?.Invoke();
	    }

	    void Start(){
	         EarlyUpdate_callbacks_MGR.instance.onEarlyUpdate3 += EarlyUpdate;
	    }

    
	    public void Save(StableProjectorz_SL spz){
	        spz.sd_workflowRibbon = spz.sd_workflowRibbon??new SD_WorkflowRibbon_SL();
	        spz.sd_workflowRibbon.workflowMode = currentMode().ToString();
	    }

	    public void Load(StableProjectorz_SL spz){
	        string modeStr = spz.sd_workflowRibbon?.workflowMode ?? "";
	        object mode;
	        bool parsed = System.Enum.TryParse(typeof(WorkflowRibbon_CurrMode), modeStr, out mode);
	        {
	            WorkflowRibbon_CurrMode val = parsed? (WorkflowRibbon_CurrMode)mode : WorkflowRibbon_CurrMode.ProjectionsMasking;
	            Set_CurrentMode(val);
	        }
	    }


	}
}//end namespace
