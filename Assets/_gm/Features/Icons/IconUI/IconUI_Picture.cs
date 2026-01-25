using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Belongs to some IconUI element. 
	// Controls displaying of art images.
	public class IconUI_Picture : MonoBehaviour
	{
	    [SerializeField] IconUI _icon;
	    [SerializeField] RawImage_with_aspect _rawImageElem;
	    [Space(10)]
	    [SerializeField] Image _stackSheets_lines;//enabled when our icon "carries" several textures at once, instead of a single texture.
	    [SerializeField] TextMeshProUGUI _num_textureSlices_txt;//if our icon contains several textures.
	    [SerializeField] TextMeshProUGUI _width_height_txt;
	    [Space(10)]
	    [SerializeField] bool _skipShowing_1st_progressUpdate;
    
	    bool _receivedProgress = false;

	    public GenData2D genData =>  _icon._genData;
	    public IReadOnlyList<Guid> texture_guids =>  _icon.texture_guids;


	    public void OnAfterInstatiated( IconUI_Art2D_ContextMenu artMenu,  IconUI_AO_ContextMenu aoMenu ){

	        bool is_img_stack =  genData.use_many_icons == false  &&  genData.n_total > 1;
	        _stackSheets_lines.gameObject.SetActive( is_img_stack );
	        _num_textureSlices_txt.gameObject.SetActive( is_img_stack );
	        _num_textureSlices_txt.text =  genData.n_total.ToString();

	        Vector3Int texSize =  genData.textureSize(withUpscale: true);
	        _width_height_txt.text =  texSize.x.ToString() + "\n" + texSize.y;

	        StaticEvents.SubscribeAppend<bool>("IsShowTextureSizes_UI:OnTogglePressed", OnShowTextureSizes_Button);
	        OnShowTextureSizes_Button( IsShowTextureSizes_UI.instance?.isToggleOn?? false );

	        if(artMenu!=null){
	            artMenu.Act_OnSaveButton += OnSaveIconButton;
	            artMenu.Act_OnLoadButton += OnLoadIntoIcon_Button;
	            artMenu.Act_OnDepthButton += OnDepthToggle;
	        }
	        if(aoMenu!=null){
	            aoMenu.OnSaveButton += OnSaveIconButton;
	            aoMenu.OnLoadButton += OnLoadIntoIcon_Button;
	        }
	        genData.Subscribe_for_TextureUpdates(texture_guids, OnTextureUpdated);
	    }


	    public void OnLoad_AfterSpawned(){
	        Guid texGuid = texture_guids.Count>0? texture_guids[0] : default;
	        GenData_TextureRef texRef = genData.GetTexture_ref(texGuid);
	        OnTextureUpdated(texRef);
	    }


	    public void OnCleanup(){
	        genData?.Unsubscribe_from_textureUpdates(texture_guids, OnTextureUpdated);
	        StaticEvents.Unsubscribe<bool>("IsShowTextureSizes_UI:OnTogglePressed", OnShowTextureSizes_Button);
	    }


	    public void OnToggled_ContextMenu(bool isShow){
	        _width_height_txt.alpha = isShow? 0:1;//conceal because there is 'seed' button above it. We don't need this text in context menu.
	        _num_textureSlices_txt.alpha = isShow? 0:1;
	    }


	    //callback, invoked when our Generation_Data recieves an update texture for us from SD server.
	    void OnTextureUpdated( GenData_TextureRef newTex ){
	        if(newTex == null){ return; }

	        if(StableDiffusion_Hub.instance._generating  &&  !_receivedProgress  &&  _skipShowing_1st_progressUpdate){
	            _receivedProgress = true; 
	            return; 
	        }
	        //set the dimension text again, if it wasn't set properly (wasn't ready) after we got instantiated.
	        _width_height_txt.text = newTex.widthHeight().x.ToString() + "\n" + newTex.widthHeight().y;

	        _rawImageElem.ShowTexture_dontOwn( newTex.tex_by_preference(),  mySliceIx_ifArray:newTex.sliceIx, 
	                                           isGenerated:true,  CameraTexType.Nothing,  genData.kind );
	    }


	    void OnDepthToggle(bool isOn){
	        if(genData.kind == GenerationData_Kind.UvTextures_FromFile){ 
	            if(!isOn){ _rawImageElem.StopShowTexture_dontOwn(Texture2D.blackTexture); return; }
	            _rawImageElem.ShowTexture_dontOwn(Texture2D.blackTexture, 0, isGenerated:false, CameraTexType.Nothing, genData.kind);
	            return; 
	        }
	        if(!isOn){ _rawImageElem.StopShowTexture_dontOwn(genData._byproductsOfRequest.depth_disposableTex); return; }
	        _rawImageElem.ShowTexture_dontOwn( genData._byproductsOfRequest.depth_disposableTex, 0, 
	                                           isGenerated:false, CameraTexType.Nothing, genData.kind );
	    }

    
	    void OnShowTextureSizes_Button(bool isPressed){
	        _width_height_txt.gameObject.SetActive(isPressed);

	        bool is_img_stack = genData.use_many_icons == false && genData.n_total > 1;
	        _num_textureSlices_txt.gameObject.SetActive(is_img_stack && isPressed);
	    }


	    void OnSaveIconButton(){
	        var texturesDict = new Dictionary<Texture2D, UDIM_Sector>();
	        bool destroyWhenDone;

	        bool isShowingDepth = _rawImageElem.visibleTexture_ref == genData._byproductsOfRequest?.depth_disposableTex;

	        if (isShowingDepth){
	            texturesDict.Add(_rawImageElem.visibleTexture_ref as Texture2D, default);
	            destroyWhenDone = false;
	        }else { //whatever is shown in our icon (even if depth, we'll save depth - some did request ability to save depth).
	            texturesDict =  genData.GetTextures2D_expensive(out destroyWhenDone);
	        }

	        Save_MGR.instance.Save2DArt( texturesDict, destroyTexs:destroyWhenDone );
	    }


	      //allows to load a custom image into the icon
	    void OnLoadIntoIcon_Button(){
	        // If the genData expects several icon only, disallow loading multiple files into our icon.
	        // Otherwise, if expects 1 icon, there can be many texture/file stuffed into our icon.
	        bool allow_multipleFiles =  false==genData.use_many_icons;
	        Images_ImportHelper.instance.ImportCustomImageButton( genData.kind, allow_multipleFiles, OnImportCustomImage_OK);
	    }


	    void OnImportCustomImage_OK( GenerationData_Kind kind,  Dictionary<Texture2D,UDIM_Sector> texturesWithoutOwner ){

	        var textures = texturesWithoutOwner.Keys.ToList();
	        var udims    = texturesWithoutOwner.Values.ToList();

	        genData.AssignTextures_Manual( textures, udims );
	        _icon.Set_Texture_guids( genData.textureGuidsOrdered );

	        //forget them if they got cleaned up by genData (likely converted into tex array)
	        if (texturesWithoutOwner.Any(kvp => kvp.Key == null)){ textures = null; }
	        GenData_TextureRef texRef0 = genData.GetTexture_ref0();
        
	        if (genData.use_many_icons){
	            //one icon is meant to contain one texture (not several textures).
	            Debug.Assert(genData.textureGuidsOrdered.Count == 1);
	        }

	        bool is_img_stack =  genData.use_many_icons==false  &&  genData.n_total>1;
	        _stackSheets_lines.gameObject.SetActive( is_img_stack );
	        _num_textureSlices_txt.gameObject.SetActive( is_img_stack );
	        _num_textureSlices_txt.text =  genData.n_total.ToString();

	        Objects_Renderer_MGR.instance.ReRenderAll_soon();

	        // Clean up existing textures first:
	        _rawImageElem.RemoveLatestTexture_ifExists();
	        _rawImageElem.ShowTexture_dontOwn( texRef0.tex_by_preference(),  mySliceIx_ifArray:0,  isGenerated:false,   
	                                          CameraTexType.Nothing,  genData.kind );
	    }

	}
}//end namespace
