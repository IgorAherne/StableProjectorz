using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace spz {

	public class DraggableItem_UI : MonoBehaviour{

	    [SerializeField] CanvasGroup _myCanvasGrp;
	    [Space(10)]
	    [SerializeField] MouseGrab_Sensor_UI _grabSensor;//user can click this to drag us.
	    [SerializeField] FadeOutUnlessPersist_UI _grabSensorFader;//might be null

	    [SerializeField] float _returnToSqure_dur = 0.2f;
	    [SerializeField] float _followTheGrabbed_speed = 1;
	    [SerializeField] float _followTheGrabbed_speedDamp = 2;

	    RectTransform _myRectTransf;
	    bool _resized_to_square_after_init = false;//helps to ensure that we match the size of our square, once.

	    public DraggableItems_Grid_UI _myGrid { get; private set; }//where this item is parented, next to its siblings.
    
	    public List<DraggableItem_UI> _me_and_siblings { get; private set; }//SHARED list, pointed to by all my sibling items.
	    public List<DraggableItem_GridSquare_UI> _squares { get; private set; }//SHARED list.  Removing/adding will be seen by all siblings.
    
	    public DraggableItem_UI first_inSiblings => _me_and_siblings[0];
	    public DraggableItem_UI last_inSiblings => _me_and_siblings[_me_and_siblings.Count-1];

	    public bool groupIsGrabbed => _currentlyGrabbedSibling != null;
	    Vector2 _grabbed_cursorOffset;//when this item or sibling was grabbed, how far was THIS item from cursor.
	    DraggableItem_UI _currentlyGrabbedSibling = null;//only related to this group (belongs to '_me_and_siblings')

	    public DraggableItem_GridSquare_UI _mySquare { get; private set; }//even if I'm not currently parented under this square.

	    DraggableItem_GridSquare_UI _hoveredSquare;
	    Coroutine _return_to_mySquare_crtn = null;


	    public void Init( DraggableItem_GridSquare_UI currentSquare, 
	                      List<DraggableItem_UI> me_and_siblings, 
	                      List<DraggableItem_GridSquare_UI> squares ){
	        _myRectTransf = transform as RectTransform;
	        _mySquare = currentSquare;
	        _mySquare._myItem = this;
	        _myGrid = currentSquare._myGrid;
	        _myRectTransf.localPosition =  Vector3.zero;
	        _myRectTransf.localScale =  Vector3.one * _myGrid.cellSize_pcntOfGrid/0.5f;
	        _myRectTransf.sizeDelta = _myGrid.cellSize_px * (0.5f/_myGrid.cellSize_pcntOfGrid - 1);
	        _me_and_siblings =  me_and_siblings;
	        _squares = squares;
	        _myGrid.onUserRearanging += DetachIfRearanging_maybe;
	        _myGrid.onUserRearangingStop += DetachIfRearanging_maybe;//same function as during start-rearanging.
	    }


	    public bool isCoord_earlierThanGroup(Vector2 coord){
	        //notice CURRENT SQUARE, not Items. That's because items might be parented away from the square
	        RectTransform firstSib_rectTrsf = first_inSiblings._mySquare.transform as RectTransform;
	        RectTransform lastSib_rectTrsf  = last_inSiblings._mySquare.transform as RectTransform;

	        Vector3 firstPos = firstSib_rectTrsf.position;
	        Vector3 lastPos  = lastSib_rectTrsf.position;

	        float squareHeightHalf =  0.5f*firstSib_rectTrsf.rect.height * firstSib_rectTrsf.lossyScale.y;
        
	        float middleY =  0.5f*(firstPos.y + lastPos.y);
	        bool allOnSameHeight =  Mathf.Abs(middleY - firstPos.y) < 0.01f;

	        bool cursorInsideTheRow =   coord.y < firstPos.y+squareHeightHalf  
	                                 && coord.y > lastPos.y-squareHeightHalf;

	        if(allOnSameHeight  &&  cursorInsideTheRow){
	            if(coord.x < firstPos.x){ return true; }
	            if(coord.x > lastPos.x){ return false; }
	            //else, somewhere-in-between the first and last. So compare by height:
	        }
	        return coord.y > middleY;
	    }


	    bool isSquare_ofSibling(DraggableItem_GridSquare_UI square){
	        return _squares.Contains(square);
	    }

  

	    void LateUpdate(){
	        DragAround_asGrabbed_maybe();
	        DragArround_followGrabbed_maybe();
	        HoverOthers_maybe();
	        DetachIfRearanging_maybe();
	        //prevents rare bug when icon keeps following the mouse
	        //even though users released mouse button:   (July 2024)
	        if(_currentlyGrabbedSibling == this && !KeyMousePenInput.isLMBpressed() && !KeyMousePenInput.isRMBpressed()){
	            var p = new PointerEventData(EventSystem.current);
	            p.position = KeyMousePenInput.cursorScreenPos();
	            OnDropped(p);
	        }
	    }
     

	    void DragAround_asGrabbed_maybe(){
	        if(_currentlyGrabbedSibling != this){ return; }
	        Vector2 cursorPos = KeyMousePenInput.cursorScreenPos();
	        transform.position = cursorPos + _grabbed_cursorOffset;
	        _grabSensorFader?.FadeInThisFrame();
	    }


	    void DragArround_followGrabbed_maybe(){
	        if(_currentlyGrabbedSibling == null || _currentlyGrabbedSibling == this){ 
	            return; 
	        }
	        float dist = Vector3.Distance(transform.position, _currentlyGrabbedSibling.transform.position);
	        dist /= _currentlyGrabbedSibling._myRectTransf.rect.width; // Normalize distance

	        // Adjust the following formula as needed
	        float lerpFactor = 1 / (1 + dist * _followTheGrabbed_speedDamp);
	        lerpFactor = Mathf.Clamp(lerpFactor, 0, 1); // Ensure lerpFactor stays within 0 and 1

	        float lerpSpeed = Time.deltaTime * _followTheGrabbed_speed * lerpFactor;

	        Vector2 cursorPos = KeyMousePenInput.cursorScreenPos();
	        transform.position = Vector3.Lerp(transform.position, cursorPos + _grabbed_cursorOffset, lerpSpeed);
	    }


	    void HoverOthers_maybe(){
	        if(_currentlyGrabbedSibling == null){ return; }
	        if(_currentlyGrabbedSibling != this){ return; }
	        //while I am being dragged (I was the one clicked and dragged), see if I'm hovering other icons, or sensors:
	        List<RaycastResult> results = Hover_Raycast();
	        Hover_ScrollSensor_maybe(results);
	        Hover_Square_maybe(results);
	        wrapHoveredSquare_maybe();

	        // We are the actively-dragged square.
	        // If we know that we hover something, tell our siblings to hide their own squares.
	        // The siblings kept them active even when we started dragging, to avoid initial jitter.
	        // But now they can be disabled, since hovered squares will already start adding padding anyway:
	        if(_hoveredSquare!=null){  _squares.ForEach(sq=>sq.gameObject.SetActive(false));  }
	    }

	    List<RaycastResult> Hover_Raycast(){
	        var cursor = new PointerEventData(EventSystem.current);
	        cursor.position = KeyMousePenInput.cursorScreenPos();
	        var results = new List<RaycastResult>();
	        _myGrid.raycaster.Raycast(cursor, results);
	        return results;
	    }

	    void Hover_ScrollSensor_maybe( List<RaycastResult> results ){ 
	        IconUI_List_ScrollSensor scrollSensor = null;
	        int results_ix = results.FindIndex( r=>r.gameObject.tag == "IconUI_List_ScrollSensor");
	        if(results_ix != -1){  scrollSensor = results[results_ix].gameObject.GetComponent<IconUI_List_ScrollSensor>();  }
	        float itemHeight = _myRectTransf.rect.height;
	        scrollSensor?.Scroll(itemHeight);
	    }


	    void Hover_Square_maybe( List<RaycastResult> results ){
	        DraggableItem_GridSquare_UI hoveredSquare = null;
	        int results_ix = results.FindIndex( r=>r.gameObject.tag == "DraggableItemSquare" );
	        if(results_ix != -1){  hoveredSquare = results[results_ix].gameObject.GetComponentInParent<DraggableItem_GridSquare_UI>();  }

	        if(hoveredSquare==null  ||  isSquare_ofSibling(hoveredSquare)){ return; }
     
	        _hoveredSquare = hoveredSquare;
	        if(_hoveredSquare._isForPaddingOnly){ return; }

	        var hoveredItem = _hoveredSquare._myItem;
	        bool isEarlier = hoveredSquare._myItem.isCoord_earlierThanGroup( KeyMousePenInput.cursorScreenPos() );
	        if(isEarlier){ 
	            _myGrid.EnsureFreeSquares_EarlierThan(hoveredItem, _me_and_siblings.Count);
	        }else { 
	            _myGrid.EnsureFreeSquares_LaterThan(hoveredItem, _me_and_siblings.Count);
	        }
	    }


	    //is if you are hovering an square with an item (S) that sits on the left-most or right most part of the grid.
	    //If such item has free-square earlier than it (F), on previous line, then we might swap them.
	    //  O  O  F           O  O  S
	    // .S  O  O    -->   .F  O  O   (the . represents a cursor hovering the location)
	    //
	    //  or  
	    //
	    //  O  O  S.         O  O  F.
	    //  F  O  O    -->   S  O  O
	    void wrapHoveredSquare_maybe(){
	        if(_hoveredSquare == null || _hoveredSquare._isForPaddingOnly){ return; }

	        int mySiblingsCount = _me_and_siblings.Count;
	        var hoveredItem = _hoveredSquare._myItem;

	        bool isItemBelow_FirstFree;
	        int numFree_earlier = _myGrid.numFreeSquares_EarlierThan(hoveredItem, out isItemBelow_FirstFree);

	        bool isItemAbove_FirstFree;
	        int numFree_later = _myGrid.numFreeSquares_LaterThan(hoveredItem, out isItemAbove_FirstFree);

	        if (numFree_earlier<=mySiblingsCount  &&  isItemBelow_FirstFree){
	            _myGrid.EnsureFreeSquares_LaterThan(hoveredItem, _me_and_siblings.Count);
	            return;
	        }
	        if (numFree_later<=mySiblingsCount  &&  isItemAbove_FirstFree){
	            _myGrid.EnsureFreeSquares_EarlierThan(hoveredItem, _me_and_siblings.Count);
	            return;
	        }
	    }

    
	    void DetachIfRearanging_maybe(){
	        if(_myGrid == null){ return; }
	        //only if we DON'T have sibling that's currently dragged (we are just one of other squares in grid)
	        if(_currentlyGrabbedSibling!=null){ return; } 
	        if(_myGrid.gameObject.activeInHierarchy==false){ return; }//don't unparent if our grid is hidden. this prevents showing icons when panel is hidden.
	        if (UI_CurrentlyDraggedIcons_Canvas.instance == null){ return; }

	        bool isAranging = _myGrid.isUser_rearranging;
	        var curPar    = transform.parent;
	        var globalPar = _myGrid.canvas_for_dragged_icons.transform;

	        if (isAranging  &&  curPar!=globalPar){ 
	            transform.SetParent( globalPar,  worldPositionStays:true );
	            transform.SetAsFirstSibling();//to be drawn underneath the ones that user is dragging by mouse.
	        }
	        else if(!isAranging  &&  curPar==globalPar  &&  _return_to_mySquare_crtn==null){
	            _return_to_mySquare_crtn  =  StartCoroutine( Return_to_mySquare_crtn(_returnToSqure_dur) );
	        }
	        //smoothely follow the position of current-square (while detached from it)
	        if(transform.parent==globalPar && _return_to_mySquare_crtn==null){ 
	            float lerpSpeed = Time.deltaTime * _followTheGrabbed_speed / 5;
	            transform.position = Vector3.Lerp(transform.position, _mySquare.transform.position, lerpSpeed);
	        }
	    }


	    void OnGrabbed(PointerEventData p){
	        _myGrid.StartedDragging_SomeItem(this);
	        _me_and_siblings.ForEach( sib=>sib.OnSibling_WasGrabbed(this) );
	    }


	    void OnDropped(PointerEventData p){
	        if (OnDroppedBelowAboveAll_maybe(p.position)){  
	            OnDroppped_Finish(); 
	            return;  
	        }
	        OnDropped_HasHoveredSquare_maybe( p.position );
	        OnDroppped_Finish();
	    }


	    bool OnDroppedBelowAboveAll_maybe(Vector2 cursorPos){
	        _myGrid.isAboveOrBelow_AllSquares( cursorPos, this, out bool isAboveAll, out bool isBelowAll);
	        if(!isAboveAll && !isBelowAll){ return false; }
         
	        if(isAboveAll){
	            for(int i=_squares.Count-1; i>=0; --i){ _squares[i].transform.SetAsFirstSibling();  }//in REVERSE order
	        }
	        if (isBelowAll){ 
	            for(int i=0; i<_squares.Count; ++i){  _squares.ForEach(sq => sq.transform.SetAsLastSibling()); }//in USUAL order 
	        }
	        return true;
	    }


	    void OnDropped_HasHoveredSquare_maybe(Vector2 cursorPos){
	        if (_hoveredSquare == null){ return;}

	        _squares.ForEach( sq=>sq.transform.SetAsLastSibling() );
        
	        bool isBefore =  true;
	        int ix;

	        if (_hoveredSquare._isForPaddingOnly){
	            ix = _hoveredSquare.transform.GetSiblingIndex();
	        }else{
	            DraggableItem_UI hoveredItem = _hoveredSquare._myItem;
	            isBefore =  hoveredItem.isCoord_earlierThanGroup(cursorPos);
	            DraggableItem_GridSquare_UI square;
	            if(isBefore){
	                _myGrid.EnsureFreeSquares_EarlierThan( hoveredItem, _me_and_siblings.Count);
	                square = _myGrid.get_PaddingSquare_before( hoveredItem );
	                ix = square.transform.GetSiblingIndex();
	            }else {
	                _myGrid.EnsureFreeSquares_LaterThan( hoveredItem, _me_and_siblings.Count );
	                square = _myGrid.get_paddingSquare_after( hoveredItem );
	                ix = square.transform.GetSiblingIndex();
	            }
	        }

	        for(int i=_squares.Count-1; i>=0; --i){ _squares[i].transform.SetSiblingIndex(ix); }//in REVERSE order
	    }


	    void OnDroppped_Finish(){
	        _myGrid.EndedDragging_SomeItem(this);
	        _me_and_siblings.ForEach(sib => sib.OnSibling_WasDropped());
	        _hoveredSquare = null;
	    }


	    void OnSibling_WasGrabbed(DraggableItem_UI theGrabbedOne){
	        _currentlyGrabbedSibling = theGrabbedOne;
	        _myCanvasGrp.blocksRaycasts = false;
	        transform.SetParent( _myGrid.canvas_for_dragged_icons.transform,  worldPositionStays:true );
	        transform.SetAsLastSibling();
	        Vector2 myPos = new Vector2(transform.position.x, transform.position.y);
	        Vector2 cursorPos = KeyMousePenInput.cursorScreenPos();
	        _grabbed_cursorOffset =  myPos - cursorPos;
	        // NOTICE: don't hide _mySquare yet. We will hide it as soon as start hovering some other square.
	        // Otherwise, the layout would collapse, causing a jitter. Keep our squares active under selves during drag-start.
	    }


	    void OnSibling_WasDropped(){
	        _currentlyGrabbedSibling = null;
	        _myCanvasGrp.blocksRaycasts = true;
	        if(_return_to_mySquare_crtn != null){  StopCoroutine(_return_to_mySquare_crtn); }
	        _return_to_mySquare_crtn  =  StartCoroutine( Return_to_mySquare_crtn(_returnToSqure_dur) );
	        _mySquare.gameObject.SetActive(true);
	    }

     
	    IEnumerator Return_to_mySquare_crtn(float dur=0.2f){
	        float startTime = Time.time;
	        Vector3 fromPos = transform.position;
	        while (true){
	            float elapsed01 = (Time.time - startTime)/dur;
	                  elapsed01 = Mathf.Clamp01(elapsed01);
	            if(elapsed01 == 1.0f){ break; }

	            elapsed01 = Mathf.SmoothStep(0, 1, elapsed01);
	            transform.position =  Vector3.Lerp(fromPos, _mySquare.transform.position,  elapsed01 );

	            //important, if user pressed 'Change Num Cells Per Row' button and our squares became smaller:
	            Vector3 wantedScale  = Vector3.one*_myGrid.cellSize_pcntOfGrid/0.5f;
	            transform.localScale = Vector3.Lerp(transform.localScale, wantedScale, elapsed01);
	            yield return null;
	        }
	        transform.SetParent( _mySquare.transform,  worldPositionStays:true );
	        transform.localPosition = Vector3.zero; //improtant, else occasionally elements have a few millimiters off (not sure why).
	        _return_to_mySquare_crtn = null;
	    }


	    void Awake(){
	        if(_grabSensor == null){ this.enabled=false; return; }//so that Icon3D_UI works inside a grid.
	        _grabSensor._onPointerDown += OnGrabbed;
	        _grabSensor._onPointerUp += OnDropped;
	    }


	    void OnDestroy(){
	        _me_and_siblings?.Remove(this); //NOTICE: list is SHARED across all siblings. So we just removed self for them too.
	        _squares?.Remove(_mySquare);
	        _mySquare?.DestroySelf();
	        _me_and_siblings = null;
	        _squares = null;
	        _mySquare = null;

	        if (_grabSensor != null){
	            _grabSensor._onPointerDown -= OnGrabbed;
	            _grabSensor._onPointerUp -= OnDropped;
	        }
	        if (_myGrid != null){
	            _myGrid.onUserRearanging -= DetachIfRearanging_maybe;
	            _myGrid.onUserRearangingStop -= DetachIfRearanging_maybe;//same function as during start-rearanging.
	        }
	    }
	}
}//end namespace
