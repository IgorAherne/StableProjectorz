using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace spz {

	// Contains positive prompt, negative prompt, etc.
	// Also, capable of moving away the rectransform and shrinking the size of this panel.
	// This is useful when user wants to "free-up" the UI space.
	public class SD_InputPanel_UI : MonoBehaviour{
	    public static SD_InputPanel_UI instance { get; private set; } = null;

	              public ScrollRect inputColumn_scrollRect => _inputColumn_scrollRect;
	    [SerializeField] ScrollRect _inputColumn_scrollRect;

	              public SD_Neural_Models models  => SD_Neural_Models.instance;
	              public SD_VAE sd_vae            => SD_VAE.instance;
	              public SD_Upscalers sd_upscaler => SD_Upscalers.instance;
	              public SD_Samplers samplers     => SD_Samplers.instance;
	              public SD_Scheduler scheduler   => SD_Scheduler.instance;

	              public CircleSlider_Snapping_UI sampleSteps_slider => _sampleSteps_slider;
	    [SerializeField] CircleSlider_Snapping_UI _sampleSteps_slider;
	              public CircleSlider_Snapping_UI CFG_scale_slider => _CFG_scale_slider;
	    [SerializeField] CircleSlider_Snapping_UI _CFG_scale_slider;
	              public IntegerInputField seed_intField => _seed_intField;
	    [SerializeField] IntegerInputField _seed_intField;
	              public Animation seed_intFieldAnim => _seed_intFieldAnim;
	    [SerializeField] Animation _seed_intFieldAnim;

	    [SerializeField] IntegerInputField _width_input;
	    [SerializeField] IntegerInputField _height_input;
	    [SerializeField] IntegerInputField _batch_count_input;
	    [SerializeField] IntegerInputField _batch_size_input;
    
	    [Space(10)]
	    [SerializeField] Button _resolutionPreset_512;
	    [SerializeField] Button _resolutionPreset_768;
	    [SerializeField] Button _resolutionPreset_1024;
	    [SerializeField] Button _resolutionPreset_1536;
	    [SerializeField] Button _resolutionPreset_2048;

	    public int width  => _width_input.recentVal;
	    public int height => _height_input.recentVal;
	    public int batch_count => _batch_count_input.recentVal;
	    public int batch_size => _batch_size_input.recentVal;


	    [Space(10)]
	    //for the entire panel. We can move it, to "hide" this panel, moving it out of the way.
	    [SerializeField] RectTransform _movableRectTransform;
	    [SerializeField] LayoutElement _layoutElem;
	    [SerializeField] Vector2 _minAndPreferredWidth_whenHidden;

	    int _zoomRes_numHints_shown = 0;


	    public Vector2 widthHeight(){
	        #if UNITY_EDITOR
	        if(UnityEditor.EditorApplication.isPlaying==false){ return Vector2.one*512; }
	        #endif
	        return new Vector2(width, height); 
	    }

	    public void PasteSeedValue(int seed){
	        _seed_intField.SetValue( seed.ToString() );
	        _seed_intFieldAnim.Play();
	        _inputColumn_scrollRect.GetComponent<ScrollRect_AutoScroll>().ScrollToEnd(0.25f, true);
	    }


	    //helpful if we resized the entire window.
	    void Stretch(){
	        Vector2 parentSize = (_movableRectTransform.parent as RectTransform).rect.size;
	        _movableRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentSize.x);
	    }

    
	    void OnResolutionPresetButton(int res){
	        _width_input.SetValue(res.ToString());
	        _height_input.SetValue(res.ToString());
	        if(res > 1024){
	            string msg = "Careful!  SD 1.5 is made for generating 512,  SDXL for 1024.  Might be slow + give weird results."
	                        + "\nEven if 512, it's only for one of the sides!  So the total texture will end up at least 2k anyway.";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 11, false);
	        }
	        else if(res >= 768 && _zoomRes_numHints_shown<3){
	            _zoomRes_numHints_shown++;
	            string msg = "Always zoom close to the 3D object,  to capture more pixels of your projections.\n" +
	                         "Maybe increase the total resolution of the scene  (use -+ next to the 'Save 2K')";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 7, false);
	        }
	    }
    


	    public void Save( SD_GenSettingsInput_UI fill_this ){
	        models.Save(fill_this);
	        samplers.Save(fill_this);
	        sd_upscaler.Save(fill_this);
	        fill_this.sampleSteps = Mathf.RoundToInt(_sampleSteps_slider.value);
	        fill_this.cfg_scale = _CFG_scale_slider.value;
	        fill_this.seed = _seed_intField.recentVal;

	        fill_this.width = width;
	        fill_this.height = height;
	        fill_this.batch_count = batch_count;
	        fill_this.batch_size = batch_size;
	    }

	    public void Load( StableProjectorz_SL spz ){
	        models.Load(spz.sd_genSettingsInput);
	        samplers.Load(spz.sd_genSettingsInput);
	        sd_upscaler.Load(spz.sd_genSettingsInput);
	        _sampleSteps_slider.SetSliderValue(spz.sd_genSettingsInput.sampleSteps, true);
	        _CFG_scale_slider.SetSliderValue(spz.sd_genSettingsInput.cfg_scale, true);
        
	        _seed_intField.SetValue( spz.sd_genSettingsInput.seed.ToString() );

	        _width_input.SetValue( spz.sd_genSettingsInput.width.ToString() );
	        _height_input.SetValue( spz.sd_genSettingsInput.height.ToString() );

	        _batch_count_input.SetValue( spz.sd_genSettingsInput.batch_count.ToString() );
	        _batch_size_input.SetValue( spz.sd_genSettingsInput.batch_size.ToString() );
	    }


	    void Update(){
	        var movableParent = _movableRectTransform.parent as RectTransform;
	        float widthDifference = _movableRectTransform.rect.width - movableParent.rect.width;
	        if(Mathf.Abs(widthDifference) < 0.01f){ return; }//to avoid frequent layout recalculations

	        Stretch();
	    }
    
	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	    }

	    void Start(){
	        _resolutionPreset_512.onClick.AddListener( ()=>OnResolutionPresetButton(512) );
	        _resolutionPreset_768.onClick.AddListener( ()=>OnResolutionPresetButton(768) );
	        _resolutionPreset_1024.onClick.AddListener( ()=>OnResolutionPresetButton(1024) );
	        _resolutionPreset_1536.onClick.AddListener( ()=>OnResolutionPresetButton(1536) );
	        _resolutionPreset_2048.onClick.AddListener( ()=>OnResolutionPresetButton(2048) );
	    }
	}
}//end namespace
