using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class RawImage_with_aspect : MonoBehaviour{
	    [Space(10)]
	    [SerializeField] RawImage _rawImage;
	    [SerializeField] AspectRatioFitter aspectRatioFitter;
	    [SerializeField] Material _ui_material;//optional, usually null. But might be used to tint the image, etc. 
    
	    Material _ui_material_cpy;
	    List<TextureAndOwnership> _texturesToShow = new List<TextureAndOwnership>();

	    public Texture visibleTexture_ref =>_rawImage.texture;


	    class TextureAndOwnership{
	        public Texture texture = null;
	        public bool belongsToMe = false;
	        public bool isGenerated = false;//was it created by the StableDiffusion (True) or is it like some depthmap (false)
	        public float forceAspect = -1;
	        public CameraTexType texType;
	        public TextureAndOwnership( Texture tex,  bool belongsToMe,  bool isFromGeneration, 
	                                    CameraTexType texType, float forceAspect=-1 ){ 
	            this.texture = tex; 
	            this.belongsToMe = belongsToMe;  
	            this.isGenerated = isFromGeneration;
	            this.forceAspect = forceAspect;
	            this.texType = texType;
	        }
	        public void DisposeTexture_maybe(){
	            if(!belongsToMe){ return; }
	            var asRT = texture as RenderTexture;
	            if (asRT != null){ asRT.Release(); }
	            DestroyImmediate(texture);
	        }
	        public void SwapTex(Texture newTex){
	            if( ReferenceEquals(texture,newTex) ){ return; }//same, keep as is
	            DisposeTexture_maybe();//dispose the old.
	            texture = newTex;
	        }
	    }


	    // isGenerated:  was it created by the StableDiffusion (True) or is it like some depthmap (false).
	    //              If so, we will swap-out the latest image (that also is marked as isGenerated) with this image.
	    //              It allows to update them as they are being rendered.
	    // Consider invoking RemoveOrForgetTexture_ifExists(), else the images will "stack".
	    public void ShowTexture_dontOwn( Texture tex,  int mySliceIx_ifArray,  bool isGenerated,  CameraTexType texType,  
	                                     GenerationData_Kind kind=GenerationData_Kind.Unknown, float forceAspect=-1){
        
	        _rawImage.SetMaterialDirty();//chainging material stuff, so force the image to re-draw soon.

	        AddOrRefresh(tex, isGenerated, isBelongsToMe:false, texType, forceAspect);
	        _rawImage.texture = tex;
	        _ui_material_cpy?.SetTexture("_MainTex", tex);

	        UpdateAspect_match_texture();
	        TintColor();

	        var texRT = tex as RenderTexture;
	        bool isTextureArray =  texRT!=null  &&  texRT.dimension == UnityEngine.Rendering.TextureDimension.Tex2DArray;

	        if(_ui_material_cpy != null){
	            bool show_R_channel =   kind == GenerationData_Kind.AmbientOcclusion 
	                                 || texType == CameraTexType.DepthUserCamera;
	            TextureTools_SPZ.SetKeyword_Material(_ui_material_cpy, "SHOW_R_CHANNEL_ONLY", show_R_channel);
	            TextureTools_SPZ.SetKeyword_Material(_ui_material_cpy, "USING_TEXTURE_ARRAY", isTextureArray);
	            // will only matter if texture array keyword is enabled:
	            _ui_material_cpy.SetInteger("_TextureArraySlice", mySliceIx_ifArray);
	        }
	    }

    

	    // Similar to ShowTexture_dontOwn, but this version takes ownership of the texture.
	    // Consider invoking RemoveOrForgetTexture_ifExists(), else the images will "stack".
	    public void ShowTexture_takeOwnership(Texture tex, int mySliceIx_ifArray, bool isGenerated, CameraTexType texType,
	                                          GenerationData_Kind kind = GenerationData_Kind.Unknown, float forceAspect = -1){
	        _rawImage.SetMaterialDirty();

	        AddOrRefresh(tex, isGenerated, isBelongsToMe: true, texType, forceAspect);
	        _rawImage.texture = tex;
	        _ui_material_cpy?.SetTexture("_MainTex", tex);

	        UpdateAspect_match_texture();
	        TintColor();

	        var texRT = tex as RenderTexture;
	        bool isTextureArray = texRT != null && texRT.dimension == UnityEngine.Rendering.TextureDimension.Tex2DArray;

	        if (_ui_material_cpy != null){
	            bool show_R_channel = kind == GenerationData_Kind.AmbientOcclusion 
	                                 || texType == CameraTexType.DepthUserCamera;
	            TextureTools_SPZ.SetKeyword_Material(_ui_material_cpy, "SHOW_R_CHANNEL_ONLY", show_R_channel);
	            TextureTools_SPZ.SetKeyword_Material(_ui_material_cpy, "USING_TEXTURE_ARRAY", isTextureArray);
	            _ui_material_cpy.SetInteger("_TextureArraySlice", mySliceIx_ifArray);
	        }
	    }

     
	    public void StopShowTexture_dontOwn(Texture tex){
	        int ix = _texturesToShow.FindIndex(t => t.texture==tex);
	        if(ix==-1){ return; }//already stopped showing some time ago.

	        TextureAndOwnership tOwn = _texturesToShow[ix];
	        UserCameras_Permissions.LockOrUnlock_ByType(tOwn.texType, this, isLock:false);
        
	        _rawImage.SetMaterialDirty();//chainging material stuff, so force the image to re-draw soon.

	        tOwn.DisposeTexture_maybe();
	        _texturesToShow.RemoveAt(ix);
	        _rawImage.texture = null;
	        _ui_material_cpy?.SetTexture("_MainTex", null);

	        if(_texturesToShow.Count == 0){
	            UpdateAspect_match_texture();
	            return; 
	        }
	        //place new texture instead:
	        tOwn = _texturesToShow[_texturesToShow.Count-1];
	        _rawImage.texture = tOwn.texture;
	        _ui_material_cpy?.SetTexture("_MainTex", _rawImage.texture);
	        UpdateAspect_match_texture();
	    }


	    // Forgets or destroys an image at the top of the stack.
	    // use 'dontApplyRemainingImagesYet' if you want to remove multiple textures in a loop,
	    // and only update the UI once at the end, rather than updating after each removal.
	    public bool RemoveLatestTexture_ifExists(bool dontApplyRemainingImagesYet=false){
	        if (_texturesToShow.Count == 0){ return false; }
    
	        var lastTexture = _texturesToShow[_texturesToShow.Count - 1];
	        UserCameras_Permissions.LockOrUnlock_ByType(lastTexture.texType, this, isLock: false);
	        lastTexture.DisposeTexture_maybe();
	        _texturesToShow.RemoveAt(_texturesToShow.Count - 1);
	        _rawImage.SetMaterialDirty();

	        if(dontApplyRemainingImagesYet){ return true; }

	        if (_texturesToShow.Count > 0){
	            // Show previous texture if it exists
	            var prevTexture = _texturesToShow[_texturesToShow.Count - 1];
	            _rawImage.texture = prevTexture.texture;
	            _ui_material_cpy?.SetTexture("_MainTex", prevTexture.texture);
	        }else{
	            // No more textures to show
	            _rawImage.texture = null;
	            _ui_material_cpy?.SetTexture("_MainTex", null);
	        }
	        //always apply aspect, if have more texture, or if none remains (if none, aspect should be 1)
	        UpdateAspect_match_texture();

	        // Reset material settings if no textures left
	        if (_ui_material_cpy != null && _texturesToShow.Count == 0){
	            TextureTools_SPZ.SetKeyword_Material(_ui_material_cpy, "SHOW_R_CHANNEL_ONLY", false);
	            TextureTools_SPZ.SetKeyword_Material(_ui_material_cpy, "USING_TEXTURE_ARRAY", false);
	        }
	        TintColor();
	        return true;
	    }



	    void AddOrRefresh(Texture texture,  bool isGenerated,  bool isBelongsToMe,  CameraTexType texType, float forceAspect=-1){
	        int ix = _texturesToShow.FindIndex(t => ReferenceEquals(t.texture,texture) );
	            //there is typically only one image that is generated (icons have it), 
	            //so we can try searching for an entry that is 'generated', and update it:
	            ix =  (ix==-1 && isGenerated)? _texturesToShow.FindIndex(t=>t.isGenerated)  : ix;
	        if(ix==-1){
	            _texturesToShow.Add(new TextureAndOwnership(texture, isBelongsToMe, isGenerated, texType, forceAspect));
	            UserCameras_Permissions.LockOrUnlock_ByType(texType, this, isLock:true);
	            return;
	        }
	        //found, so move to the end of the list:
	        var tOwn =  _texturesToShow[ix];
	        _texturesToShow.RemoveAt(ix);
	        _texturesToShow.Add(tOwn);

	        if(isGenerated){  tOwn.SwapTex(texture); }
	    }


	    void TintColor(){
	        _rawImage.color =  _rawImage.texture==null?  Color.gray : Color.white;
	    }

	    void UpdateAspect_match_texture(){
	        bool notReady =  _rawImage==null || _rawImage.texture==null || aspectRatioFitter==null;
	        if(notReady){
	            aspectRatioFitter.aspectRatio = 1.0f;
	            return; 
	        }
	        TextureAndOwnership tOwn = _texturesToShow[_texturesToShow.Count-1];

	        float aspect =  _rawImage.texture.width / (float)_rawImage.texture.height;
	              aspect =  tOwn.forceAspect > 0?  tOwn.forceAspect  :  aspect;
	        aspectRatioFitter.aspectRatio = aspect;
	    }


	    void OnEnable(){
	        for(int i=0; i<_texturesToShow.Count; ++i){
	            TextureAndOwnership tOwn = _texturesToShow[i];
	            UserCameras_Permissions.LockOrUnlock_ByType(tOwn.texType, this, isLock:true);
	        }
	    }

	    void OnDisable(){
	        for(int i=0; i<_texturesToShow.Count; ++i){
	            TextureAndOwnership tOwn = _texturesToShow[i];
	            UserCameras_Permissions.LockOrUnlock_ByType(tOwn.texType, this, isLock:false);
	        }
	    }

	    void Awake(){
	        if (_ui_material != null){
	            _ui_material_cpy = new Material(_ui_material);
	            _ui_material = null;//to avoid mistakes. Use the _cpy from now on.
	            _rawImage.material = _ui_material_cpy;
	        }
	    }

	    void OnDestroy(){
	        foreach(TextureAndOwnership tao in _texturesToShow){
	            if(!tao.belongsToMe){ continue; }
	            var asRT = tao.texture as RenderTexture;
	            if (asRT != null){ asRT.Release(); }
	            DestroyImmediate(tao.texture);
	        }
	        _texturesToShow.Clear();

	        if (_ui_material_cpy != null){  DestroyImmediate(_ui_material_cpy); }
	        _ui_material_cpy = null;
	    }

	}
}//end namespace
