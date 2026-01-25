using System;
using UnityEngine;

namespace spz {

	// Belongs to some IconUI element. 
	// Listens and processes the clicking and cursor-hovering, for this icon.
	public class IconUI_ClickAndHover : MonoBehaviour{
    
	    [SerializeField] IconUI _icon;
	    [SerializeField] IconUI_HideSolo_Buttons _hideSolo_buttons;
	    [Space(10)]
	    [SerializeField] FadeOutUnlessPersist_UI _wholeIconFader;//tries to keep the icon Transparent, unless we actively forcing it as visible
	    [SerializeField] FadeOutUnlessPersist_UI _grabRibbonFader;//tries to keep ribbon INVISIBLE, unless we actively forcing it as visible
	    [Space(10)]
	    [SerializeField] MouseClickSensor_UI _wholeIcon_button;
	    [SerializeField] MouseClickSensor_UI _doubleClickSurface;//can capture second click but disables quickly after enabled

	    GenData2D genData => _icon._genData;
    
	    static Action<IconUI> Act_OnSomeIconRightClicked = null;
	    static Action<IconUI> Act_onSomeIcon_CursorHover_Started;
	    static Action<IconUI> Act_onSomeIcon_CursorHover_Ended;


	    public void OnAfterInstantiated(){
	        //ensure we are NOT tinted (we might start be tinted after hover).
	        //THis helps to see the entire batch better:
	        _wholeIconFader.KeepFadingIn(false, forceMinAlphaNow: true, forceMaxAlphaNow: false, forceMin0: true);

	        StaticEvents.SubscribeAppend<bool>("HighlightHoveredIcons_UI_MGR:OnTogglePressed", OnHighlightHover_globalSetting);
	        OnHighlightHover_globalSetting(HighlightHoveredIcons_UI_MGR.instance?.isToggleOn??false);//manually invoke too.

	                _wholeIcon_button._onMouseClick += OnClicked_WholeIconButton;
	        _hideSolo_buttons.hidingSurfaceButton._onMouseClick += OnClicked_WholeIconButton;
	        _hideSolo_buttons.soloSurfaceButton._onMouseClick += OnClicked_WholeIconButton;

	        IconUI.Act_OnSomeIconClicked += OnSomeIconClicked;
	        Act_OnSomeIconRightClicked += OnSomeIcon_RightClicked;
	        _doubleClickSurface._onMouseClick += OnDoubleClickingSurface;

	        Act_onSomeIcon_CursorHover_Started += OnSomeIcon_CursorHover_Started;
	        Act_onSomeIcon_CursorHover_Ended += OnSomeIcon_CursorHover_Ended;
	    }


	    public void OnLoadAfterSpawned(bool isChosen_in_group){
	        if(isChosen_in_group){  OnClicked_WholeIconButton(buttonIx:0);  }
	    }


	    public void OnCleanup(){
	        Act_OnSomeIconRightClicked -= OnSomeIcon_RightClicked;
	        IconUI.Act_OnSomeIconClicked -= OnSomeIconClicked;

	        Act_onSomeIcon_CursorHover_Started -= OnSomeIcon_CursorHover_Started;
	        Act_onSomeIcon_CursorHover_Ended   -= OnSomeIcon_CursorHover_Ended;

	        StaticEvents.Unsubscribe<bool>("HighlightHoveredIcons_UI_MGR:OnTogglePressed", OnHighlightHover_globalSetting);
	    }

    
	    void OnHighlightHover_globalSetting( bool isAllowed_highlightIcons ){
	        _wholeIconFader.gameObject.SetActive( isAllowed_highlightIcons );
	    }


	    public void StartedHover() => Act_onSomeIcon_CursorHover_Started?.Invoke(_icon);
	    public void OnHoverStopped() => Act_onSomeIcon_CursorHover_Ended?.Invoke(_icon);
    
	    public void OnHoveredThisFrame(){
	        _grabRibbonFader.FadeInThisFrame();//Show ribbon regardless of whether we are chosen or not.
	        HighlightProjCam_ifHovered();

	        if(_icon.isMainSelected==false){ return; }
	        if(IconAutoContextMenu_UI_MGR.instance.isToggleOn ==false){ return; }
	        if(_hideSolo_buttons.is_HideOrSoloCover_showing){ return;}
        
	        _icon.Toggle_ContextMenu(true);
	    }


