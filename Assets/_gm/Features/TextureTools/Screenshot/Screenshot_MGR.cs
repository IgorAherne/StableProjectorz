using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace spz {

	public class Screenshot_MGR : MonoBehaviour
	{
	    public static Screenshot_MGR instance { get; private set; } = null;

	    [SerializeField] Shader _screenshotRegion_sh;
	    [SerializeField] Image _frameImage_moveMe;

	    // Frame has shadow around it, so we spread the image more to compensate:
	    [SerializeField] float _frameOffset_px = 16;

	    Material _screenshotRegion_mat;

	    LocksHashset_OBJ _keepCapturingSnippets = new LocksHashset_OBJ();
	    Vector2 _viewportClickScreenPos = Vector2.zero; // in pixels
	    bool _isDragging = false;
	    bool _forbidFrameImage = false;

	    // minimum screen coord (in pixels), maximum screen coord (in pixels).
	    // NOTICE: each subscribe will BECOME THE OWNER of a texture2D (we clone them as needed).
	    // SO, REMEMBER TO DESTROY YOUR TEXTURE WHEN ITS NO LONGER NEEDED, TO AVOID MEMORY LEAKS.
	    public Action<Vector2, Vector2, Texture2D> _Act_OnTakeScreenshotTexture { get; set; } = null;

	    //A simple notification, without allocating additional textures.
	    // bool: 'isBecauseMouseDragged' true if screenshot was taken because of dragging the mouse.
	    //                               false if screenshot was requested from some script.
	    public static Action<bool> _Act_OnScreenshot { get; set; }


	    public void PrefferCaptureSnippets(object requestor) => _keepCapturingSnippets.Lock(requestor);
	    public void PrefferAvoidSnippets(object originalRequestor) => _keepCapturingSnippets.Unlock(originalRequestor);
	    public bool isPrefferCaptureSnippets() => _keepCapturingSnippets.isLocked();


	    //manually request to grab portion of what's visible in the viewport
	    public void ScreenshotViewport_viaScript(Vector2 minView01, Vector2 maxView01, 
	                                             Action<Vector2,Vector2,Texture2D> onHaveTexture_plzDeleteLater) {
    
	       innerViewport_to_screenPixels(minView01, maxView01, out Vector2 minScreen, out Vector2 maxScreen);

	        // Convert to Vector2Int for pixel precision
	        Vector2Int min_px = new Vector2Int( Mathf.RoundToInt(minScreen.x), Mathf.RoundToInt(minScreen.y));
	        Vector2Int max_px = new Vector2Int( Mathf.RoundToInt(maxScreen.x), Mathf.RoundToInt(maxScreen.y));
	        var size_px = max_px - min_px;
	        StopAllCoroutines();
	        StartCoroutine(MakeScreenshot_crtn(min_px, max_px, size_px, onHaveTexture_plzDeleteLater, isBecause_mouseDragged:false));
	    }


	    void innerViewport_to_screenPixels(Vector2 minView01, Vector2 maxView01, out Vector2 minScreen_, out Vector2 maxScreen_ ){
	        // Get the viewport rect
	        RectTransform viewportRect = MainViewport_UI.instance.innerViewportRect;
	         // Convert normalized coordinates (0-1) to local viewport coordinates
	        Vector2 minLocal = new Vector2(
	            Mathf.Lerp(viewportRect.rect.xMin, viewportRect.rect.xMax, minView01.x),
	            Mathf.Lerp(viewportRect.rect.yMin, viewportRect.rect.yMax, minView01.y)
	        );
	        Vector2 maxLocal = new Vector2(
	            Mathf.Lerp(viewportRect.rect.xMin, viewportRect.rect.xMax, maxView01.x),
	            Mathf.Lerp(viewportRect.rect.yMin, viewportRect.rect.yMax, maxView01.y)
	        );
	        // Convert local coordinates to world space
	        Vector3 minWorld = viewportRect.TransformPoint(minLocal);
	        Vector3 maxWorld = viewportRect.TransformPoint(maxLocal);
	        // Convert world coordinates to screen space
	        minScreen_ = RectTransformUtility.WorldToScreenPoint(null, minWorld);
	        maxScreen_ = RectTransformUtility.WorldToScreenPoint(null, maxWorld);
	    }


	    void Update(){
	        _frameImage_moveMe.gameObject.SetActive(false);

	        if (!_keepCapturingSnippets.isLocked() || isDoing_somethingElse()){
	            _isDragging = false;
	            return;
	        }
	        Update_FrameImage();

	        // Start dragging if LMB pressed this frame
	        if (KeyMousePenInput.isLMBpressedThisFrame()){
	            // Ensure user is clicking in the viewport
	            if (!MainViewport_UI.instance.isCursorHoveringMe()){
	                return;
	            }
	            _viewportClickScreenPos = screenPos_clampedInsideViewport();
	            Update_FrameImage(); // re-update once to avoid jump
	            _isDragging = true;
	            return;
	        }
	        if (!KeyMousePenInput.isLMBpressed() && _isDragging){ // Release drag
	            _isDragging = false;
	            Vector2 currentPos_px = screenPos_clampedInsideViewport();
	            Vector2 min =  Vector2.Min(_viewportClickScreenPos, currentPos_px);
	            Vector2 max =  Vector2.Max(_viewportClickScreenPos, currentPos_px);
	            Vector2Int min_px  = new Vector2Int(Mathf.RoundToInt(min.x), Mathf.RoundToInt(min.y));
	            Vector2Int max_px  = new Vector2Int(Mathf.RoundToInt(max.x), Mathf.RoundToInt(max.y));
	            Vector2Int size_px  = min_px - max_px;
	                       size_px.x = Mathf.Abs(size_px.x);
	                       size_px.y = Mathf.Abs(size_px.y);
	            if(size_px.x<16  &&  size_px.x<16){ return; }//likely a click, don't take a screenshot.
	            StartCoroutine( MakeScreenshot_crtn(min_px, max_px, size_px, _Act_OnTakeScreenshotTexture, isBecause_mouseDragged:true) );
	        }
	    }

	    bool isDoing_somethingElse(){
	        if (KeyMousePenInput.isKey_alt_pressed()) { return true; }
	        if (KeyMousePenInput.isKey_CtrlOrCommand_pressed()) { return true; }
	        if (KeyMousePenInput.isKey_Shift_pressed()) { return true; }
	        if (KeyMousePenInput.isRMBpressed()) { return true; }
	        // If not dragging, and no fresh LMB press, block
	        if (!_isDragging && !KeyMousePenInput.isLMBpressedThisFrame()) { return true; }
	        return false;
	    }

	    void Update_FrameImage()
	    {
	        if (_forbidFrameImage){
	            _frameImage_moveMe.gameObject.SetActive(false);
	            return;
	        }
	        if (_isDragging){
	            _frameImage_moveMe.gameObject.SetActive(true);
	        }
	        Vector2 screenPos = screenPos_clampedInsideViewport();
	        Vector2 size_noPad = screenPos - _viewportClickScreenPos;
	        Vector2 sign = new Vector2(Mathf.Sign(size_noPad.x), Mathf.Sign(size_noPad.y));

	        _frameImage_moveMe.transform.position = new Vector3(
	            _viewportClickScreenPos.x - _frameOffset_px * sign.x,
	            _viewportClickScreenPos.y - _frameOffset_px * sign.y,
	            _frameImage_moveMe.transform.position.z
	        );

	        // *2 => we pad both directions
	        Vector2 size_withPad = size_noPad + new Vector2(
	            _frameOffset_px * 2f * sign.x,
	            _frameOffset_px * 2f * sign.y
	        );
	        Vector2 size_absPad = new Vector2(Mathf.Abs(size_withPad.x), Mathf.Abs(size_withPad.y));
	        _frameImage_moveMe.rectTransform.sizeDelta = size_absPad;

	        // Cancel out canvas scaler
	        Vector3 parentScale = _frameImage_moveMe.rectTransform.parent.lossyScale;
	        _frameImage_moveMe.rectTransform.localScale = new Vector3(
	            1f / parentScale.x,
	            1f / parentScale.y,
	            1f / parentScale.z
	        );

	        // Flip if needed
	        Vector3 locScale = _frameImage_moveMe.rectTransform.localScale;
	        _frameImage_moveMe.rectTransform.localScale = new Vector3(
	            locScale.x * sign.x,
	            locScale.y * sign.y,
	            locScale.z
	        );
	    }

	    Vector2 screenPos_clampedInsideViewport(){
	        Vector2 screenPos = KeyMousePenInput.cursorScreenPos();
	        RectTransform viewportRect = MainViewport_UI.instance.innerViewportRect;

	        // 1) Screen -> local
	        Vector2 localPoint;
	        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect, screenPos, null, out localPoint);

	        // 2) Clamp
	        Rect r = viewportRect.rect;
	        localPoint.x = Mathf.Clamp(localPoint.x, r.xMin, r.xMax);
	        localPoint.y = Mathf.Clamp(localPoint.y, r.yMin, r.yMax);

	        // 3) Local -> screen
	        Vector3 worldPoint = viewportRect.TransformPoint(localPoint);
	        return RectTransformUtility.WorldToScreenPoint(null, worldPoint);
	    }


	    IEnumerator MakeScreenshot_crtn(Vector2Int min_px, Vector2Int max_px, Vector2Int size_px, 
	                                    Action<Vector2,Vector2,Texture2D> onScreenshotRdy, 
	                                    bool isBecause_mouseDragged ){//false if requested from some script
	        // (1) Figure out size
	        RenderTexture screenRT = new RenderTexture(Screen.width, Screen.height, 0);

	        RenderTexture portionRT = new RenderTexture(Mathf.RoundToInt(size_px.x),
	                                                    Mathf.RoundToInt(size_px.y),  0);
	        // Hide the framing-rectangle, capture.
	        // We'll apply alpha during screenshot blit, so make sure skybox manager doesn't apply alpha this frame.
	        _forbidFrameImage = true;
	        _frameImage_moveMe.gameObject.SetActive(false);
	        Viewport_StatusText.instance.PreferHidden(requestor:this);
	        SkyboxBackground_MGR.instance.FullAlpha_Lock(requestor:this);
	        yield return new WaitForEndOfFrame();
	        ScreenCapture.CaptureScreenshotIntoRenderTexture(screenRT);
	        SkyboxBackground_MGR.instance.FullAlpha_StopLock(originalRequestor: this);
	        Viewport_StatusText.instance.PreferVIsible(originalRequestor:this);
	        _forbidFrameImage = false;

	        Grab_Portion(screenRT, portionRT, min_px, max_px, size_px);

	        // (8) Readback async
	        bool isDone = false;
	        Action<Texture2D> onTex2D_fetched = (Texture2D tex2D) => {
	            // Fire the delegates
	            Delegate[] delegates = onScreenshotRdy?.GetInvocationList();
	            if (delegates == null || delegates.Length == 0){
	                DestroyImmediate(tex2D);
	                isDone = true;
	                return;
	            }
	            // All but last
	            for (int i = 0; i < delegates.Length - 1; i++){
	                if (delegates[i] is Action<Vector2, Vector2, Texture2D> cb){
	                    Texture2D copy = new Texture2D(tex2D.width, tex2D.height, tex2D.format, mipCount:tex2D.mipmapCount, !tex2D.isDataSRGB );
	                    Graphics.CopyTexture(tex2D, copy);
	                    cb.Invoke(min_px, max_px, copy);
	                }
	            }
	            // Last
	            if (delegates[delegates.Length - 1] is Action<Vector2, Vector2, Texture2D> lastCb){
	                lastCb.Invoke(min_px, max_px, tex2D);
	            }else{
	                DestroyImmediate(tex2D);
	            }
	            isDone = true;

	            _Act_OnScreenshot?.Invoke(isBecause_mouseDragged);
	        };

	        TextureTools_SPZ.RenderTexture_to_Texture2D_Async(portionRT, onTex2D_fetched);
	        while(!isDone){ yield return null; }

	        DestroyImmediate(screenRT);
	        DestroyImmediate(portionRT);
	    }


	    // cuts out a region of the screenRT into the portionRT texture.
	    // Applies the mask where the background should remain transparent.
	    void Grab_Portion( RenderTexture screenRT,  RenderTexture portionRT, 
	                       Vector2Int min_px,  Vector2Int max_px,  Vector2Int size_px ){
	        // Convert region to 0..1 offset
	        Vector2 min01  =  new Vector2(min_px.x/(float)Screen.width,  min_px.y/(float)Screen.height);
	        Vector2 sizeUV =  new Vector2(size_px.x/(float)Screen.width,  size_px.y/(float)Screen.height);
	        _screenshotRegion_mat.SetVector("_OffsetAndScale", new Vector4(
	            min01.x, min01.y, sizeUV.x, sizeUV.y
	        ));

	        // Set up the background mask
	        bool clearBG =  !ArtBG_IconsUI_List.instance.hasBackground( considerGradientColors:true );
	        if(clearBG){
	            // Background is clear and its mask doesn't exist. 
	            // We will use the 2D texture of the viewport-camera instead.
	            // Its Alpha has the silhuettes as white and the rest as black, so can be used as a mask.
	            TextureTools_SPZ.SetKeyword_Material(_screenshotRegion_mat, "USING_TEXTURE_ARRAY", false);
	            _screenshotRegion_mat.SetTexture("_bgTex", null);
	            _screenshotRegion_mat.SetFloat("_hasBgTex", 0);
	            _screenshotRegion_mat.SetTexture("_bgMaskTex", null);
	            _screenshotRegion_mat.SetFloat("_isForceMaskAlpha1", 0);
	        }
	        else{
	            // We will use the background-mask (which is a TextureArray with just 1 slice).
	            // will work with its Red channel, not Alpha.
	            Texture bgMask2D = Background_Painter.instance.current_BG_MaskRenderUdim()?.texArray;
	            TextureTools_SPZ.SetKeyword_Material(_screenshotRegion_mat, "USING_TEXTURE_ARRAY", true);
	            // we also want the actual background texture, because its alpha will
	            // also be used, together with the mask.
	            Texture bgTexture = ArtBG_IconsUI_List.instance._mainSelectedIcon?.texture0().tex2D;
	            _screenshotRegion_mat.SetTexture("_bgTex", bgTexture);
	            _screenshotRegion_mat.SetFloat("_hasBgTex", bgTexture != null ? 1 : 0);
	            _screenshotRegion_mat.SetTexture("_bgMaskTex", bgMask2D);
	            _screenshotRegion_mat.SetFloat("_isForceMaskAlpha1", bgMask2D==null?1:0);
	        }

	        RenderTexture viewTex2D = UserCameras_MGR.instance.camTextures._viewCam_RT_ref;
	        _screenshotRegion_mat.SetTexture("_viewportTex", viewTex2D);

	        // Compute how the background is shown in screen coords, then store as UV offset+scale
	        Vector4 bgScreenTransform = ComputeBackgroundScreenRect01( Background_Painter.instance.getBackgroundRect() );
	        Vector4 viewScreenTransform = ComputeBackgroundScreenRect01( MainViewport_UI.instance.mainViewportRect );

	        _screenshotRegion_mat.SetVector("_BG_ScreenRect01", bgScreenTransform);
	        _screenshotRegion_mat.SetVector("_View_ScreenRect01", viewScreenTransform);
	        // Blit
	        TextureTools_SPZ.Blit(screenRT, portionRT, _screenshotRegion_mat);
	    }


	    // Measures how the background is actually displayed on-screen,
	    // then converts that to (offsetX, offsetY, scaleX, scaleY)
	    // in [0..1] "screen UV" space.
	    //
	    // We'll assume you have a RectTransform for the background,
	    // or some object that covers the entire background region
	    // on the Canvas. If your BG is placed differently, adapt accordingly.
	    // 
	    // For example: 
	    //     - 'backgroundRect' is your background's RectTransform
	    //     - we measure its corners in screen space
	    //     - from that, we get offset=(minX/Screen.width, minY/Screen.height)
	    //     - scale=(widthInScreen/Screen.width, heightInScreen/Screen.height)
	    // 
	    // Then in the shader, we'll invert that transform to map the screen
	    // pixel to the BG mask's 0..1 UV.
	    Vector4 ComputeBackgroundScreenRect01(RectTransform bg_RectTransform){
	        // If there's no BG or no relevant rect, return identity
       
	        if (!bg_RectTransform){
	            return new Vector4(0, 0, 1, 1);
	        }
	        // Get the four world corners of the backgroundRect
	        Vector3[] corners = new Vector3[4];
	        bg_RectTransform.GetWorldCorners(corners);

	        // corners[0] is bottom-left, [2] is top-right (depending on pivot).
	        // Let's find min/max in screen space:
	        float minX = float.MaxValue, minY = float.MaxValue;
	        float maxX = float.MinValue, maxY = float.MinValue;

	        for (int i=0; i<4; i++){
	            Vector3 sc = RectTransformUtility.WorldToScreenPoint(null, corners[i]);
	            if(sc.x < minX){ minX = sc.x; }
	            if(sc.y < minY){ minY = sc.y; }
	            if(sc.x > maxX){ maxX = sc.x; }
	            if(sc.y > maxY){ maxY = sc.y; }
	        }
	        float bgWidth = Mathf.Max(2f, (maxX - minX));
	        float bgHeight = Mathf.Max(2f, (maxY - minY));

	        // Convert to [0..1] in screen space
	        float offsetX = minX / Screen.width;
	        float offsetY = minY / Screen.height;
	        float scaleX  = bgWidth / Screen.width;
	        float scaleY  = bgHeight / Screen.height;

	        return new Vector4(offsetX, offsetY, scaleX, scaleY);
	    }

    

	    void Awake(){
	        if (instance != null){
	            DestroyImmediate(this);
	            return;
	        }
	        instance = this;
	        _screenshotRegion_mat = new Material(_screenshotRegion_sh);
	    }

	    void OnDestroy(){
	        DestroyImmediate(_screenshotRegion_mat);
	    }

	}
}//end namespace
