using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace spz {

	// Attach this script onto a sprite renderer.
	// Stretches a sprite to follow corners of parent rect.
	// Make sure our sprite is parented to a "dummy" rectTransform and stretch that parent instead of sprite's tranform.
	//
	// IgorAherne March 2017
	// Marrt  July 2015
	// https://forum.unity3d.com/threads/overdraw-spriterenderer-in-ui.339912/#post-3009616

	[RequireComponent(typeof(RectTransform))]
	[ExecuteInEditMode]
	public class SpriteScalerRelativeToAnchors : UIBehaviour,  ILayoutSelfController {
    
	    [SerializeField] RectTransform _parentRectTransf;
	    [SerializeField] bool _keep_GO_Layer_as_UI = true;
	    [SerializeField, HideInInspector] RectTransform _myRectTransform;
	    [SerializeField, HideInInspector] SpriteRenderer _mySpriteRenderer;

	    //if true, we assume the parent will never stretch during the game.
	    // Therefore, when playing the game, adjustment will only happen once, during the Awake (during play mode).
	    // This saves peformance and can be quite significant.
	    public bool _optimize_parentNeverResizes = false;


	#if UNITY_EDITOR
	    protected override void Reset() {
	        InitComponentReferences();
	    }
	#endif


	    protected void InitComponentReferences() {
	        _parentRectTransf = transform.parent as RectTransform;
	        _mySpriteRenderer = GetComponent(typeof(SpriteRenderer)) as SpriteRenderer;
	        _myRectTransform = transform as RectTransform;
	    }


	#region layout controller native functions
	    public void SetLayoutHorizontal() {
	        keepScaleRelativeToParent();
	    }


	    public void SetLayoutVertical() {
	        keepScaleRelativeToParent();
	    }


	    protected override void OnRectTransformDimensionsChange() {
	        LayoutRebuilder.MarkLayoutForRebuild( _myRectTransform );
	        keepScaleRelativeToParent();
	    }


	#if UNITY_EDITOR
	    protected override void OnValidate() {
	        LayoutRebuilder.MarkLayoutForRebuild( _myRectTransform );
	    }
	#endif
	    #endregion layout controller native functions


	    protected override void Awake() {
	        #if UNITY_EDITOR
	            if( UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode == false){ return; }//OnUpdate() will be auto-invoked if in Runtime;
	        #endif

	        if(_optimize_parentNeverResizes){//at least once
	            keepScaleRelativeToParent();
	        }
	    }


	    //[ExecuteInEditMode]
	    public void Update() {
	        #if UNITY_EDITOR
	            if( UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode){ return; }//will use LateUpdate instead.
	            keepScaleRelativeToParent();
	        #endif
	    }


	    public void LateUpdate() {
	        #if UNITY_EDITOR
	            if( UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode  &&  _optimize_parentNeverResizes ){ return; }//OnUpdate() will be auto-invoked if in Runtime;
	        #else
	            if(_optimize_parentNeverResizes){ return; }
	        #endif
	            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
	        keepScaleRelativeToParent();
	    }



	    public void keepScaleRelativeToParent() {
	        if (_mySpriteRenderer == null || _parentRectTransf == null || _myRectTransform == null) {
	            InitComponentReferences();
	        }

	        //Adds a child GameObject to the UI-GameObject that copies the sprite of the image
	        //Needs to be a child because we need to scale it but still anchor its position to the old Object
	        //If we would add a SpriteRenderer to the actual Image-Object and scale it accordingly, children of it will be scaled too.

	        float pxWidth = _parentRectTransf.rect.width;  //width  of the scaled UI-Object in pixel
	        float pxHeight = _parentRectTransf.rect.height;//height of the scaled UI-Object in pixel

	        if (float.IsNaN(pxHeight)  ||  float.IsNaN(pxWidth)) {
	            //unity hasn't not yet initialized (usually happens during start of the game)
	            return;
	        }

	        float spriteSizeX = 1;
	        float spriteSizeY = 1;

	        if (_mySpriteRenderer != null  &&  _mySpriteRenderer.sprite != null) {
	            spriteSizeX = _mySpriteRenderer.sprite.bounds.size.x;  //width  of the unscaled sprite in pixel
	            spriteSizeY = _mySpriteRenderer.sprite.bounds.size.y;  //height of the unscaled sprite in pixel
	        }

	        //since we will be soon dividing by these values, we need to ensure they are always above zero
	        spriteSizeX = Mathf.Max(spriteSizeX, 0.1f);// 1/10th of pixel at least.
	        spriteSizeY = Mathf.Max(spriteSizeY, 0.1f);

	        //create new SpriteGameObject in UI
	        if (_keep_GO_Layer_as_UI){ 
	            this.gameObject.layer = LayerMask.NameToLayer("UI");            //culling layer, if needed
	        }

	        // Set the anchor points to corners
	        _myRectTransform.anchorMin = new Vector2(0, 0);
	        _myRectTransform.anchorMax = new Vector2(1, 1);


	        // Reset the anchored position to (0, 0, z)
	        _myRectTransform.anchoredPosition3D = new Vector3(0, 0, _myRectTransform.anchoredPosition3D.z);

	        // Calculate the scaling factors
	        float scaleX = pxWidth / spriteSizeX;
	        float scaleY = pxHeight / spriteSizeY;

	        _myRectTransform.sizeDelta = new Vector2(-pxWidth+spriteSizeX, -pxHeight+spriteSizeY);

	        // Update the local scale
	        _myRectTransform.localScale = new Vector3(scaleX, scaleY, 1F);

	    }

	}







}//end namespace