	    void HighlightProjCam_ifHovered(){
	        bool specialKey  = KeyMousePenInput.isKey_CtrlOrCommand_pressed();
	             specialKey |= KeyMousePenInput.isKey_Shift_pressed();

	        View_UserCamera cam = UserCameras_MGR.instance._curr_viewCamera;
	        bool isMovingAround  = cam.cameraOrbit._isOrbiting;
	             isMovingAround |= cam.cameraDolly._isZooming;

	        if (specialKey && !isMovingAround && genData._projCamera != null){
	            ProjectorCameras_MGR.instance.HighlightProjCamera(genData._projCamera );
	        }
	    }


	    void OnClicked_WholeIconButton(int buttonIx){
	        GenerationData_Kind kind =  genData!=null? genData.kind : GenerationData_Kind.Unknown;
	        switch (buttonIx){
	          case 0:
	            _doubleClickSurface.ActivateFor(0.26f);
	            IconUI.Act_OnSomeIconClicked(_icon, kind);
	            break;
	          case 1:
	            Act_OnSomeIconRightClicked?.Invoke(_icon);
	            IconUI.Act_OnSomeIconClicked(_icon, kind);//right click will also "click" the icon.
	            break;
	        }
	    }


	    //will be invoked if our Double-Click surface gets clicked.
	    //That sufrace gets qucikly disabled when our icon is clicked for first time.
	    void OnDoubleClickingSurface(int button_ix){
	        if(button_ix==1){
	            // Forwarding the right-click. Else, right click is unavaialble for 0.3 seconds after clicking icon
	            // (due to the double-clicking surface showing for some time)
	            OnClicked_WholeIconButton(1);
	            return; 
	        }
	        UserCameras_MGR.instance.Restore_CamerasPlacements(genData);
	    }


	//Some icon:
	    void OnSomeIconClicked(IconUI someIcon, GenerationData_Kind kind){
	        if(someIcon!=_icon){ _icon.Toggle_ContextMenu(false); }
	    }

	    void OnSomeIcon_RightClicked(IconUI which){
	        bool contextOn =  which==_icon  &&  _icon.isShowing_contextMenu()==false;
	        _icon.Toggle_ContextMenu(contextOn);
	    }


	    void OnSomeIcon_CursorHover_Started(IconUI someIcon){
	        bool canHighlight = HighlightHoveredIcons_UI_MGR.instance.isToggleOn;
	        bool autoContextMenu = IconAutoContextMenu_UI_MGR.instance.isToggleOn;

	        if (canHighlight){
	            bool sameGen =  genData == someIcon._genData;
	            bool isHideCoverEnabled = _hideSolo_buttons.is_HideOrSoloCover_showing;
	            _wholeIconFader.KeepFadingIn( !sameGen && !isHideCoverEnabled,  forceMinAlphaNow:sameGen,  
	                                           forceMaxAlphaNow:false, forceMin0:true );
	        }
	        if (autoContextMenu){ 
	            if(_icon==someIcon  &&  _icon.isMainSelected){ //show context only if isMainSelected. Otherwise elements of context menu 
	                _icon.Toggle_ContextMenu(true);            //make it hard to actually click through and select a non-selected icon.
	            }else{ //if it's not my icon, toggle my context off. 
	                _icon.Toggle_ContextMenu(false);
	            }
	        }
	    }


	    void OnSomeIcon_CursorHover_Ended(IconUI stoppedIcon){
        
	        bool canHighlight = HighlightHoveredIcons_UI_MGR.instance.isToggleOn;
	        if (canHighlight){  
	            _wholeIconFader.KeepFadingIn(false, forceMinAlphaNow:false, forceMaxAlphaNow:false, forceMin0:true);
	        }

	        if(_icon==stoppedIcon){
	            //if user auto-context-menu, then it should shown or hidden (hidden right now) when hovering:
	            if(IconAutoContextMenu_UI_MGR.instance.isToggleOn){  _icon.Toggle_ContextMenu(false);  }
	        }
	    }

	}
}//end namespace
