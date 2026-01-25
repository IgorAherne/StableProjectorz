using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace spz {

	public class BrushRibbon_UI_EyeDropperTool : MonoBehaviour
	{
	    [Space(10)]
	    [SerializeField] Toggle _eyeDropper_toggle;
	    [SerializeField] int _magnify_pixels = 32;
	    [SerializeField] float _MagnificationPreview_scale = 4f;
	    [SerializeField] Shader _magnificationBlit_shader;
	    [SerializeField] float _CircleEdgeSmoothing = 0.01f;
	    [SerializeField] float _alt_sampleMoveThresh = 0.002f;


	    TextureFetchHelper _textureFetchHelper;
	    Coroutine _pickColor_crtn;

	    Vector2 _latestLMB_clickViewPos01;
	    bool _isLMBCurrentlyPressed = false;
	    bool _isFirstToggleActivation = false;
	    bool _isAltEyedropperEnabled = true;
	    bool _wasAltPressedLastFrame = false;


	    public static Action<Color> _onResult = null;
	    public bool IsMagnificationActive => isCan_preview_eyeDropper();


	    // Manages toggle state changes
	    // Activates UI blocking and sets first activation flag to prevent immediate sampling
	    void OnEyeDropperToggle(bool isOn){
	        if (isOn) { 
	            GlobalClickBlocker.Lock(who_is_requesting:this);
	        }else { 
	            GlobalClickBlocker.Unlock_if_can(who_is_requesting:this);
	        }
	        _isFirstToggleActivation = isOn;
	    }

	    // Core eyedropper functionality
	    // Handles preview and sampling based on toggle state and Alt key
	    // Uses end-of-frame to ensure all rendering is complete before capturing
	    IEnumerator PickColor_crtn(){
	        while (true){ 
	            if(isCan_preview_eyeDropper() || isCan_SampleNow()){ 
	                GlobalClickBlocker.Lock(who_is_requesting:this);
	                yield return new WaitForEndOfFrame();

	                RenderTexture tempScreenRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
	                _textureFetchHelper.PrepareAndCompositeMagnifiedImage(tempScreenRT, KeyMousePenInput.cursorScreenPos());

	                if(isCan_SampleNow() && !KeyMousePenInput.isLMBpressed()){
	                    _textureFetchHelper.StartAsyncGPUReadback(tempScreenRT, KeyMousePenInput.cursorScreenPos(), OnCompleteReadback);
	                }
	                RenderTexture.ReleaseTemporary(tempScreenRT);
	            }
	            else {
	                GlobalClickBlocker.Unlock_if_can(who_is_requesting: this);
	                ResetEyeDropperState();
	            }
	            yield return null;
	        }
	    }

	    // Processes the sampled color
	    // Invokes the result action and resets the eyedropper state
	    void OnCompleteReadback(Color sampledColor){
	        _onResult?.Invoke(sampledColor);
	        ResetEyeDropperState();
	    }

	    // Resets all eyedropper-related states
	    // Called after sampling or when eyedropper mode is exited
	    void ResetEyeDropperState(){
	        _eyeDropper_toggle.isOn = false;
	        GlobalClickBlocker.Unlock_if_can(who_is_requesting:this);
	        _isFirstToggleActivation = false;
	    }

	    // Determines if color sampling is currently allowed
	    // Handles toggle mode and Alt key mode separately
	    // Prevents accidental sampling on first toggle activation
	    bool isCan_SampleNow(){
	        if(IsDoingSomethingElse()){ return false; }

	        bool isAltPressed = KeyMousePenInput.isKey_alt_pressed();
	        bool isToggleOn = IsToggleActive();

	        if (isToggleOn) {
	            if (_isFirstToggleActivation && KeyMousePenInput.isLMBreleasedThisFrame()) {
	                _isFirstToggleActivation = false;
	                return false;
	            }
	            return KeyMousePenInput.isLMBreleasedThisFrame();
	        }

	        return isAltPressed && KeyMousePenInput.isLMBreleasedThisFrame() && _isAltEyedropperEnabled;
	    }

	    // Checks if magnification preview should be shown
	    // Considers both toggle state and Alt key press
	    // Alt key preview is subject to movement restrictions (_isAltEyedropperEnabled)
	    bool isCan_preview_eyeDropper(){
	        if (IsDoingSomethingElse()) { return false; }
	        bool isTogglePreviewActive = IsToggleActive();
	        bool isAltPreviewActive    = KeyMousePenInput.isKey_alt_pressed() && _isAltEyedropperEnabled;
	        return isTogglePreviewActive || isAltPreviewActive;
	    }

	    // Verifies if the eyedropper toggle is currently active
	    // Checks both the toggle state and if its GameObject is active in the hierarchy
	    bool IsToggleActive() {
	        return _eyeDropper_toggle.gameObject.activeInHierarchy && _eyeDropper_toggle.isOn;
	    }

	    // Detects conflicting actions that should disable eyedropper functionality
	    // Excludes Alt key as it's used for temporary eyedropper activation
	    bool IsDoingSomethingElse()
	        => MainViewport_UI.instance.showing != MainViewport_UI.Showing.UsualView ||
	           KeyMousePenInput.isMMBpressed() || 
	           KeyMousePenInput.isRMBpressed() || 
	           KeyMousePenInput.isKey_CtrlOrCommand_pressed() || 
	           KeyMousePenInput.isKey_Shift_pressed();
    

	    // Updates eyedropper state each frame
	    // Manages Alt key press/release, mouse interactions, and movement restrictions
	    // Ensures eyedropper behaves correctly with both toggle and Alt key activations
	    void Update(){
	        _textureFetchHelper.CheckIfNewScreenResolution();
	        bool isAltPressed = KeyMousePenInput.isKey_alt_pressed();

	        // Reset Alt action validity on new press
	        if (isAltPressed && !_wasAltPressedLastFrame) {
	            _isAltEyedropperEnabled = true;
	            _latestLMB_clickViewPos01 = KeyMousePenInput.cursorViewPos01();
	        }

	        // Track mouse press/release
	        if (KeyMousePenInput.isLMBpressedThisFrame()) {
	            _isLMBCurrentlyPressed = true;
	            _latestLMB_clickViewPos01 = KeyMousePenInput.cursorViewPos01();
	        }
	        if (KeyMousePenInput.isLMBreleasedThisFrame()) {
	            _isLMBCurrentlyPressed = false;
	        }

	        // Disable Alt sampling if mouse dragged too far
	        if (isAltPressed && _isLMBCurrentlyPressed) {
	            float distFromPressDown = (KeyMousePenInput.cursorViewPos01() - _latestLMB_clickViewPos01).magnitude;
	            if (distFromPressDown >= _alt_sampleMoveThresh) {
	                _isAltEyedropperEnabled = false;
	            }
	        }

	        // Reset Alt validity when key released
	        if (!isAltPressed) {
	            _isAltEyedropperEnabled = true;
	        }

	        _wasAltPressedLastFrame = isAltPressed;
	    }


	    private void Awake(){
	        StaticEvents.Bind_Clickable_to_event( nameof(BrushRibbon_UI_EyeDropperTool), this);
	    }

	    // Sets up the eyedropper tool, initializes texture fetching, and starts the color picking coroutine
	    // Toggle listener is set here to handle activation/deactivation of the eyedropper mode
	    void Start(){
	        _textureFetchHelper = new TextureFetchHelper(_magnify_pixels, _MagnificationPreview_scale, _magnificationBlit_shader, _CircleEdgeSmoothing);

	        _eyeDropper_toggle.onValueChanged.AddListener(OnEyeDropperToggle);
        
	        _pickColor_crtn = Coroutines_MGR.instance.StartCoroutine(PickColor_crtn());//start on MGR, we might get disabled.
	        GlobalClickBlocker.Unlock_if_can(who_is_requesting:this);
	    }


	    void OnDestroy(){
	        _textureFetchHelper.Dispose();
	        if (_pickColor_crtn != null && Coroutines_MGR.instance!=null){
	            Coroutines_MGR.instance.StopCoroutine(_pickColor_crtn);
	        }
	        _pickColor_crtn = null;
	        _eyeDropper_toggle.onValueChanged.RemoveListener(OnEyeDropperToggle);
	    }
	}



	public class TextureFetchHelper{
	    RenderTexture _magnifyingTexture;
	    Material _magnificationBlit_mat;
	    int _magnify_pixels;
	    float _MagnificationPreview_scale;
	    float _CircleEdgeSmoothing;
	    float _scaleFactor;
	    int _effectiveMagnifyPixels;

	    Vector2Int _prevFrameScreenSize;

	    public TextureFetchHelper(int magnifyPixels, float magnificationPreviewScale,
	                              Shader magnificationBlitShader, float circleEdgeSmoothing)
	    {
	        _magnify_pixels = magnifyPixels;
	        _MagnificationPreview_scale = magnificationPreviewScale;
	        _CircleEdgeSmoothing = circleEdgeSmoothing;

	        CheckIfNewScreenResolution();

	        _magnificationBlit_mat = new Material(magnificationBlitShader);
	    }


	    public void CheckIfNewScreenResolution(){ //invoked every frame, by our owner class.
	        if(_prevFrameScreenSize.x == Screen.width  &&  _prevFrameScreenSize.y == Screen.height){ return; }
	        _prevFrameScreenSize = new Vector2Int(Screen.width, Screen.height);

	        // Calculate scale factor based on screen height
	        _scaleFactor = Screen.height / 1080.0f;

	        // Calculate the effective magnify pixels based on the scale factor
	        _effectiveMagnifyPixels = Mathf.RoundToInt(_magnify_pixels * _scaleFactor);

	        if(_magnifyingTexture != null){ Texture.DestroyImmediate(_magnifyingTexture); }
	        _magnifyingTexture = new RenderTexture(_effectiveMagnifyPixels, _effectiveMagnifyPixels, 0);
	    }


	    public void PrepareAndCompositeMagnifiedImage(RenderTexture tempScreenRT, Vector2 screenPos){
	        ScreenCapture.CaptureScreenshotIntoRenderTexture(tempScreenRT);
	        PrepareMagnifiedImage(tempScreenRT, screenPos);
	        CompositeWithMagnifiedImage(tempScreenRT, screenPos);
	    }

	    void PrepareMagnifiedImage(RenderTexture tempScreenRT, Vector2 screenPos){
	        Vector2 samplePos = screenPos;
	        samplePos.y = SystemInfo.graphicsUVStartsAtTop ? Screen.height - samplePos.y : samplePos.y;
	        int x = Mathf.Clamp((int)samplePos.x - _effectiveMagnifyPixels / 2, 0, Screen.width - _effectiveMagnifyPixels);
	        int y = Mathf.Clamp((int)samplePos.y - _effectiveMagnifyPixels / 2, 0, Screen.height - _effectiveMagnifyPixels);

	        // Blit the area around the cursor into the magnifying texture
	        Graphics.CopyTexture(tempScreenRT, 0, 0, x, y, _effectiveMagnifyPixels, _effectiveMagnifyPixels,
	                             _magnifyingTexture, 0, 0, 0, 0);
	    }

	    void CompositeWithMagnifiedImage(RenderTexture tempScreenRT, Vector2 screenPos)
	    {
	        _magnificationBlit_mat.SetTexture("_MainTex", tempScreenRT);
	        _magnificationBlit_mat.SetTexture("_MagnifiedTex", _magnifyingTexture);

	        // Calculate the magnification rectangle in UV space (centered on cursor)
	        float rectX = (screenPos.x / Screen.width) - (_effectiveMagnifyPixels * _MagnificationPreview_scale / (float)Screen.width / 2);
	        float rectY = (screenPos.y / Screen.height) - (_effectiveMagnifyPixels * _MagnificationPreview_scale / (float)Screen.height / 2);
	        float rectWidth = _effectiveMagnifyPixels / (float)Screen.width;
	        float rectHeight = _effectiveMagnifyPixels / (float)Screen.height;

	        _magnificationBlit_mat.SetVector("_MagnificationRect", new Vector4(rectX, rectY, rectWidth, rectHeight));
	        _magnificationBlit_mat.SetFloat("_MagnificationScale", _MagnificationPreview_scale);
	        _magnificationBlit_mat.SetFloat("_CircleEdgeSmoothing", _CircleEdgeSmoothing);
	        _magnificationBlit_mat.SetFloat("_ScreenAspectRatio", (float)Screen.width / Screen.height);

	        // Blit to the screen using the custom shader
	        Graphics.Blit(tempScreenRT, (RenderTexture)null, _magnificationBlit_mat);
	    }

	    public void StartAsyncGPUReadback(RenderTexture tempScreenRT, Vector2 screenPos, Action<Color> onComplete){
	        int x = Mathf.Clamp((int)screenPos.x, 0, Screen.width - 1);
	        int y = Mathf.Clamp((int)screenPos.y, 0, Screen.height - 1);

	        // Adjust y-coordinate based on graphics API
	        if (SystemInfo.graphicsUVStartsAtTop){
	            y = Screen.height - 1 - y;// DirectX (UV starts at top)
	        }

	        AsyncGPUReadback.Request(tempScreenRT, 0, x, 1, y, 1, 0, 1, 
	                                 TextureFormat.RGBA32, 
	                                 request => OnCompleteReadback(request, onComplete));
	    }

	    void OnCompleteReadback(AsyncGPUReadbackRequest request, Action<Color> onComplete){
	        if (request.hasError){
	            Debug.LogError("GPU readback error detected.");
	            return;
	        }
	        var data = request.GetData<Color32>();
	        if (data.Length > 0){
	            Color sampledColor = data[0];
	            onComplete?.Invoke(sampledColor);
	        }else{
	            Debug.LogWarning("No color data received from GPU readback.");
	        }
	    }

	    public void Dispose(){
	        if(_magnifyingTexture != null) { UnityEngine.Object.Destroy(_magnifyingTexture); }
	        if(_magnificationBlit_mat != null) { UnityEngine.Object.DestroyImmediate(_magnificationBlit_mat); }
	    }
	}
}//end namespace
