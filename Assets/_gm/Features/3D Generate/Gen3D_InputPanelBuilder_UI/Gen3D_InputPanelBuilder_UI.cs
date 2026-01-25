using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace spz {

	/* Component that allows building UI dynamically from a string specification.
	   This allows different webui or repositories to tell StableProjectorz what inputs they expect.
	   They do this by providing a layout string to StableProjectorz, which then spawns the needed input fields.
	   Values can later be retrieved via GetAllValues()

	ELEMENTS:
	vgroup [props]:          // Container with vertical layout
	   [child elements]      // Indented children
	hgroup [props]:          // Container with horizontal layout  
	   [child elements]      // Indented children
	go [props]:              // Basic GameObject with RectTransform
	   [child elements]      // Indented children

	space                    // Empty layout element with LayoutElement component

	header:                  // text header with optional styling
	visible_name=(text)      // Header text
	color=(R,G,B,A)          // Optional: Text color (RGB 0-255, A 0-1), default white
	align=left|center|right  // Optional: Text alignment, defaults to left


	CONTROLS:
	slider [props]           // Circular slider control
	toggle [props]           // Toggle/checkbox
	button [props]           // Button control
	intinput [props]        // Integer input field
	strinput [props]        // Text input field
	dropdown [props]         // Dropdown selection menu

	LAYOUT GROUP PROPERTIES:
	padding=N               // Uniform padding all sides
	padl=N                  // Left padding
	padr=N                  // Right padding
	padt=N                  // Top padding
	padb=N                  // Bottom padding
	spacing=N               // Space between children
	childForceExpandWidth=true|false   // Force children expand horizontally (false by default)
	childForceExpandHeight=true|false  // Force children expand vertically (false by default)
	childAlignment=TopLeft|TopCenter|TopRight|MiddleLeft|MiddleCenter|MiddleRight|LowerLeft|LowerCenter|LowerRight

	LAYOUT ELEMENT PROPERTIES - Adding any of these automatically adds a LayoutElement component:
	minWidth=N              // Minimum width
	minHeight=N             // Minimum height
	preferredWidth=N        // Preferred width
	preferredHeight=N       // Preferred height
	flexibleWidth=N         // Flex grow factor (0 by default)
	flexibleHeight=N        // Flex grow factor for height
	ignoreLayout=true|false // Whether to ignore this element in layout calculations

	VISUAL PROPERTIES - Adding any of these automatically adds an Image component:
	bg.color=(R,G,B,A)     // Background color (RGB 0-255, A 0-1)
	bg.ppu=N               // Background pixels per unit multiplier (10 if not specified)

	RECT TRANSFORM PROPERTIES:
	rect.anchors=(minX,minY,maxX,maxY)  // Anchor points (0-1)
	rect.offsetMin=(x,y)    // Position offset from min anchor
	rect.offsetMax=(x,y)    // Position offset from max anchor
	rect.pivot=(x,y)        // Pivot point (0-1)
	rect.scale=(x,y,z)      // Local scale

	CONTROL SPECIFIC PROPERTIES:

	slider:
	code_name=(name)        // Internal identifier
	visible_name=(label)    // Display label
	min=N                   // Minimum value (float)
	max=N                   // Maximum value (float)
	default=N               // Default value (float)
	show_n_decimals=N       // how many decimals to show after the dot (float) 0 if not specified.

	toggle:
	code_name=(name)        // Internal identifier
	visible_name=(label)    // Display label
	default=true|false      // Default state

	button:
	code_name=(name)        // Internal identifier
	visible_name=(label)    // Display label

	int_vertical:
	code_name=(name)        // Internal identifier
	visible_name=(label)    // Display label
	min=N                   // Minimum value (integer)
	max=N                   // Maximum value (integer)
	default=N               // Default value (integer)

	int_horizontal:
	same as the int_vertical, but also can have  as_seed=true|false  
	if as_seed is true, it shows a small dice button, resets to default value if pressed.

	strInput:
	code_name=(name)        // Internal identifier
	visible_name=(label)    // Display label
	default=(text)          // Default text value
	content_min_num_needed=N // unless we have this many characters, the Gen3D button should remain inactive. 0 if not specified.

	text_prompt:
	code_name=(name)        // Internal identifier
	visible_name=(label)    // Display label
	default=(text)          // Default text value
	content_min_num_needed=N  // unless we have this many characters, the Gen3D button should remain inactive.  0 if not specified.
	is_positive=true|false

	single_multi_img_input:
	content_min_num_needed=N   // unless we have this many images, the Gen3D button should remain inactive.  0 if not specified.

	dropdown:
	code_name=(name)        // Internal identifier
	visible_name=(label)    // Display label
	options=[option1,option2,option3]  // Comma-separated list of options
	default=N               // Default selected index (integer)

	EXAMPLES:

	// Basic layout with layout element and two child GameObjects
	go bg.color=(255,255,255,0.04) bg.ppu=10 rect.anchors=(0,0,1,1) flexibleWidth=1:
	   go rect.anchors=(0,0,0.5,1) bg.color=(255,0,0,0.04) bg.ppu=10
	   go rect.anchors=(0.5,0,1,1) bg.color=(0,255,0,0.04) bg.ppu=10

	// Complex UI with groups and controls
	vgroup padl=10 padr=20 padt=15 padb=10 childForceExpandWidth=true bg.color=(255,255,255,0.04) bg.ppu=10 rect.anchors=(0,0,1,1) rect.offsetMin=(5,5) rect.offsetMax=(-5,-5) rect.pivot=(0.5,0.5) rect.scale=(1,1,1):
	    hgroup spacing=10 childAlignment=MiddleCenter childForceExpandHeight=false:
	        slider code_name=(base_scale) visible_name=(Base Scale) min=0.1 max=2.0 default=1.0
	        space minWidth=20 preferredWidth=20 flexibleWidth=1
	        slider code_name=(detail_scale) visible_name=(Detail Scale) min=0.1 max=2.0 default=1.0
	    space minHeight=15 preferredHeight=15
    
	    hgroup spacing=5 bg.color=(0,0,0,0.1) bg.ppu=10:
	        toggle code_name=(advanced_mode) visible_name=(Advanced Mode) default=false
	        space minWidth=10 preferredWidth=10 flexibleWidth=1
	        toggle code_name=(high_quality) visible_name=(High Quality) default=true
    
	    vgroup padding=5 childForceExpandWidth=true:
	        intinput code_name=(seed) visible_name=(Random Seed) min=0 max=999999 default=42
	        dropdown code_name=(style) visible_name=(Art Style) options=[Realistic,Cartoon,Abstract] default=0
	        strinput code_name=(project_name) visible_name=(Project Name) default=(my project)
        
	        hgroup spacing=10 childAlignment=MiddleCenter:
	            button code_name=(reset_btn) visible_name=(Reset)
	            space minWidth=30
	            button code_name=(generate_btn) visible_name=(Generate)
    
	    space minHeight=20 preferredHeight=20 flexibleHeight=2
	*/
	public class Gen3D_InputPanelBuilder_UI : MonoBehaviour
	{
	    public static Gen3D_InputPanelBuilder_UI instance { get; private set; } = null;

	    [SerializeField] RectTransform containerRect;  // Assign your ScrollRect's content

	    // Prefab references - assign in inspector
	    [SerializeField] Gen3D_InputElement_UI _headerPrefab;
	    [SerializeField] Gen3D_InputElement_UI _sliderPrefab;
	    [SerializeField] Gen3D_InputElement_UI _togglePrefab;
	    [SerializeField] Gen3D_InputElement_UI _buttonPrefab;
	    [SerializeField] Gen3D_InputElement_UI _int_horiz_Prefab;
	    [SerializeField] Gen3D_InputElement_UI _int_vertic_Prefab;
	    [SerializeField] Gen3D_InputElement_UI _str_input_Prefab;
	    [SerializeField] Gen3D_InputElement_UI _dropdownPrefab;
	    [SerializeField] Gen3D_InputElement_UI _textPrompt_Prefab;
	    [SerializeField] Gen3D_InputElement_UI _imageInputs_Prefab;//has tabs, one page for single-image, one for multi-image input.
	    [Space(10)]
	    [SerializeField] Sprite _panel_bg_sprite;

	    // for example, all the sliders, all the toggles etc, that exist right now.
	    List<Gen3D_InputElement_UI> _known_inputs = new List<Gen3D_InputElement_UI>();

    

	    public List<Gen3D_InputElement_UI> MakeLayout_from_text(string layout_str){
	        // Clear existing layout
	        foreach (Transform child in containerRect){
	            Destroy(child.gameObject);
	        }
	        _known_inputs.Clear();
	        var rootPortion = ParseLayoutText(layout_str);
	        Spawn_Element(rootPortion, containerRect);
	        return _known_inputs;
	    }

	    TxtLayoutPortion ParseLayoutText(string layout_str){
	        var lines = layout_str.Split('\n');
	        TxtLayoutPortion root = null;
	        var stack = new Stack<TxtLayoutPortion>();

	        // Create a root vgroup to contain everything.
	        // This way, any root - level elements will be properly parented to the root vgroup,
	        // and the stack will never be empty when we try to Peek() it.
	        root = new TxtLayoutPortion { 
	            type = "vgroup", 
	            indentationLevel = -1// This ensures it's always the parent. 
	        };
	        stack.Push(root);

	        foreach (var rawLine in lines){
	            var line = rawLine.TrimEnd();
	            if (string.IsNullOrWhiteSpace(line)) continue;

	            var indentation = line.Length - line.TrimStart().Length;
	            var trimmedLine = line.Trim();

	            var portion = ParseLineToPortion(trimmedLine);
	            portion.indentationLevel = indentation;

	            // Pop stack until we find the parent at the correct indentation level
	            while (stack.Count > 1 && stack.Peek().indentationLevel >= indentation){
	                stack.Pop();
	            }
	            stack.Peek().children.Add(portion);
	            stack.Push(portion);
	        }
    
	        // If root has only one child and it's a vgroup, return that instead
	        if (root.children.Count == 1 && root.children[0].type == "vgroup"){
	            return root.children[0];
	        }
	        return root;
	    }

	    TxtLayoutPortion ParseLineToPortion(string line){
	        var portion = new TxtLayoutPortion();
	        var parts = line.Split(new[] { ' ' }, 2);
	        portion.type = parts[0].ToLower();

	        if (parts.Length == 1){ return portion; }
	        var propText = parts[1];
	        var bracketDepth = 0;
	        var currentProp = "";
	        var currentValue = "";
	        var collectingName = true;

	        foreach (var c in propText + " "){
	            if (c == '('){
	                bracketDepth++;
	                if (bracketDepth == 1) continue;
	            }
	            else if (c == ')'){
	                bracketDepth--;
	                if (bracketDepth == 0){
	                    portion.properties[currentProp] = currentValue;
	                    currentProp = "";
	                    currentValue = "";
	                    collectingName = true;
	                    continue;
	                }
	            }

	            if (bracketDepth == 0 && (c == ' ' || c == ':')){
	                if (!string.IsNullOrEmpty(currentProp)){
	                    portion.properties[currentProp] = currentValue;
	                    currentProp = "";
	                    currentValue = "";
	                    collectingName = true;
	                }
	                continue;
	            }

	            if (bracketDepth == 0 && c == '='){
	                collectingName = false;
	                continue;
	            }
	            if (collectingName)
	                currentProp += c;
	            else
	                currentValue += c;
	        }//end foreach
	        return portion;
	    }


	    void AddBackgroundImage(GameObject go, TxtLayoutPortion portion){
	        if (portion.properties.TryGetValue("bg.color", out var colorStr)){

	            var existingGraphic = go.GetComponent<Graphic>();
	            Image image   = existingGraphic as Image;
	            if(existingGraphic != null){
	                if(image==null){ return; }//maybe text is already attached.
	            }
	            //else no graphic, so add:
	            image = go.AddComponent<Image>();
	            image.sprite = _panel_bg_sprite;
	            image.type = Image.Type.Sliced;
	            var colorParts = colorStr.Split(',');
	            image.color = new Color(
	                float.Parse(colorParts[0]) / 255.0f,
	                float.Parse(colorParts[1]) / 255.0f,
	                float.Parse(colorParts[2]) / 255.0f,
	                float.Parse(colorParts[3]) / 255.0f
	            );
	            if (portion.properties.TryGetValue("bg.ppu", out var ppuStr)){
	                image.pixelsPerUnitMultiplier = float.Parse(ppuStr);
	            }else { 
	                image.pixelsPerUnitMultiplier = 10;
	            }
	        }
	    }

	    void ConfigureRectTransform(RectTransform rect, TxtLayoutPortion portion){
	        if (portion.properties.TryGetValue("rect.anchors", out var anchorsStr)){
	            var parts = anchorsStr.Split(',');
	            rect.anchorMin = new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
	            rect.anchorMax = new Vector2(float.Parse(parts[2]), float.Parse(parts[3]));
	        }else{
	            rect.anchorMin = Vector2.zero;
	            rect.anchorMax = Vector2.one;
	        }

	        if (portion.properties.TryGetValue("rect.offsetMin", out var offsetMinStr)){
	            var parts = offsetMinStr.Split(',');
	            rect.offsetMin = new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
	        }else{
	            rect.offsetMin = new Vector2(0,0);
	        }

	        if (portion.properties.TryGetValue("rect.offsetMax", out var offsetMaxStr)){
	            var parts = offsetMaxStr.Split(',');
	            rect.offsetMax = new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
	        }else{
	            rect.offsetMax= new Vector2(0,0);//notice, not 1,1 because it's an OFFSET.
	        }

	        if (portion.properties.TryGetValue("rect.pivot", out var pivotStr)){
	            var parts = pivotStr.Split(',');
	            rect.pivot = new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
	        }
	        if (portion.properties.TryGetValue("rect.scale", out var scaleStr)){
	            var parts = scaleStr.Split(',');
	            rect.localScale = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
	        }
	    }

	    void Configure_min_amount_toAllowGenerate(Gen3D_InputElement_UI elem, TxtLayoutPortion portion){
	        string min_num_str = "";
	        if (portion.properties.TryGetValue("content_min_num_needed", out min_num_str) ==false){ return; }
	        try { 
	            // Check if it contains operation:number pairs
	            if (min_num_str.Contains(":")){
	                var dict = new Dictionary<string, int>();
	                var pairs = min_num_str.Split(',');
	                foreach (var pair in pairs){
	                    var keyValue = pair.Split(':');
	                    if (keyValue.Length == 2 && int.TryParse(keyValue[1].Trim(), out int value)){
	                        var key = keyValue[0].Trim();
	                        dict[key] = value;
	                    }
	                }
	                elem.Set_min_amount_toAllowGenerate(dict);
	            }
	            // Backward compatibility for single number
	            else if (int.TryParse(min_num_str, out int min_num)){
	                var dict = new Dictionary<string, int>{ 
	                    { "make_meshes_and_tex", min_num } 
	                };
	                elem.Set_min_amount_toAllowGenerate(dict);
	            }
	        }catch(Exception e){
	            #if UNITY_EDITOR
	            Debug.LogError(e.Message);
	            #endif
	        }
	    }



	    void ConfigureLayoutGroup(HorizontalOrVerticalLayoutGroup group, TxtLayoutPortion portion) {
	        // Handle individual padding values with shortened names
	        if (portion.properties.TryGetValue("padl", out var paddingLeft))
	            group.padding.left = int.Parse(paddingLeft);
	        if (portion.properties.TryGetValue("padr", out var paddingRight))
	            group.padding.right = int.Parse(paddingRight);
	        if (portion.properties.TryGetValue("padt", out var paddingTop))
	            group.padding.top = int.Parse(paddingTop);
	        if (portion.properties.TryGetValue("padb", out var paddingBottom))
	            group.padding.bottom = int.Parse(paddingBottom);

	        // Keep the uniform padding option for convenience
	        if (portion.properties.TryGetValue("padding", out var padding)){
	            var uniformPadding = int.Parse(padding);
	            group.padding = new RectOffset(uniformPadding, uniformPadding, uniformPadding, uniformPadding);
	        }
	        // Existing layout group configuration
	        if (portion.properties.TryGetValue("childForceExpandWidth", out var expandWidth))
	            group.childForceExpandWidth = bool.Parse(expandWidth);
	        else{
	            group.childForceExpandWidth = false;
	        }
	        if (portion.properties.TryGetValue("childForceExpandHeight", out var expandHeight))
	            group.childForceExpandHeight = bool.Parse(expandHeight);
	        else{
	            group.childForceExpandHeight = false;
	        }
	        if (portion.properties.TryGetValue("childAlignment", out var alignment))
	            group.childAlignment = (TextAnchor)System.Enum.Parse(typeof(TextAnchor), alignment);
	    }


	    void AddLayoutElement(GameObject go, TxtLayoutPortion portion){
	        //add layout element if doesn't yet exist on the game object
	        var layoutElement = go.GetComponent<LayoutElement>();
	            layoutElement = layoutElement==null ? go.AddComponent<LayoutElement>() : layoutElement;

	        if (portion.properties.TryGetValue("minWidth", out var minWidth))
	            layoutElement.minWidth = float.Parse(minWidth);

	        if (portion.properties.TryGetValue("minHeight", out var minHeight))
	            layoutElement.minHeight = float.Parse(minHeight);

	        if (portion.properties.TryGetValue("preferredWidth", out var preferredWidth))
	            layoutElement.preferredWidth = float.Parse(preferredWidth);
        
	        if (portion.properties.TryGetValue("preferredHeight", out var preferredHeight))
	            layoutElement.preferredHeight = float.Parse(preferredHeight);

	        if (portion.properties.TryGetValue("flexibleWidth", out var flexWidth))
	            layoutElement.flexibleWidth = float.Parse(flexWidth);

	        if (portion.properties.TryGetValue("flexibleHeight", out var flexHeight))
	            layoutElement.flexibleHeight = float.Parse(flexHeight);

	        if (portion.properties.TryGetValue("ignoreLayout", out var ignoreLayout))
	            layoutElement.ignoreLayout = bool.Parse(ignoreLayout);
	    }


	    bool addLayoutElement_if_keywords(GameObject go, TxtLayoutPortion elem){
	        if (elem.properties.Keys.Any(key =>
	            key == "minWidth" || key == "preferredWidth" || key == "flexibleWidth" ||
	            key == "minHeight" || key == "preferredHeight" || key == "flexibleHeight" ||
	            key == "ignoreLayout")){
	            AddLayoutElement(go, elem);
	            return true;
	        }
	        return false;
	    }

	    string ParseBracketedValue(string value){// Remove outer brackets if present:
	        if (value.StartsWith("(") && value.EndsWith(")")){
	            return value.Substring(1, value.Length - 2);
	        }
	        return value;
	    }

	    List<string> ParseList(string value){// Remove outer brackets and split by comma:
	        if (value.StartsWith("[") && value.EndsWith("]")){
	            var listContent = value.Substring(1, value.Length - 2);
	            return listContent.Split(',')
	                .Select(s => s.Trim())
	                .Where(s => !string.IsNullOrEmpty(s))
	                .ToList();
	        }
	        return new List<string>();
	    }

	    void Spawn_Element(TxtLayoutPortion portion, RectTransform parent){
	        GameObject go = null;
	        RectTransform rectTransform = null;

	        switch (portion.type){
	            case "vgroup":
	                go = new GameObject("VerticalGroup");
	                rectTransform = go.AddComponent<RectTransform>();
	                var vlg = go.AddComponent<VerticalLayoutGroup>();
	                if (portion.properties.TryGetValue("padding", out var vpadding))
	                    vlg.padding = new RectOffset(int.Parse(vpadding), int.Parse(vpadding), int.Parse(vpadding), int.Parse(vpadding));
	                if (portion.properties.TryGetValue("spacing", out var vspacing))
	                    vlg.spacing = float.Parse(vspacing);
	                ConfigureLayoutGroup(vlg, portion);
	                break;

	            case "hgroup":
	                go = new GameObject("HorizontalGroup");
	                rectTransform = go.AddComponent<RectTransform>();
	                var hlg = go.AddComponent<HorizontalLayoutGroup>();
	                if (portion.properties.TryGetValue("padding", out var hpadding))
	                    hlg.padding = new RectOffset(int.Parse(hpadding), int.Parse(hpadding), int.Parse(hpadding), int.Parse(hpadding));
	                if (portion.properties.TryGetValue("spacing", out var hspacing))
	                    hlg.spacing = float.Parse(hspacing);
	                ConfigureLayoutGroup(hlg, portion);
	                break;

	            case "space"://space always has a layoutElement
	                go = new GameObject("Space");
	                rectTransform = go.AddComponent<RectTransform>();
	                AddLayoutElement(go, portion);
	                break;
               
	            case "go": //a simplest entity which only gets rect transform
	                go = new GameObject("GameObject");
	                rectTransform = go.AddComponent<RectTransform>();
	                break;

	            case "header":
	                go = Instantiate(_headerPrefab.gameObject);
	                var headerUI = go.GetComponent<Gen3D_InputElement_UI>();
	                var headerText = headerUI.GetComponentInChildren<TextMeshProUGUI>();
    
	                headerUI.Init(ParseBracketedValue(portion.properties.GetValueOrDefault("code_name", "")),
	                              ParseBracketedValue(portion.properties.GetValueOrDefault("visible_name", "Unnamed Header")),
	                              Gen3D_InputElement_Kind.Header);

	                if (portion.properties.TryGetValue("color", out var colorStr)){
	                    var parts = colorStr.Split(',');
	                    if (parts.Length >= 3)
	                        headerText.color = new Color(float.Parse(parts[0]) / 255f, 
	                                                  float.Parse(parts[1]) / 255f,
	                                                  float.Parse(parts[2]) / 255f,
	                                                  parts.Length > 3 ? float.Parse(parts[3]) : 1f);
	                }

	                if (portion.properties.TryGetValue("align", out var alignStr)){
	                    headerText.alignment = alignStr.ToLower() switch {
	                        "center" => TextAlignmentOptions.Center,
	                        "right" => TextAlignmentOptions.Right,
	                        _ => TextAlignmentOptions.Left
	                    };
	                }
	                break;

	            case "slider":
	                go = Instantiate(_sliderPrefab.gameObject);
	                var sliderUI = go.GetComponent<Gen3D_InputElement_UI>();
	                sliderUI.Init(
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("code_name", "unnamed_slider")),
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("visible_name", "Unnamed Slider")),
	                    Gen3D_InputElement_Kind.CircleSlider
	                );
	                // Initialize int input values
	                int.TryParse(portion.properties.GetValueOrDefault("show_n_decimals", "0"), out int show_n_decimals);

	                // Initialize slider values
	                if (float.TryParse(portion.properties.GetValueOrDefault("min", "0"), out float minVal) &&
	                    float.TryParse(portion.properties.GetValueOrDefault("max", "1"), out float maxVal) &&
	                    float.TryParse(portion.properties.GetValueOrDefault("default", "0.5"), out float defaultVal)){
	                    sliderUI.SetMinMax_Float_noNotify(minVal, maxVal, defaultVal, show_n_decimals);
	                }
	                break;

	            case "toggle":
	                go = Instantiate(_togglePrefab.gameObject);
	                var toggleUI = go.GetComponent<Gen3D_InputElement_UI>();
	                toggleUI.Init(
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("code_name", "unnamed_toggle")),
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("visible_name", "Unnamed Toggle")),
	                    Gen3D_InputElement_Kind.Toggle
	                );
	                // Initialize toggle value
	                if (bool.TryParse(portion.properties.GetValueOrDefault("default", "false"), out bool toggleDefault)){
	                    toggleUI.SetToggleValue_noNotify(toggleDefault);
	                }
	                break;

	            case "button":
	                go = Instantiate(_buttonPrefab.gameObject);
	                var buttonUI = go.GetComponent<Gen3D_InputElement_UI>();
	                buttonUI.Init(
	                    portion.properties.GetValueOrDefault("code_name", "unnamed_button"),
	                    portion.properties.GetValueOrDefault("visible_name", "Unnamed Button"),
	                    Gen3D_InputElement_Kind.Button
	                );
	                break;

	            case "int_horizontal":{
	                go = Instantiate(_int_horiz_Prefab.gameObject);
	                var intUI = go.GetComponent<Gen3D_InputElement_UI>();
	                intUI.Init(
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("code_name", "unnamed_int")),
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("visible_name", "Unnamed Int")),
	                    Gen3D_InputElement_Kind.Int_Horiz
	                );
	                //Initialize int input values
	                if (int.TryParse(portion.properties.GetValueOrDefault("min", "0"), out int intMin) &&
	                    int.TryParse(portion.properties.GetValueOrDefault("max", "100"), out int intMax) &&
	                    int.TryParse(portion.properties.GetValueOrDefault("default", "0"), out int intDefault)){
	                    intUI.SetMinMax_Int_noNotify(intMin, intMax, intDefault);
	                }
	                if(bool.TryParse(portion.properties.GetValueOrDefault("as_seed", "false"), out bool as_seed)){
	                    intUI.Set_IntInput_AsSeed(as_seed);
	                }
	            }break;
	            case "int_vertical":{
	                go = Instantiate(_int_vertic_Prefab.gameObject);
	                var intUI = go.GetComponent<Gen3D_InputElement_UI>();
	                intUI.Init(
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("code_name", "unnamed_int")),
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("visible_name", "Unnamed Int")),
	                    Gen3D_InputElement_Kind.Int_Vertical
	                );
	                if (int.TryParse(portion.properties.GetValueOrDefault("min", "0"), out int intMin) &&
	                    int.TryParse(portion.properties.GetValueOrDefault("max", "100"), out int intMax) &&
	                    int.TryParse(portion.properties.GetValueOrDefault("default", "0"), out int intDefault)){
	                    intUI.SetMinMax_Int_noNotify(intMin, intMax, intDefault);
	                }
	            }break;
	            case "str_input":{
	                go = Instantiate(_str_input_Prefab.gameObject);
	                var str_input = go.GetComponent<Gen3D_InputElement_UI>();
	                str_input.Init(
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("code_name", "unnamed_str")),
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("visible_name", "Unnamed String")),
	                    Gen3D_InputElement_Kind.StrInput
	                );
	                var defaultText = ParseBracketedValue(portion.properties.GetValueOrDefault("default", ""));
	                str_input.SetText_noNotify(defaultText);
	                Configure_min_amount_toAllowGenerate(str_input, portion);
	            }
	            break;
	            case "text_prompt":{
	                go = Instantiate(_textPrompt_Prefab.gameObject);
	                var prompt = go.GetComponent<Gen3D_InputElement_UI>();
	                prompt.Init(
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("code_name", "unnamed_prompt")),
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("visible_name", "Unnamed Prompt")),
	                    Gen3D_InputElement_Kind.TextPrompt
	                );
	                string defaultText = ParseBracketedValue(portion.properties.GetValueOrDefault("default", ""));
	                bool.TryParse( portion.properties.GetValueOrDefault("is_positive", "true"), out bool is_positive );
	                prompt.SetText_noNotify(defaultText);
	                prompt.SetTextPropompt_isPositive(is_positive);
	                Configure_min_amount_toAllowGenerate(prompt, portion);
	            }break;
	            case "single_multi_img_input":{
	                go = Instantiate(_imageInputs_Prefab.gameObject);
	                var imgInputs = go.GetComponent<Gen3D_InputElement_UI>();
	                imgInputs.Init(
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("code_name", "unnamed_str")),
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("visible_name", "Unnamed Images")),
	                    Gen3D_InputElement_Kind.SingleMultiImageInputs
	                );
	                Configure_min_amount_toAllowGenerate(imgInputs, portion);
	            }break;
	            case "dropdown":
	                go = Instantiate(_dropdownPrefab.gameObject);
	                var dropdownUI = go.GetComponent<Gen3D_InputElement_UI>();
	                dropdownUI.Init(
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("code_name", "unnamed_dropdown")),
	                    ParseBracketedValue(portion.properties.GetValueOrDefault("visible_name", "Unnamed Dropdown")),
	                    Gen3D_InputElement_Kind.Dropdown
	                );
	                // Initialize dropdown values
	                var options = ParseList(portion.properties.GetValueOrDefault("options", "[]"));
	                if (int.TryParse(portion.properties.GetValueOrDefault("default", "0"), out int dropdownDefault)){
	                    dropdownUI.SetDropDownChoices_noNotify(options, dropdownDefault);
	                }
	                break;
	        }//end switch

	        if(go != null){
	            go.transform.SetParent(parent, false);
	            rectTransform = go.GetComponent<RectTransform>();
            
	            try{
	                AddBackgroundImage(go, portion);
	            }catch(Exception ex){}//some elements (text) already contain graphic element and will refuse an extra image.

	            ConfigureRectTransform(rectTransform, portion);
	            addLayoutElement_if_keywords(go, portion);

	            var elem = go.GetComponent<Gen3D_InputElement_UI>();
	            if (elem != null){ _known_inputs.Add(elem); }
            
	            // Process children
	            foreach (var child in portion.children){
	                Spawn_Element(child, rectTransform);
	            }
	        }
	    }//end()

	    public class TxtLayoutPortion{
	        public string type;  // "vgroup", "hgroup", "slider", "toggle", "button", "space"
	        public Dictionary<string, string> properties;
	        public List<TxtLayoutPortion> children;
	        public int indentationLevel;
	        public TxtLayoutPortion(){
	            properties = new Dictionary<string, string>();
	            children = new List<TxtLayoutPortion>();
	        }
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	    }
	}


}//end namespace
