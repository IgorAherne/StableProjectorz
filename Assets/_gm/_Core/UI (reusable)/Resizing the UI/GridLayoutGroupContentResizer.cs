using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace spz {

	// Igor Aherne Jan 2024  facebook.com/igor.aherne
	//
	// enforces the contents of the GridLayoutGroup to be resized relative to 
	// the required dimention. 
	//
	// You can specify aspect and the percentage of the orientation-dimension.
	public class GridLayoutGroupContentResizer : LayoutGroup {
	         public enum Orientation { WidthControllsHeight, HeightControllsWidth }
	    [SerializeField] Orientation _orientation = Orientation.WidthControllsHeight;
    
	    [SerializeField] float _aspectRatio = 1;
	    [SerializeField] bool _orUseScreenAspect = false;//if true, we will assume the same aspect as the display's one.

	    //we will scale resulting elements (after aspect adjustment) by this percentage of our width or height:
	              public float cell_pcntOfMySize => _cell_pcntOfMySize;
	    [SerializeField] float _cell_pcntOfMySize = 0.2f;
	    [SerializeField] float _spacing_pcntOfMySize = 0;

	    RectTransform _rectTransf;

	    public Action _onUpdatedLayout { get; set; } //invoked when we repositioned the cells inside self.
	    bool _didUpdateLayouts = false;

	    public Vector2 cellSize_px => _cellSize;
	    Vector2 _cellSize;

	    public void change_cell_pcntOfMySize(float newPcnt){
	        _cell_pcntOfMySize = newPcnt-0.00001f; //minu tiny value because sometimes 0.3333337 prevents fitting 3 squares.
	        LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
	    }

	    public float get_spacing_px(){
	        return _orientation==Orientation.WidthControllsHeight ? _rectTransf.rect.width * _spacing_pcntOfMySize
	                                                             : _rectTransf.rect.height * _spacing_pcntOfMySize;
	    }

	    //line could be horizontal or vertical depending on our _orientation.
	    public int numCells_perLine(){
	        _rectTransf = _rectTransf??transform as RectTransform;
	        Vector2 size = _rectTransf.rect.size;
	        return _orientation==Orientation.WidthControllsHeight ?  Mathf.FloorToInt(size.x / (_cellSize.x + _spacing_pcntOfMySize*size.x))
	                                                               : Mathf.FloorToInt(size.y / (_cellSize.y + _spacing_pcntOfMySize*size.y));
	    }

	    public List<RectTransform> get_enabledCells(){
	        int childCount = transform.childCount;
	        List<RectTransform> active = new List<RectTransform>(childCount);
	        for(int i=0; i<childCount; ++i){
	            Transform child = transform.GetChild(i);
	            if(child.gameObject.activeSelf){  active.Add(child as RectTransform);  }
	        }
	        return active;
	    }

	    public void ManuallyTriggerResize(){
	        _rectTransf = _rectTransf??transform as RectTransform;
	        LayoutRebuilder.MarkLayoutForRebuild(_rectTransf);
	    }


	    public override void CalculateLayoutInputHorizontal(){
	        base.CalculateLayoutInputHorizontal();
	        RecalcCellSizes();

	        if(_orientation == Orientation.WidthControllsHeight){
	            float width = _rectTransf.rect.width;// In case of HeightControllsWidth, the width 
	            SetLayoutInputForAxis(width, width, -1, 0);// is not directly controlled by this layout
	            return;
	        }
	        //else HeightControlsWidth:
	        // Calculate the total width based on the total height from Resize method
	        float totalWidth = CalcContainerLength(_cellSize, _orientation);
	        SetLayoutInputForAxis(totalWidth, totalWidth, -1, 0); // Assigning the total width to the layout system
	    }


	    public override void CalculateLayoutInputVertical(){
	        RecalcCellSizes();

	        if(_orientation == Orientation.HeightControllsWidth){
	            float height = _rectTransf.rect.height; // In case of HeightControlsWidth, the height 
	            SetLayoutInputForAxis(height, height, -1, 1); // is not directly controlled by this layout.
	            return;
	        }
	        // Calculate the total height based on the total width from Resize method
	        float totalHeight = CalcContainerLength(_cellSize, _orientation);
	        SetLayoutInputForAxis(totalHeight, totalHeight, -1, 1); // Assigning the total height to the layout system
	    }


	    void RecalcCellSizes(){
	        _rectTransf = _rectTransf ?? transform as RectTransform;

	        float ratio = _orUseScreenAspect ? Screen.width / (float)Screen.height : _aspectRatio;
	        Vector2 size = _rectTransf.rect.size;
	        float cellDimension = _orientation==Orientation.WidthControllsHeight ? size.x*_cell_pcntOfMySize : size.y*_cell_pcntOfMySize;

	        // Calculate cell size based on aspect ratio
	        _cellSize  =  _orientation==Orientation.WidthControllsHeight? new Vector2(cellDimension, cellDimension / ratio) 
	                                                                    : new Vector2(cellDimension * ratio, cellDimension);
	    }


	    float CalcContainerLength(Vector2 cellSize, Orientation orientation){

	        float spacing_px = get_spacing_px();
	        // Calculate number of cells per line (row or column)
	        List<RectTransform> enabledCells = get_enabledCells();
	        int cellCount = enabledCells.Count;
	        int cellsPerLine = numCells_perLine();
	        cellsPerLine =  cellsPerLine <= 0? 1 : cellsPerLine;// Avoid division by zero

	        // Calculate number of lines needed
	        int lineCount = Mathf.CeilToInt( cellCount/(float)cellsPerLine );
	        int ix = 0;
	        float totalDimension = 0;

	        // Calculate total HEIGHT (width dictates it)
	        if (orientation == Orientation.WidthControllsHeight){

	            for(int l=0; l<lineCount; ++l){
	                float lineHeight = 0;
	                for(int c=0; c<cellsPerLine; ++c){ 
	                    if(ix >= cellCount){ c = l = int.MaxValue-1; break; }//stops both loops, allowing one final 'totalDimension +='
	                    RectTransform cellTransf = enabledCells[ix];
	                    float cellHeight =  cellTransf.localScale.y * cellSize.y; 
	                    lineHeight =  lineHeight>=cellHeight? lineHeight : cellHeight;
	                    ix++;
	                }
	                totalDimension +=  lineHeight + spacing_px;
	            }
	        }else{//calculate total WIDTH (height dictates it):
	            for(int l=0; l<lineCount; ++l){
	                float lineWidth = 0;
	                for(int c=0; c<cellsPerLine; ++c){
	                    if(ix >= cellCount){ c = l = int.MaxValue-1; break; }//stops both loops, allowing one final 'totalDimension +='
	                    RectTransform cellTransf =  enabledCells[ix];
	                    float cellWidth =  cellTransf.localScale.x * cellSize.x;
	                    lineWidth =  lineWidth>=cellWidth? lineWidth : cellWidth;
	                    ix++;
	                }
	                totalDimension +=  lineWidth + spacing_px;
	            }
	        }
	        return totalDimension;
	    }


	    public override void SetLayoutHorizontal(){
	        float spacing_px = get_spacing_px();

	        float onIter(RectTransform cell, float currOffset){
	            SetChildAlongAxisWithScale(cell, 0, currOffset, _cellSize.x, cell.localScale.x);
	            return _cellSize.x*cell.localScale.x + spacing_px;
	        }
	        SetLayout( isHorizontal:true,  OnIter:onIter );
	        _didUpdateLayouts = true;
	    }


	    public override void SetLayoutVertical(){
	        float spacing_px = get_spacing_px();

	        float onIter(RectTransform cell, float currOffset){
	            SetChildAlongAxisWithScale(cell, 1, currOffset, _cellSize.y, cell.localScale.y);
	            return _cellSize.y*cell.localScale.y + spacing_px;
	        }
	        SetLayout( isHorizontal:false,  OnIter:onIter );
	        _didUpdateLayouts = true;
	    }


	    //OnIter: RectTransform:childCell,  float:currentOffsetSoFar,  returns float: pixelSizeOfCell_withSpacing
	    void SetLayout(bool isHorizontal, Func<RectTransform,float,float> OnIter ){

	        int cellsPerLine = numCells_perLine();
	        cellsPerLine =  cellsPerLine <= 0? 1 : cellsPerLine;// Avoid division by zero

	        float currOffset = 0;
	        int numInLine = 0;
	        var marchingDir  = isHorizontal? Orientation.WidthControllsHeight : Orientation.HeightControllsWidth;

	        List<RectTransform> enabledCells = get_enabledCells();
	        int cellCount = enabledCells.Count;

	        if(_orientation == marchingDir){
	            for (int i=0; i<cellCount; i++){
	                float size = OnIter(enabledCells[i], currOffset);
	                currOffset += size;
	                numInLine++;
	                if(numInLine < cellsPerLine){ continue; }
	                numInLine = 0;
	                currOffset=0;
	            }
	        }else { 
	            for (int i=0; i<cellCount; i++){
	                float size = OnIter(enabledCells[i], currOffset);
	                numInLine++;
	                if(numInLine < cellsPerLine){ continue; }
	                numInLine = 0;
	                currOffset += size;
	            }
	        }
	    }


	    //will force immediate recalculation of dimensions.
	    //This is important when loading project save-files, and will spawn icons
	    public void OnLoad(){
	        this.CalculateLayoutInputHorizontal();//will recalculate cellSize instantly.
	    }

    
	#region monoMethods
	#if UNITY_EDITOR
	    protected override void OnValidate(){
	        RecalcCellSizes();
	        LayoutRebuilder.MarkLayoutForRebuild(_rectTransf);
	        base.OnValidate();
	    }
	    public void OnValidate_forCustomEditor() => OnValidate();
	#endif
	    protected override void OnDidApplyAnimationProperties() =>LayoutRebuilder.MarkLayoutForRebuild(_rectTransf);
	    protected override void OnRectTransformDimensionsChange() =>LayoutRebuilder.MarkLayoutForRebuild(_rectTransf);
	    protected override void Start(){  RecalcCellSizes(); }
	    protected void Update(){
	        if(_didUpdateLayouts){ 
	            _didUpdateLayouts = false; 
	            _onUpdatedLayout?.Invoke(); 
	        }
	    }
	#endregion
	}
}//end namespace
