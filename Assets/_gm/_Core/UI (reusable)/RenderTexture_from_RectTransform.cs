using UnityEngine;

namespace spz {

	// Ensures the render texture has same size as _rectTransf.
	// Or a child class can override GetTargetRectTransform().
	[ExecuteInEditMode]
	public class RenderTexture_from_RectTransform : MonoBehaviour{
	    [Tooltip("Helps save performance. Time in seconds to wait before updating the texture after resize")]
	    [SerializeField] protected float _textureUpdateTimeLag = 0.2f;
	    [SerializeField] protected bool renderTex_enableRandomWrite =false; //for example, if you intend to paint it.
	    [SerializeField] protected RectTransform _rectTransf;
	    [SerializeField] protected int _depthBits = 0;

	    protected RenderTexture _renderTexture = null;
	    protected float _timeOfNextResizeCheck = 0f;

	    // Getter only for reading, plz don't destroy texture, it belongs to us.
	    // Might get destroyed whenever we resize, so maybe grab it often.
	    System.Action<RenderTexture> _onWillDestroyRT;
	    System.Action<RenderTexture> _onCreatedRT;

	    protected virtual RectTransform GetTargetRectTransform() => _rectTransf;


	    // For exmaple, when we clicked "hide" button, and hiding some sibling panel.
	    // During these moements we want to resize every frame.
	    // This should only happen for short period of time, after which you should invoke with "false".
	    public void UpdateTextureEveryFrame(bool isDo){ _updateTextureEveryFrame=isDo;  _timeOfNextResizeCheck=Time.time-1; }
	    bool _updateTextureEveryFrame = false;


	    public void Subscribe( System.Action<RenderTexture> onWillDestroyRT,  
	                           System.Action<RenderTexture> onCreatedRT ){
	        _onWillDestroyRT += onWillDestroyRT;
	        _onCreatedRT += onCreatedRT;
	        //NOTICE, invoking ONLY user's callback 'onCreatedRT' not '_onCreatedRT'
	        if (_renderTexture!=null){  onCreatedRT(_renderTexture); }
	    }

	    public void Unsubscribe( System.Action<RenderTexture> onWillDestroyRT,  
	                             System.Action<RenderTexture> onCreatedRT ){
	        _onWillDestroyRT -= onWillDestroyRT;
	        _onCreatedRT -= onCreatedRT;
	    }
    

	#if UNITY_EDITOR
	    public RenderTexture renderTex_ref_IN_EDITOR =>_renderTexture;
	#endif


	    protected virtual void Start() => Resize_RenderTexture_maybe();
	    protected virtual void OnDestroy() => Destroy_renderTex();
	    //[ExecuteInEditMode]
	    protected virtual void Update() => Resize_RenderTexture_maybe();



	    // If the resize flag is set and the time has elapsed, update the texture
	    public virtual void Resize_RenderTexture_maybe(){
	        RectTransform rectTransf = GetTargetRectTransform();
	        if(rectTransf == null){ return; }//scenes are probably still loading.

	        bool hasRT    = _renderTexture!=null;
	        bool tooEarly = Time.time < _timeOfNextResizeCheck;
	        bool sizesSame = hasRT  &&  _renderTexture.width == Mathf.RoundToInt(rectTransf.rect.width)
	                                &&  _renderTexture.height== Mathf.RoundToInt(rectTransf.rect.height);
	        if(hasRT && tooEarly){return;}
	        if(hasRT && sizesSame){return;}
	        _timeOfNextResizeCheck = Time.time +  (_updateTextureEveryFrame? 0 : _textureUpdateTimeLag);
	        Destroy_renderTex();
	        CreateRenderTex();
	    }
       

	    protected virtual void CreateRenderTex(){
	        if(_renderTexture != null){ return; }
        
	        RectTransform rectTransf = GetTargetRectTransform();
	        if (rectTransf == null) { return; }//scenes are probably still loading.

	        // Create a new RenderTexture with the updated size:
	        int width  = Mathf.RoundToInt(rectTransf.rect.width);
	        int height = Mathf.RoundToInt(rectTransf.rect.height);
	        width  = Mathf.Max(32, width); //to prevent spamming console with errors ("texture cant have size zero", etc))
	        height = Mathf.Max(32, height);
	        _renderTexture = new RenderTexture(width, height, _depthBits, RenderTextureFormat.ARGB32);
	        _renderTexture.enableRandomWrite = renderTex_enableRandomWrite;
	        _renderTexture.Create(); // Ensure the RenderTexture is initialized
        
	        _onCreatedRT?.Invoke(_renderTexture);
	    }


	     protected virtual void Destroy_renderTex(){
	        if (_renderTexture == null){ return; }
	        _onWillDestroyRT?.Invoke(_renderTexture);

	        _renderTexture.Release();
	        DestroyImmediate(_renderTexture);
	        _renderTexture = null;
	    }

	}
}//end namespace
