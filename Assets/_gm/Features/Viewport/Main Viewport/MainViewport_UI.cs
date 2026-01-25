using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;


namespace spz {

	public class MainViewport_UI : MonoBehaviour{
	    public static MainViewport_UI instance { get; private set; } = null;

	    [SerializeField] RectTransform _container_rectTransf;

	    public RectTransform mainViewportRect =>_viewportRect;
	    [SerializeField] RectTransform _viewportRect;
	    [Space(10)]
	    [SerializeField] RectTransform _viewport_holder_rect;
	    [SerializeField] RectTransform _viewportWithRibbons_rect;
	    [SerializeField] RectTransform _viewport_LeftVerticalRibbon_rect;
	    [SerializeField] RectTransform _viewport_RightVerticalRibbon_rect;
	    public RectTransform innerViewportRect => InnerViewport_SizeReference.instance.rectTransf;

	    [SerializeField] GameObject _depthCamera_PreviewRect;
	    [SerializeField] GameObject _contentCamera_PreviewRect;
	    [SerializeField] RawImage _mainCamera_rawImg;
	    [SerializeField] RawImage_with_aspect _depthCamera_rawImg;
	    [SerializeField] RawImage_with_aspect _contentCamera_rawImg;
	    [Space]
	    [SerializeField] Viewport_ContextMenu_MGR_UI _viewportContextMenu_mgr;

	    Vector2 _viewport_initial_offset_min;
	    Vector2 _viewport_initial_offset_max;

	    //calculated at the start of each frame, our Update() is earliest in execution order
	    public Vector2 cursorMainViewportPos01 { get; private set; }
	    public Vector2 cursorInnerViewportPos01 { get; private set; }//fitted inside main viewport & is usually smaller.

	    public bool isCursorHoveringMe(){
	        //those ones are on different canvas than this Viewport, so it's important to manually check if they are on:
	        if(CheckForUpdates_MGR.instance.isShowing){ return false; }
	        if(WelcomeScreenCMD_MGR._isShowing){ return false; }
	        if(WelcomeScreenNovices_MGR.instance._isShowing){ return false; }
	        if(LoadIntroScreen_Panel_UI.isShowing){ return false; }
	        if(_viewportContextMenu_mgr.isShowing){ return false; }
	        return MainViewport_UI_EventListener.instance.TryRaycastTowardsSelf();
	    }


	    //Returns true even if cursor hovers a header, or some panel. As long as cursor is inside my horizontal span.
	    public bool IsCursorInside_my_width(){
	        if(CheckForUpdates_MGR.instance.isShowing){ return false; }
	        if(WelcomeScreenCMD_MGR._isShowing){ return false; }
	        if(WelcomeScreenNovices_MGR.instance._isShowing){ return false; }
	        if(LoadIntroScreen_Panel_UI.isShowing){ return false; }
	        return MainViewport_UI_EventListener.instance.IsCursorIn_my_width();
	    }

	    public enum Showing{  UsualView, Depth, Reserved_A, Reserved_B, Reserved_C, }
	    public Showing showing { get; private set; } = Showing.UsualView;
    
	    public static Action<Showing,Showing> Act_OnChanged_ShowingMode { get; set; } = null; //previous, current (what's showing now)
    

	    void Change_Showing( Showing whatToShow ){
	        Showing prev = this.showing;
	        this.showing = whatToShow;
	        Act_OnChanged_ShowingMode?.Invoke(prev,whatToShow);
	    }


	    public void ToggleShowDepth(bool isEnable){
	        hideAll_PreviewPanels();
	        if(showing != Showing.Depth && isEnable){
	            Change_Showing(Showing.Depth);
	            _depthCamera_PreviewRect.SetActive(true);
	        }
	        else if(showing == Showing.Depth && !isEnable){
	            Change_Showing(Showing.UsualView);
	        }
	    }


	    void hideAll_PreviewPanels(){
	        _depthCamera_PreviewRect.SetActive(false);
	        _contentCamera_PreviewRect.SetActive(false);
	    }



