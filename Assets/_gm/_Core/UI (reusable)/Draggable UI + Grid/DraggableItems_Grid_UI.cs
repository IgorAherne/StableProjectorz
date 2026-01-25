using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class DraggableItems_Grid_UI : MonoBehaviour { 

	    [SerializeField] ScrollRect _scrollRect;
	    [SerializeField] GridLayoutGroupContentResizer _gridLayoutGroup;
	    [SerializeField] DraggableItem_GridSquare_UI _square_PREFAB;
	              public GraphicRaycaster raycaster => _raycaster;
	    [SerializeField] GraphicRaycaster _raycaster;
	    public Canvas canvas_for_dragged_icons => UI_CurrentlyDraggedIcons_Canvas.instance?.canvas;
    
                
	    public float cellSize_pcntOfGrid => _gridLayoutGroup.cell_pcntOfMySize;
	    public Vector2 cellSize_px => _gridLayoutGroup.cellSize_px;


	    List<DraggableItem_GridSquare_UI> _squares = new List<DraggableItem_GridSquare_UI>();
	    PaddingSquares _paddingSquares = null;

	    public int numSquares_inRow => _numSquares_inRow;
	    int _numSquares_inRow;

	    public bool isUser_rearranging { get; private set; } = false;
	    public System.Action onUserRearanging { get; set; } = null;
	    public System.Action onUserRearangingStop { get; set; } = null;
	    public System.Action _onUpdatedLayout_afterRearrange { get; set; } //invoked once, soon after we reposition the cells inside self.


	    public void ForgetDestroyedSquare( DraggableItem_GridSquare_UI square ){
	        if (square._isForPaddingOnly){ 
	            _paddingSquares.ForgetDestroyedSquare(square); 
	        }else{ 
	            _squares.Remove(square); 
	        }
	    }


	    public List<DraggableItem_GridSquare_UI> SpawnSquares( int num,  bool adoptSpawnedSquares=true, 
	                                                           bool areForPaddingOnly=false,  int ixName=0){
	        var newSquares = new List<DraggableItem_GridSquare_UI>();

	        for(int i=0; i<num; ++i){
	            var square_go = GameObject.Instantiate(_square_PREFAB, _gridLayoutGroup.transform);
	            square_go.name += (ixName+i);
	            square_go.transform.SetParent( _gridLayoutGroup.transform );
	            square_go.transform.localScale = Vector3.one;

	            var square =  square_go.GetComponent<DraggableItem_GridSquare_UI>();
	            square.Init(this, areForPaddingOnly);

	            if(adoptSpawnedSquares){ _squares.Add( square ); }
	            newSquares.Add(square);
	        }
	        _gridLayoutGroup.ManuallyTriggerResize();
	        return newSquares;
	    }


	    public void EnsureFreeSquares_EarlierThan(DraggableItem_UI item, int numSquares_hopefully){
	        item = item.first_inSiblings;
	        _paddingSquares.PadEarlierThan( item, numSquares_hopefully );
	    }


	    public void EnsureFreeSquares_LaterThan(DraggableItem_UI item, int numSquares_hopefully){
	        item = item.last_inSiblings;
	        _paddingSquares.PadLaterThan( item, numSquares_hopefully );
	    }

	    public int numFreeSquares_EarlierThan(DraggableItem_UI item, out bool isItemBelow_firstFree_){
	        item = item.first_inSiblings;
	        return _paddingSquares.numFreeSquares_EarlierThan(item, out isItemBelow_firstFree_);
	    }

	    public int numFreeSquares_LaterThan(DraggableItem_UI item, out bool isItemAbove_firstFree_){
	        item = item.last_inSiblings;
	        return _paddingSquares.numFreeSquares_LaterThan(item, out isItemAbove_firstFree_);
	    }


	    public void StartedDragging_SomeItem(DraggableItem_UI item){
	        isUser_rearranging = true;
	        onUserRearanging?.Invoke();//AFTER setting to true.
	    }
	    public void EndedDragging_SomeItem(DraggableItem_UI item){
	        _paddingSquares.HideAll();
	        isUser_rearranging = false;
	        onUserRearangingStop?.Invoke();//AFTER setting to false.
	    }


	    public void isAboveOrBelow_AllSquares(Vector2 cursorPos, DraggableItem_UI item, out bool isAboveAll_, out bool isBelowAll_){
	        isAboveAll_ = isBelowAll_ = false;
	        //search for first and last children that are:  active + have an item inside.
	        Transform firstSquare = get_firstSquare_withItem();
	        Transform lastSquare  = get_lastSquare_withItem();
	        if (firstSquare==null){ return; }

	        Vector2 itemSizeHalf   =  0.5f * item.transform.lossyScale  * (item.transform as RectTransform).rect.size;
	        Vector2 squareSizeHalf =  0.5f * firstSquare.transform.lossyScale * (firstSquare as RectTransform).rect.size;
	        isAboveAll_ = item.transform.position.y-itemSizeHalf.y > firstSquare.position.y+squareSizeHalf.y;
	        isBelowAll_ = item.transform.position.y+itemSizeHalf.y < lastSquare.position.y-squareSizeHalf.y;

	        if(isAboveAll_ || isBelowAll_){ return; }

	        bool isInFirstRow =   cursorPos.y > firstSquare.position.y-squareSizeHalf.y 
	                           && cursorPos.y < firstSquare.position.y+squareSizeHalf.y; 
                             
	        bool isInLastRow  =   cursorPos.y > lastSquare.position.y-squareSizeHalf.y
	                           && cursorPos.y < lastSquare.position.y+squareSizeHalf.y;

	        if(isInFirstRow){
	            if(cursorPos.x  <  firstSquare.position.x - squareSizeHalf.x){
	                isAboveAll_ = true;  
	                return;
	            }
	        }

	        if(isInLastRow){
	            if(cursorPos.x  >  lastSquare.position.x + squareSizeHalf.x){
	                isBelowAll_ = true;
	                return;
	            }
	        }
	    }//end()


	    Transform get_firstSquare_withItem(){
	        int numChildren = transform.childCount;
	        for(int i=0; i<numChildren; ++i){
	            Transform child = transform.GetChild(i);
	            if(child.gameObject.activeSelf == false){ continue; }
	            if(child.GetComponent<DraggableItem_GridSquare_UI>()._isForPaddingOnly){ continue; }
	            return child;
	        }
	        return null;
	    }

	    Transform get_lastSquare_withItem(){
	        int numChildren = transform.childCount;
	        for (int i=numChildren-1; i>=0; --i){
	            Transform child = transform.GetChild(i);
	            if(child.gameObject.activeSelf == false){ continue; }
	            if(child.GetComponent<DraggableItem_GridSquare_UI>()._isForPaddingOnly){ continue; }
	            return child;
	        }
	        return null;
	    }

    
	    public DraggableItem_GridSquare_UI get_PaddingSquare_before( DraggableItem_UI item ){
	        DraggableItem_GridSquare_UI square = item.first_inSiblings._mySquare;
	        int ix = square.transform.GetSiblingIndex();
	        DraggableItem_GridSquare_UI earliestPad = null;

	        for(int i=ix; i>=0; i--){
	            var sq = transform.GetChild(i).GetComponent<DraggableItem_GridSquare_UI>();
	            if (sq._isForPaddingOnly){ earliestPad = sq; continue; }
	            if (earliestPad != null){ break; }
	        }
	        return earliestPad;
	    }

	    public DraggableItem_GridSquare_UI get_paddingSquare_after( DraggableItem_UI item ){
	        DraggableItem_GridSquare_UI square = item.last_inSiblings._mySquare;
	        int numChildren = transform.childCount;
	        int ix = square.transform.GetSiblingIndex();
	        DraggableItem_GridSquare_UI earliestPad = null;

	        for(int i=ix; i<numChildren; i++){
	            var sq = transform.GetChild(i).GetComponent<DraggableItem_GridSquare_UI>();
	            if (sq._isForPaddingOnly){ earliestPad = sq; break; }
	        }
	        return earliestPad;
	    }


	    public void ChangeNumCells_perRow(int num){
	        isUser_rearranging = true;
	        onUserRearanging?.Invoke();//AFTER setting to true.
	        float cellPcntSize = 1.0f / num;
	        _gridLayoutGroup.change_cell_pcntOfMySize( cellPcntSize );
	        _numSquares_inRow = num;
	        _paddingSquares.ChangeNumCells_PerRow(num);
	        isUser_rearranging = false;
	        onUserRearangingStop?.Invoke();////AFTER setting to false.
	    }

	    public void MoveItem_and_its_Siblings(DraggableItem_UI item, bool isMoveEarlier){
	        isUser_rearranging = true;
	        onUserRearanging?.Invoke();//AFTER setting to true.

	        if (isMoveEarlier){
	            item = item.first_inSiblings;
	            var itemSquare = item._mySquare;
	            int earlierIx = 0;
	            for(int i=itemSquare.transform.GetSiblingIndex()-1; i>=0; --i){
	                Transform trsf = transform.GetChild(i);
	                var sq = trsf.GetComponent<DraggableItem_GridSquare_UI>();
	                if(sq == null || sq._isForPaddingOnly){ continue; }
	                DraggableItem_UI earlierItem = sq._myItem.first_inSiblings;
	                DraggableItem_GridSquare_UI earlierSquare = earlierItem._mySquare;
	                earlierIx = earlierSquare.transform.GetSiblingIndex();
	                break;
	            }
	            for(int i=item._squares.Count-1; i>=0; --i){
	                item._squares[i].transform.SetSiblingIndex(earlierIx);
	            }
	        }
	        else{
	            int childCount = transform.childCount;
	            item = item.last_inSiblings;
	            var itemSquare = item._mySquare;
	            DraggableItem_GridSquare_UI laterSquare = null;
	            for(int i=itemSquare.transform.GetSiblingIndex()+1; i<childCount; ++i){
	                var sq = transform.GetChild(i).GetComponent<DraggableItem_GridSquare_UI>();
	                if(sq == null || sq._isForPaddingOnly){ continue; }
	                DraggableItem_UI laterItem =  sq._myItem.last_inSiblings;
	                laterSquare =  laterItem._mySquare;
	                break;
	            }
	            if (laterSquare != null){  
	                item._squares.ForEach( sq=>sq.transform.SetAsLastSibling() );// so they don't change idxs as we'll move them further in transform hierarchy.
	                int destinIx = laterSquare==null? childCount : laterSquare.transform.GetSiblingIndex()+1;
	                for(int i=item._squares.Count-1; i>=0; --i){
	                    item._squares[i].transform.SetSiblingIndex(destinIx);
	                }
	            }
	        }
	        isUser_rearranging = false;
	        onUserRearangingStop?.Invoke();////AFTER setting to false.
	    }


	    void OnDidLayoutUpdate(){
	        _onUpdatedLayout_afterRearrange?.Invoke();
	    }


	    //will force immediate recalculation of dimensions.
	    //This is important when loading project save-files, and will spawn icons
	    public void OnLoad(){
	        _gridLayoutGroup.OnLoad();
	    }


	    void Awake(){
	        // Very important, otherwise the grid will return _numSquares_inRow as zero in Build .exe
	        //( will be ok in unity editor)
	        LayoutRebuilder.ForceRebuildLayoutImmediate(_gridLayoutGroup.GetComponent<RectTransform>());
	        Canvas.ForceUpdateCanvases();

	        var entries = _gridLayoutGroup.GetComponentsInChildren<DraggableItem_GridSquare_UI>().ToList();
	        for(int i=0; i<entries.Count; ++i){  Destroy(entries[i].gameObject); }
	        _numSquares_inRow =  _gridLayoutGroup.numCells_perLine();
	        _paddingSquares = new PaddingSquares(this, _numSquares_inRow);
	        _gridLayoutGroup._onUpdatedLayout += OnDidLayoutUpdate;
	    }
	}



	class PaddingSquares{
	    DraggableItems_Grid_UI _myGrid;
	    List<DraggableItem_GridSquare_UI> _padding_available;

	    int _numSquares_inRow;
	    public void ChangeNumCells_PerRow(int numCellsInRow){
	        _numSquares_inRow = numCellsInRow;
	        Prepare_Padding_Squares();
	    }

	    public PaddingSquares( DraggableItems_Grid_UI grid,  int numSquares_inRow ){
	        _myGrid = grid;
	        _numSquares_inRow = numSquares_inRow;
	        _padding_available = new List<DraggableItem_GridSquare_UI>();
	        Prepare_Padding_Squares();
	    }


	    void Prepare_Padding_Squares(){
	        //create squares that we will enable if items will be dragged around.
	        //They will help to see an empty space, where items can be dropped into.
	        int currentTotal = _padding_available.Count;
	        int numNeeded =  _numSquares_inRow*3;
	        int num =  numNeeded-currentTotal;
	        List<DraggableItem_GridSquare_UI> morePadding = _myGrid.SpawnSquares(num,  adoptSpawnedSquares:false,  areForPaddingOnly:true);
	        morePadding.ForEach( sq=>sq.gameObject.SetActive(false) );
	        _padding_available.AddRange(morePadding);
	    }


	    public void PadEarlierThan(DraggableItem_UI item, int numSquares_hopefully){
        
	        int square_siblingIx = item._mySquare.transform.GetSiblingIndex();//then, calc siblingIx of square.

	        if (square_siblingIx > 0){ 
	            Transform prevSibling = _myGrid.transform.GetChild(square_siblingIx-1);
	            var prevSquare = prevSibling.GetComponent<DraggableItem_GridSquare_UI>();
	            if(prevSquare._isForPaddingOnly && prevSquare.gameObject.activeSelf){  return;  }//there is already padding.
	        }
	        _padding_available.ForEach(sq=>sq.gameObject.SetActive(false));

	        int numFreeSquaresNeeded =  Mathf.Min( numSquares_hopefully,  _myGrid.numSquares_inRow*2 );
	        if(numFreeSquaresNeeded > _padding_available.Count){ return; }

	        var chosenPads = new List<DraggableItem_GridSquare_UI>();

	        for (int i=0; i<numFreeSquaresNeeded; ++i){
	            var sq = _padding_available[i];
	            chosenPads.Add(sq);
	            sq.transform.SetAsLastSibling();//as Last, to avoid issues with SetSiblingIndex() on next few lines
	        }

	        square_siblingIx = item._mySquare.transform.GetSiblingIndex();//then, re-calc siblingIx of square.

	        for (int i=0; i< chosenPads.Count; ++i){
	            chosenPads[i].transform.SetSiblingIndex( square_siblingIx );
	            chosenPads[i].gameObject.SetActive(true);
	        }
	    }


	    public void PadLaterThan( DraggableItem_UI item, int numSquares_hopefully ){
	        int square_siblingIx = item._mySquare.transform.GetSiblingIndex();//then, calc siblingIx of square.

	        if (square_siblingIx < _myGrid.transform.childCount-1){ 
	            Transform nextSibling = _myGrid.transform.GetChild(square_siblingIx+1);
	            var nextSquare = nextSibling.GetComponent<DraggableItem_GridSquare_UI>();
	            if(nextSquare._isForPaddingOnly && nextSquare.gameObject.activeSelf){ return; }//there is already padding.
	        }
	        _padding_available.ForEach(sq=>sq.gameObject.SetActive(false));

	        int numFreeSquaresNeeded =  Mathf.Min( numSquares_hopefully,  _myGrid.numSquares_inRow*2 );
	        if(numFreeSquaresNeeded > _padding_available.Count){ return; }

	        var chosenPads = new List<DraggableItem_GridSquare_UI>();
        
	        for(int i=0; i<numFreeSquaresNeeded; ++i){
	            var sq = _padding_available[_padding_available.Count-1];
	            sq.transform.SetAsLastSibling();//as Last.
	            chosenPads.Add(sq);
	        }

	        square_siblingIx = item._mySquare.transform.GetSiblingIndex();//then, re-calc siblingIx of square.

	        for (int i=0; i<chosenPads.Count; ++i){
	            chosenPads[i].transform.SetSiblingIndex( square_siblingIx+1 );
	            chosenPads[i].gameObject.SetActive(true);
	        }
	    }

	    public int numFreeSquares_EarlierThan(DraggableItem_UI item, out bool isItemBelow_firstFree_){
	        int numFree = 0;
	        isItemBelow_firstFree_ = false;
	        int sibIx = item._mySquare.transform.GetSiblingIndex() -1;//-1 to start from the previous square adjacent to item.
	        for(int i=sibIx; i>=0; --i){
	            var square = _myGrid.transform.GetChild(i).GetComponent<DraggableItem_GridSquare_UI>();
	            if(square._isForPaddingOnly==false){ break; }
	            if(numFree==0){  isItemBelow_firstFree_ =  square.transform.position.y > item.transform.position.y; }
	            numFree++;
	        }
	        return numFree;
	    }

	    public int numFreeSquares_LaterThan(DraggableItem_UI item, out bool isItemAbove_firstFree_){
	        int numFree = 0;
	        isItemAbove_firstFree_ = false;
	        int numChildren = _myGrid.transform.childCount;
	        int sibIx = item._mySquare.transform.GetSiblingIndex() +1;//+1 to start from the next square adjacent to item.
	        for(int i=sibIx; i<numChildren; ++i){
	            var square = _myGrid.transform.GetChild(i).GetComponent<DraggableItem_GridSquare_UI>();
	            if(square._isForPaddingOnly==false){ break; }
	            if(numFree==0){  isItemAbove_firstFree_ =  square.transform.position.y < item.transform.position.y; }
	            numFree++;
	        }
	        return numFree;
	    }


	    public void HideAll(){
	        _padding_available.ForEach(sq => sq.gameObject.SetActive(false));
	    }

	    public void ForgetDestroyedSquare( DraggableItem_GridSquare_UI square ){
	        if(square._isForPaddingOnly == false){ return; }
	        _padding_available.Remove(square);
	    }
	}
}//end namespace
