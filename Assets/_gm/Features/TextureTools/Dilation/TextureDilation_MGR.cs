using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace spz {

	public enum DilationRule{
	    One_of_4_samples,   //first sample that has non-zero alpha is used. Doesn't fade out, good for textures.
	    NineSamplesAveraged,//all samples that have non-zero alpha are used (averaged).
	}

	public enum DilateByChannel { NotSet, R, G, B, A, Use_Separate_UVChunksR8 }


	public class DilationArg{
	    // Either a usual 2d render texture, or a texture-array renderTexture.
	    // used as the starting point for dilation, and the result will get outputted here.
	    public RenderTexture this_and_intoHere;

	    public int numberOfTexelsExpand;
	    public bool isExpand = true;//if false we'll contract instead of dilating outwards.
	    public Action<RenderTexture> onCompleted_withYourRenderTex;

	    public bool isRunInstantly=false;//blocks the main thread and only continues when dilation (expansion) has completed.

	    // 9 samples is higher quality but might fade colors and is slower to calculate.
	    // Use non-averaged variant if it's for some internal info-textures, where values are technical and shouldn't change.
	    public DilationRule rule = DilationRule.NineSamplesAveraged;

	    public DilateByChannel dilateByChannel = DilateByChannel.A;

	    public bool findUVchunkBorders_thenBlurThem = false;//Searches borders (edges) of the UV chunks, blurs them in source texture, THEN does dilation.
	    public bool bordersWiderBlur=false;//Only if 'detectUVchunkBorders=true'.

	    // Optional argument, if you already have an appropriate texture for buffer, to avoid allocations.
	    // Having your same texture is good, if you will repetitively apply dilation again and again in loop.
	    public RenderTexture blitHelperTex=null;

	    // Optional, only used if you DON'T want us to dilate based on alpha of the original texture.
	    // Your texture can have any number of channels, but Algorithm will work even if it's R8 format (single-channel).
	    // Having this texture allows A of 'this_and_intoHere' to be treated same as RGB are.
	    // NOTICE:  Usually you can keep this texture as null. In this case, the algorithm will use
	    //          (and will modify as needed) the Alpha of your texture to detect "Emptiness" around the UV chunks (2ce faster)
	    public RenderTexture separate_UVchunks_R8_WillAlter = null;

	    // this_and_intoHere_2d_or_texArray:  either a usual 2d render texture, or a texture-array renderTexture.
	    public DilationArg( RenderTexture this_and_intoHere_2d_or_texArray,  int numberOfTexelsExpand, DilateByChannel dilateByChannel,
	                        Action<RenderTexture> onCompleted_withYourRenderTex, bool isRunInstantly=false ){
	        this.this_and_intoHere = this_and_intoHere_2d_or_texArray;
	        this.numberOfTexelsExpand = numberOfTexelsExpand;
	        this.dilateByChannel = dilateByChannel;
	        this.onCompleted_withYourRenderTex = onCompleted_withYourRenderTex;
	        this.isRunInstantly = isRunInstantly;
	    }
	    public DilationArg(){ }

	    public DilationArg Clone(){
	        return new DilationArg {
	            this_and_intoHere = this.this_and_intoHere,
	            numberOfTexelsExpand = this.numberOfTexelsExpand,
	            isExpand = this.isExpand,
	            onCompleted_withYourRenderTex = this.onCompleted_withYourRenderTex,
	            isRunInstantly = this.isRunInstantly,
	            rule = this.rule,
	            dilateByChannel = this.dilateByChannel,
	            findUVchunkBorders_thenBlurThem = this.findUVchunkBorders_thenBlurThem,
	            bordersWiderBlur = this.bordersWiderBlur,
	            blitHelperTex = this.blitHelperTex,
	            separate_UVchunks_R8_WillAlter = this.separate_UVchunks_R8_WillAlter,
	        };
	    }
	}


	// you give it a texture and it "spreads the color outwards"
	// from texels that have non-zero alpha, to texels that have zero alpha.
	// This helps to expand the colors beyond where uv chunks land.
	// It's called Dilation, and helps to avoid seams on 3d model, when using mipmap of your texture.
	public class TextureDilation_MGR : MonoBehaviour {
	    public static TextureDilation_MGR instance { get; private set; } = null;

	    [SerializeField] Material _detectUVchunkBorders_mat;//highlights the texels in white when finds border of some UV chunk (looks at alphas of neighbor texels)
	    [SerializeField] Material _improveUVchunkBorders_mat;
	    [SerializeField] Material _dilationMat;

	    Material _detectUVchunkBorders_matCopy = null;
	    Material _improveUVchunkBorders_matCopy = null;
	    Material _dilation_matCopy = null;

	    [SerializeField] MeshRenderer _previewMesh1 = null; //for debugging only
	    [SerializeField] MeshRenderer _previewMesh2 = null;
	    [SerializeField] MeshRenderer _previewMesh3 = null;

	    bool _createdMyOwnHelper = false;
	    RenderTexture _blit_helper_RT = null;
	    RenderTexture _uvChunksBorders_RT = null; //highlights the texels in white when finds border of some UV chunk (looks at alphas of neighbor texels)

	    // numberOfTexels: how far dilation should extend. For example 128 texels.
	    // Pass isRunInstantly to block until it completes. You own't need the 'onCompleted' callback in that case.
	    //
	    // 'edgesWiderBlur' can help to reduce "gaps" around the corners of UV chunks. But might make the seams more noticeable.
	    // Useful if you are delating alpha maks which have the same color.  Keep false otherwise.
	    public void Dillate( DilationArg arg){
	        //DO NOT CLONE arg, if arg has TextureArray, it will be very expensive.
	        //User should have already cloned by now.
	        StartCoroutine( dilate_crtn(arg) ); 
	    }


	    IEnumerator dilate_crtn( DilationArg a ){
	        CheckAsserts(a);

	        Vector4 tex_invSize;
	        Dilate_Preliminaries(a, out tex_invSize);

	        if (a.findUVchunkBorders_thenBlurThem){
	            RenderTexture_DetectBorders( tex_invSize,  a);
	            Improve_UV_Chunk_Edges( tex_invSize,  a.bordersWiderBlur,  a.this_and_intoHere);
	        }

	        if (a.isRunInstantly){
	            var innerRoutine = dilate_iters_crtn(a, tex_invSize);
	            while(innerRoutine.MoveNext()){} // This loop will run through the entire coroutine in one go
	        }else{
	            yield return StartCoroutine(  dilate_iters_crtn(a, tex_invSize)  );
	        }

	        Cleanup();
	        a.onCompleted_withYourRenderTex?.Invoke(a.this_and_intoHere);
	    }


	    void CheckAsserts( DilationArg a ){
	        int numChannels = TextureTools_SPZ.GetChannelCount(a.this_and_intoHere);
	        switch (a.dilateByChannel){
	            case DilateByChannel.R: Debug.Assert(numChannels>=1); break;
	            case DilateByChannel.G: Debug.Assert(numChannels>=2); break;
	            case DilateByChannel.B: Debug.Assert(numChannels>=3); break;
	            case DilateByChannel.A: Debug.Assert(numChannels>=4); break;
	            case DilateByChannel.Use_Separate_UVChunksR8:
	                Debug.Assert(a.separate_UVchunks_R8_WillAlter != null, "you want to use separate uv chunks for dilation, but haven't provided it!");
	            break;
	        }

	        if(a.separate_UVchunks_R8_WillAlter != null){
	            //COMMENTED OUT, KEPT FOR PRECAUTION  it's actually ok if it's RGBA but we'll still use R channel:)
	            /*Debug.Assert(a.separate_UVchunks_RT_r8.format == RenderTextureFormat.R8);*/
	            Debug.Assert(a.separate_UVchunks_R8_WillAlter.width == a.this_and_intoHere.width);
	            Debug.Assert(a.separate_UVchunks_R8_WillAlter.height == a.this_and_intoHere.height);
	            Debug.Assert(a.dilateByChannel == DilateByChannel.Use_Separate_UVChunksR8,
	                         "you provided separate uv chunks for dilation, but didn't specify it in 'DilateByChannel'. You gave:" + a.dilateByChannel);
	            int channels = TextureTools_SPZ.GetChannelCount(a.separate_UVchunks_R8_WillAlter);
	            Debug.Assert(channels == 1, "separate_UVchunks_R8 needs to be 1 channel, not "+ channels);
	            Debug.Assert(a.separate_UVchunks_R8_WillAlter.dimension == a.this_and_intoHere.dimension, 
	                         $"Dillation: SeparateUVChunks dimension-mismatch. Maybe one of the textures is a textureArray while another is tex2D?");
	        }
	        if(a.blitHelperTex != null){
	            Debug.Assert(a.blitHelperTex.dimension == a.this_and_intoHere.dimension, 
	                         $"Dillation: Helper dimension-mismatch. Maybe one of the textures is a textureArray while another is a 2d-texture?");
	        }
	    }

    

	    void Dilate_Preliminaries(DilationArg a,  out Vector4 tex_invSize_){
	        TextureTools_SPZ.Dispose_RT(ref _blit_helper_RT, isTemporary:true);
	        TextureTools_SPZ.Dispose_RT(ref _uvChunksBorders_RT, isTemporary:true);

	        if (a.findUVchunkBorders_thenBlurThem){
	            // We might need a texture array (if descriptor mentions array), but we need to ensure the graphics format is R8.
	            var desc = a.this_and_intoHere.descriptor;
	            desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm;
	            _uvChunksBorders_RT = RenderTexture.GetTemporary(desc);
	            _uvChunksBorders_RT.filterMode = FilterMode.Point;
	        }

	        tex_invSize_ =  new Vector4( 1.0f/a.this_and_intoHere.width, 1.0f/a.this_and_intoHere.height,
	                                     1.0f/a.this_and_intoHere.width, 1.0f/a.this_and_intoHere.height );

	        _createdMyOwnHelper =  a.blitHelperTex == null;

	        if(_createdMyOwnHelper){
	            _blit_helper_RT = RenderTexture.GetTemporary( a.this_and_intoHere.descriptor );
	            _blit_helper_RT.filterMode = FilterMode.Point;//important, instead of default Bilenear.
	        }else{
	            // Important, instead of default Bilenear.
	            // FORCING IT, because sometimes helper is reused from elsewhere and doesn't have bilinear:
	            a.blitHelperTex.filterMode = FilterMode.Point;
	            _blit_helper_RT = a.blitHelperTex;
	            Debug.Assert( a.blitHelperTex.descriptor.Equals(a.this_and_intoHere.descriptor) );
	        }
	        a.blitHelperTex = _blit_helper_RT;
	    }



	    void RenderTexture_DetectBorders( Vector4 tex_invSize,  DilationArg a){
	        bool separately =  a.separate_UVchunks_R8_WillAlter!=null;
	        Texture source  =  separately?  a.separate_UVchunks_R8_WillAlter  :  a.this_and_intoHere;
        
	        _detectUVchunkBorders_matCopy.SetVector("_SrcTex_invSize", tex_invSize);
	        TextureTools_SPZ.SetKeyword_Material(_detectUVchunkBorders_matCopy, "DETECT_VIA_R_CHANNEL", separately);

	        bool isArray =  a.this_and_intoHere.dimension == TextureDimension.Tex2DArray;
	        RenderUdims.SetNumUdims( isArray,  a.this_and_intoHere.volumeDepth,
	                                _detectUVchunkBorders_matCopy );

	        TextureTools_SPZ.Blit(source, _uvChunksBorders_RT, _detectUVchunkBorders_matCopy);
	        //NOTICE: from 'source' into '_uvChunksBorders_RT'.
	    }


	    void Improve_UV_Chunk_Edges( Vector4 tex_invSize,  bool edgesWiderBlur,  RenderTexture this_and_intoHere ){
        
	        TextureTools_SPZ.SetKeyword_Material(_improveUVchunkBorders_matCopy, "BORDERS_WIDER_BLUR", edgesWiderBlur);
        
	        _improveUVchunkBorders_matCopy.SetTexture("_UVchunksBorders_Tex", _uvChunksBorders_RT);
	        _improveUVchunkBorders_matCopy.SetVector("_BordersTex_invSize", tex_invSize);
	        _improveUVchunkBorders_matCopy.SetTexture("_CurrentTexture", _blit_helper_RT);

	        bool isArray =  this_and_intoHere.dimension == TextureDimension.Tex2DArray;
	        RenderUdims.SetNumUdims( isArray,  this_and_intoHere.volumeDepth,
	                                 _improveUVchunkBorders_matCopy );

	        TextureTools_SPZ.Blit(this_and_intoHere, _blit_helper_RT);
	        TextureTools_SPZ.Blit(null, this_and_intoHere, _improveUVchunkBorders_matCopy);
	    }




	    IEnumerator dilate_iters_crtn(DilationArg a, Vector4 tex_invSize){
        
	        a.numberOfTexelsExpand =  a.numberOfTexelsExpand > a.this_and_intoHere.width ? a.this_and_intoHere.width : a.numberOfTexelsExpand;
	        a.numberOfTexelsExpand =  a.numberOfTexelsExpand > a.this_and_intoHere.height? a.this_and_intoHere.height: a.numberOfTexelsExpand;

	        bool isSeparately = a.separate_UVchunks_R8_WillAlter!=null;
	        _dilation_matCopy.SetVector("_SrcTex_invSize", tex_invSize);

	        toggleKeyword("SHRINK", !a.isExpand); //control the direction of dilation.
	        toggleKeyword("AVERAGE_THE_COLORS", a.rule==DilationRule.NineSamplesAveraged);

	        bool isArray =  a.this_and_intoHere.dimension == TextureDimension.Tex2DArray;
	        RenderUdims.SetNumUdims( isArray,  a.this_and_intoHere.volumeDepth, _dilation_matCopy );
	        for (int i=0; i<a.numberOfTexelsExpand; ++i){
	            disableAlphaKeywords();
	            enableAlphaKeyword(a.dilateByChannel);

	            _dilation_matCopy.SetTexture("_Separate_UV_Chunks_R8", a.separate_UVchunks_R8_WillAlter);//can be null if not provided
	            TextureTools_SPZ.Blit(a.this_and_intoHere, a.blitHelperTex);
	            TextureTools_SPZ.Blit(a.blitHelperTex, a.this_and_intoHere, _dilation_matCopy);

	            if (isSeparately){
	                disableAlphaKeywords();
	                toggleKeyword("SEPARATE_UV_CHUNKS_TEX_R8", false);
	                toggleKeyword("ALPHA_R", true);
	                _dilation_matCopy.SetTexture("_Separate_UV_Chunks_R8",null);
	                TextureTools_SPZ.Blit( a.separate_UVchunks_R8_WillAlter,  a.blitHelperTex );
	                TextureTools_SPZ.Blit( a.blitHelperTex,  a.separate_UVchunks_R8_WillAlter, _dilation_matCopy ); 
	            }
            
	            if(!a.isRunInstantly && i%8==0){ yield return null; }
	        }//end for
	    }
   

	    void enableAlphaKeyword(DilateByChannel dilateByChannel){
	        switch (dilateByChannel){
	            case DilateByChannel.R: toggleKeyword("ALPHA_R", true); break;
	            case DilateByChannel.G: toggleKeyword("ALPHA_G", true); break;
	            case DilateByChannel.B: toggleKeyword("ALPHA_B", true); break;
	            case DilateByChannel.A: toggleKeyword("ALPHA_A", true); break;
	            case DilateByChannel.Use_Separate_UVChunksR8: toggleKeyword("SEPARATE_UV_CHUNKS_TEX_R8", true); break;
	            default: Debug.Assert(false, "uknown DilateByChannel: " + dilateByChannel); break;
	        }
	    }

	    void disableAlphaKeywords(){
	        toggleKeyword("ALPHA_R", false);
	        toggleKeyword("ALPHA_G", false);
	        toggleKeyword("ALPHA_B", false);
	        toggleKeyword("ALPHA_A", false);
	        toggleKeyword("SEPARATE_UV_CHUNKS_TEX_R8", false);
	    }
    
	    void toggleKeyword(string keyword, bool isOn){ 
	        if(isOn){ _dilation_matCopy.EnableKeyword(keyword); }
	        else{ _dilation_matCopy.DisableKeyword(keyword); }
	    }


	    void Cleanup(){
	        TextureTools_SPZ.Dispose_RT(ref _uvChunksBorders_RT, isTemporary: true);

	        if(_createdMyOwnHelper){ 
	            TextureTools_SPZ.Dispose_RT(ref _blit_helper_RT, isTemporary: true); 
	        }
	        // Important, had a bug where in Preliminaries (in next frame) would dispose someone's texture, 
	        // thinking we still have old one.  Keep here even though Dispose_RT sets to null:
	        _blit_helper_RT = null; 
	        _createdMyOwnHelper = false;
	    }


	    void Awake(){
	        if(instance != null){  DestroyImmediate(this.gameObject);  return;  }
	        instance = this;
	        _detectUVchunkBorders_matCopy = new Material(_detectUVchunkBorders_mat);//to avoid modiying original mat. Use this copy from now on
	        _improveUVchunkBorders_matCopy = new Material(_improveUVchunkBorders_mat);
	        _dilation_matCopy = new Material(_dilationMat);

	        if (_previewMesh1 != null){
	            _previewMesh1.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
	            _previewMesh2.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
	            _previewMesh3.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
	        }
	    }


	    void OnDestroy(){
	        TextureTools_SPZ.Dispose_RT(ref _blit_helper_RT, isTemporary: true);
	        TextureTools_SPZ.Dispose_RT(ref _uvChunksBorders_RT, isTemporary: true);

	        if (_detectUVchunkBorders_matCopy != null){ 
	            DestroyImmediate(_detectUVchunkBorders_matCopy);
	            _detectUVchunkBorders_matCopy = null;
	        }
	        if (_improveUVchunkBorders_matCopy != null){
	            DestroyImmediate(_improveUVchunkBorders_matCopy);
	            _improveUVchunkBorders_matCopy = null;
	        }
	        if (_dilation_matCopy != null){
	            DestroyImmediate(_dilation_matCopy);
	            _dilation_matCopy = null;
	        }
	        if (_previewMesh1 != null){ DestroyImmediate(_previewMesh1.sharedMaterial); }
	        if (_previewMesh2 != null){ DestroyImmediate(_previewMesh2.sharedMaterial); }
	        if (_previewMesh3 != null){ DestroyImmediate(_previewMesh3.sharedMaterial); }
	    }

	}
}//end namespace
