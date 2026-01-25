using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace spz {

	//changes the visibility of the dark-frame (its four sides separately).
	//This is useful when we need to show which icons belong to the same generation.
	// If they are from same generation, borders will be hidden between them
	public class IconUI_DarkBorder : MonoBehaviour{
    
	    [SerializeField] DraggableItem_UI _dragItemScript;
	    [SerializeField] RawImage _img;
	    [SerializeField] RectTransform _content_rectTransf;
	    Material _imgMat = null;

	    Vector2 _anchorsMin_start;
	    Vector2 _anchorsMax_start;

	    DraggableItems_Grid_UI _myGrid;

	    public void ShowBorders(bool left, bool top, bool right, bool bottom){
	        Vector4 vec = new Vector4( left?1:0,  top?1:0,  right?1:0,  bottom?1:0);
	        _imgMat.SetVector("_LTRB_borders", vec);

	        Vector2 anchorMin = Vector2.zero;
	        Vector2 anchorMax = Vector2.one;
        
	        if(left){ anchorMin.x = _anchorsMin_start.x; }
	        if(top){ anchorMax.y = _anchorsMax_start.y; }

	        if(right){ anchorMax.x = _anchorsMax_start.x; }
	        if(bottom){ anchorMin.y = _anchorsMin_start.y; }
	        _content_rectTransf.anchorMin = anchorMin;
	        _content_rectTransf.anchorMax = anchorMax;
	        _content_rectTransf.offsetMin = _content_rectTransf.offsetMax = Vector2.zero;
	    }



	    void OnUpdatedLayout_afterCellsRearranged(){
	        Transform mySquareTransf = _dragItemScript._mySquare.transform;
	        var squares = _dragItemScript._squares;
	        Vector3 myPosition = mySquareTransf.position;

	        bool hasLeftSibling=false, hasRightSibling=false, hasTopSibling=false, hasBotSibling=false;

	        float tolerance = 1;// allow deviation under which we still consider the squares as adjacent.

	        foreach (var square in squares) {
	            if (square.transform == mySquareTransf) continue; // Skip the square itself

	            Vector2 diff =  myPosition - square.transform.position;
	            Vector2 diffAbs =  new Vector2( Mathf.Abs(diff.x), Mathf.Abs(diff.y));

	            if(diffAbs.x < tolerance){
	                if(diff.y > 0){ hasBotSibling = true; }
	                else { hasTopSibling = true; }
	            }

	            if(diffAbs.y < tolerance){
	                if(diff.x > 0){ hasLeftSibling = true; }
	                else { hasRightSibling = true; }
	            }
	        }

	        //if line is wrapping, ensure we DON'T have a border where our line splits:
	        if(!hasBotSibling && !hasTopSibling && !hasRightSibling && _dragItemScript != _dragItemScript.last_inSiblings){  hasRightSibling=true;  }
	        if(!hasBotSibling && !hasTopSibling && !hasLeftSibling && _dragItemScript != _dragItemScript.first_inSiblings){ hasLeftSibling=true; }
        
	        ShowBorders(!hasLeftSibling, !hasTopSibling, !hasRightSibling, !hasBotSibling);
	    }


	    //start (not awake) because we need to ensure '_dragItemScript' is Init().
	    void Start(){
	        if(_dragItemScript._myGrid == null){ return; }//we are a placeholder (will get destroyed now)

	        _myGrid = _dragItemScript._myGrid;
	        _myGrid._onUpdatedLayout_afterRearrange += OnUpdatedLayout_afterCellsRearranged;

	        _anchorsMin_start = _content_rectTransf.anchorMin;
	        _anchorsMax_start = _content_rectTransf.anchorMax;

	        _imgMat = new Material(_img.material);
	        _img.material = _imgMat;
	        OnUpdatedLayout_afterCellsRearranged();
	    }



	    private void OnDestroy(){
	        if (_myGrid != null){
	            _myGrid._onUpdatedLayout_afterRearrange -= OnUpdatedLayout_afterCellsRearranged;
	        }
	        if(_imgMat != null){ DestroyImmediate(_imgMat); }
	        _imgMat = null;
	    }
	}
}//end namespace