	    //invoked every frame. Checks if we quickly right-clicked
	    void OpenContextMenuMaybe(){
	        if (KeyMousePenInput.isKey_alt_pressed() || KeyMousePenInput.isKey_Shift_pressed()){ return; }
        
	        //now see if ctrl is held down and the user pressed down RMB this frame:
	        if(!KeyMousePenInput.isKey_CtrlOrCommand_pressed()){ return;}
	        if(!KeyMousePenInput.isRMBpressedThisFrame()){ return;}
	        //show context menu, if can:
	        if(_viewportContextMenu_mgr.isShowing){ return; }
	        _viewportContextMenu_mgr.Show();
	    }


	    void OnEarlyUpdate(){
	        // ensure our recttransform has the same placement as defined by the ui-skeleton:
	        Global_Skeleton_UI.instance.Place_onto_MainViewport(_container_rectTransf);

	        //refresh our cursor pos, so that others can query (reuse) our cursor pos during their Update()
	        if (Application.isFocused){//only if focused, else bothers people who work in other windows.
	            Vector2 cursorScreenPixelPos = KeyMousePenInput.cursorScreenPos();
        
	            RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewportRect, cursorScreenPixelPos, null, out Vector2 localPoint);
	            cursorMainViewportPos01 = Rect.PointToNormalized(_viewportRect.rect, localPoint);

	            RectTransformUtility.ScreenPointToLocalPointInRectangle(innerViewportRect, cursorScreenPixelPos, null, out localPoint);
	            cursorInnerViewportPos01 = Rect.PointToNormalized(innerViewportRect.rect, localPoint);
	        }
	    }


	    void Update(){
	        OpenContextMenuMaybe();

	        //don't move/animate, it's too distracting:
	        //  
	        //  bool ui_ribbonsSwapped = Settings_MGR.instance.get_viewport_isSwapVerticalRibbons();
	        //  
	        //  DimensionMode currMode = DimensionMode_MGR.instance._dimensionMode;
	        //  
	        //  switch (currMode){
	        //      case DimensionMode.dim_3d:
	        //          if (ui_ribbonsSwapped){
	        //              _viewport_holder_rect.offsetMin = Vector2.MoveTowards(_viewport_holder_rect.offsetMin, 
	        //                                                                    Vector2.zero,  Time.deltaTime*300);
	        //          }
	        //          else { 
	        //              _viewport_holder_rect.offsetMax = Vector2.MoveTowards(_viewport_holder_rect.offsetMax, 
	        //                                                                    Vector2.zero,  Time.deltaTime*300);
	        //          }
	        //          break;
	        //      case DimensionMode.dim_uv:
	        //      case DimensionMode.dim_sd:
	        //      default:
	        //          if (ui_ribbonsSwapped) {
	        //              _viewport_holder_rect.offsetMin = Vector2.MoveTowards(_viewport_holder_rect.offsetMin, 
	        //                                                                    _viewport_initial_offset_min,  Time.deltaTime*300);
	        //          }else{
	        //              _viewport_holder_rect.offsetMax = Vector2.MoveTowards(_viewport_holder_rect.offsetMax, 
	        //                                                                    _viewport_initial_offset_max,  Time.deltaTime*300);
	        //          }
	        //          break;
	        //  }
	    }


	    void LateUpdate(){
	        if(UserCameras_MGR.instance == null){ return; }

	        UserCameras_MGR_CamTextures ct =  UserCameras_MGR.instance.camTextures;

	        //keep updating, because render texture might be destroyed and new one allocated, during resizing, etc.
	        _mainCamera_rawImg.texture =  ct._viewCam_RT_ref;
	        _depthCamera_rawImg.ShowTexture_dontOwn( ct._SD_depthCam_RT_R32_contrast, 0, isGenerated:false,  CameraTexType.DepthUserCamera);
	        _contentCamera_rawImg.ShowTexture_dontOwn( ct._contentCam_RT_ref, 0, isGenerated:false,  CameraTexType.ContentUserCam);
	        ShowOrderOfProjections_maybe();
	    }


