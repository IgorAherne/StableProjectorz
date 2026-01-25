using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser; // CHANGED: Replaced Crosstales.FB
using System.IO;
using UnityEngine.Experimental.Rendering;

namespace spz {

	public class Texture2D_withOwners : IDisposable{
	    public Texture2D tex = null;
	    LocksHashset_OBJ owners = null;
	    public Texture2D_withOwners(ref Texture2D texTakeOwnership, object initialOwner){
	        tex = texTakeOwnership;
	        texTakeOwnership = null;
	        owners = new LocksHashset_OBJ();
	        AddOwner(initialOwner);
	        Update_callbacks_MGR.general_UI += OnUpdate;
	        Debug.Log("created");
	    }
	    public void AddOwner(object owner) => owners.Lock(owner);
	    public void RmvOwner(object owner) => owners.Unlock(owner);

	    void OnUpdate(){
	        if(owners.isLocked()){ return; }
	        Dispose();
	    }

	    public void Dispose(){
	        Debug.Log("disposed");
	        if(tex!=null){ Texture2D.Destroy(tex); }
	        tex = null;
	        Update_callbacks_MGR.general_UI -= OnUpdate;
	    }
	}


	public enum WhatImageToSend_CTRLNET { None, Depth, Normals, VertexColors, ContentCam, CustomFile, };
	public enum HowToResizeImg_CTRLNET { Stretch, ScaleToFit_InnerFit, Envelope_OutterFit, };//https://github.com/Mikubill/sd-webui-controlnet/wiki/API#controlnetunitrequest-json-object


	//helper class of ControlNetUnit, helps it to deal with the images,
	//and with the choices such as stretch, inner-envelope etc.
	public class ControlNetUnit_ImagesDisplay : MonoBehaviour{
	    [Space(10)]
	    [SerializeField] RawImage_with_aspect _rawImage;
	    [Space(10)]
	    [SerializeField] GameObject _contextMenu_gameObj;
	    [SerializeField] GameObject _contextMenu_resize_ifCustom_go;//if custom-image is selected, we'll enable this gameObj.
	    [SerializeField] MouseClickSensor_UI _wholeGraphic_button;
	    [SerializeField] Toggle _imgToSend_none_toggle;
	    [SerializeField] Toggle _imgToSend_depth_toggle;
	    [SerializeField] Toggle _imgToSend_normals_toggle;
	    [SerializeField] Toggle _imgToSend_vertexColors_toggle;
	    [SerializeField] Toggle _imgToSend_contentCam_toggle;
	    [SerializeField] Toggle _imgToSend_customFile_toggle;
	    [SerializeField] Texture2D _withoutImage_texture; //placeholder, shown if we won't send any texture.
	    [Space(10)]
	    [SerializeField] Toggle _customImg_scale_toggle; //how to resize when custom image is used.
	    [SerializeField] Toggle _customImg_innerFit_toggle;
	    [SerializeField] Toggle _customImg_envelope_toggle;
	    [Space(10)]
	    [SerializeField] ControlNetUnit_ThreshSliders _threshSliders;

	    bool _sendToggles_neverAltered = true;
	    Texture2D_withOwners _myCustomImg_from_sysFile = null;//loaded from filepath

	    static Action<ControlNetUnit_ImagesDisplay, int> OnSome_CTRLNet_graphicClicked = null;//NOTICE: private static.


	    public void CopyFromAnother(ControlNetUnit_ImagesDisplay other){

	        OnWhatToSendToggle(true, other._whatImageToSend, allowOpenFileNow:false);
	        _imgToSend_none_toggle.SetIsOnWithoutNotify(other._imgToSend_none_toggle.isOn);
	        _imgToSend_depth_toggle.SetIsOnWithoutNotify(other._imgToSend_depth_toggle.isOn);
	        _imgToSend_normals_toggle.SetIsOnWithoutNotify(other._imgToSend_normals_toggle.isOn);
	        _imgToSend_vertexColors_toggle.SetIsOnWithoutNotify(other._imgToSend_vertexColors_toggle.isOn);
	        _imgToSend_contentCam_toggle.SetIsOnWithoutNotify(other._imgToSend_contentCam_toggle.isOn);
	        _imgToSend_customFile_toggle.SetIsOnWithoutNotify(other._imgToSend_customFile_toggle.isOn);

	        OnCustomImage_HowResize(true, _customImg_howResize);
	        _customImg_scale_toggle.SetIsOnWithoutNotify(other._customImg_scale_toggle.isOn);
	        _customImg_innerFit_toggle.SetIsOnWithoutNotify(other._customImg_innerFit_toggle.isOn);
	        _customImg_envelope_toggle.SetIsOnWithoutNotify(other._customImg_envelope_toggle.isOn);

	        _threshSliders.CopyFromAnother(other._threshSliders);

	        //forget the old custom image (don't dispose it in case someone else uses and points to it)
	        _myCustomImg_from_sysFile?.RmvOwner(this);
	        _myCustomImg_from_sysFile = null;
	        //reference the custom image, if it exists:
	        _myCustomImg_from_sysFile = other._myCustomImg_from_sysFile;
	        _myCustomImg_from_sysFile?.AddOwner(this);
	    }


