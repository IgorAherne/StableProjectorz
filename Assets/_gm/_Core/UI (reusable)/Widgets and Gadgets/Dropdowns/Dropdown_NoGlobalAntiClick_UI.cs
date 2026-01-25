using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;

namespace spz {

	[RequireComponent(typeof(Dropdown_ClickThroughAllowThose_UI))]
	public class Dropdown_NoGlobalAntiClick_UI : TMP_Dropdown
	{
	    List<RectTransform> _parentsToAllow;
	    CustomDropdownBlockerRaycaster _customRaycaster;

	    protected override void Start(){
	        base.Start();
	        _parentsToAllow = GetComponent<Dropdown_ClickThroughAllowThose_UI>().allowThose;
	    }

	    protected override GameObject CreateBlocker(Canvas rootCanvas){
	        GameObject blocker = base.CreateBlocker(rootCanvas);
	        if (blocker != null)
	        {
	            GraphicRaycaster blockerRaycaster = blocker.GetComponent<GraphicRaycaster>();
	            if (blockerRaycaster != null)
	            {
	                Destroy(blockerRaycaster);
	                _customRaycaster = blocker.AddComponent<CustomDropdownBlockerRaycaster>();
	                _customRaycaster.Init(this, _parentsToAllow);
	            }
	        }
	        return blocker;
	    }

	    public override void OnPointerClick(PointerEventData eventData)
	    {
	        if (_customRaycaster != null && _customRaycaster.IsClickAllowed(eventData)){
	            // If the click is on an allowed area, don't close the dropdown
	            return;
	        }
	        base.OnPointerClick(eventData);
	    }
	}


	public class CustomDropdownBlockerRaycaster : GraphicRaycaster
	{
	    Dropdown_NoGlobalAntiClick_UI parentDropdown;
	    List<RectTransform> allowedParents;

	    public void Init(Dropdown_NoGlobalAntiClick_UI dropdown, List<RectTransform> parents){
	        parentDropdown = dropdown;
	        allowedParents = parents;
	    }

	    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList){
	        base.Raycast(eventData, resultAppendList);

	        // Don't clear the results, we want to allow clicks through to allowed areas
	        // Just remove the blocker from results if we hit an allowed area
	        if (resultAppendList.Exists(r => IsChildOfAllowedParent(r.gameObject.transform as RectTransform))){
	            resultAppendList.RemoveAll(r => r.gameObject == gameObject);
	        }
	    }

	    public bool IsClickAllowed(PointerEventData eventData){
	        var results = new List<RaycastResult>();
	        base.Raycast(eventData, results);
	        return results.Exists(r => IsChildOfAllowedParent(r.gameObject.transform as RectTransform));
	    }

	    bool IsChildOfAllowedParent(RectTransform transform){
	        if (transform == null){ return false; }
	        foreach (var parent in allowedParents){
	            if (parent == transform || transform.IsChildOf(parent)){
	                return true;
	            }
	        }
	        return false;
	    }//end()
	}
}//end namespace
