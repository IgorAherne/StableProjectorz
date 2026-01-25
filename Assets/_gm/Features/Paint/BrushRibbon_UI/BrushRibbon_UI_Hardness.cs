using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace spz {

	//helps the 'BrushRibbon_UI' component.
	// Only deals with and represents the small button that controls the brush hardness.
	public class BrushRibbon_UI_Hardness : MonoBehaviour
	{
	    [SerializeField] BrushRibbon_UI _rib;
	    [Space(10)]
	    [SerializeField] Button _hardnessButton;
	    [SerializeField] Image _hardnessChoiceIcon;
	    [SerializeField] List<Sprite> _brushHardnessTextures;
	    [SerializeField] Animation _currHardnessAnim;
	    public int hardnessIx { get; private set; } = 0;
	    public Texture2D _brushHardnessTex => _brushHardnessTextures[hardnessIx].texture;
	    public Texture2D readSpecificHardnessTex(int hardnessIx) => _brushHardnessTextures[hardnessIx].texture;
	    public Action onHovered { get; set; }


	    void OnHardnessButtonHover(PointerEventData pe){
	        if(KeyMousePenInput.isLMBpressed()){ return; }//likely dragging some slider, don't distract user.
	        onHovered?.Invoke();
	    }


	    void OnHardnessButton(){
	        hardnessIx++; //loop around maybe. Notice 1, because 0 is always 'the current':
	        hardnessIx = hardnessIx > 2 ? 0 : hardnessIx;
	        _hardnessChoiceIcon.sprite = _brushHardnessTextures[hardnessIx];
	        _currHardnessAnim.Play();
	    }

	    void SetExactHardness(int exactHardness_textureIx, bool playAnimation=true){
	        hardnessIx = exactHardness_textureIx;
	        _hardnessChoiceIcon.sprite = _brushHardnessTextures[hardnessIx];
	        if(playAnimation){ _currHardnessAnim.Play(); }
	    }


	    void OnStartEditMode(MultiView_StartEditMode_Args args){
	        if(Art2D_IconsUI_List.instance._mainSelectedIcon == null){  return; }
	        if(Art2D_IconsUI_List.instance._mainSelectedIcon._genData.povInfos.numEnabled == 1){ return; }
	        //softest brush isn't sufficient for multiview. Its preview is barely visible. Switching to medium brush:
	        SetExactHardness(1);
	    }

	    void Update(){
	        // COMMENTED OUT, KEPT FOR PRECAUTION. Allow user to do it from anywhere, without hovering the viewport:
	        //    if(MainViewport_UI.instance.isCursorHoveringMe() == false){ return; }
	        if(KeyMousePenInput.isSomeInputFieldActive()){ return; }//maybe typing text, etc

	        if(Input.GetKeyDown(KeyCode.H)){  OnHardnessButton(); }//to next brush hardness

	        bool hasCTRL = KeyMousePenInput.isKey_CtrlOrCommand_pressed();
	        bool hasShift = KeyMousePenInput.isKey_Shift_pressed();
	        if (hasCTRL && !hasShift){
	            if(Input.GetKeyDown(KeyCode.Alpha1)){ SetExactHardness(0); }
	            if(Input.GetKeyDown(KeyCode.Alpha2)){ SetExactHardness(1); }
	            if(Input.GetKeyDown(KeyCode.Alpha3)){ SetExactHardness(2); }
	        }
	    }

	    void Awake(){
	        _hardnessButton.onClick.AddListener( OnHardnessButton );

	        SetExactHardness(hardnessIx);
	        _hardnessButton.GetComponentInChildren<MouseHoverSensor_UI>().onSurfaceEnter += OnHardnessButtonHover;

	        MultiView_Ribbon_UI.OnStartEditMode += OnStartEditMode;
	    }


	    public void Save(BrushRibbon_UI_SL trSL){
	        trSL.maskBrush_hardnessIx = hardnessIx;
	    }

	    public void Load(BrushRibbon_UI_SL trSL){
	        int hardnessIx = trSL.maskBrush_hardnessIx;
	        SetExactHardness(hardnessIx);
	    }
	}
}//end namespace
