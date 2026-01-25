using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


namespace spz {

	// Available in each GenData2D.
	// Contains textures, masks, etc, that are used when painting (fine-tuning) a certain texture-generation, on a 3D mesh.
	public class GenData_Masks
	{
	    public static int COLOR_BRUSH_RESOLUTION => Settings_MGR.instance.get_uv_brushPrecision_res();
	    public static int MASK_RESOLUTION => Settings_MGR.instance.get_uv_brushPrecision_res();

	    public static readonly GraphicsFormat colorBrushFormat = GraphicsFormat.R8G8B8A8_UNorm;
	    public static readonly GraphicsFormat masksFormat = GraphicsFormat.R8_UNorm;//R8 for very efficient mask texture
	    public static readonly GraphicsFormat visibilityFormat = GraphicsFormat.R8G8_UNorm;

	    public static readonly FilterMode colorBrushFilter = FilterMode.Bilinear;
	    public static readonly FilterMode masksFilter = FilterMode.Bilinear;
	    public static readonly FilterMode visibilityFilter = FilterMode.Bilinear;


	    public GenData2D genData { get; private set; }

	    //one entry per Camera POV
	    //single-channel texture, describes a black-and-white painted mask
	    //(user can use brush to fine-tune projections).
	    //Helps to reveal or conceal the texture on 3d object by painting it.
	    public List<RenderUdims> _ObjectUV_brushedMaskR8 { get; private set; } = null;

	    //one entry per Camera POV
	    //Where projection lands vs where it's invisible to projector camera.
	    //Essentially, it's depth of projector camera, just in UV space.
	    //One per POV of the projector camera.
	    //If non-zero, a texel is visible by the projection camera.
	    // R: With fade-effect applied to edges of model.
	    // G: without any fade effect. True (real visibility) of texel to the projector camera. 
	    //    Helps to identify front-facing reverse side of 3d models.
	    public List<RenderUdims> _ObjectUV_visibilityR8G8 { get; private set; } = null;
	    public int numPOV { get; private set; }


	    //if -1, masks will use standard resolution (1024x1024 etc)
	    //Useful if you want mask of a custom non-square size (for BGs, etc)
	    public GenData_Masks(GenData2D myGenData, int width=-1, int height=-1){
	        this.genData = myGenData;
	        _ObjectUV_brushedMaskR8 = new List<RenderUdims>();
	        _ObjectUV_visibilityR8G8 = new List<RenderUdims>();
	        this.numPOV = 0;
	        // backgrounds don't care about camera POVs nor about UDIMs. They always have just 1
	        bool onlyDefaultUDIM = myGenData.kind == GenerationData_Kind.TemporaryDummyNoPics ||
	                               myGenData.kind == GenerationData_Kind.SD_Backgrounds;

	        bool full_visibility = myGenData.kind == GenerationData_Kind.UvTextures_FromFile ||
	                               myGenData.kind == GenerationData_Kind.UvNormals_FromFile ||
	                               myGenData.kind == GenerationData_Kind.UvNormals_FromFile ||
	                               myGenData.kind == GenerationData_Kind.SD_Backgrounds;
        
	        Color visibilityCol = full_visibility? Color.white : Color.clear;

	        Vector2Int resolution =  new Vector2Int(width<=0? MASK_RESOLUTION : width,
	                                                height<=0? MASK_RESOLUTION : height);
	        if (onlyDefaultUDIM){
	            CreateUdims_maybe( 0, _ObjectUV_brushedMaskR8,  Color.white,  masksFormat, 
	                                masksFilter,  resolution,  onlyDefaultUDIM:true);
	            //skip the visibility because it's only needed for 3d objects.
	            this.numPOV = 1;
	            return;
	        }
	        for (int i=0; i<myGenData.povInfos.povs.Count; ++i){
	            // For masks, gray color (masks are packed into [0, 0.5] range, whith 0.5 reserved for overcoming invisibilities, etc.
	            // For visibility, zero alpha (important for delation later on, it undersds zero as empty space):
	            CreateUdims_maybe(i, _ObjectUV_brushedMaskR8, Color.gray, masksFormat, masksFilter, resolution);
	            CreateUdims_maybe(i, _ObjectUV_visibilityR8G8, visibilityCol, visibilityFormat, visibilityFilter, resolution);
	        }                                                                                    
	        this.numPOV = _ObjectUV_brushedMaskR8.Count(m => m != null);
	    }


