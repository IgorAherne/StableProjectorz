using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class ControlNetUnits_ThumbsList_UI : MonoBehaviour
	{
	    [SerializeField] List<RectTransform> _listHover;
	    [Space(10)]
	    [SerializeField] RectTransform _thumbs_parent;
	    [SerializeField] ControlNetUnit_Thumb_UI _thumb_PREFAB;
	    [Space(10)]
	    [SerializeField] ControlNetUnit_UI _unit_previewPanel;
	    [SerializeField] CanvasGroup _unit_previewPanel_canvGrp;

	    List<ControlNetUnit_Thumb_UI> _thumbs = new List<ControlNetUnit_Thumb_UI>(); // Initialize the list

	    public ControlNetUnit_Thumb_UI _clickedThumb { get; private set; } = null;
	    public bool isShowing_previewPanel => _unit_previewPanel.gameObject.activeSelf;
	    public bool _isHoveringSomePanels { get; private set; } = false;


	    void Update(){
	        // Ensure thumbs match the ControlNet units
	        RemoveThumb_withDeadUnits();
	        SD_ControlNetsList_UI.instance?.DoForEvery_CtrlUnit( Match_Thumbs_to_Units );
	        //Refresh BEFORE the hide, in case if we need
	        //to urgently apply info (custom image picked, etc)
	        Refresh_the_real_ControlUnit(); 
	        HidePanel_maybe();              
	    }
    
	    void RemoveThumb_withDeadUnits(){
	        for(int i=0; i<_thumbs.Count; ++i){
	            ControlNetUnit_Thumb_UI thumb = _thumbs[i];
	            if(thumb._myUnit != null){ continue; }
	            Destroy(thumb.gameObject);
	            _thumbs.RemoveAt(i);
	            i--;
	        }
	    }


	    void Match_Thumbs_to_Units(ControlNetUnit_UI unit, int ix){
	        if(ix >= _thumbs.Count){ 
	            InstantiateThumb(unit); 
	        }
	        _thumbs[ix].OnUpdate();
	    }


	    void InstantiateThumb( ControlNetUnit_UI unit ){
	        ControlNetUnit_Thumb_UI newThumb = Instantiate(_thumb_PREFAB.gameObject, _thumbs_parent)
	                                                .GetComponent<ControlNetUnit_Thumb_UI>();
	        _thumbs.Add(newThumb);
	        newThumb.Init(ownerList:this, myUnit:unit);
	    }


	    void Refresh_the_real_ControlUnit(){
	        //copy the data from the preview panel into the actual true Controlnet Unit.
	        //For example, if we chagned some value in the preview panel, we nede to change it in the actual unit too.
	        if(_clickedThumb==null){ return; }
	        _clickedThumb._myUnit.CopyFromAnother(_unit_previewPanel);
	    }


	    int _pretendHover_numFrames = 0;
	    void HidePanel_maybe(){
	        // if we are picking a custom image inside the panel, we need to complete its assignment
	        // and to Copy into the its actual ControlNetUnit.
	        // If so, don't reset the _clickedThumb, keep waiting. (takes 2 frames)
	        _pretendHover_numFrames--;
	        if(_pretendHover_numFrames > 0){ return; }

	        _isHoveringSomePanels = false;
	        Vector2 cursorPos =  KeyMousePenInput.cursorScreenPos();
	        for(int i=0; i<_listHover.Count; ++i){
	            bool objON = _listHover[i].gameObject.activeSelf;
	            _isHoveringSomePanels |=  objON && RectTransformUtility.RectangleContainsScreenPoint(_listHover[i], cursorPos);
	            if(_isHoveringSomePanels){ break; }
	        }
	        if(_isHoveringSomePanels){ _pretendHover_numFrames=1; return;}
	        if (KeyMousePenInput.isLMBpressed()){ _pretendHover_numFrames=1; return; }//maybe dragging some sliders inside the previewPanel.
	        _unit_previewPanel_canvGrp.alpha = 0;
	        _unit_previewPanel_canvGrp.interactable = false;
	        _unit_previewPanel_canvGrp.blocksRaycasts = false;
	        _unit_previewPanel.gameObject.SetActive(false);
	        _clickedThumb = null;
	    }

    
	    void OnClickThumb_ShowPanel( ControlNetUnit_Thumb_UI clickedThumb ){
	        _clickedThumb = clickedThumb;
	        //copy the data from the actual true ControlnetUnit into the preview panel.
	        _unit_previewPanel.CopyFromAnother( _clickedThumb._myUnit );

	        _unit_previewPanel_canvGrp.alpha = 1.0f;
	        _unit_previewPanel_canvGrp.interactable = true;
	        _unit_previewPanel_canvGrp.blocksRaycasts = true;
	        _unit_previewPanel.gameObject.SetActive(true);
	    }


	    void Start(){
	        ControlNetUnit_Thumb_UI._Act_OnUnitThumb_Pressed += OnClickThumb_ShowPanel;

	        //remove placeholders:
	        int numChildren = _thumbs_parent.childCount;
	        for(int i=0; i<numChildren; ++i){
	            DestroyImmediate( _thumbs_parent.GetChild(0).gameObject );
	        }
	    }
	}
}//end namespace
