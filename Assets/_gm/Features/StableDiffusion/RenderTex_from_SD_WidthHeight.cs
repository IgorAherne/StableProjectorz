using UnityEngine;

namespace spz {

	// Looks at the "Width" and "Height" sliders on the Input panel.
	// Ensures render texture has same dimensions as those sliders.
	public class RenderTex_from_SD_WidthHeight : MonoBehaviour
	{
	    [SerializeField] bool _renderTex_enableRandomWrite = false;
	    [SerializeField] float _textureUpdateTimeLag = 0.05f;//50ms = 20fps, is good enough (checking that often, not re-allocating)
	    [Space(10)]
	    [Header("will ignore your format if depthBits != 0")]
	    [SerializeField] UnityEngine.Experimental.Rendering.GraphicsFormat _format = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
	    [SerializeField] int _depthBits = 0;
	    [SerializeField] int _res_ForceNoMoreThan = -1; //for example, 768 means largest dimension no more than 768, and another dim will adjust to match aspect.

	    RenderTexture _renderTexture = null;
	    float _timeOfNextResizeCheck = 0f;

	    // Getter only for reading, plz don't destroy texture, it belongs to us.
	    // Might get destroyed whenever we resize, so maybe grab it often.
	    System.Action<RenderTexture> _onWillDestroyRT;
	    System.Action<RenderTexture> _onCreatedRT;
	    public void Subscribe( System.Action<RenderTexture> onWillDestroyRT,  
	                           System.Action<RenderTexture> onCreatedRT ){
	        _onWillDestroyRT += onWillDestroyRT;
	        _onCreatedRT += onCreatedRT;
	        //NOTICE, invoking ONLY user's callback 'onCreatedRT' not '_onCreatedRT'
	        if(_renderTexture!=null){ onCreatedRT(_renderTexture); }
	    }

	    public void Unsubscribe( System.Action<RenderTexture> onWillDestroyRT,  
	                             System.Action<RenderTexture> onCreatedRT ){
	        _onWillDestroyRT -= onWillDestroyRT;
	        _onCreatedRT -= onCreatedRT;
	    }

    
	    public void set_is_Want(bool isWant){//for example, if texture isn't needed right now (not rendered, to save performance).
	        if(isWant){  Resize_RenderTexture_maybe(skipTimeCheck:true); }//repetitive invokations are ignored, if tex size when the same.
	        else{  Destroy_renderTex(); }//releases memory + sends a callback.
	    }


	    void Start() => Resize_RenderTexture_maybe();
	    void OnDestroy() => Destroy_renderTex();
	    //[ExecuteInEditMode]
	    void Update() => Resize_RenderTexture_maybe();


	    // If the resize flag is set and the time has elapsed, update the texture
	    void Resize_RenderTexture_maybe(bool skipTimeCheck=false){

	        if(SD_InputPanel_UI.instance == null){ return; }//probably scenes are still loading.

	        bool hasRT    = _renderTexture!=null;
	        bool tooEarly = Time.time < _timeOfNextResizeCheck  &&  !skipTimeCheck;

	        Vector2 wantedSize =  SD_InputPanel_UI.instance.widthHeight();
	                wantedSize =  clampRes(wantedSize);

	        Vector2Int wantedSizeInt =  new Vector2Int( Mathf.RoundToInt(wantedSize.x), Mathf.RoundToInt(wantedSize.y) );
                
	        bool sizesSame = hasRT  &&  _renderTexture.width == wantedSizeInt.x
	                                &&  _renderTexture.height== wantedSizeInt.y;

	        if(hasRT && tooEarly){ return; }
	        if(hasRT && sizesSame){ return; }
	        _timeOfNextResizeCheck = Time.time + _textureUpdateTimeLag;
	        Destroy_renderTex();
	        CreateRenderTex(wantedSizeInt);
	    }
       

	    void CreateRenderTex( Vector2Int wantedSizeInt ){
	        if(_renderTexture != null){ return; }
	        // Create a new RenderTexture with the updated size:
	        if (_depthBits > 0){
	            _renderTexture = new RenderTexture(wantedSizeInt.x, wantedSizeInt.y, _depthBits, RenderTextureFormat.Depth,  mipCount:0);
	        }else { 
	            _renderTexture = new RenderTexture(wantedSizeInt.x, wantedSizeInt.y, _depthBits, _format,  mipCount:0);
	        }
        
	        _renderTexture.enableRandomWrite = _renderTex_enableRandomWrite;
	        _renderTexture.Create(); // Ensure the RenderTexture is initialized

	        _onCreatedRT?.Invoke(_renderTexture);
	    }


	    // Ensure the largest side is not greater than '_res_ForceNoMoreThan'.
	    // And adjust shorter side to maintain its aspect.
	    Vector2 clampRes(Vector2 res){
	        if(_res_ForceNoMoreThan <= 0){ return res; }
        
	        float aspect = res.x / (float)res.y;

	        if(res.x > res.y){
	            res.x  = _res_ForceNoMoreThan;
	            res.y  = Mathf.RoundToInt(res.x/aspect);
	        }else {
	            res.y = _res_ForceNoMoreThan;
	            res.x = Mathf.RoundToInt(res.y*aspect);
	        }
	        return res;
	    }


	    void Destroy_renderTex(){
	        if (_renderTexture == null){ return; }
	        _onWillDestroyRT?.Invoke(_renderTexture);

	        if(RenderTexture.active == _renderTexture){ RenderTexture.active=null; }
	        _renderTexture.Release();
	        DestroyImmediate(_renderTexture);
	        _renderTexture = null;
	    }
	}
}//end namespace
