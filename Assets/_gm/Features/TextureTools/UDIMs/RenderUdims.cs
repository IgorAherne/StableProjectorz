using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


namespace spz {

	// Contains a TextureArray (one texture-slice per UDIM).
	// Allows to render several 3d objects into it, as a common texture.
	// These objects are allowed to have uvs in different sectors [0,0], [1,0], [2,0] etc.
	public class RenderUdims{
	    bool texturesBelongToMe = false;
    
	    public RenderTexture texArray { get; private set; } //TextureArray, one slice per UDIM
	    public List<UDIM_Sector> udims_sectors { get; private set; } = new List<UDIM_Sector>();//where on UV space each texture maps to.
	    Vector4[] _udims_shaderCoords = null;

	    public int width => texArray.width;
	    public int height => texArray.height;
	    public Vector2Int widthHeight => new Vector2Int(width, height);
	    public int UdimsCount => texArray.volumeDepth;
	    public GraphicsFormat graphicsFormat => texArray.graphicsFormat;
	    public RenderTextureDescriptor descriptorRT => texArray.descriptor;

	    public FilterMode filterMode => texArray.filterMode;
	    public void Set_FilterMode(FilterMode filterMode) => texArray.filterMode = filterMode;


	    public RenderUdims Clone(bool copyTextureContents=true){
	        var clone  = new RenderUdims();

	        clone.texturesBelongToMe = texturesBelongToMe;

	        clone.udims_sectors = new List<UDIM_Sector>();

	        clone.texArray = TextureTools_SPZ.Clone_RenderTex(texArray, copyTextureContents);

	        for(int i=0; i<UdimsCount; ++i){
	            clone.udims_sectors.Add( udims_sectors[i] );
	        }
	        clone._udims_shaderCoords = Init_Udim_shaderCoords(clone.udims_sectors);
	        return clone;
	    }
    
	    public void Dispose(){
	        if(RenderTexture.active == texArray){ RenderTexture.active=null; }
	        if(texturesBelongToMe){  Texture.DestroyImmediate(texArray);  }
	        udims_sectors.Clear();
	        _udims_shaderCoords = null;
	        texturesBelongToMe = false;
	    }


	    public RenderUdims( RenderTexture textureArray, List<UDIM_Sector> udims_coords,  bool texturesBelongToMe ){
	        Debug.Assert(textureArray.mipmapCount==1, "RenderUdims shouldn't be used for mipmaps ...probably");
	        this.texturesBelongToMe = texturesBelongToMe;
	        this.texArray = textureArray;
	        this.udims_sectors = udims_coords;
	        _udims_shaderCoords = Init_Udim_shaderCoords(udims_sectors);
	    }

	    public RenderUdims(){}//empty constructor, usually used before invoking my Load().


	    public RenderUdims( IReadOnlyList<UDIM_Sector> udim_coords,  Vector2Int widthHeight, 
	                        GraphicsFormat format,  FilterMode filter,  Color clearingColor, int depthBits=0 ){
	        texturesBelongToMe = true;
	        texArray = TextureTools_SPZ.CreateTextureArray(widthHeight, format, filter, numSlices:udim_coords.Count, depthBits);
	        TextureTools_SPZ.ClearRenderTexture(texArray, clearingColor);

	        this.udims_sectors = udim_coords.ToList();//toList makes copy
	        _udims_shaderCoords = Init_Udim_shaderCoords(udims_sectors);
	    }
    

	    static Vector4[] Init_Udim_shaderCoords(IReadOnlyList<UDIM_Sector> sectors){
	        // NOTICE: Always using max size (24), because unity can't adjust count 
	        // (between 8 and 24) on the fly. Even if we were to use material keywords.
	        int num = sectors.Count;
	        int maxNum = UDIMs_Helper.MAX_NUM_UDIMS;
	        var coords = new Vector4[maxNum];

	        float eps = 0.00001f;
	        // Set values we have, and set the remainder of its entries to large values:
	        for(int i=0; i<num; ++i){
	            coords[i] =  new Vector4( sectors[i].x-eps, sectors[i].y-eps, 
	                                      sectors[i].x+1+eps,  sectors[i].y+1+eps);  
	        }
	        for (int i=num;  i<maxNum;  ++i){
	            coords[i] =  Vector4.one*99999;
	        }
	        return coords;
	    }

    
	    public Vector3Int CalcGroups_for_ComputeShader() => ComputeShaders_MGR.calcNumGroups(width,height,UdimsCount);