	    void ShowOrderOfProjections_maybe(){
	        ProjectorCameras_MGR projs = ProjectorCameras_MGR.instance;
	        if(projs ==null){ return; } //scenes are probably still loading

	        bool key_held =  Keyboard.current.rKey.isPressed;
	        bool key_down =  Keyboard.current.rKey.wasPressedThisFrame;
	        bool hovering =  isCursorHoveringMe() || KeyMousePenInput.isSomeInputFieldActive()==false;

	        if (!hovering || !key_held){
	            projs._showOrderOfProjections = false;
	            return;
	        }
	        projs._showOrderOfProjections = true;
	        //user wants to erase the mask. Exit the inpaint for comfort, to avoid user frustration:
	        if (key_down){ 
	            WorkflowRibbon_UI.instance.Set_CurrentMode( WorkflowRibbon_CurrMode.ProjectionsMasking, playAttentionAnim:true);
	        }
	    }


	    void OnSettings_ViewportInCenter(bool keepInCenter){
	        if (keepInCenter){
	            _viewportWithRibbons_rect.SetSiblingIndex(1);
	        }else{
	            _viewportWithRibbons_rect.SetAsLastSibling();
	        }
	    }


	    void OnSettings_VerticalRibbonsSwapped(bool isSwapped){
	        RectTransform leftRect  = _viewport_LeftVerticalRibbon_rect;
	        RectTransform rightRect = _viewport_RightVerticalRibbon_rect;
	        if (isSwapped){
	            leftRect.anchorMin = new Vector2(1, 0);
	            leftRect.anchorMax = new Vector2(1, 1);
	            leftRect.pivot     = new Vector2(1, 0.5f);
	            leftRect.anchoredPosition = Vector2.zero;

	            rightRect.anchorMin = new Vector2(0, 0);
	            rightRect.anchorMax = new Vector2(0, 1);
	            rightRect.pivot     = new Vector2(0, 0.5f);
	            rightRect.anchoredPosition = Vector2.zero;

	            _viewport_holder_rect.offsetMin = new Vector2(rightRect.sizeDelta.x, 0);
	            _viewport_holder_rect.offsetMax = new Vector2(-leftRect.sizeDelta.x, 0);
	            return;
	        }//else not swapped:

	        rightRect.anchorMin = new Vector2(1, 0);
	        rightRect.anchorMax = new Vector2(1, 1);
	        rightRect.pivot     = new Vector2(1, 0.5f);
	        rightRect.anchoredPosition = Vector2.zero;

	        leftRect.anchorMin = new Vector2(0, 0);
	        leftRect.anchorMax = new Vector2(0, 1);
	        leftRect.pivot     = new Vector2(0, 0.5f);
	        leftRect.anchoredPosition = Vector2.zero;

	        _viewport_initial_offset_min = _viewport_holder_rect.offsetMin = new Vector2(leftRect.sizeDelta.x, 0);
	        _viewport_initial_offset_max = _viewport_holder_rect.offsetMax = new Vector2(-rightRect.sizeDelta.x, 0);
	    }


	#region init
	    void Awake(){
	        if(instance != null){  DestroyImmediate(this.gameObject); return; }
	        instance = this;
	        Settings_MGR._Act_viewportInCenterChanged += OnSettings_ViewportInCenter;
	        Settings_MGR._Act_verticalRibbonsSwapped += OnSettings_VerticalRibbonsSwapped;
	        _viewport_initial_offset_min = _viewport_holder_rect.offsetMin;
	        _viewport_initial_offset_max = _viewport_holder_rect.offsetMax;
	    }

	    void Start(){
	        EarlyUpdate_callbacks_MGR.instance.onEarlyUpdate3 += OnEarlyUpdate;
	        OnSettings_ViewportInCenter( Settings_MGR.instance.get_viewport_in_center() );
	        OnSettings_VerticalRibbonsSwapped( Settings_MGR.instance.get_viewport_isSwapVerticalRibbons() );
	    }

	    void OnDestroy(){
	        if (EarlyUpdate_callbacks_MGR.instance != null){
	            EarlyUpdate_callbacks_MGR.instance.onEarlyUpdate3 -= OnEarlyUpdate;
	        }
	    }
	#endregion
	}
}//end namespace
