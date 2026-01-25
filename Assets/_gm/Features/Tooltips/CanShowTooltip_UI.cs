using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace spz {

	public class CanShowTooltip_UI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
	    [SerializeField] float _hoverDelayBeforeShow = 0.5f;
	    [SerializeField] float _mouseVieportMoveThreshold = 0.005f;//if mouse moves too much we restart the hover delay.
    
	    [TextArea][SerializeField] string _defaultMessage = "This is a toolitp\nfor " + nameof(CanShowTooltip_UI);
	    string _overrideMessage = "";//will use it unless it's "".
	    public string set_overrideMessage(string msg) => _overrideMessage = msg;
	    public string tooltipText =>  _overrideMessage!=""? _overrideMessage : _defaultMessage;

	    public bool isHovered => _isHovered;
	    bool _isHovered;
	    bool _requestedShowTooltip;
	    float _hoverStartTime;

	    public void OnPointerEnter(PointerEventData eventData){
	        _isHovered = true;
	        _hoverStartTime = Time.time;
	        _requestedShowTooltip = false;
	    }

	    public void OnPointerExit(PointerEventData eventData){
	        _isHovered = false;
	    }


	    void OnDisable(){
	        _isHovered = false;
	    }


	    public void Update(){
	        if(!_isHovered){ return; }

	        Vector2 dt = KeyMousePenInput.delta_cursor( normalizeByScreenDiagonal:true );
	        if(dt.magnitude > _mouseVieportMoveThreshold){
	            _hoverStartTime = Time.time;
	            return;
	        }
	        float elapsed = Time.time - _hoverStartTime;
	        if(elapsed < _hoverDelayBeforeShow){ return; }

	        if(KeyMousePenInput.isLMBpressed()){ return; } //likely doing something
	        if(KeyMousePenInput.isRMBpressed()){ return; }
	        if(KeyMousePenInput.isKey_CtrlOrCommand_pressed()){ return; }
	        if(KeyMousePenInput.isKey_Shift_pressed()){ return; }
	        if(KeyMousePenInput.isKey_alt_pressed()){ return; }

	        if (_requestedShowTooltip){ return; }
	        _requestedShowTooltip = true;
	        Tootlips_UI_MGR.instance?.ShowTooltipFor(this);
	    }
	}
}//end namespace