	    public static void SetNumUdims( RenderUdims countOfThis,  Material intoHere ){
	        SetNumUdims(isUsingArray:true, countOfThis.UdimsCount,  intoHere,  sh:null );
	        Set_UdimCoords_ShaderArray( countOfThis._udims_shaderCoords,  intoHere, sh:null);
	    }

	    public static void SetNumUdims( RenderUdims countOfThis, ComputeShader intoHere){
	        SetNumUdims(isUsingArray:true, countOfThis.UdimsCount, mat:null, sh:intoHere);
	        Set_UdimCoords_ShaderArray( countOfThis._udims_shaderCoords,  mat: null, sh:intoHere);
	    }

	    public static void SetNumUdims( IReadOnlyList<UDIM_Sector> udims, Material intoHere){
	        SetNumUdims(isUsingArray:true, udims.Count, mat:intoHere, sh:null);
	        Vector4[] shaderCoords = Init_Udim_shaderCoords(udims);
	        Set_UdimCoords_ShaderArray( shaderCoords, mat:intoHere, sh:null);
	    }

	    public static void SetNumUdims( bool isUsingArray, int numUdims, Material mat=null, ComputeShader sh=null ){
	        mat?.DisableKeyword($"NUM_SLICES_UPTO_24");  sh?.DisableKeyword($"NUM_SLICES_UPTO_24");
	        mat?.DisableKeyword($"NUM_SLICES_UPTO_16");  sh?.DisableKeyword($"NUM_SLICES_UPTO_16");
	        mat?.DisableKeyword($"NUM_SLICES_UPTO_8");   sh?.DisableKeyword($"NUM_SLICES_UPTO_8");
	        int upto = 8;
	            upto = numUdims>8?  16 : upto;
	            upto = numUdims>16? 24 : upto;
	        if(upto>0){  
	            mat?.EnableKeyword($"NUM_SLICES_UPTO_{upto}");   sh?.EnableKeyword($"NUM_SLICES_UPTO_{upto}"); 
	        }
	        // NOTICE: Set both slices and udims, just in case. Because sometimes our includes use either of them.
	        // UdimsCount, not the 'upto', because want exact value:
	        mat?.SetInteger("_NumSlices", numUdims);  sh?.SetInt("_NumSlices", numUdims);
	        mat?.SetInteger("_NumUDIM", numUdims);  sh?.SetInt("_NumUDIM", numUdims); 
     
	        if(mat!=null){ TextureTools_SPZ.SetKeyword_Material(mat, "USING_TEXTURE_ARRAY", isUsingArray); }
	        if(sh !=null){ TextureTools_SPZ.SetKeyword_ComputeShader(sh, "USING_TEXTURE_ARRAY", isUsingArray); }
	    }


	    //enables global shader properties "NUM_SLICES_UPTO_8" etc, invokes callback, then disables them.
	    //Doesn't need a material.
	    //NOTICE: even if your material uses only few pov, these ones will still be "enabled".
	    public static void TempEnable_UDIMs_Keywords_GLOBAL(int numUdim, Action doStuff){
	        var keywords = new List<int>(){8, 16, 24};
	        keywords.ForEach(i =>Shader.DisableKeyword("NUM_SLICES_UPTO_"+i) );
	        for(int i=0; i<keywords.Count; ++i){
	            bool lastIter =  i==(keywords.Count-1);
	            if(!lastIter && numUdim > keywords[i]){ continue; }
	            Shader.EnableKeyword("NUM_SLICES_UPTO_" + keywords[i]);
	            Shader.SetGlobalInt("_NumUDIM", numUdim); //Set both udims and slices, just in case.
	            Shader.SetGlobalInt("_NumSlices", numUdim);//Because sometimes our includes use either of them.
	            Shader.EnableKeyword("USING_TEXTURE_ARRAY");
	            break;
	        }
	        doStuff();
	        keywords.ForEach(i =>Shader.DisableKeyword("NUM_SLICES_UPTO_"+i) );
	        Shader.DisableKeyword("USING_TEXTURE_ARRAY");
	    }


