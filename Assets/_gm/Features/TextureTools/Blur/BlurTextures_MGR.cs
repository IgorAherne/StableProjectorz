using UnityEngine;
using UnityEngine.Rendering;

namespace spz {

	public enum BlurByChannel { NotSet, R, G, B, A, };


	public class BlurTextures_MGR : MonoBehaviour{
	    public static BlurTextures_MGR instance { get; private set; } = null;

	    [SerializeField] Material _blur_mat; //Use _blur_matCopy, will assing null to this after init.
	    [SerializeField] Material _blur_mat_insideUvChunks;
    
	    Material _blur_matCopy;
	    Material _blur_mat_insideUvChunksCopy;


	    //numIters_for_512: be careful! grows exponentially. Use 1 or max 2 ...maaaaaybe 3.
	    public static int calc_NumInvocations(Texture tex, int numIters_for_512=1){
	        int smallestSide = Mathf.Min(tex.width, tex.height);
	        float scaleFactor = smallestSide / 512f;
	        int numInvoke = Mathf.CeilToInt(numIters_for_512 * scaleFactor * scaleFactor);
	        return numInvoke;
	    }

	    public static float calc_StepAmplification(Texture tex){ 
	        int smallestSide = Mathf.Min(tex.width, tex.height);
	        float scaleFactor = smallestSide / 512f;
	        //0.05 is good for 512x512 images.
	        //0.25 empirically confirmed to be best for 1024 SDXL, with dynamic 'numInvoke'.
	        //so, using this formula:
	        float amplification = 0.05f*scaleFactor*scaleFactor;
	        return amplification;
	    }


	    public class BlurTextureArg{
	        public RenderTexture blurThis_then_outputHere; //can be a 2d-texture or texture-array.
	        public RenderTexture helperTex;
	        public int blurBoxHalfSize_1_to_12;
	        public float stepLength = 1;
	        public bool ignoreSamples_thatHave_0rgb = false;
	        public float skipSample_if_differenceGrtr = 1;
	        public bool is_for_uv_chunks=false; //edges of uv-chunks are blured, but they won't blur each other, if they are close-by.

	        //iterations on the sides of the box will make larger and larger steps. Allows for cheap blur.
	        //From 0 to 1.  Zero will disable this effect entirely.
	        public float farSteps_amplification01 = 0.0f;
	        public BlurByChannel blurByChannel = BlurByChannel.A;
	        public BlurTextureArg( RenderTexture this_and_intoHere_2d_or_texArray,  RenderTexture helperTex,//helper might be null.
	                               int blurBoxHalfSize_1_to_12, float stepLength=1.0f){
	            this.blurThis_then_outputHere = this_and_intoHere_2d_or_texArray;
	            this.helperTex = helperTex;
	            this.blurBoxHalfSize_1_to_12 = blurBoxHalfSize_1_to_12;
	            this.stepLength = stepLength;
	        }
	    }


	    //skipSamples_R_differenceOf For example 0.05  If we are bluring depth,
	    //we might want to ignore sample if its much darker than the main texel (far along z):
	    public void Blur_texture( BlurTextureArg a ){
	        CheckAsserts(a);

	        bool madeMyOwnHelper = CreateHelperMaybe(a);
	        TextureTools_SPZ.Blit(a.blurThis_then_outputHere, dest:a.helperTex);

	        Material mat =  a.is_for_uv_chunks? _blur_mat_insideUvChunksCopy : _blur_matCopy;

	        bool isArray =  a.blurThis_then_outputHere.dimension == TextureDimension.Tex2DArray;
	        RenderUdims.SetNumUdims(isArray, a.blurThis_then_outputHere.volumeDepth, mat);

	        mat.SetTexture("_SrcTex", a.helperTex);

	        Vector4 step =  new Vector4(1.0f/a.helperTex.width, 1.0f/a.helperTex.height, 0, 0);
	        mat.SetVector("_Tex_invSize", step*a.stepLength);
	        mat.SetFloat("_FarSteps_amplification01", a.farSteps_amplification01);

	        mat.SetFloat("_SkipSample_if_zero_RGB", a.ignoreSamples_thatHave_0rgb?1:0);
	        mat.SetFloat("_SkipSample_if_differenceGrtr", a.skipSample_if_differenceGrtr);


	        a.blurBoxHalfSize_1_to_12 = Mathf.Clamp(a.blurBoxHalfSize_1_to_12, 0, 12);
	        Blur_Change_numIters( a.blurBoxHalfSize_1_to_12,  mat );
        
	        enableAlphaKeyword(mat, a.blurByChannel);

	        TextureTools_SPZ.Blit(a.helperTex, dest:a.blurThis_then_outputHere, mat);

	        if (madeMyOwnHelper){  RenderTexture.ReleaseTemporary(a.helperTex); }
	    }


