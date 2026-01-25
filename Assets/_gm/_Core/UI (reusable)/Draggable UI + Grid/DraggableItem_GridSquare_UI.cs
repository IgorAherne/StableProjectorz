using UnityEngine;

namespace spz {

	// sits inside the GridLayoutGroupContentResizer, nearby with other squares.
	// Can contain one Item (item can be dragged away from here).
	public class DraggableItem_GridSquare_UI : MonoBehaviour{

	    public DraggableItem_UI _myItem { get; set; } = null;//even if item is not parented under me.
	    public DraggableItems_Grid_UI _myGrid { get; private set; }
    
	    public bool _isForPaddingOnly { get; private set; }

	    public void Init(DraggableItems_Grid_UI myGrid, bool isForPaddingOnly){
	        gameObject.tag = "DraggableItemSquare";
	        _myGrid = myGrid;
	        _isForPaddingOnly = isForPaddingOnly;
	    }

	    public void DestroySelf(){
	        _myGrid.ForgetDestroyedSquare(this);
	        _myItem = null;
	        _isForPaddingOnly = false;
	        Destroy(this.gameObject);
	    }
	}
}//end namespace
