using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System;
using SimpleFileBrowser; // CHANGED: Replaced Crosstales.FB

namespace spz {

	public class Trellis_ImageSlot : MonoBehaviour
	{
	    [SerializeField] RawImage_with_aspect _image;
	    [SerializeField] MouseClickSensor_UI _imageButton;
	    [SerializeField] Button _closeButton;
	    [SerializeField] GameObject _cover_go; //shows a '+' icon and a text 'click to import'.
	    public bool isMultiImage => _isMultiImage;
	    [SerializeField] bool _isMultiImage;
	    [SerializeField] bool _showCloseButton;
	    [Space(10)]
	    [SerializeField] Button _save_image_button;
	    [SerializeField] Button _mirrorButton;
	    [SerializeField] bool _showMirrorButton;


	    public static Action<Trellis_ImageSlot,Texture2D> _Act_OnImageFile;
	    public static Action<Trellis_ImageSlot> _Act_onCloseButton;
	    public static Action<Trellis_ImageSlot> _Act_onMirrorButton;

	    public bool has_image()
	        => _image.visibleTexture_ref != null;

	    public Texture visibleTexture_ref 
	        => _image.visibleTexture_ref;
    

	    public string image_as_base64(){
	        RenderTexture as_rt = _image.visibleTexture_ref as RenderTexture;
	        Texture2D as_2D    = _image.visibleTexture_ref as Texture2D;
	        if(as_rt != null){
	            return TextureTools_SPZ.TextureToBase64(as_rt, forceAlpha1_ifSingleChannel:true);
	        }
	        if(as_2D != null){
	            return TextureTools_SPZ.TextureToBase64(as_2D);
	        }
	        return "";
	    }


	    public void SwapWithNewImage(Texture2D tex_takeOwnership){
	        // MODIF check if we may pass ownership, or if GenData was made for this imported image
	        _image.gameObject.SetActive(true);
	        _cover_go.SetActive(false);
	        _image.RemoveLatestTexture_ifExists();
	        _image.ShowTexture_takeOwnership( tex_takeOwnership, 0, isGenerated:false, CameraTexType.Nothing);
	    }

	    public void DisposeTheImage(){
	        _image.RemoveLatestTexture_ifExists();
	        _image.gameObject.SetActive(false);
	        _cover_go.SetActive(true);
	    }

	    public void DestroySelf(){
	        DisposeTheImage();
	        Destroy(this.gameObject);
	    }


	    void OnImageButton(int buttonIx){
	        Images_ImportHelper.instance.ImportCustomImageButton( GenerationData_Kind.SD_Backgrounds, 
	                                                              allow_multipleFiles:_isMultiImage, 
	                                                              OnImportedImage, OnImportedFail );
	    }

	    void OnImportedImage( GenerationData_Kind kind,  Dictionary<Texture2D,UDIM_Sector> images ){
	        //don't show the texture yet, let the callback reciever (our owner) decide where to put it.
	        foreach (var kvp in images){
	            _Act_OnImageFile?.Invoke(this,kvp.Key);
	        }
	    }

	    void OnImportedFail( GenerationData_Kind kind, string msg ){
	    }


	    void OnMirrorButton(){
	        _Act_onMirrorButton?.Invoke(this);
	    }

	    void OnSaveImageButton(){
	        // CHANGED: SimpleFileBrowser implementation for saving.
        
	        FileBrowser.SetFilters(true, new FileBrowser.Filter("Image", "png"));
	        FileBrowser.SetDefaultFilter("png");

	        FileBrowser.ShowSaveDialog((paths) => {
	            if(paths.Length == 0) return;
	            string filepath = paths[0];

	            Texture2D tex2D = _image.visibleTexture_ref as Texture2D;
	            if (tex2D != null){ 
	                TextureTools_SPZ.EncodeAndSaveTexture(tex2D, filepath);
	                //don't clean up the tex2D here, it already existed before and belongs to someone else.
	            }else{
	                tex2D = TextureTools_SPZ.RenderTextureToTexture2D(_image.visibleTexture_ref as RenderTexture);
	                TextureTools_SPZ.EncodeAndSaveTexture(tex2D, filepath);
	                DestroyImmediate(tex2D);//clean up the temporary tex2D.
	            }
	        }, 
	        null, FileBrowser.PickMode.Files, false, null, "spz_screenshot", "Save Image", "Save");
	    }

	    void OnCloseButton(){
	        _Act_onCloseButton?.Invoke(this);
	        //don't dispose of the the texture yet, let the callback reciever (our owner) decide.
	    }


	    void ShowMirrorButton_maybe(){ 
	        if(_showMirrorButton == false){ return; }
	        bool contains = RectTransformUtility.RectangleContainsScreenPoint( transform as RectTransform, 
	                                                                           KeyMousePenInput.cursorScreenPos() );
	        _mirrorButton.gameObject.SetActive(contains && has_image());
	        _save_image_button.gameObject.SetActive(contains && has_image());
	    }


	    void Update(){
	        ShowMirrorButton_maybe();
	    }


	    void Awake(){
	        _imageButton._onMouseClick += OnImageButton;

	        //always begin Mirror button as off (will show if hovered)
	        if (_mirrorButton != null){ _mirrorButton.gameObject.SetActive(false); }
	        if (_save_image_button != null){ _save_image_button.gameObject.SetActive(false); }
	        if(_showMirrorButton){
	            _mirrorButton.onClick.AddListener( OnMirrorButton );
	            _save_image_button.onClick.AddListener(OnSaveImageButton);
	        }

	        if (_showCloseButton){
	            _closeButton.onClick.AddListener( OnCloseButton );
	        }else{
	            if(_closeButton!=null){ _closeButton.gameObject.SetActive(false); }
	        }
	    }

	}
}//end namespace
