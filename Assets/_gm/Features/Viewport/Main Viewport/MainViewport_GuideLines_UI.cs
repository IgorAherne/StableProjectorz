using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	public class MainViewport_GuideLines_UI : MonoBehaviour
	{
	    [SerializeField] RectTransform _BG_Viewport_rectTransf;
	    [Space(10)]
	    [Header("edges:")]
	    [SerializeField] RectTransform _left_GuideLine;
	    [SerializeField] RectTransform _right_GuideLine;
	    [SerializeField] RectTransform _bottom_GuideLine;
	    [SerializeField] RectTransform _top_GuideLine;
    
	    [Header("2x2 grid:")]
	    [Space(10)]
	    [SerializeField] RectTransform _2x2_gridParent;

	    [Space(10)]
	    [SerializeField] RectTransform _viewport_leftAnchor;
	    [SerializeField] RectTransform _viewport_rightAnchor;
	    [SerializeField] RectTransform _viewport_topAnchor;
	    [SerializeField] RectTransform _viewport_botAnchor;

	    bool _wasShowingGrid = false;
	    int _gridHint_numShown = 0;

	    void Update(){
	        _left_GuideLine.position  = _viewport_leftAnchor.position;
	        _right_GuideLine.position = _viewport_rightAnchor.position;
	        _top_GuideLine.position   = _viewport_topAnchor.position;
	        _bottom_GuideLine.position = _viewport_botAnchor.position;

	        bool isShowGrid = MultiView_Ribbon_UI.instance._isShowGrid;
	        _2x2_gridParent.gameObject.SetActive( isShowGrid );

	        if(isShowGrid){
	            RectTransform viewRectTransf = _viewport_leftAnchor.parent as RectTransform;
	            float width  = viewRectTransf.rect.width;
	            float height = viewRectTransf.rect.height;
	            _2x2_gridParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
	            _2x2_gridParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
	        }

	        if(!_wasShowingGrid && isShowGrid && _gridHint_numShown<2){
	            _gridHint_numShown++;
	            string msg = "Grid is only for a general help, not strict viewports.  Remember to zoom on objects," +
	                         "\nto use projections optimally.  Always try to avoid any empty areas.";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 5, false);
	        }
	        _wasShowingGrid = isShowGrid;
	    }


	    void OnShowViewGrid_2x2(bool isShow){
	        _2x2_gridParent.gameObject.SetActive(isShow);
	    }

	}
}//end namespace