	    static void Set_UdimCoords_ShaderArray( Vector4[] udimShaderLimits, Material mat=null, ComputeShader sh=null){
	        // NOTICE: Always using max size (24), because unity can't adjust count 
	        // (between 8 and 24) on the fly. Even if we were to use material-keywords and #pragma multi_compile.
	        Debug.Assert(udimShaderLimits.Length == 24);
	        mat?.SetVectorArray("_UV_toUdimIx", udimShaderLimits);
	        sh?.SetVectorArray("_UV_toUdimIx", udimShaderLimits);
	    }


	    public void ClearTheTextures(Color col, bool clearColor=true, bool clearDepth=true){
	        TextureTools_SPZ.ClearRenderTexture(texArray, col, clearColor, clearDepth);
	    }
    
	    public static void BlitMat_withAllUdims(RenderUdims src, RenderUdims dest, Material mat){
	        RenderUdims minUdims = dest;
	        if(src!=null){  minUdims =  src.UdimsCount<dest.UdimsCount?  src : dest;  }
	        SetNumUdims(minUdims, mat);
	        mat.SetInteger("_NumUDIM", src.UdimsCount);
	        mat.SetInteger("_NumSlices", src.UdimsCount);
	        Graphics.Blit(src.texArray,dest.texArray, mat);
	    }

	    public static void assertSameSize(params RenderUdims[] renderUdims){
	        #if UNITY_EDITOR
	        if (renderUdims.Length<2){ return; }
	        RenderUdims first = renderUdims[0];
	        bool allMatch  = renderUdims.All(udims => udims.width==first.width  
	                                              &&  udims.height==first.height  
	                                              &&  udims.UdimsCount==first.UdimsCount);
	        Debug.Assert(allMatch, "Textures were expected to have the same width and height!");
	        #endif
	    }

	    public RenderUdims_SL Save( string filepath_dataDir,  string prefix,  string genData_guid ){
	        Debug.Assert(texturesBelongToMe, "We should only save RenderUdims that have ownership of their textures");
	        var sl = new RenderUdims_SL();
	        sl.udim_coords = udims_sectors.ToList();
	        sl.textures = new List<string>();
	        SaveTex(filepath_dataDir, sl.textures,  prefix, genData_guid);
	        return sl;
	    }

	    public void Load( string filepath_dataDir,  RenderUdims_SL sl,  GraphicsFormat format,  FilterMode filter){
	        Dispose();
	        texturesBelongToMe = true;
	        udims_sectors = sl.udim_coords.ToList();
	        _udims_shaderCoords = Init_Udim_shaderCoords(udims_sectors);

	        List<Texture2D> tex2d_list = new List<Texture2D>();
	        sl.textures.ForEach( path => LoadTex(filepath_dataDir, path, format, filter, tex2d_list) );

	        RenderTexture texArr =  this.texArray;
	        TextureTools_SPZ.Texture2DList_to_TextureArray( ref texArr,  tex2d_list,  0,  format );
	        texArr.filterMode =  filter;
	        this.texArray =  texArr;

	        tex2d_list.ForEach( t=>Texture.DestroyImmediate(t) );
	    }


	    void SaveTex( string filepath_dataDir, List<string> addFilepathsHere, string prefix, string genData_guid){
	        if (texArray == null){
	            addFilepathsHere.Add("");
	            return;
	        }
	        List<Texture2D> texs2d = TextureTools_SPZ.TextureArray_to_Texture2DList(texArray);
	        for(int i=0; i<texs2d.Count; ++i){
	            string pathInDataFolder =  prefix + $"{i}_{genData_guid}.png";
	            ProjectSaveLoad_Helper.Save_Tex2D_To_DataFolder(texs2d[i], filepath_dataDir, pathInDataFolder);
	            addFilepathsHere.Add(pathInDataFolder);
	        }
	        texs2d.ForEach( t=>Texture.DestroyImmediate(t) );
	    }


	    void LoadTex( string filepath_dataDir,  string pathInDataFolder, 
	                  GraphicsFormat format,  FilterMode filter,  List<Texture2D> addHere){

	        if(string.IsNullOrEmpty(pathInDataFolder)){  texArray = null;  return;  }

	        Texture2D tex = ProjectSaveLoad_Helper.Load_Texture2D_from_DataFolder( filepath_dataDir,  pathInDataFolder, format, format );
	        addHere.Add(tex);
	    }

	}
}//end namespace