	    void CreateUdims_maybe( int i,  List<RenderUdims> list,  Color clearColor, GraphicsFormat format,  FilterMode filter,
	                            Vector2Int resolution,  bool onlyDefaultUDIM=false){
	        if (genData.povInfos.povs[i].wasEnabled == false){ 
	            list.Add(null); // Adds rt or a null entry. Adding null so that it's easier
	            return;         // to 'map' the indexes in future, between pov and these textures.
	        }
	        IReadOnlyList<UDIM_Sector> udims = onlyDefaultUDIM? new List<UDIM_Sector>(){ new UDIM_Sector(0,0)}
	                                                          : ModelsHandler_3D.instance._allKnownUdims;
        
	        var renderUdims = new RenderUdims(udims, resolution, format, filter, clearColor);
	        list.Add(renderUdims);
	    }

	    public void Dispose(){
	        DisposeUdimsList(_ObjectUV_brushedMaskR8);
	        DisposeUdimsList(_ObjectUV_visibilityR8G8);
	    }

	    void DisposeUdimsList(List<RenderUdims> list){
	        if (list == null){ return; }
	        for(int i=0; i<list.Count; ++i){
	            RenderUdims udims = list[i];
	            udims?.Dispose();
	        }
	        list.Clear();
	    }

	    public GenData_Masks Clone(GenData2D genData_ofClone){
	        var clone = new GenData_Masks(genData_ofClone);
	        clone._ObjectUV_brushedMaskR8  = this._ObjectUV_brushedMaskR8.Select(u => u?.Clone()).ToList();
	        clone._ObjectUV_visibilityR8G8 = this._ObjectUV_visibilityR8G8.Select(u => u?.Clone()).ToList();
	        clone.numPOV = this.numPOV;
	        return clone;
	    }

	    public GenData_Masks_SL Save(StableProjectorz_SL spz){
	        var saveLoad = new GenData_Masks_SL();
	        saveLoad.objectUV_brushMasks = new List<RenderUdims_SL>();
	        saveLoad.objectUV_visibilities = new List<RenderUdims_SL>();

	        for (int i=0; i<_ObjectUV_brushedMaskR8.Count; i++){
	            if(i < _ObjectUV_brushedMaskR8.Count){ 
	                SaveUdims(spz, saveLoad.objectUV_brushMasks, "_uv_mask_", i, _ObjectUV_brushedMaskR8[i]);
	            }
	            if(i < _ObjectUV_visibilityR8G8.Count){ 
	                SaveUdims(spz, saveLoad.objectUV_visibilities, "_uv_visibil_", i, _ObjectUV_visibilityR8G8[i]);
	            }
	        }
	        return saveLoad;
	    }

	    void SaveUdims( StableProjectorz_SL spz, List<RenderUdims_SL> addHere, 
	                    string prefix,  int i,  RenderUdims udims ){
	        if (udims == null){
	            addHere.Add(null);
	            return;
	        }
	        var renderUdimsSL =  udims.Save( spz.filepath_dataDir,  prefix,  $"{i}_{genData.total_GUID}" );
	        addHere.Add(renderUdimsSL);
	    }

	    public void Load(StableProjectorz_SL spz, GenData_Masks_SL masksSL){
	        Dispose();//just in case.
	        _ObjectUV_brushedMaskR8 = LoadUdimsList(spz, masksSL.objectUV_brushMasks, masksFormat, masksFilter);
	        _ObjectUV_visibilityR8G8 = LoadUdimsList(spz, masksSL.objectUV_visibilities, visibilityFormat, visibilityFilter);
	        numPOV = _ObjectUV_brushedMaskR8.Count(m=>m!=null);
	    }

	    List<RenderUdims> LoadUdimsList(StableProjectorz_SL spz, List<RenderUdims_SL> slList, GraphicsFormat format, FilterMode filter)
	    {
	        return slList.Select(sl => {
	            if(sl == null){ return null; }
	            var udims = new RenderUdims();
	            udims.Load(spz.filepath_dataDir, sl, format, filter);
	            return udims;
	        }).ToList();
	    }
	}
}//end namespace
