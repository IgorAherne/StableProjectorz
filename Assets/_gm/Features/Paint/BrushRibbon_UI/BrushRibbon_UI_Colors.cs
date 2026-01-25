using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;


namespace spz {

	//helps the 'BrushRibbon_UI' component.
	// Only deals with and represents the button that shows color.
	public class BrushRibbon_UI_Colors : MonoBehaviour{

	    [SerializeField] WorkflowRibbon_UI _rib;
	    [SerializeField] SD_BrushRibbon_UI_Direction _direction;
	    [SerializeField] Image _brushColorIcon;
	    [SerializeField] Button _brushColorButton;//RGB color if painting.
	    [SerializeField] Animation _brushColor_anim;//can play scale-pop animation, for user's attention
	    [SerializeField] float _maxDragDist_contextMenu = 0.001f;

	    bool _is_RMB_pressed;
	    Vector2 _RMB_startCoord;

	    public Color _brushColor { get; private set; } = Color.black;
	    public Action<Color> _onBrushColorUpdated { get; set; } = null;
	    public bool IsEyeDropperMagnified => EventsBinder.FindComponent<BrushRibbon_UI_EyeDropperTool>("BrushRibbon_UI_EyeDropperTool")?
	                                                     .IsMagnificationActive ?? false;

	    void OnBrushColorButton(){
	        _onBrushColorUpdated?.Invoke(_brushColor);//invoke callback for others.
	        ShowColorPicker();
	    }

	    void OnEyeDropperTool_Sampled(Color col){
	        ChangeBrushColor(col, ensureInpaint:true);
	        _brushColor_anim.Play();
	    }

	    void OnBucketFill(){
	        _brushColor_anim.Play();
	    } 


	    void ChangeBrushColor(Color wantedColor, bool ensureInpaint=false, bool invokeCallback=true){
	        if (ensureInpaint){
	            // For convenience. If user wants to choose RGB color, they most likely want to inpaint. So show it:
	            _rib.Set_CurrentMode( WorkflowRibbon_CurrMode.Inpaint_Color, playAttentionAnim:true );
	        }
	        _brushColor = wantedColor;
	        //NOTICE: don't change the icon color, it will be changed during Update()

	        if(invokeCallback){  _onBrushColorUpdated?.Invoke(wantedColor); }
	    }
    

	    // check if it's not possible to interpret RMB 
	    // as a "right click that would open context-menu colorpicker".
	    bool CanShowColorPicker(){
	        if(KeyMousePenInput.isKey_Shift_pressed()){ return false; }
	        if(KeyMousePenInput.isKey_CtrlOrCommand_pressed()){ return false; }
	        if(KeyMousePenInput.isKey_alt_pressed()){ return false; }
	        if(MainViewport_UI.instance.isCursorHoveringMe() == false){ return false; }
	        return true;
	    }


	    void ShowColorPicker_if_releasedRMB(){ //if we stopped holding right mouse button.
        
	        if(!_is_RMB_pressed && KeyMousePenInput.isRMBpressedThisFrame()){ 
	            _RMB_startCoord = KeyMousePenInput.cursorViewPos01();
	            _is_RMB_pressed = true;
	        }

	        if(!CanShowColorPicker()){
	            _is_RMB_pressed = false;
	            return;
	        }
	        if(KeyMousePenInput.isRMBpressed()){ return; }//still holding right mouse button.
	        if(!_is_RMB_pressed){ return; }//didn't hold right mouse button.

	        _is_RMB_pressed = false;//else, was holding RMB before, but no more.

	        Vector2 currPos = KeyMousePenInput.cursorViewPos01();
	        float dist = (currPos - _RMB_startCoord).magnitude;
	        if(dist > _maxDragDist_contextMenu){ return; }//too far, not a click. Probably a camera rotation, etc.

	        ShowColorPicker();
	    }


	    void ShowColorPicker_if_spaceKey(){
	        if(!CanShowColorPicker()){ return; }
	        if (KeyMousePenInput.isSomeInputFieldActive()) { return; }
	        if(Keyboard.current.spaceKey.wasPressedThisFrame==false){ return; }
	        ShowColorPicker();
	    }


	    void ShowColorPicker() => MouseWorkbench_Zone.instance?.ShowAtScreenCoord( KeyMousePenInput.cursorScreenPos(), _brushColor,
	                                                                           (Color c)=>ChangeBrushColor(c,ensureInpaint:true), 
	                                                                           MouseWorkbench_Zone.ShowPreference.CenterOnCursor );

	    //invoked even if our GameObject is disabled.
	    void OnUpdate(){
	        ShowColorPicker_if_releasedRMB();
	        ShowColorPicker_if_spaceKey();
	        //match the icon's color to the current mode and color
	        _brushColorIcon.color = _brushColor;
	    }


	    void Awake(){ 
	        _brushColorButton.onClick.AddListener( OnBrushColorButton );

	        BrushRibbon_UI_BucketFill._Act_onClicked += OnBucketFill;
	        BrushRibbon_UI_EyeDropperTool._onResult += OnEyeDropperTool_Sampled;
	    }

	    void Start(){
	        Update_callbacks_MGR.general_UI += OnUpdate;
	        ChangeBrushColor(Color.gray, ensureInpaint:false, invokeCallback:false);
	    }

	    void OnDestroy(){
	        Update_callbacks_MGR.general_UI -= OnUpdate;
	    }


	    public void Save(BrushRibbon_UI_SL trSL){
	        trSL.maskBrush_color = _brushColor;
	    }

	    public void Load(BrushRibbon_UI_SL trSL){
	        ChangeBrushColor(trSL.maskBrush_color);
	    }
	}
}//end namespace