	    void CheckAsserts(BlurTextureArg a){
	        int numChannels = TextureTools_SPZ.GetChannelCount(a.blurThis_then_outputHere);
	        switch (a.blurByChannel){
	            case BlurByChannel.R: Debug.Assert(numChannels>=1); break;
	            case BlurByChannel.G: Debug.Assert(numChannels>=2); break;
	            case BlurByChannel.B: Debug.Assert(numChannels>=3); break;
	            case BlurByChannel.A: Debug.Assert(numChannels>=4); break;
	        }
	    }


	    bool CreateHelperMaybe( BlurTextureArg a ){
	        bool madeMyOwnHelper = a.helperTex == null;
	        a.helperTex =  madeMyOwnHelper ? RenderTexture.GetTemporary(a.blurThis_then_outputHere.descriptor)
	                                       : a.helperTex;
	        if (!madeMyOwnHelper){
	            Debug.Assert(a.helperTex.dimension == a.blurThis_then_outputHere.dimension, 
	                         $"Blurring: Helper dimension-mismatch. Maybe one of the textures is a textureArray while another is a 2d-texture?");
	            Debug.Assert(a.helperTex.graphicsFormat == a.blurThis_then_outputHere.graphicsFormat, "Bluring: helper texture graphics format mismatch");
	            Debug.Assert(a.helperTex.width == a.blurThis_then_outputHere.width, "Bluring: helper texture resolution mismatch");
	            Debug.Assert(a.helperTex.height== a.blurThis_then_outputHere.height, "Bluring: helper texture resolution mismatch");
	        }
	        return madeMyOwnHelper;
	    }


	    //iterations affect how blurry the image is, but are exponentially more expensive.
	    void Blur_Change_numIters(int blurBoxHalfSize_1_to_12, Material mat){
	        //turn off previously set keyword (only 1 should be active)
	        for(int i=0; i<11; ++i){  mat.DisableKeyword($"BLUR_HALF_SIZE_{i}");  }
	        //enable the new keyword:
	        mat.EnableKeyword($"BLUR_HALF_SIZE_{blurBoxHalfSize_1_to_12}");
	    }


	    void enableAlphaKeyword(Material mat, BlurByChannel blurByChannel){
	        switch (blurByChannel){
	            case BlurByChannel.R: TextureTools_SPZ.SetKeyword_Material(mat, "ALPHA_R", true); break;
	            case BlurByChannel.G: TextureTools_SPZ.SetKeyword_Material(mat, "ALPHA_G", true); break;
	            case BlurByChannel.B: TextureTools_SPZ.SetKeyword_Material(mat, "ALPHA_B", true); break;
	            case BlurByChannel.A: TextureTools_SPZ.SetKeyword_Material(mat, "ALPHA_A", true); break;
	            default: Debug.Assert(false, "uknown BlurByChannel: " + blurByChannel); break;
	        }
	    }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	        _blur_matCopy = new Material(_blur_mat);
	        _blur_mat = null;

	        _blur_mat_insideUvChunksCopy = new Material(_blur_mat_insideUvChunks);
	        _blur_mat_insideUvChunks = null;
	    }


	    void OnDestroy(){
	        DestroyImmediate( _blur_matCopy );
	        DestroyImmediate( _blur_mat_insideUvChunksCopy );
	    }
	}
}//end namespace