	    public WhatImageToSend_CTRLNET _whatImageToSend { get; private set; } = WhatImageToSend_CTRLNET.Depth;
	    public HowToResizeImg_CTRLNET _customImg_howResize { get; private set; } = HowToResizeImg_CTRLNET.ScaleToFit_InnerFit;
	    public static string HowToResizeImg_tostr(HowToResizeImg_CTRLNET how){
	        switch (how){//Automatic1111 needs exact string, with spaces (no longer accepts integers, from May2024).
	            case HowToResizeImg_CTRLNET.Stretch:  return "Just Resize";       //so, using this method to convert to str.
	            case HowToResizeImg_CTRLNET.ScaleToFit_InnerFit: return "Crop and Resize";
	            case HowToResizeImg_CTRLNET.Envelope_OutterFit: return "Resize and Fill";  //inside 'sd-webui-controlnet/scripts/enums.py'
	            default: return "Just Resize";
	        }
	    }


	    public Texture2D GetCustomImg_sysFile_disposableCpy(){
	        if(_myCustomImg_from_sysFile == null){ return null; }
	        var copy = new Texture2D( _myCustomImg_from_sysFile.tex.width,  _myCustomImg_from_sysFile.tex.height, 
	                                  _myCustomImg_from_sysFile.tex.format,  mipChain:_myCustomImg_from_sysFile.tex.mipmapCount>1);
	        Graphics.CopyTexture(_myCustomImg_from_sysFile.tex, copy);
	        return copy;
	    }



	    // For example to also show this texture in some preview-thumbnail elsewhere.
	    // Beware, the texture might be destroyed, so your thumb might show black.
	    public Texture getTexture_ref_ownedBySomeone( bool returnPlaceholder_ifNone=false ){
	        if(UserCameras_MGR.instance==null){
	            return returnPlaceholder_ifNone ? null : _withoutImage_texture;
	        }
	        switch (_whatImageToSend){
	            //NOTICE: return null, not a placeholder image. This is safer:
	            case WhatImageToSend_CTRLNET.None:  return returnPlaceholder_ifNone?null : _withoutImage_texture; 
	            case WhatImageToSend_CTRLNET.Depth:  return UserCameras_MGR.instance.camTextures._SD_depthCam_RT_R32_contrast;
	            case WhatImageToSend_CTRLNET.Normals: return UserCameras_MGR.instance.camTextures._normalsCam_RT_ref;
	            case WhatImageToSend_CTRLNET.VertexColors: return UserCameras_MGR.instance.camTextures._vertexColorsCam_RT_ref;
	            case WhatImageToSend_CTRLNET.ContentCam:  return UserCameras_MGR.instance.camTextures._contentCam_RT_ref;
	            case WhatImageToSend_CTRLNET.CustomFile: return _myCustomImg_from_sysFile?.tex;
	            default:  Debug.LogError("unknown WhatImageToSend");  break;
	        }
	        return null;
	    }


	    void Update(){
	        //keep updating, because render taxture might be destroyed and new one allocated, during resizing, etc.
	        Texture whatToShow =  getTexture_ref_ownedBySomeone( returnPlaceholder_ifNone:true );
	        CameraTexType texType = UserCameras_Permissions.convert(_whatImageToSend);
	        _rawImage.ShowTexture_dontOwn( whatToShow, 0, isGenerated:false, texType );
	    }


	    void OnDestroy(){
	        if(_wholeGraphic_button!=null){ _wholeGraphic_button._onMouseClick -= OnMyGraphicClick; }
	        ControlNetUnit_ImagesDisplay.OnSome_CTRLNet_graphicClicked -= this.OnSomeCtrlNet_GraphicClicked;
	        _myCustomImg_from_sysFile?.RmvOwner(this);
	        _myCustomImg_from_sysFile = null; //don't Dispose() - there might be other objects still pointing to it.
	    }

