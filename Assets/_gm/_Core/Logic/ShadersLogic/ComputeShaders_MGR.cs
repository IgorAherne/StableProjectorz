using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	public class ComputeShaders_MGR : MonoBehaviour{
	    public static ComputeShaders_MGR instance { get; private set; } = null;

	    [SerializeField] ComputeShader _blitUInt32Tex_shader;

	    // Threads work in a group and share their fast memory.
	    // For example you can do  _compShader.Dispatch(kernelHandle, tex.width/x, tex.height/y, z);
	    // This will dispatch 
	    // This also must reflect what's used inside the source code of shaders.
	    // If you change values here, make sure to change them in the source code of all shaders too.
	    public static Vector3Int computeShaders_threadsXYZ { get; private set; } = new Vector3Int(32, 1, 1);


	    // Effectively rounds up the result of the division. This ensures that every block
	    // still gets processed, even partial, by dispatching an extra thread group for it:
	    public static Vector3Int calcNumGroups(RenderTexture forThis){
	        int numSlices =  forThis.volumeDepth>=1? forThis.volumeDepth : 1;
	        return calcNumGroups( forThis.width,  forThis.height,  numSlices );
	    }


	    // textureSlices=1 when we are only dealing with 2D textures, not texture arrays.
	    public static Vector3Int calcNumGroups(int textureWidth, int textureHeight, int textureSlices=1){
	        Vector3Int xyz = new Vector3Int(32, 1, 1);
	        var numGroupsXYZ = new Vector3Int();
	        numGroupsXYZ.x = (textureWidth + xyz.x - 1) / xyz.x;
	        numGroupsXYZ.y = (textureHeight+ xyz.y - 1) / xyz.y;
	        numGroupsXYZ.z = textureSlices;//1 if we are only dealing with 2D textures, not arrays.
	        return numGroupsXYZ;
	    }


	    //texName: what is your texture called in the source code of the shader.
	    public void Dispatch_for_uInt32Texture( ComputeShader sh, int kernelHandleInShader, 
	                                            RenderTexture tex, string texName = "_OutputTexture"){
	        AssertSuitableTex(tex); 
	        sh.SetTexture(kernelHandleInShader, texName, tex, 0);
	        sh.SetInt("_Output_Width", tex.width);
	        sh.SetInt("_Output_Height", tex.height);

	        Vector3Int numGroups = calcNumGroups(tex);
	        sh.Dispatch(kernelHandleInShader, numGroups.x, numGroups.y, numGroups.z);
	    }


	    public void Blit_to_uInt32texture( RenderTexture src_intTex, RenderTexture dest_intTex ){
	        Debug.Assert(src_intTex.width == dest_intTex.width);
	        Debug.Assert(src_intTex.height== dest_intTex.height);
	        AssertSuitableTex(src_intTex);
	        AssertSuitableTex(dest_intTex);
	        int kernelHandleInShader = _blitUInt32Tex_shader.FindKernel("CSMain");
	        _blitUInt32Tex_shader.SetTexture(kernelHandleInShader, "_CopyFromTexture", src_intTex, 0);
	        Dispatch_for_uInt32Texture(_blitUInt32Tex_shader, kernelHandleInShader, dest_intTex, "_OutputTexture");
	    }


	    void AssertSuitableTex(RenderTexture tex){
	        Debug.Assert(tex.graphicsFormat == UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_UInt);
	        Debug.Assert(tex.enableRandomWrite);
	        Debug.Assert(tex.filterMode == FilterMode.Point);
	        Debug.Assert(tex.anisoLevel == 1);
	        Debug.Assert(tex.mipmapCount<=1); //no mipmaps actually means count is 1. But I use 0 in code sometimes :)
	    }


	    void Awake(){
	        if (instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	    }
	}
}//end namespace
