using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Small floating panel for RingProjection_MGR. It is created at runtime so
	// the original scenes and prefabs stay untouched.
	public class RingProjection_UI : MonoBehaviour {
	    RingProjection_MGR _manager = null;

	    GameObject _canvasGO = null;
	    GameObject _panelGO = null;
	    GameObject _bodyGO = null;
	    Text _modeText = null;
	    Text _viewsText = null;
	    Text _statusText = null;
	    Button _modeButton = null;
	    Button _generateButton = null;
	    Button _stopButton = null;
	    Button _collapseButton = null;
	    Button _minusButton = null;
	    Button _plusButton = null;

	    Font _font = null;
	    bool _collapsed = false;

	    static readonly Color PanelColor = new Color(0.075f, 0.085f, 0.105f, 0.94f);
	    static readonly Color ButtonColor = new Color(0.21f, 0.23f, 0.28f, 1f);
	    static readonly Color AccentColor = new Color(0.78f, 0.40f, 0.12f, 1f);
	    static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
	    static readonly Color MutedTextColor = new Color(0.66f, 0.69f, 0.76f, 1f);


	    void Start() {
	        _manager = RingProjection_MGR.instance;
	        if (_manager == null) { enabled = false; return; }

	        _font = LoadFont();
	        BuildUI();
	        _manager.OnStateChanged += Refresh;
	        Refresh();
	    }


	    void OnDestroy() {
	        if (_manager != null) { _manager.OnStateChanged -= Refresh; }
	    }


	    void Update() {
	        if (_panelGO == null) { return; }
	        bool show = StableDiffusion_Hub.instance != null
	                 && DimensionMode_MGR.instance != null
	                 && DimensionMode_MGR.instance._dimensionMode == DimensionMode.dim_sd;
	        if (_panelGO.activeSelf != show) { _panelGO.SetActive(show); }
	    }


	    void BuildUI() {
	        _canvasGO = new GameObject("RingProjection_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
	        _canvasGO.transform.SetParent(transform, false);

	        var canvas = _canvasGO.GetComponent<Canvas>();
	        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
	        canvas.sortingOrder = 200;

	        var scaler = _canvasGO.GetComponent<CanvasScaler>();
	        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
	        scaler.referenceResolution = new Vector2(1920f, 1080f);
	        scaler.matchWidthOrHeight = 0.5f;

	        _panelGO = new GameObject("RingPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
	        var panelRT = (RectTransform)_panelGO.transform;
	        panelRT.SetParent(_canvasGO.transform, false);
	        // Keep the panel out of the middle of the viewport: bottom-left corner,
	        // just inside the 3D view (the left tool column ends at ~450px in the
	        // 1920x1080 reference layout). The old top-centre position covered the
	        // model the user is about to texture.
	        panelRT.anchorMin = new Vector2(0f, 0f);
	        panelRT.anchorMax = new Vector2(0f, 0f);
	        panelRT.pivot = new Vector2(0f, 0f);
	        panelRT.anchoredPosition = new Vector2(466f, 14f);
	        panelRT.sizeDelta = new Vector2(412f, 0f);

	        var panelImg = _panelGO.GetComponent<Image>();
	        panelImg.color = PanelColor;
	        panelImg.raycastTarget = true;

	        var vlg = _panelGO.GetComponent<VerticalLayoutGroup>();
	        vlg.padding = new RectOffset(10, 10, 8, 8);
	        vlg.spacing = 5f;
	        vlg.childAlignment = TextAnchor.UpperLeft;
	        vlg.childControlWidth = true;
	        vlg.childControlHeight = true;
	        vlg.childForceExpandWidth = true;
	        vlg.childForceExpandHeight = false;

	        var fitter = _panelGO.GetComponent<ContentSizeFitter>();
	        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
	        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

	        // Header
	        var headerGO = MakeRow(_panelGO.transform, "Header", 22f);
	        var header = headerGO.GetComponent<HorizontalLayoutGroup>();
	        header.childForceExpandWidth = false;

	        var title = MakeText(headerGO.transform, "Title", "RING TEXTURING", 13, TextColor, TextAnchor.MiddleLeft);
	        var titleLE = title.gameObject.AddComponent<LayoutElement>();
	        titleLE.flexibleWidth = 1f;
	        titleLE.preferredHeight = 22f;

	        _collapseButton = MakeButton(headerGO.transform, "Collapse", "-", 24f, 20f, ToggleCollapsed);
	        _collapseButton.GetComponentInChildren<Text>().fontSize = 16;

	        // Body
	        _bodyGO = new GameObject("Body", typeof(RectTransform), typeof(VerticalLayoutGroup));
	        _bodyGO.transform.SetParent(_panelGO.transform, false);
	        var bodyVLG = _bodyGO.GetComponent<VerticalLayoutGroup>();
	        bodyVLG.padding = new RectOffset(0, 0, 0, 0);
	        bodyVLG.spacing = 5f;
	        bodyVLG.childAlignment = TextAnchor.UpperLeft;
	        bodyVLG.childControlWidth = true;
	        bodyVLG.childControlHeight = true;
	        bodyVLG.childForceExpandWidth = true;
	        bodyVLG.childForceExpandHeight = false;

	        // Mode / Generate / Stop row
	        var actionRowGO = MakeRow(_bodyGO.transform, "Actions", 28f);
	        var actionRow = actionRowGO.GetComponent<HorizontalLayoutGroup>();
	        actionRow.childForceExpandWidth = false;

	        _modeButton = MakeButton(actionRowGO.transform, "Mode", "Ring mode: OFF", 166f, 28f, () => _manager.ToggleEnabled());
	        _modeText = _modeButton.GetComponentInChildren<Text>();
	        _modeText.fontSize = 12;

	        _generateButton = MakeButton(actionRowGO.transform, "Generate", "Generate ring", 116f, 28f, GenerateRing);
	        _generateButton.GetComponentInChildren<Text>().fontSize = 12;

	        _stopButton = MakeButton(actionRowGO.transform, "Stop", "Stop", 54f, 28f, () => _manager.Cancel());
	        _stopButton.GetComponentInChildren<Text>().fontSize = 12;

	        // Views row
	        var viewsRowGO = MakeRow(_bodyGO.transform, "Views", 26f);
	        var viewsRow = viewsRowGO.GetComponent<HorizontalLayoutGroup>();
	        viewsRow.childForceExpandWidth = false;

	        var viewsLabel = MakeText(viewsRowGO.transform, "ViewsLabel", "Views", 12, MutedTextColor, TextAnchor.MiddleLeft);
	        var viewsLabelLE = viewsLabel.gameObject.AddComponent<LayoutElement>();
	        viewsLabelLE.preferredWidth = 44f;
	        viewsLabelLE.preferredHeight = 26f;

	        _minusButton = MakeButton(viewsRowGO.transform, "Minus", "-", 26f, 26f, () => _manager.SetNumViews(_manager.numViews - 1));
	        _minusButton.GetComponentInChildren<Text>().fontSize = 16;

	        _viewsText = MakeText(viewsRowGO.transform, "ViewsValue", "4", 14, TextColor, TextAnchor.MiddleCenter);
	        var viewsValueLE = _viewsText.gameObject.AddComponent<LayoutElement>();
	        viewsValueLE.preferredWidth = 34f;
	        viewsValueLE.preferredHeight = 26f;

	        _plusButton = MakeButton(viewsRowGO.transform, "Plus", "+", 26f, 26f, () => _manager.SetNumViews(_manager.numViews + 1));
	        _plusButton.GetComponentInChildren<Text>().fontSize = 16;

	        var hint = MakeText(viewsRowGO.transform, "Hint", "2-8 full-res generations", 11, MutedTextColor, TextAnchor.MiddleLeft);
	        var hintLE = hint.gameObject.AddComponent<LayoutElement>();
	        hintLE.flexibleWidth = 1f;
	        hintLE.preferredHeight = 26f;

	        // Status
	        _statusText = MakeText(_bodyGO.transform, "Status", "Idle", 11, MutedTextColor, TextAnchor.UpperLeft);
	        _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
	        _statusText.verticalOverflow = VerticalWrapMode.Truncate;
	        var statusLE = _statusText.gameObject.AddComponent<LayoutElement>();
	        statusLE.preferredHeight = 30f;
	        statusLE.minHeight = 30f;
	    }


	    void UpdateLabels() {
	        if (_manager == null) { return; }

	        _modeText.text = _manager.isEnabled ? "Ring mode: ON" : "Ring mode: OFF";
	        _modeButton.GetComponent<Image>().color = _manager.isEnabled ? AccentColor : ButtonColor;
	        _viewsText.text = _manager.numViews.ToString();
	        _statusText.text = _manager.statusText;

	        _generateButton.interactable = _manager.isEnabled && !_manager.isRunning;
	        _stopButton.interactable = _manager.isRunning;

	        _modeButton.interactable = !_manager.isRunning;
	        _minusButton.interactable = !_manager.isRunning && _manager.numViews > RingProjection_MGR.MIN_VIEWS;
	        _plusButton.interactable = !_manager.isRunning && _manager.numViews < RingProjection_MGR.MAX_VIEWS;
	        _generateButton.GetComponent<Image>().color = _manager.isEnabled ? new Color(0.24f, 0.42f, 0.27f, 1f) : ButtonColor;
	    }


	    void Refresh() {
	        if (_modeText == null) { return; }
	        UpdateLabels();
	    }


	    void GenerateRing() {
	        if (StableDiffusion_Hub.instance == null) { return; }
	        StableDiffusion_Hub.instance.RequestGenerateArt();
	    }


	    void ToggleCollapsed() {
	        _collapsed = !_collapsed;
	        _bodyGO.SetActive(!_collapsed);
	        _collapseButton.GetComponentInChildren<Text>().text = _collapsed ? "+" : "-";
	    }


	    GameObject MakeRow(Transform parent, string name, float height) {
	        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
	        go.transform.SetParent(parent, false);
	        var hlg = go.GetComponent<HorizontalLayoutGroup>();
	        hlg.spacing = 6f;
	        hlg.childAlignment = TextAnchor.MiddleLeft;
	        hlg.childControlWidth = true;
	        hlg.childControlHeight = true;
	        hlg.childForceExpandWidth = true;
	        hlg.childForceExpandHeight = false;
	        var le = go.GetComponent<LayoutElement>();
	        le.preferredHeight = height;
	        le.minHeight = height;
	        return go;
	    }


	    Text MakeText(Transform parent, string name, string value, int size, Color color, TextAnchor anchor) {
	        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
	        go.transform.SetParent(parent, false);
	        var text = go.GetComponent<Text>();
	        text.font = _font;
	        text.fontSize = size;
	        text.color = color;
	        text.text = value;
	        text.alignment = anchor;
	        text.raycastTarget = false;
	        text.supportRichText = true;
	        text.horizontalOverflow = HorizontalWrapMode.Overflow;
	        text.verticalOverflow = VerticalWrapMode.Truncate;
	        return text;
	    }


	    Button MakeButton(Transform parent, string name, string label, float width, float height, UnityEngine.Events.UnityAction onClick) {
	        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
	        go.transform.SetParent(parent, false);

	        var img = go.GetComponent<Image>();
	        img.color = ButtonColor;

	        var le = go.GetComponent<LayoutElement>();
	        le.preferredWidth = width;
	        le.minWidth = width;
	        le.preferredHeight = height;
	        le.minHeight = height;

	        var button = go.GetComponent<Button>();
	        button.targetGraphic = img;
	        var colors = button.colors;
	        colors.normalColor = Color.white;
	        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
	        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
	        colors.selectedColor = colors.highlightedColor;
	        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
	        button.colors = colors;
	        button.onClick.AddListener(onClick);

	        var text = MakeText(go.transform, "Label", label, 12, TextColor, TextAnchor.MiddleCenter);
	        var textRT = (RectTransform)text.transform;
	        textRT.anchorMin = Vector2.zero;
	        textRT.anchorMax = Vector2.one;
	        textRT.offsetMin = new Vector2(3f, 1f);
	        textRT.offsetMax = new Vector2(-3f, -1f);
	        text.horizontalOverflow = HorizontalWrapMode.Overflow;

	        return button;
	    }


	    static Font LoadFont() {
	        try {
	            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
	            if (f != null) { return f; }
	        } catch { }
	        try {
	            Font f = Resources.GetBuiltinResource<Font>("Arial.ttf");
	            if (f != null) { return f; }
	        } catch { }
	        return Font.CreateDynamicFontFromOSFont("Arial", 14);
	    }
	}
}//end namespace
