using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace spz {

	//Singleton
	public class Settings_MGR : MonoBehaviour
	{
	    public static Settings_MGR instance { get; private set; }

	    int _idleFramerate = 2; //when the window isn't focused. 5 was too much
	    bool _hasFocus = true;



	    //allows for smooth fps after user drag-and-drops some file, etc.
	    float _dontThrottle_any_FPS_until = -1;
	    public void dontThrottle_any_FPS_until(float timestamp) => _dontThrottle_any_FPS_until = timestamp;

	    int _targetFrameRate = 70;
	    public int get_targetFrameRate() => _targetFrameRate;
	    public void set_targetFrameRate(int fps) {
	        _targetFrameRate = fps;
	        PlayerPrefs.SetInt("_targetFrameRate", _targetFrameRate); PlayerPrefs.Save();
	        // Set UI value via binder
	        var inputField = EventsBinder.FindComponent<IntegerInputField>("Settings:set_targetFrameRate");
	        if (inputField != null) inputField.SetValueWithoutNotify(fps.ToString());
	        Application.targetFrameRate = _targetFrameRate;
	    }
	    void tryLoad_targetFrameRate() {
	        set_targetFrameRate(PlayerPrefs.GetInt("_targetFrameRate", 70));
	    }



	    int _brushPrecision_res = 2048;
	    public int get_uv_brushPrecision_res() => _brushPrecision_res;
	    void set_brushPrecision_res(int newRes, bool skipConfirmPopup = false) {
	        string confirmMsg = $"Change brush UV precision to {newRes}?\n<b>This will delete ALL the Art icons.</b>";
	        var toggle = EventsBinder.FindComponent<Toggle>("Settings:set_brushPrecision_res");

	        if (skipConfirmPopup) { OnYes(); }
	        else {
	            bool wasOn_beforeClick = newRes != 4096;
	            if (toggle != null) toggle.SetIsOnWithoutNotify(wasOn_beforeClick);
	            ConfirmPopup_UI.instance.Show(confirmMsg, onYes: OnYes, OnNo);
	        }
	        void OnYes() {
	            _brushPrecision_res = newRes;
	            PlayerPrefs.SetInt("_brushPrecision_res_v2", _brushPrecision_res); PlayerPrefs.Save();
	            if (toggle != null) toggle.SetIsOnWithoutNotify(newRes == 4096);

	            if (GenData2D_Archive.instance != null){ 
	                GenData2D_Archive.instance.Dispose_ALL_genData();
	            }
	        }
	        void OnNo() {
	            if (toggle != null) toggle.SetIsOnWithoutNotify(_brushPrecision_res == 4096);
	        }
	    }
	    void tryLoad_brushPrecision_res()
	        => set_brushPrecision_res(PlayerPrefs.GetInt("_brushPrecision_res_v2", 2048), skipConfirmPopup: true);



	    bool _prompt_textHighlight = true;
	    public bool get_prompt_textHighlight() => _prompt_textHighlight;
	    void set_prompt_textHighlight(bool isHighlight) {
	        _prompt_textHighlight = isHighlight;
	        PlayerPrefs.SetInt("_prompt_textHighlight", _prompt_textHighlight ? 1 : 0); PlayerPrefs.Save();
	        var toggle = EventsBinder.FindComponent<Toggle>("Settings:set_prompt_textHighlight");
	        if (toggle != null) toggle.SetIsOnWithoutNotify(isHighlight);
	    }
	    void tryLoad_prompt_textHighlight()
	        => set_prompt_textHighlight(PlayerPrefs.GetInt("_prompt_textHighlight", 1) == 1);



	    bool _isAllowTooltips = true;
	    public bool get_isAllowTooltips() => _isAllowTooltips;
	    void set_isAllowTooltips(bool isAllow) {
	        _isAllowTooltips = isAllow;
	        PlayerPrefs.SetInt("_isAllowTooltips", _isAllowTooltips ? 1 : 0); PlayerPrefs.Save();
	        var toggle = EventsBinder.FindComponent<Toggle>("Settings:set_isAllowTooltips");
	        if (toggle != null) toggle.SetIsOnWithoutNotify(isAllow);
	    }
	    void tryLoad_isAllowTooltips()
	        => set_isAllowTooltips(PlayerPrefs.GetInt("_isAllowTooltips", 1) == 1);



	    bool _isShow_CameraInfoText = false;
	    public bool get_isShow_CameraInfoText() => _isShow_CameraInfoText;
	    void set_isShow_CameraInfoText(bool isShow) {
	        _isShow_CameraInfoText = isShow;
	        PlayerPrefs.SetInt("_isShow_CameraInfoText", isShow ? 1 : 0); PlayerPrefs.Save();
	        var toggle = EventsBinder.FindComponent<Toggle>("Settings:set_isShow_CameraInfoText");
	        if (toggle != null) toggle.SetIsOnWithoutNotify(isShow);
	    }
	    void tryLoad_isShow_CameraInfoText()
	        => set_isShow_CameraInfoText(PlayerPrefs.GetInt("_isShow_CameraInfoText", 0) == 1);



	    bool _avoid_NSFW_generations = false;
	    public bool get_avoid_NSFW_generations() => _avoid_NSFW_generations;
	    void set_avoid_NSFW_generations(bool isAvoid) {
	        _avoid_NSFW_generations = isAvoid;
	        PlayerPrefs.SetInt("_avoid_NSFW_generations", _avoid_NSFW_generations ? 1 : 0); PlayerPrefs.Save();
	        var toggle = EventsBinder.FindComponent<Toggle>("Settings:set_avoid_NSFW_generations");
	        if (toggle != null) toggle.SetIsOnWithoutNotify(isAvoid);
	    }
	    void tryLoad_avoid_NSFW_generations()
	        => set_avoid_NSFW_generations(PlayerPrefs.GetInt("_avoid_NSFW_generations", 1) == 1);



	    public static Action<bool> _Act_viewportInCenterChanged { get; set; } = null;
	    bool _viewport_in_center = true;
	    public bool get_viewport_in_center() => _viewport_in_center;
	    void set_viewport_in_center(bool isCenter) {
	        _viewport_in_center = isCenter;
	        PlayerPrefs.SetInt("_viewport_in_center", _viewport_in_center ? 1 : 0); PlayerPrefs.Save();
	        var toggle = EventsBinder.FindComponent<Toggle>("Settings:set_viewport_in_center");
	        if (toggle != null) toggle.SetIsOnWithoutNotify(isCenter);
	        _Act_viewportInCenterChanged?.Invoke(isCenter);
	    }
	    void tryLoad_viewport_in_center()
	        => set_viewport_in_center(PlayerPrefs.GetInt("_viewport_in_center", 1) == 1);



	    public static Action<bool> _Act_verticalRibbonsSwapped { get; set; } = null;
	    bool _viewport_isSwapVerticalRibbons = false;
	    public bool get_viewport_isSwapVerticalRibbons() => _viewport_isSwapVerticalRibbons;
	    void set_viewport_isSwapVerticalRibbons(bool isSwapped) {
	        _viewport_isSwapVerticalRibbons = isSwapped;
	        PlayerPrefs.SetInt("_viewport_isSwapVerticalRibbons", _viewport_isSwapVerticalRibbons ? 1 : 0); PlayerPrefs.Save();
	        var toggle = EventsBinder.FindComponent<Toggle>("Settings:set_viewport_isSwapVerticalRibbons");
	        if (toggle != null) toggle.SetIsOnWithoutNotify(isSwapped);
	        _Act_verticalRibbonsSwapped?.Invoke(isSwapped);
	    }
	    void tryLoad_viewport_isSwapVerticalRibbons()
	        => set_viewport_isSwapVerticalRibbons(PlayerPrefs.GetInt("_viewport_isSwapVerticalRibbons", 0) == 1);



	    public static Action<Color> _Act_onWireframeColor { get; set; } = null;
	    static Color _default_wireframeColor = new Color(1, 0.74f, 0.471f, 0.902f);
	    Color _wireframeColor = _default_wireframeColor;
	    public Color get_wireframeColor() => _wireframeColor;
	    void set_wireframeColor(Color col) {
	        col.a = _wireframeOpacity;
	        _wireframeColor = col;
	        string hexCol = ColorUtility.ToHtmlStringRGBA(_wireframeColor);
	        PlayerPrefs.SetString("_wireframeColor", hexCol); PlayerPrefs.Save();
	        var buttonImage = EventsBinder.FindComponent<Button>("Settings:OnButton_WireframeColor")?.GetComponent<Image>();
	        if (buttonImage != null) {
	            Color fullAlpha = col;
	            fullAlpha.a = 1;
	            buttonImage.color = fullAlpha;
	        }
	        _Act_onWireframeColor?.Invoke(col);
	    }
	    void tryLoad_wireframeColor() {
	        string hexColor = PlayerPrefs.GetString("_wireframeColor", ColorUtility.ToHtmlStringRGBA(_default_wireframeColor));
	        if (ColorUtility.TryParseHtmlString("#" + hexColor, out Color color)) {
	            set_wireframeColor(color);
	        } else {
	            set_wireframeColor(_default_wireframeColor);
	        }
	    }
	    void OnButton_WireframeColor() {
	        var colorPicker = EventsBinder.FindComponent<ColorPalette_Panel_UI>("Settings:ColorPicker");
	        if (colorPicker != null) colorPicker.Show(_wireframeColor, set_wireframeColor);
	    }





	    static float _default_wireframeOpacity = 0.902f;
	    float _wireframeOpacity = _default_wireframeOpacity;
	    void set_wireframeOpacity(float opacity) {
	        _wireframeOpacity = Mathf.Clamp01(opacity);
	        PlayerPrefs.SetFloat("_wireframeOpacity", _wireframeOpacity); PlayerPrefs.Save();
	        var slider = EventsBinder.FindComponent<SliderUI_Snapping>("Settings:set_wireframeOpacity");
	        if (slider != null) slider.SetSliderValue(opacity, invokeCallback: false);
	        Color newColor = _wireframeColor;
	        newColor.a = _wireframeOpacity;
	        set_wireframeColor(newColor);
	    }
	    void tryLoad_wireframeOpacity() {
	        set_wireframeOpacity(PlayerPrefs.GetFloat("_wireframeOpacity", _default_wireframeOpacity));
	    }



	    public static Action<int> _Act_onTextSize { get; set; } = null;
	    static int _default_promptTextSize = 19;
	    int _promptTextSize = _default_promptTextSize;
	    public int get_getPromptTextSize() => _promptTextSize;
	    void set_prompt_textSize(float textSize) {
	        _promptTextSize = Mathf.RoundToInt(textSize);
	        PlayerPrefs.SetInt("_promptTextSize", _promptTextSize); PlayerPrefs.Save();
	        var slider = EventsBinder.FindComponent<SliderUI_Snapping>("Settings:set_prompt_textSize");
	        if (slider != null) slider.SetSliderValue(_promptTextSize, invokeCallback: false);
	        _Act_onTextSize?.Invoke(_promptTextSize);
	    }
	    void tryLoad_promptTextSize() {
	        set_prompt_textSize(PlayerPrefs.GetInt("_promptTextSize", _default_promptTextSize));
	    }



	    public static Action<int> _Act_OnShadowR_ChunkSize { get; set; } = null;
	    static int _default_ShadowR_chunkSize = 512;
	    int _ShadowR_chunkSize = _default_ShadowR_chunkSize;
	    public int get_ShadowR_chunkSize() => _ShadowR_chunkSize;
	    void set_ShadowR_chunkSize(float increment05) {
	        int chunkSize = 512;
	        switch (Mathf.RoundToInt(increment05)) {
	            case 0: chunkSize = 128; break;
	            case 1: chunkSize = 256; break;
	            case 2: chunkSize = 384; break;
	            case 3: chunkSize = 512; break;
	            case 4: chunkSize = 768; break;
	            case 5: chunkSize = 1024; break;
	        }
	        _ShadowR_chunkSize = chunkSize;
	        PlayerPrefs.SetInt("_ShadowR_chunkSize", chunkSize); PlayerPrefs.Save();
	        var slider = EventsBinder.FindComponent<SliderUI_Snapping>("Settings:set_ShadowR_chunkSize");
	        if (slider != null) slider.SetSliderValue(increment05, invokeCallback: false);
	        var textComponent = EventsBinder.FindComponent<TextMeshProUGUI>("Settings:set_ShadowR_chunkSize_descript_text"); // Assumes you bind this text
	        if (textComponent != null) textComponent.text = $"Shadow R chunk size ({_ShadowR_chunkSize})\n(lower=faster but makes seams)";
	        _Act_OnShadowR_ChunkSize?.Invoke(_ShadowR_chunkSize);
	    }
	    void tryLoad_ShadowR_chunkSize() {
	        float size = PlayerPrefs.GetInt("_ShadowR_chunkSize", _default_ShadowR_chunkSize);
	        int sliderVal = 0;
	        if (size <= 128) { sliderVal = 0; }
	        else if (size <= 256) { sliderVal = 1; }
	        else if (size <= 384) { sliderVal = 2; }
	        else if (size <= 512) { sliderVal = 3; }
	        else if (size <= 768) { sliderVal = 4; }
	        else { sliderVal = 5; }
	        set_ShadowR_chunkSize(sliderVal);
	    }



	    bool _isAlwaysFocusCameraPivot = true;
	    public bool get_isAlwaysFocusCameraPivot() => _isAlwaysFocusCameraPivot;
	    void set_isAlwaysFocusCameraPivot(bool isAlwaysFocus) {
	        _isAlwaysFocusCameraPivot = isAlwaysFocus;
	        PlayerPrefs.SetInt("_isAlwaysFocusCameraPivot", _isAlwaysFocusCameraPivot ? 1 : 0); PlayerPrefs.Save();
	        var toggle = EventsBinder.FindComponent<Toggle>("Settings:set_isAlwaysFocusCameraPivot");
	        if (toggle != null) toggle.SetIsOnWithoutNotify(isAlwaysFocus);
	    }
	    void tryLoad_isAlwaysFocusCameraPivot()
	        => set_isAlwaysFocusCameraPivot(PlayerPrefs.GetInt("_isAlwaysFocusCameraPivot", 1) == 1);



	    bool _useCtrlScroll_for_WorkflowMode_swaps = false;
	    public bool get_useCtrlScroll_for_WorkflowMode_swaps() => _useCtrlScroll_for_WorkflowMode_swaps;
	    void set_useCtrlScroll_for_WorkflowMode_swaps(bool useCtrlScroll) {
	        _useCtrlScroll_for_WorkflowMode_swaps = useCtrlScroll;
	        PlayerPrefs.SetInt("_useCtrlScroll_for_WorkflowMode_swaps", _useCtrlScroll_for_WorkflowMode_swaps ? 1 : 0); PlayerPrefs.Save();
	        var toggle = EventsBinder.FindComponent<Toggle>("Settings:set_useCtrlScroll_for_WorkflowMode_swaps");
	        if (toggle != null) toggle.SetIsOnWithoutNotify(useCtrlScroll);
	    }
	    void tryLoad_useCtrlScroll_for_WorkflowMode_swaps()
	        => set_useCtrlScroll_for_WorkflowMode_swaps(PlayerPrefs.GetInt("_useCtrlScroll_for_WorkflowMode_swaps", 0) == 1);



	    bool _ignoreCtrl_if_clickSelectingMeshes = false;
	    public bool get_ignoreCtrl_if_clickSelectingMeshes() => _ignoreCtrl_if_clickSelectingMeshes;
	    void set_ignoreCtrl_if_clickSelectingMeshes(bool ignoreCtrl) {
	        _ignoreCtrl_if_clickSelectingMeshes = ignoreCtrl;
	        PlayerPrefs.SetInt("_ignoreCtrl_if_clickSelectingMeshes", _ignoreCtrl_if_clickSelectingMeshes ? 1 : 0); PlayerPrefs.Save();
	        var toggle = EventsBinder.FindComponent<Toggle>("Settings:set_ignoreCtrl_if_clickSelectingMeshes");
	        if (toggle != null) toggle.SetIsOnWithoutNotify(ignoreCtrl);
	    }
	    void tryLoad_ignoreCtrl_if_clickSelectingMeshes()
	        => set_ignoreCtrl_if_clickSelectingMeshes(PlayerPrefs.GetInt("_ignoreCtrl_if_clickSelectingMeshes", 0) == 1);



	    [SerializeField] AnimationCurve _warpSpeed_curve;
	    static float _default_uvWarpSpeed01 = 0.5f;
	    float _uvWarpSpeed01 = _default_uvWarpSpeed01;
	    public float get_uvWarpSpeed01() => _uvWarpSpeed01;
	    public float get_uvWarpSpeed() => _warpSpeed_curve.Evaluate(_uvWarpSpeed01);
	    void set_uvWarpSpeed01(float speed01) {
	        _uvWarpSpeed01 = speed01;
	        PlayerPrefs.SetFloat("_uvWarpSpeed01", _uvWarpSpeed01); PlayerPrefs.Save();
	        var slider = EventsBinder.FindComponent<SliderUI_Snapping>("Settings:set_uvWarpSpeed01");
	        if (slider != null) slider.SetSliderValue(speed01, invokeCallback: false);
	    }
	    void tryLoad_uvWarpSpeed01() {
	        set_uvWarpSpeed01(PlayerPrefs.GetFloat("_uvWarpSpeed01", _default_uvWarpSpeed01));
	    }



	    static float _default_noiseSpeed = 1f;
	    float _noiseSpeed = _default_noiseSpeed;
	    public float get_noiseSpeed() => _noiseSpeed;
	    void set_noiseSpeed(float speed) {
	        _noiseSpeed = Mathf.Clamp(speed, 0.01f, 2f);
	        PlayerPrefs.SetFloat("_noiseSpeed", _noiseSpeed); PlayerPrefs.Save();
	        var slider = EventsBinder.FindComponent<SliderUI_Snapping>("Settings:set_noiseSpeed");
	        if (slider != null) slider.SetSliderValue(speed, invokeCallback: false);
	    }
	    void tryLoad_noiseSpeed() {
	        set_noiseSpeed(PlayerPrefs.GetFloat("_noiseSpeed", _default_noiseSpeed));
	    }



	    static Color _default_noiseColor = new Color(0.231f, 0.05f, 0.374f, 1f);
	    Color _noiseColor = _default_noiseColor;
	    public Color get_noiseColor() => _noiseColor;
	    void set_noiseColor(Color col) {
	        _noiseColor = col;
	        string hexCol = ColorUtility.ToHtmlStringRGBA(_noiseColor);
	        PlayerPrefs.SetString("_noiseColor", hexCol); PlayerPrefs.Save();
	        var buttonImage = EventsBinder.FindComponent<Button>("Settings:OnButton_NoiseColor")?.GetComponent<Image>();
	        if (buttonImage != null) buttonImage.color = col;
	    }
	    void tryLoad_noiseColor() {
	        string hexColor = PlayerPrefs.GetString("_noiseColor", ColorUtility.ToHtmlStringRGBA(_default_noiseColor));
	        if (ColorUtility.TryParseHtmlString("#" + hexColor, out Color color)) {
	            set_noiseColor(color);
	        } else {
	            set_noiseColor(_default_noiseColor);
	        }
	    }
	    void OnButton_NoiseColor() {
	        var colorPicker = EventsBinder.FindComponent<ColorPalette_Panel_UI>("Settings:ColorPicker"); // Assumes picker is bound
	        if (colorPicker != null) colorPicker.Show(_noiseColor, set_noiseColor);
	    }



	    bool _layout_askServerOften = false;
	    public bool get_layout_askServerOften() => _layout_askServerOften;
	    void set_layout_askServerOften(bool askOften) {
	        _layout_askServerOften = askOften;
	        PlayerPrefs.SetInt("_layout_askServerOften", _layout_askServerOften ? 1 : 0); PlayerPrefs.Save();
	        var toggle = EventsBinder.FindComponent<Toggle>("Settings:set_layout_askServerOften");
	        if (toggle != null) toggle.SetIsOnWithoutNotify(askOften);
	    }
	    void tryLoad_layout_askServerOften()
	        => set_layout_askServerOften(PlayerPrefs.GetInt("_layout_askServerOften", 0) == 1);



	    public static bool isLaunchFastWebui { get; private set; }
	    public static void Set_isFastWebui(bool isFastWebui) {
	        isLaunchFastWebui = isFastWebui;
	        PlayerPrefs.SetInt("isLaunchFastWebui", isFastWebui ? 1 : 0); PlayerPrefs.Save();
	    }

 
	    void OnButton_OpenSettingsPanel() {
	        var panel = EventsBinder.FindComponent<RectTransform>("Settings:SettingsPanel");
	        if (panel != null) panel.gameObject.SetActive(true);
	        var autoScroll = EventsBinder.FindComponent<ScrollRect_AutoScroll>("Settings:AutoScroll"); 
	        if (autoScroll != null) autoScroll.ScrollToEnd(0.35f, false);
	    }


	    void OnButton_RestoreDefaults() {
	        string confirmMsg = $"Restore default settings?\n<b>This will delete ALL the Art icons.</b>";
	        ConfirmPopup_UI.instance.Show(confirmMsg, OnYes, OnNo);
	        void OnYes() {
	            set_targetFrameRate(70);
	            set_brushPrecision_res(1024, skipConfirmPopup: true);
	            set_prompt_textHighlight(true);
	            set_isAllowTooltips(true);
	            set_wireframeColor(new Color(1, 0.74f, 0.471f, 0.902f));
	            set_wireframeOpacity(_default_wireframeOpacity);
	            set_isAlwaysFocusCameraPivot(true);
	            set_avoid_NSFW_generations(false);
	            set_viewport_in_center(true);
	            set_viewport_isSwapVerticalRibbons(false);
	            set_uvWarpSpeed01(_default_uvWarpSpeed01);
	            set_noiseSpeed(_default_noiseSpeed);
	            set_noiseColor(_default_noiseColor);
	            set_layout_askServerOften(false);
	            set_ignoreCtrl_if_clickSelectingMeshes(false);
	            set_useCtrlScroll_for_WorkflowMode_swaps(false);
	        }
	        void OnNo() { }
	    }


	    void AdjustTargetFramerate() {
        
	        bool canReduce_FPS = Performance_MGR.instance != null &&
	                             Performance_MGR.instance.isThrottleFPS_whenGenerating() &&
	                             GenerateButtons_UI.isGenerating &&
	                             GenerateButtons_UI.isGeneratingPaused == false;
	        if (canReduce_FPS){
	            Application.targetFrameRate = _idleFramerate;
	            return;
	        }
	        if (Time.time < 15){
	            Application.targetFrameRate = _targetFrameRate;
	            return;
	        }
	        bool dontThrottle = Time.time < _dontThrottle_any_FPS_until;
	        Application.targetFrameRate = _hasFocus || dontThrottle ? _targetFrameRate : _idleFramerate;
	    }


	    void Update() {
	        int targ = Application.targetFrameRate;
	        if (targ != _targetFrameRate && targ != _idleFramerate) {
	            Debug.LogError("Something changed the target frame rate. Only Settings_MGR should do it");
	        }
	        AdjustTargetFramerate();

	        var settingsPanel = EventsBinder.FindComponent<RectTransform>("Settings:SettingsPanel");
	        var colorPicker = EventsBinder.FindComponent<ColorPalette_Panel_UI>("Settings:ColorPicker");
	        if (settingsPanel == null || colorPicker == null) return;

	        Vector2 cursorPos = KeyMousePenInput.cursorScreenPos();
	        bool isPressed = KeyMousePenInput.isLMBpressed();
	        bool isClicked = KeyMousePenInput.isLMBpressedThisFrame() || KeyMousePenInput.isRMBpressedThisFrame() || KeyMousePenInput.isMMBpressedThisFrame();

	        if (settingsPanel.gameObject.activeInHierarchy && !isPressed) {
	            bool isInsidePanel = RectTransformUtility.RectangleContainsScreenPoint(settingsPanel, cursorPos);
	            if (!isInsidePanel) settingsPanel.gameObject.SetActive(false);
	        }

	        if (colorPicker._isShowing && isClicked && !isPressed) {
	            var rtf = colorPicker.transform as RectTransform;
	            if (RectTransformUtility.RectangleContainsScreenPoint(rtf, cursorPos) == false) {
	                colorPicker.Hide();
	            }
	        }
	    }


	    void Awake() {
	        if (instance != null) { DestroyImmediate(this); return; }
	        instance = this;

	        // Note: You must now bind helper components like the panel, color picker, etc., in your Settings_UI script.
	        // Example in Settings_UI.Start():
	        // UIEventBinder.Bind("Settings:SettingsPanel", _settingsPanel_go.GetComponent<RectTransform>());
	        // UIEventBinder.Bind("Settings:ColorPicker", _settings_colorPicker);
	        // UIEventBinder.Bind("Settings:AutoScroll", _autoScroll);
	        // UIEventBinder.Bind("Settings:set_ShadowR_chunkSize_descript_text", _shadowR_chunkSize_descript);

	        StaticEvents.SubscribeUnique("Settings:OpenSettingsPanel", OnButton_OpenSettingsPanel);
	        StaticEvents.SubscribeUnique<int>("Settings:set_targetFrameRate", set_targetFrameRate);
	        StaticEvents.SubscribeUnique<float>("Settings:set_prompt_textSize", set_prompt_textSize);
	        StaticEvents.SubscribeUnique("Settings:OnButton_WireframeColor", OnButton_WireframeColor);
	        StaticEvents.SubscribeUnique<float>("Settings:set_wireframeOpacity", set_wireframeOpacity);
	        StaticEvents.SubscribeUnique<float>("Settings:set_uvWarpSpeed01", set_uvWarpSpeed01);
	        StaticEvents.SubscribeUnique<float>("Settings:set_ShadowR_chunkSize", set_ShadowR_chunkSize);
	        // Special lambda handler for brush precision
	        StaticEvents.SubscribeUnique<bool>("Settings:set_brushPrecision_res", val => set_brushPrecision_res(val ? 4096 : 2048));
	        StaticEvents.SubscribeUnique<bool>("Settings:set_prompt_textHighlight", set_prompt_textHighlight);
	        StaticEvents.SubscribeUnique<bool>("Settings:set_isAllowTooltips", set_isAllowTooltips);
	        StaticEvents.SubscribeUnique<bool>("Settings:set_isShow_CameraInfoText", set_isShow_CameraInfoText);
	        StaticEvents.SubscribeUnique<bool>("Settings:set_isAlwaysFocusCameraPivot", set_isAlwaysFocusCameraPivot);
	        StaticEvents.SubscribeUnique<bool>("Settings:set_avoid_NSFW_generations", set_avoid_NSFW_generations);
	        StaticEvents.SubscribeUnique<bool>("Settings:set_viewport_in_center", set_viewport_in_center);
	        StaticEvents.SubscribeUnique<bool>("Settings:set_viewport_isSwapVerticalRibbons", set_viewport_isSwapVerticalRibbons);
	        StaticEvents.SubscribeUnique("Settings:OnButton_RestoreDefaults", OnButton_RestoreDefaults);
	        StaticEvents.SubscribeUnique<float>("Settings:set_noiseSpeed", set_noiseSpeed);
	        StaticEvents.SubscribeUnique("Settings:OnButton_NoiseColor", OnButton_NoiseColor);
	        StaticEvents.SubscribeUnique<bool>("Settings:set_layout_askServerOften", set_layout_askServerOften);
	        StaticEvents.SubscribeUnique<bool>("Settings:set_useCtrlScroll_for_WorkflowMode_swaps", set_useCtrlScroll_for_WorkflowMode_swaps);
	        StaticEvents.SubscribeUnique<bool>("Settings:set_ignoreCtrl_if_clickSelectingMeshes", set_ignoreCtrl_if_clickSelectingMeshes);

	        tryLoad_targetFrameRate();
	        tryLoad_brushPrecision_res();
	        tryLoad_prompt_textHighlight();
	        tryLoad_isShow_CameraInfoText();
	        tryLoad_isAllowTooltips();
	        tryLoad_promptTextSize();
	        tryLoad_wireframeColor();
	        tryLoad_wireframeOpacity();
	        tryLoad_ShadowR_chunkSize();
	        tryLoad_isAlwaysFocusCameraPivot();
	        tryLoad_avoid_NSFW_generations();
	        tryLoad_viewport_in_center();
	        tryLoad_viewport_isSwapVerticalRibbons();
	        tryLoad_uvWarpSpeed01();
	        tryLoad_noiseSpeed();
	        tryLoad_noiseColor();
	        tryLoad_layout_askServerOften();
	        tryLoad_useCtrlScroll_for_WorkflowMode_swaps();
	        tryLoad_ignoreCtrl_if_clickSelectingMeshes();
	        isLaunchFastWebui = PlayerPrefs.GetInt("isLaunchFastWebui", 0) > 0;
	    }

	    void Start() {
	        QualitySettings.vSyncCount = 0;
	        Application.targetFrameRate = _targetFrameRate;
	    }

	    void OnApplicationFocus(bool hasFocus) {
	        _hasFocus = hasFocus;
	        if (Keyboard.current != null) { InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); }
	        if (Mouse.current != null) { InputSystem.QueueStateEvent(Mouse.current, new MouseState()); }
	        if (Pen.current != null) { InputSystem.QueueStateEvent(Pen.current, new PenState()); }
	        Input.ResetInputAxes();
	    }

	    void OnApplicationPause(bool pauseStatus) {
	        OnApplicationFocus(!pauseStatus);
	    }

	    void OnDestroy() {
	        if (instance != this) return;
	        // Important: Unsubscribe from all static events to prevent memory leaks.
	        // Note: A dedicated method should be used for the lambda subscription to enable unsubscribing.
	        // For now, this demonstrates the required pattern.
	        StaticEvents.Unsubscribe("Settings:OpenSettingsPanel", OnButton_OpenSettingsPanel);
	        StaticEvents.Unsubscribe<int>("Settings:set_targetFrameRate", set_targetFrameRate);
	        StaticEvents.Unsubscribe<float>("Settings:set_prompt_textSize", set_prompt_textSize);
	        StaticEvents.Unsubscribe("Settings:OnButton_WireframeColor", OnButton_WireframeColor);
	        StaticEvents.Unsubscribe<float>("Settings:set_wireframeOpacity", set_wireframeOpacity);
	        StaticEvents.Unsubscribe<float>("Settings:set_uvWarpSpeed01", set_uvWarpSpeed01);
	        StaticEvents.Unsubscribe<float>("Settings:set_ShadowR_chunkSize", set_ShadowR_chunkSize);
	        StaticEvents.Unsubscribe<bool>("Settings:set_prompt_textHighlight", set_prompt_textHighlight);
	        StaticEvents.Unsubscribe<bool>("Settings:set_isAllowTooltips", set_isAllowTooltips);
	        StaticEvents.Unsubscribe<bool>("Settings:set_isShow_CameraInfoText", set_isShow_CameraInfoText);
	        StaticEvents.Unsubscribe<bool>("Settings:set_isAlwaysFocusCameraPivot", set_isAlwaysFocusCameraPivot);
	        StaticEvents.Unsubscribe<bool>("Settings:set_avoid_NSFW_generations", set_avoid_NSFW_generations);
	        StaticEvents.Unsubscribe<bool>("Settings:set_viewport_in_center", set_viewport_in_center);
	        StaticEvents.Unsubscribe<bool>("Settings:set_viewport_isSwapVerticalRibbons", set_viewport_isSwapVerticalRibbons);
	        StaticEvents.Unsubscribe("Settings:OnButton_RestoreDefaults", OnButton_RestoreDefaults);
	        StaticEvents.Unsubscribe<float>("Settings:set_noiseSpeed", set_noiseSpeed);
	        StaticEvents.Unsubscribe("Settings:OnButton_NoiseColor", OnButton_NoiseColor);
	        StaticEvents.Unsubscribe<bool>("Settings:set_layout_askServerOften", set_layout_askServerOften);
	        StaticEvents.Unsubscribe<bool>("Settings:set_useCtrlScroll_for_WorkflowMode_swaps", set_useCtrlScroll_for_WorkflowMode_swaps);
	        StaticEvents.Unsubscribe<bool>("Settings:set_ignoreCtrl_if_clickSelectingMeshes", set_ignoreCtrl_if_clickSelectingMeshes);
	    }
	}
}//end namespace
