using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


namespace spz {

	// allows to select color for the Bottom and the Top of the skybox, via UI buttons.
	// Also, has a few buttons as pressets, to quickly assign uniform color to both the Bottom and Top.
	public class SkyboxColorButtons_UI_MGR : MonoBehaviour{
	    public static SkyboxColorButtons_UI_MGR instance { get; private set; } = null;

	    [SerializeField] Button _preset_clear_button;//for choosing clear background color (for txt2Img)
	    [SerializeField] Image _preset_clear_image;
	    [Space(10)]
	    [SerializeField] Button _preset_white_button;
	    [SerializeField] Image _preset_white_image;
	    [Space(10)]
	    [SerializeField] Button _preset_gray_button;
	    [SerializeField] Image _preset_gray_image;
	    [Space(10)]
	    [SerializeField] Button _preset_black_button;
	    [SerializeField] Image _preset_blackimage;
    
	    //for setting the actual current colors of the background
	    [Space(10)]
	    [SerializeField] Button _bot_button;
	    [SerializeField] Image _bot_image;
	    [Space(10)]
	    [SerializeField] Button _top_button;
	    [SerializeField] Image _top_image;

	    [Space(10)]
	    [SerializeField] Button _copy_to_top_button;
    

	    void OnPresetButton(Color col){//presets always set the color to both Top and Bottom:
	        OnColorChanged(isTop: false, col);
	        OnColorChanged(isTop: true, col);
	    }

	    void OnButton_CopyToTop(){
	        OnColorChanged(isTop:true, _bot_image.color);
	    }


	    void OnColorChanged(bool isTop, Color newColor){
	        if(SkyboxBackground_MGR.instance == null){ return; }//scenes are probably still loading

	        SkyboxBackground_MGR.instance.SetTopOrBottomColor(isTop, newColor);
	        Image img = isTop ? _top_image : _bot_image;
	        if(newColor == Color.clear){
	            img.color  = _preset_clear_image.color;
	            img.sprite = _preset_clear_image.sprite;
	        }else{
	            img.color = newColor;
	            img.sprite = null;
	            //not clear color. Ensure the other part isn't clear any more either:
	            if(_top_image.sprite == _preset_clear_image.sprite){
	                _top_image.color = Color.black;
	                _top_image.sprite = null;
	                SkyboxBackground_MGR.instance.SetTopOrBottomColor(isTop:true, _top_image.color);
	            } 
	            if(_bot_image.sprite == _preset_clear_image.sprite){
	                _bot_image.color = Color.black;
	                _bot_image.sprite = null;
	                SkyboxBackground_MGR.instance.SetTopOrBottomColor(isTop:false, _bot_image.color);
	            }
	        }//end if newColor not clear
	    }


	    void OnButton_BottomOrTop(bool isTop){
	        Button button = isTop ? _top_button : _bot_button;
	        Image image   = isTop ? _top_image : _bot_image;
	        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
	        MouseWorkbench_Zone.instance.ShowAtScreenCoord(screenPoint, image.color, 
	                                                    c=>OnColorChanged(isTop, c), 
	                                                    MouseWorkbench_Zone.ShowPreference.RightOfCursor );
	    }


	    public void Save(StableProjectorz_SL spz){
	        spz.skyboxColorButtons = new SkyboxColorButtons_UI_SL();
	        spz.skyboxColorButtons.color_bot  =  _bot_image.sprite==_preset_clear_image.sprite ?  Color.clear : _bot_image.color;
	        spz.skyboxColorButtons.color_top  =  _top_image.sprite==_preset_clear_image.sprite ?  Color.clear : _top_image.color;
	    }

	    public void Load(StableProjectorz_SL spz){
	        OnColorChanged(isTop:false, spz.skyboxColorButtons.color_bot);
	        OnColorChanged(isTop:true,  spz.skyboxColorButtons.color_top);
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;
	        _preset_clear_button.onClick.AddListener(()=> OnPresetButton(Color.clear)); //enables txt2image
	        _preset_white_button.onClick.AddListener(()=>OnPresetButton(Color.white));
	        _preset_gray_button.onClick.AddListener( ()=>OnPresetButton(Color.gray) );
	        _preset_black_button.onClick.AddListener(()=>OnPresetButton(Color.black));

	        _copy_to_top_button.onClick.AddListener(OnButton_CopyToTop);

	        _bot_button.onClick.AddListener( ()=>OnButton_BottomOrTop(isTop:false) );
	        _top_button.onClick.AddListener( ()=>OnButton_BottomOrTop(isTop:true) );
	    }

	    void Start(){
	        // begin with Text-to-Image, by using clear color for the skybox.
	        // (and we have no backgrounds icon initially)
	        OnPresetButton(Color.clear);
	    }
	}
}//end namespace
