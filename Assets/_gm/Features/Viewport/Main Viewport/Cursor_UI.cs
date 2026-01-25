using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace spz {

	// Sits on the small UI element inside a large view area.
	// Shows and hides different cursors inside itself.
	public class Cursor_UI : MonoBehaviour{
	    public static Cursor_UI instance { get; private set; } = null;

	    [SerializeField] RawImage _cursorImg;
	    [SerializeField] Image _eyeDropperImg;
	    [Space(10)]
	    [SerializeField] protected RectTransform _maskableMove_rectTransf; //cursors affected by RectMask2D
	    [SerializeField] protected RectTransform _nonMaskableMove_rectTransf;

	    [SerializeField] float _brushCursorPreview_shrink = 0.93f;

	    [SerializeField] float cursor_B_min = 0.4f;
	    [SerializeField] float cursor_B_max = 0.2f;
	    [SerializeField] float cursor_C_min = 0.2f;
	    [SerializeField] float cursor_C_max = 0.1f;

	    Material _brushCursor_mat;//for drawing a circle onto the screen

	    LocksHashset_OBJ _keepHidden_lock  = new LocksHashset_OBJ();

	    public void DoHiddenCursor_Lock(object requestor){
	        _keepHidden_lock.Lock(requestor);
	        _cursorImg.transform.parent.gameObject.SetActive(false);
	    }

	    public void NoHiddenCursor_Unlock(object originalRequestor){
	        _keepHidden_lock.Unlock(originalRequestor);
	        if(_keepHidden_lock.isLocked() == false){  _cursorImg.transform.parent.gameObject.SetActive(true);  }
	    }


	    public void PositionCursor(float brushScaleFactor){
        
	        Vector2 brushPos01 = MainViewport_UI.instance.cursorMainViewportPos01;
        
	        //mvoe cursors that should stay inside the viewport:
	        _maskableMove_rectTransf.anchorMin =  _maskableMove_rectTransf.anchorMax  =  brushPos01;
	        _maskableMove_rectTransf.anchoredPosition = Vector3.zero; //ensure it's on top of anchors.

	        //move cursors that are meant to move across entire screen, not only within the viewport:
	        _nonMaskableMove_rectTransf.transform.position = KeyMousePenInput.cursorScreenPos();

	        float drawView_yPixels = MainViewport_UI.instance.mainViewportRect.rect.size.y;

	        Vector2 sizeDelta =   Vector2.one * drawView_yPixels * brushScaleFactor
	                                          * _brushCursorPreview_shrink;

	        _maskableMove_rectTransf.sizeDelta    = sizeDelta;
	        _nonMaskableMove_rectTransf.sizeDelta = sizeDelta;
	    }


	    public void SetCursorColor(Color col){
	        _cursorImg.color = col;
	        //NOTICE: don't affect the eye-dropper, keep it white.
	    }


	    public void SetCursorThickness(float brushSize01){
	        //we will sample the cursor texture 3 times, shrinking towards center each time.
	        //When cursor is small, having 3 circles makes it thicker and more visible
	        float strengthA = 1;
	        float strengthB =  Mathf.Lerp(1, 0, Mathf.InverseLerp(cursor_B_max, cursor_B_min, brushSize01) );
	        float strengthC =  Mathf.Lerp(1, 0, Mathf.InverseLerp(cursor_C_max, cursor_C_min, brushSize01) );
	        _brushCursor_mat.SetFloat("_CursorA_Strength", strengthA);
	        _brushCursor_mat.SetFloat("_CursorB_Strength", strengthB);
	        _brushCursor_mat.SetFloat("_CursorC_Strength", strengthC);
	    }



	    void Update(){
	        bool hideCursor = false;
	        DimensionMode currMode = DimensionMode_MGR.instance._dimensionMode;
	        switch (currMode){
	            case DimensionMode.dim_uv:
	                hideCursor = true;
	                break;
	            case DimensionMode.dim_gen_3d:
	                hideCursor =  Gen3D_WorkflowOptionsRibbon_UI.instance._is_can_adjust_BG == false;
	                break;
	            case DimensionMode.dim_sd:
	            default:
	                hideCursor = false;
	                break;
	        }
        
	        hideCursor |= !Application.isFocused;//to avoid distractions

	        if(hideCursor){
	            NoHiddenCursor_Unlock(originalRequestor:this);//unhide any previous request
	            DoHiddenCursor_Lock(requestor:this);//then hide
	        }else{
	            NoHiddenCursor_Unlock(originalRequestor:this);
	        }
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	        _brushCursor_mat = _cursorImg.material;
	    }

	    void OnDestroy(){
	        _brushCursor_mat = null;
	    }
	}
}//end namespace
