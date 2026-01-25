using UnityEngine;

namespace spz {

	public class StaticShaders_MGR : MonoBehaviour{
	    public static StaticShaders_MGR instance { get; private set; } = null;

	    // Allows to create a thinner texture-array,
	    // which has 1 slice less than its original texture-array.
	    [SerializeField] Shader _TextureArrayRemoveSlice_shader;
	    public Material TextureArrayRemoveSlice_mat { get; private set; }


	    // Allows to fill some slices of a texture-array.
	    // Can only fill a fixed number of slices, so invoke several Blit() maybe.
	    [SerializeField] Shader _TextureArrayFillSlices_shader;
	    public Material TextureArrayFillSlices_mat { get; private set; }


	    [SerializeField] ComputeShader _TexArray_ClearSlicesSparse_shader;
	    public ComputeShader TexArray_ClearSlicesSparse_shader => _TexArray_ClearSlicesSparse_shader;


	    [SerializeField] Shader _TextureArrayReadSlice_shader;
	    public Material TextureArrayReadSlice_mat { get; private set; }


	    [SerializeField] Shader _Depth_ShadowcasterSimple;
	    public Shader Depth_ShadowcasterSimple => _Depth_ShadowcasterSimple;

    
	    [SerializeField] Shader _R_to_RGBA_shader; //converts from single-channel texture (depth visualization) into RGBA texture.
	    public Material R_to_RGBA_mat { get; private set; }


	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	        TextureArrayRemoveSlice_mat = new Material(_TextureArrayRemoveSlice_shader);
	        TextureArrayFillSlices_mat = new Material(_TextureArrayFillSlices_shader);
	        TextureArrayReadSlice_mat = new Material(_TextureArrayReadSlice_shader);
	        R_to_RGBA_mat = new Material(_R_to_RGBA_shader);
	    }

	    void OnDestroy(){
	        DestroyImmediate(TextureArrayRemoveSlice_mat);
	        DestroyImmediate(TextureArrayFillSlices_mat);
	        DestroyImmediate(TextureArrayReadSlice_mat);
	        DestroyImmediate(R_to_RGBA_mat);
	        TextureArrayRemoveSlice_mat = null;  TextureArrayFillSlices_mat = null;  TextureArrayReadSlice_mat = null;
	    }
	}
}//end namespace