	    void Awake(){
	        _wholeGraphic_button._onMouseClick += OnMyGraphicClick;
	        ControlNetUnit_ImagesDisplay.OnSome_CTRLNet_graphicClicked += this.OnSomeCtrlNet_GraphicClicked;
	        Init_GraphicContextMenu();
	    }

    
	    void Init_GraphicContextMenu(){
	        _imgToSend_none_toggle.onValueChanged.AddListener( (isOn)=>OnWhatToSendToggle(isOn, WhatImageToSend_CTRLNET.None) );
	        _imgToSend_depth_toggle.onValueChanged.AddListener( (isOn)=>OnWhatToSendToggle(isOn, WhatImageToSend_CTRLNET.Depth) );
	        _imgToSend_normals_toggle.onValueChanged.AddListener( (isOn)=>OnWhatToSendToggle(isOn, WhatImageToSend_CTRLNET.Normals) );
	        _imgToSend_vertexColors_toggle.onValueChanged.AddListener( (isOn)=>OnWhatToSendToggle(isOn, WhatImageToSend_CTRLNET.VertexColors) );
	        _imgToSend_contentCam_toggle.onValueChanged.AddListener( (isOn)=>OnWhatToSendToggle(isOn, WhatImageToSend_CTRLNET.ContentCam) );
	        _imgToSend_customFile_toggle.onValueChanged.AddListener( (isOn)=>OnWhatToSendToggle(isOn, WhatImageToSend_CTRLNET.CustomFile) );

	        _customImg_scale_toggle.onValueChanged.AddListener( (isOn)=>OnCustomImage_HowResize(isOn, HowToResizeImg_CTRLNET.Stretch) );
	        _customImg_innerFit_toggle.onValueChanged.AddListener( (isOn)=>OnCustomImage_HowResize(isOn, HowToResizeImg_CTRLNET.ScaleToFit_InnerFit) );
	        _customImg_envelope_toggle.onValueChanged.AddListener( (isOn)=>OnCustomImage_HowResize(isOn, HowToResizeImg_CTRLNET.Envelope_OutterFit) );

	        _contextMenu_gameObj.SetActive(true);//enables ToggleGroups while we flick one of their toggles.
	            if(_sendToggles_neverAltered){ _imgToSend_depth_toggle.isOn = true; }
	            _customImg_innerFit_toggle.isOn = true;
	        _contextMenu_gameObj.SetActive(false);
	        _contextMenu_resize_ifCustom_go.SetActive(false);
	    }


	    void OnWhatToSendToggle( bool isOn,  WhatImageToSend_CTRLNET what, bool allowOpenFileNow=true ){
	        if(!isOn){ return; }
	        if(what == _whatImageToSend){ return; }//avoid duplicate invocations, especially when CopyFromOther()
	        _whatImageToSend = what;
	        _sendToggles_neverAltered = false;

	        if (what == WhatImageToSend_CTRLNET.CustomFile  &&  allowOpenFileNow){
	            LoadCustomImage( onChosen );
	            void onChosen() =>_contextMenu_resize_ifCustom_go.SetActive(_myCustomImg_from_sysFile != null);
	        }else{
	            _contextMenu_resize_ifCustom_go.SetActive(false);
	        }
	        //Notify, because changing our image might have changed the type we are considerd to be (depth, normals etc)
	        //our type is also inferred from the image, so maybe we are no longer considered "depth" etc:
	        _threshSliders.OnUnitAltered();
	    }


	    void OnCustomImage_HowResize(bool isOn, HowToResizeImg_CTRLNET how){
	        if (!isOn){ return; }
	        if(how == _customImg_howResize){ return; }//avoid duplicate invocations, especially when CopyFromOther()
	        _customImg_howResize = how;
	    }


	    void OnMyGraphicClick( int buttonIx ){
	        ControlNetUnit_ImagesDisplay.OnSome_CTRLNet_graphicClicked?.Invoke(this, buttonIx);
	    }

	    void OnSomeCtrlNet_GraphicClicked(ControlNetUnit_ImagesDisplay menu, int mouseButtonIx){
	        if (menu!=this || mouseButtonIx != 1){ 
	            _contextMenu_gameObj.SetActive(false); 
	            return; 
	        }
	        //else, self AND right mouse button:
	        bool wasActive = _contextMenu_gameObj.activeSelf;
	        _contextMenu_gameObj.SetActive( !wasActive );
	    }


	    void LoadCustomImage(Action onChosen){
	        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", "png", "jpg", "jpeg"));
	        FileBrowser.SetDefaultFilter("png");
        
	        FileBrowser.ShowLoadDialog( (paths) => {
	            // Success
	            OnLoadCustomImage_FileChosen(paths, onChosen);
	        }, 
	        () => {
	            // Cancelled - pass null/empty to handle "no choice" logic
	            OnLoadCustomImage_FileChosen(null, onChosen);
	        },
	        FileBrowser.PickMode.Files, false, null, null, "Load Image", "Load");
	    }


