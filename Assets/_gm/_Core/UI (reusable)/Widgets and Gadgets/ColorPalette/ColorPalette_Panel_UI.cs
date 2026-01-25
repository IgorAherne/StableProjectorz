using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Rectangle with ui controls for selecting colors. 
	// Can infer color from position of elements and set their pos based on color.
	public class ColorPalette_Panel_UI : MonoBehaviour{

	    [SerializeField] RectTransform _gradientArea_rt; //large area, which shows a specific hue + allows to tint
	    [SerializeField] RectTransform _hueArea_rt;//area, which allows to select a hue from a rainbow.
	    [SerializeField] RawImage _gradientArea_img;
	    [SerializeField] RawImage _hueArea_img;

	    [Space(10)]
	    [SerializeField] RectTransform _gradientArea_dot; //little dot which can be dragged around
	    [SerializeField] RectTransform _hueArea_dot;

	    [Space(10)]
	    [SerializeField] Image finalColorImage;
	    [SerializeField] TextMeshProUGUI _hexColorText;
	    [SerializeField] TMP_InputField _hexColor_inputText;
	    [Space(10)]
	    [SerializeField] Image _icon; //will add the color to "recent colors" queue of UI elements.
	    [SerializeField] Button _finalColor_button;

	    bool _init = false;
	    public bool _isShowing => gameObject.activeSelf;

	    int _gradTexWidth;
	    int _gradTexHeight;
	    int _hueTexWidth;
	    int _hueTexHeight;

	    bool _currPress = false;
	    Vector2 _currPresScreenPos;

	    enum AreaType{  None,  Gradient,  Hue, }
	    AreaType _clickedAreaType = AreaType.None;


	    Vector3 _gradientArea_min;//world coordinate of the buttom left corner of the gradient-area region
	    Vector3 _gradientArea_max;
	    Vector3 _hueArea_min;
	    Vector3 _hueArea_max;

	    Texture2D _gradientArea_texture;
	    Texture2D _hueArea_texture;

	    Action<Color> _OnColorChanged = null;

	    public Color Get_CurrentColor(){
	        Vector2 normalizedPos = Rect.PointToNormalized(_gradientArea_rt.rect, _gradientArea_dot.anchoredPosition);
	        return Color.HSVToRGB(Get_CurrentHue(), normalizedPos.x, normalizedPos.y);
	    }

	    float Get_CurrentHue(){
	        return 1 - Rect.PointToNormalized(_hueArea_rt.rect, _hueArea_dot.anchoredPosition).y;
	    }

	    public void Set_CurrentColor(Color col) {
	        col = new Color(Mathf.Clamp01(col.r), Mathf.Clamp01(col.g), Mathf.Clamp01(col.b), Mathf.Clamp01(col.a));
	        finalColorImage.color = col;
	        string htmlColor = ColorUtility.ToHtmlStringRGB(col).ToUpper();
	        _hexColorText.text = "#" + htmlColor;
	        _hexColor_inputText.text = htmlColor;

	        Color.RGBToHSV(col, out float h, out float s, out float v);

	        // Set hue dot position (centered horizontally)
	        _hueArea_dot.anchoredPosition = new Vector2(
	            _hueArea_rt.rect.center.x,
	            _hueArea_rt.rect.height * (1 - h) + _hueArea_rt.rect.yMin
	        );

	        // Set gradient dot position
	        _gradientArea_dot.anchoredPosition = new Vector2(
	            _gradientArea_rt.rect.width * s + _gradientArea_rt.rect.xMin,
	            _gradientArea_rt.rect.height * v + _gradientArea_rt.rect.yMin
	        );

	        if (_init) ChangeColor_GradientArea();
	    }


	    void ChangeColor_GradientArea(){
	        float hueValue = Get_CurrentHue();//caching it for faster speed.

	        Color[] row = new Color[_gradTexWidth];
	        for(int i=0; i<_gradTexHeight; ++i){
	            for(int j=0; j< _gradTexWidth; ++j){ 
	                Color col =  Color.HSVToRGB(hueValue, j/(float)_gradTexWidth, i/(float)_gradTexHeight);
	                row[j] =  col; 
	            }
	            _gradientArea_texture.SetPixels( 0, i, _gradTexWidth, 1, row );
	        }
	        _gradientArea_texture.Apply();
	    }
        

	    public void Show( Color startingColor, Action<Color> onColorUpdated ){
	        if(_isShowing){ return; }
	        gameObject.SetActive(true);
	        _OnColorChanged = onColorUpdated;

	        Set_CurrentColor(startingColor);
	    }


	    public void Hide(){
	        if(!_isShowing){ return; }
	        _clickedAreaType = AreaType.None;
	        _currPress = false;
	        _currPresScreenPos = Vector2.zero;
	        gameObject.SetActive(false);
	    }



	    void Start(){
	        Init_minMaxCorners_ofAreas();

	        _gradTexWidth  = (int)(_gradientArea_max.x-_gradientArea_min.x);
	        _gradTexHeight = (int)(_gradientArea_max.y-_gradientArea_min.y);
	        _gradientArea_texture = new Texture2D(_gradTexWidth, _gradTexHeight);

	        _hueTexWidth =  (int)(_hueArea_max.x - _hueArea_min.x);
	        _hueTexHeight = (int)(_hueArea_max.y - _hueArea_min.y);
	        _hueArea_texture = new Texture2D(_hueTexWidth, _hueTexHeight);

	        _gradientArea_img.texture = _gradientArea_texture;
	        _hueArea_img.texture = _hueArea_texture;

	        InitColor_on_HSV_slider();
	        ChangeColor_GradientArea();
	        _icon.color = Get_CurrentColor();
	        _init = true;

	        _hexColor_inputText.onEndEdit.AddListener(OnHexColorInput_EndedEdit);
	    }


	    void OnDestroy(){
	        DestroyImmediate(_gradientArea_texture);
	        DestroyImmediate(_hueArea_texture);
	    }


	    void OnHexColorInput_EndedEdit(string hexString){
	        Color newColor;
	        if (hexString.Contains("#")==false){ hexString = "#"+ hexString; }
	        if(ColorUtility.TryParseHtmlString(hexString, out newColor)){
	            Set_CurrentColor(newColor);
	            _OnColorChanged(Get_CurrentColor());
	        }else {
	            Debug.Log("Wrong Hex string");
	        }
	    }

    
	    void Init_minMaxCorners_ofAreas(){
	        var worldCorners = new Vector3[4];
	        _gradientArea_rt.GetWorldCorners(worldCorners);
	        _gradientArea_min = worldCorners[0];
	        _gradientArea_max = worldCorners[2];
	        _hueArea_rt.GetWorldCorners(worldCorners);
	        _hueArea_min = worldCorners[0];
	        _hueArea_max = worldCorners[2];
	    }


	    void InitColor_on_HSV_slider(){
	        Color[] row = new Color[_hueTexWidth];
	        for(int i=0; i<_hueTexHeight; ++i){
	            Color col = Color.HSVToRGB( 1.0f-i/(float)_hueTexHeight, 1, 1);
	            for(int j=0; j<_hueTexWidth;++j){ row[j] = col; }
	            _hueArea_texture.SetPixels( 0, i, _hueTexWidth, 1, row );
	        }
	        _hueArea_texture.Apply();
	    }


	    void Update(){
	        if(!_isShowing){return; }

	        DetectPress();
	        if(!_currPress){ return; }

	        if (_clickedAreaType == AreaType.None){
	            FigureOut_AreaType();//see if we entered some area by dragging from outside.
	            if(_clickedAreaType == AreaType.None){ return; }//clicked elsewhere.
	        }
	        if(_clickedAreaType == AreaType.Gradient){ 
	            Area_UpdatePos(_gradientArea_rt, _gradientArea_min, _gradientArea_max, _gradientArea_dot, isHue:false);
	        }
	        if(_clickedAreaType == AreaType.Hue){ 
	            Area_UpdatePos(_hueArea_rt, _hueArea_min, _hueArea_max, _hueArea_dot, isHue:true);
	        }


	        Color c = Get_CurrentColor();
	        finalColorImage.color = c;
	        string hexCol = ColorUtility.ToHtmlStringRGB(c).ToUpper();
	        _hexColorText.text = "#" + hexCol;
	        _hexColor_inputText.text = "#" + hexCol;

	        _OnColorChanged( Get_CurrentColor() );
	    }


	    void DetectPress(){
	        _currPress = KeyMousePenInput.isLMBpressed();
	        _currPresScreenPos = KeyMousePenInput.cursorScreenPos();
	        if(!_currPress){ _clickedAreaType = AreaType.None; }
	    }

	    void FigureOut_AreaType(){ 
	        if(RectTransformUtility.RectangleContainsScreenPoint( _gradientArea_rt, _currPresScreenPos, null )) {
	            _clickedAreaType = AreaType.Gradient;
	        }else if(RectTransformUtility.RectangleContainsScreenPoint( _hueArea_rt, _currPresScreenPos, null )){
	            _clickedAreaType = AreaType.Hue;
	        }else { 
	            _clickedAreaType = AreaType.None;
	        }
	    }

	    void Area_UpdatePos(RectTransform area, Vector3 areaMin, Vector3 areaMax, RectTransform pointer, bool isHue) {
	        Vector2 localPoint;
	        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, _currPresScreenPos, null, out localPoint);
	        Vector2 normalizedPos   = Rect.PointToNormalized(area.rect, localPoint);
	                normalizedPos.x = isHue? 0.5f: normalizedPos.x;// Center horizontally for hue
	        pointer.anchoredPosition = Vector2.Scale(area.rect.size, normalizedPos) + area.rect.min;
	        if(isHue){ ChangeColor_GradientArea(); }
	    }

	}//end class
}//end namespace