	    // Changed signature to string[]
	    void OnLoadCustomImage_FileChosen(string[] files, Action onChosen){

	        invoke_OnMainThread();

	        void invoke_OnMainThread(){
	            _myCustomImg_from_sysFile?.RmvOwner(this);//don't dispose, some other owners might be pointing to it.
	            _myCustomImg_from_sysFile = null;

	            if(files == null || files.Length== 0){ onChosen(); return; }
	            string path = files[0]; // Get the first selected file path

	            if (string.IsNullOrEmpty(path)){ onChosen(); return; }

	            byte[] fileData = File.ReadAllBytes(path);

	            // The size here is a placeholder, it will be replaced by LoadImage
	            Texture2D texture = new Texture2D( 2, 2,  GraphicsFormat.R8G8B8A8_UNorm,  mipCount:0,
	                                               TextureCreationFlags.DontInitializePixels,  
	                                               new MipmapLimitDescriptor(useMipmapLimit:false,"Default") );
	            bool isLoaded = texture.LoadImage(fileData); // Load the image data into the texture

	            if (!isLoaded){
	                Debug.LogError("Failed to load image from path: " + path);
	                if (texture != null){ DestroyImmediate(texture); }
	                onChosen();
	                return;
	            }// Else, the texture is loaded successfully

	            _myCustomImg_from_sysFile = new Texture2D_withOwners(ref texture, initialOwner:this);
	            onChosen();
	        }

	    }


	    public void Save(int ix, ControlNetUnit_SL unit_sl, string dataDir){
	        unit_sl.whatImageToSend = _whatImageToSend.ToString();
	        unit_sl.customImg_howResize = _customImg_howResize.ToString();

	        if (_myCustomImg_from_sysFile!=null){ 
	            string pathInDataFolder = $"_ctrl_custom_img_{ix}.png";
	            ProjectSaveLoad_Helper.Save_Tex2D_To_DataFolder(_myCustomImg_from_sysFile.tex, dataDir, pathInDataFolder);
	            unit_sl.myCustomImg = pathInDataFolder;
	        }
	    }


	    public void Load(ControlNetUnit_SL unit_sl, string dataDir){
	        object parsedWhatToSend;
	        bool parsed = Enum.TryParse( typeof(WhatImageToSend_CTRLNET), unit_sl.whatImageToSend, out parsedWhatToSend);
	        _whatImageToSend = parsed? (WhatImageToSend_CTRLNET)parsedWhatToSend : WhatImageToSend_CTRLNET.Depth;
	        switch (_whatImageToSend){
	            case WhatImageToSend_CTRLNET.None: _imgToSend_none_toggle.isOn = true;  break;
	            case WhatImageToSend_CTRLNET.Depth: _imgToSend_depth_toggle.isOn = true;  break;
	            case WhatImageToSend_CTRLNET.Normals: _imgToSend_normals_toggle.isOn = true;  break;
	            case WhatImageToSend_CTRLNET.VertexColors: _imgToSend_vertexColors_toggle.isOn = true; break;
	            case WhatImageToSend_CTRLNET.ContentCam: _imgToSend_contentCam_toggle.isOn = true;  break;
	            case WhatImageToSend_CTRLNET.CustomFile: _imgToSend_customFile_toggle.SetIsOnWithoutNotify(true);  break;
	            default:  Debug.LogError("unknown WhatImageToSend during Load()");  break;
	        }
	        object howResize;
	        parsed = Enum.TryParse( typeof(HowToResizeImg_CTRLNET), unit_sl.customImg_howResize, out howResize);
	        _customImg_howResize = parsed? (HowToResizeImg_CTRLNET)howResize : HowToResizeImg_CTRLNET.ScaleToFit_InnerFit;

	        LoadCustomTex(unit_sl, dataDir);
	    }


	    void LoadCustomTex(ControlNetUnit_SL unit_sl, string dataDir){
	        if(_myCustomImg_from_sysFile != null){ 
	            _myCustomImg_from_sysFile.RmvOwner(this);
	            _myCustomImg_from_sysFile = null;
	        }
	        Texture2D tex = ProjectSaveLoad_Helper.Load_Texture2D_from_DataFolder(dataDir, unit_sl.myCustomImg,
	                                                                    GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.R8G8B8A8_UNorm);
	        if(tex != null){ 
	            _myCustomImg_from_sysFile = new Texture2D_withOwners(ref tex, initialOwner:this); 
	        }
	    }

	}
}//end namespace
