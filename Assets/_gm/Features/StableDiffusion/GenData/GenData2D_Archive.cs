using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace spz {

	// Has 'GenData2D' for each "Generate" query.
	// Can find any GenData - bunch of arguments used during a particular generation (and resulting textures).
	// Stuff like depth textures, masks, parameters, camera position.
	public class GenData2D_Archive  : MonoBehaviour{
	    public static GenData2D_Archive instance { get; private set; } = null;

	    //key is a Globaly Unqiue Identifier. Stable Diffusion won't know this, this is for our use here in unity.
	    Dictionary<Guid, GenData2D> _internalGUID_to_genData =  new Dictionary<Guid, GenData2D>(512);
	    List<Guid> _GUIDs_ordered = new List<Guid>();
	    public Guid latestGeneration_GUID => _GUIDs_ordered.Count==0?  default : _GUIDs_ordered.LastOrDefault();

	    public static Action<GenData2D> OnWillGenerate { get; set; } = null;
	    public static Action<GenData2D> OnWillDispose_GenerationData { get; set; } = null;


	    // 'internal_guid' is a 'Globaly Unqiue Identifier'.
	    // Stable Diffusion won't know this, so this is for our use here in unity.
	    public void WillGenerate(GenData2D data){
	        _internalGUID_to_genData.Add( data.total_GUID,  data );
	        _GUIDs_ordered.Add( data.total_GUID );
	        OnWillGenerate?.Invoke(data);
	    }

	    public GenData2D GenerationGUID_toData(Guid guid){
	        GenData2D data;
	        _internalGUID_to_genData.TryGetValue(guid, out data);
	        return data;
	    }


	    public void OnTerminatedGeneration(GenData2D which){
	        if(which == null){ return; }
	        DisposeGenerationData(which.total_GUID );
	    }


	    public GenData2D Find_GenData_ofKind( GenerationData_Kind kind,  bool search_lastToFirst ){
	        if(_GUIDs_ordered.Count==0){ 
	            return null; 
	        }
	        int ix =  search_lastToFirst? _GUIDs_ordered.Count-1 : 0;
	        int to =  search_lastToFirst? -1 : _GUIDs_ordered.Count;
	        int dir = search_lastToFirst? -1 : 1;
	        while(ix!=to){
	            Guid guid = _GUIDs_ordered[ix];
	            GenData2D genDat = _internalGUID_to_genData[guid];
	            if (genDat.isBeingDisposed){ ix += dir; continue; }
	            if(genDat.kind == kind){ return genDat; }
	            ix += dir;
	        }
	        return null;
	    }


	    public List<GenData2D> FindAll_GenData_ofKind( GenerationData_Kind kind ){
	        List<GenData2D> list =  _internalGUID_to_genData.Select(kvp=>kvp.Value)
	                                        .Where(genDat=> genDat.kind == kind).ToList();
	        return list;
	    }

	    public void Save(StableProjectorz_SL here){
	        here.generations_MGR = new Generations2D_MGR_SL();
	        here.generations_MGR.guid_to_genData = new Dictionary<string, GenData2D_SL>();

	        foreach(var kvp in _internalGUID_to_genData){
	            Guid guid = kvp.Key;
	            GenData2D_SL genDat =  kvp.Value.Save(here);
	            here.generations_MGR.guid_to_genData.Add(guid.ToString(), genDat);
	        }
	        here.generations_MGR.GUIDs_ordered = _GUIDs_ordered.Select(g=>g.ToString()).ToList();
	        here.generations_MGR.latestGeneration_GUID = latestGeneration_GUID.ToString();
	    }

	    public void Load(StableProjectorz_SL spz){
	        //first forget everything. Go in reverse order, to prevent re-allocations:
	        for(int i=_GUIDs_ordered.Count-1; i>=0; --i){
	            Guid guid = _GUIDs_ordered[i];
	            DisposeGenerationData(guid);
	        }
	        //now, load:
	        foreach(var kvp in spz.generations_MGR.guid_to_genData){
	            Guid guid = new Guid(kvp.Key);
	            GenData2D_SL genSL = kvp.Value;

	            GenData2D genData = GenData2D.Make_via_Load(spz, genSL);
	            if(genData == null){ continue; }
	            _GUIDs_ordered.Add(guid);
	            _internalGUID_to_genData.Add(guid, genData);
	        }
	    }

	    public void OnAfter_AllLoaded(StableProjectorz_SL spz){
	        foreach(var kvp in _internalGUID_to_genData){
	            kvp.Value.OnAfter_AllLoaded(spz);
	        }
	    }
    

	    //for example, when you clicked Remove on every IconUI that were relevant to this generation.
	    public void DisposeGenerationData(Guid guid_of_generation){
	        GenData2D data = GenerationGUID_toData(guid_of_generation);
	        if(data == null){ return; }
	        data.BeforeDispose_internal();
	        OnWillDispose_GenerationData?.Invoke(data);
	        data.Dispose_internal();
	        _GUIDs_ordered.Remove(data.total_GUID);
	        _internalGUID_to_genData.Remove(guid_of_generation);
        
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();
	    }

	    public void Dispose_ALL_genData(){
	        List<Guid> guidKeys = _GUIDs_ordered.ToList();
	        foreach(Guid guid in guidKeys){
	            DisposeGenerationData(guid);
	        }
	    }

	    void Awake(){
	        if(instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;
	    }
	}



	public enum GenerationData_Kind{
	    Unknown,
	    TemporaryDummyNoPics,
	    SD_ProjTextures,//2D images that we "shine" onto 3d model from specific direction
	    SD_Backgrounds,//2D images used as a curtain behind a 3d model.
	    UvTextures_FromFile,//usual texture, to be wrapped around object. Loaded from user's directories.
	    UvPaintedBrush,//same as UvTextures, but needs additive blending shader, not usual alpha-blending.
	    UvNormals_FromFile, //texture to be wrapped around object.
	    BgNormals_FromFile, //texture to be shown as background
	    AmbientOcclusion, //shading texture, to be wrapped around object. We bake it in this app (not via StableDiffusion)
	}



	[Serializable]
	public class CameraPovInfo{
	    public readonly bool wasEnabled;//was the camera showing this angle, or it was hidden.
	    public readonly Vector3Serializable camera_pos;
	    public readonly QuaternionSerializable camera_rot;
	    public readonly float camera_fov;
	    public readonly Vector2Serializable perspectiveCenter01;//traditionally (0.5, 0.5) is middle of viewport.

	    public CameraPovInfo( bool wasEnabled, Vector3 camera_pos, Quaternion camera_rot, 
	                          float camera_fov, Vector2 perspectiveCenter01 ){
	        this.wasEnabled = wasEnabled;
	        this.camera_pos = camera_pos;
	        this.camera_rot = camera_rot;  
	        this.camera_fov = camera_fov;
	        this.perspectiveCenter01 = perspectiveCenter01;
	    }

	    public CameraPovInfo Clone(bool wasEnabled_override){
	        return new CameraPovInfo(wasEnabled_override, camera_pos, camera_rot, camera_fov, perspectiveCenter01 );
	    }
	}


	[System.Serializable]
	public class CameraPovInfos{
	    public readonly int numEnabled;//only those used.
	    public readonly int numAll;//includes non-enabled.
	    public readonly List<CameraPovInfo> povs; //this same generation might capture several view directions at once.

	    public CameraPovInfos(List<CameraPovInfo> povs){ 
	        this.povs = povs;
	        this.numEnabled = povs.Count(p=>p.wasEnabled);
	        this.numAll = povs.Count();
	    }
	    public CameraPovInfo get_Nth_active_pov(int n){
	        int ix = 0;
	        for(int i=0; i<povs.Count; ++i){ 
	            if(povs[i].wasEnabled==false){ continue; }
	            if(ix==n){ return povs[i]; }
	            ix++;
	        }
	        return null;
	    }
	}


	// A block of data, usually specific to some particular generation-request that we made in the past.
	// However, sometimes might actually not be related to an actual image synthesis,
	// and instead might contain textures loaded manually from file, by the user.
	public class GenData2D {
	    GenData2D_SL _genSL = null;//used only during loading
	    public bool isBeingDisposed { get; private set; } = false;
	    public GenerationData_Kind kind { get; private set; }

	    public Guid total_GUID { get; private set; } = default;//Stable Diffusion won't know this, this is for our use here in unity.
	    public IReadOnlyList<Guid> textureGuidsOrdered => _generatedTextures?.orderedGuids;
	    public bool use_many_icons => _generatedTextures.use_many_icons;//should all my textures be shown by several UI-elements or by single.

	    public Vector3 _selected3dModel_pos { get; private set; }

	    public CameraPovInfos povInfos { get; private set; }
	    public ProjectorCamera _projCamera { get; private set; }//can be null (if we are for AO or 2Dbackground)



	    public SD_GenRequestArgs_byproducts _byproductsOfRequest { get; private set; } = null;

	    GenData_ResultTextures _generatedTextures = null;//will be receiving the generated textures.
	    public GenData_Masks _masking_utils { get; private set; } = null;


	    public SD_txt2img_payload txt2img_req { get; private set; }= null; //what was used to make the inital request.
	    public SD_img2img_payload img2img_req { get; private set; } = null; //what was used to make the inital request.
	    public SD_img2extra_payload ext_req { get; private set; } = null;//what was use dto make the intial request (upscale, etc).

	    public int n_iter =>  txt2img_req?.n_iter ??  img2img_req?.n_iter ??  1;
	    public int batch_size =>  txt2img_req?.batch_size ?? img2img_req?.batch_size 
	                             ?? _generatedTextures?.Count ?? 1;
	    public int n_total =>  n_iter * batch_size;

	    public Vector3Int textureSize(bool withUpscale){
	        if(txt2img_req!=null){ 
	            float w =  (withUpscale && txt2img_req.enable_hr)?  txt2img_req.width *txt2img_req.hr_scale  : txt2img_req.width;
	            float h =  (withUpscale && txt2img_req.enable_hr)?  txt2img_req.height*txt2img_req.hr_scale : txt2img_req.height;
	            return new Vector3Int(Mathf.RoundToInt(w), Mathf.RoundToInt(h), 1); 
	        }
	        if(img2img_req!=null){ //if upscale was used for img2img, it's already "applied" into the width and height:
	            var im  = img2img_req;
	            float w = (withUpscale && im.enable_hr_spz)? im.width : Mathf.Round(im.width / im.hr_scale_spz);
	            float h = (withUpscale && im.enable_hr_spz)? im.height : Mathf.Round(im.height / im.hr_scale_spz);
	            return new Vector3Int(Mathf.RoundToInt(w), Mathf.RoundToInt(h), 1); 
	        }
	        if (ext_req != null){
	            return new Vector3Int(ext_req.rslt_imageWidths, ext_req.rslt_imageHeights, 1); 
	        }
	        if(_generatedTextures != null && _generatedTextures.Count>0){
	            return _generatedTextures.widthHeight();
	        }
	        return Vector3Int.zero;
	    }

	    public int seed(){
	        if(txt2img_req != null){ return txt2img_req.seed; }
	        if(img2img_req != null){ return img2img_req.seed; }
	        return -1;
	    }

	    public float camera_aspect(){
	        if(txt2img_req != null){ return txt2img_req.width/(float)txt2img_req.height; }
	        if(img2img_req != null){ return img2img_req.width/(float)img2img_req.height; }
	        if(ext_req != null){ return ext_req.rslt_imageWidths / (float)ext_req.rslt_imageHeights; }
	        if(_generatedTextures!=null && _generatedTextures.Count > 0){
	            Vector3Int widthHeight = _generatedTextures.widthHeight();
	            return widthHeight.x / (float)widthHeight.y;
	        }
	        return 1;
	    }
    
    
	    //ctor, used when expecting to receive images soon, from StableDiffusion.
	    public GenData2D( GenerationData_Kind kind, bool use_many_icons, Vector3 selected3dModel_pos,
	                      List<CameraPovInfo> cameraPOVs, ProjectorCamera projCamera,
	                      SD_txt2img_payload t2i_req,  SD_img2img_payload i2i_req,  SD_img2extra_payload ext_req,
	                      SD_GenRequestArgs_byproducts byproducts = null ){
        
	        InitVars(kind, selected3dModel_pos, cameraPOVs, projCamera, byproducts);

	        Debug.Assert(t2i_req==null || i2i_req==null, 
	                     "only 'SD_txt2img_payload' or 'SD_img2img_payload' please, not both");
	        this.txt2img_req = t2i_req;
	        this.img2img_req = i2i_req;
	        this.ext_req = ext_req;

	        var begin_with_guids =  new List<Guid>(n_total);
	        for(int i=0; i<n_total; ++i){  begin_with_guids.Add(Guid.NewGuid());  }

	        _generatedTextures = new GenData_ResultTextures( this, use_many_icons, begin_with_guids);
	        _masking_utils = new GenData_Masks(this);
	    }


	    public GenData2D( GenerationData_Kind kind, bool use_many_icons, Vector3 selected3dModel_pos, 
	                      List<CameraPovInfo> cameraPOVs,  ProjectorCamera projCamera=null,
	                      SD_GenRequestArgs_byproducts byproducts=null, 
	                      int masks_width=-1, int masks_height=-1 ){//if -1, masks will use standard resolution (1024x1024 etc)
	                                                                //Useful if you want mask of a custom non-square size (for BGs, etc)
	        InitVars(kind, selected3dModel_pos,  cameraPOVs, projCamera, byproducts);

	        if (kind != GenerationData_Kind.TemporaryDummyNoPics){ 
	            _generatedTextures = new GenData_ResultTextures( this, use_many_icons);
	            _masking_utils = new GenData_Masks(this, masks_width, masks_height);
	        }
	        if(kind==GenerationData_Kind.SD_ProjTextures){  Debug.Assert(projCamera!=null); }
	        if(kind!=GenerationData_Kind.SD_ProjTextures){  Debug.Assert(projCamera==null); }
	    }


	    public GenData2D(GenData2D other, out ProjectorCamera projCam_initMeLater_){
	        //Don't copy the GUID, we'll make a new one.

	        ProjectorCamera projCam = null;
	        if (other._projCamera != null){  projCam =  ProjectorCameras_MGR.instance.Spawn_ProjCamera(); }
	        //don't init the ProjCamera yet. The Art List 2D needs to create an icon, assigned to the ProjCamera.

	        var byproducts = other._byproductsOfRequest?.Clone();

	        InitVars( other.kind, other._selected3dModel_pos,
	                  other.povInfos.povs, projCam, byproducts );

	        projCam_initMeLater_ = projCam;

	        this.txt2img_req = other.txt2img_req?.Clone();//shallow copy.
	        this.img2img_req = other.img2img_req?.Clone();
	        this.ext_req = other.ext_req?.Clone();
	        this._generatedTextures = other._generatedTextures?.Clone( genData_ofClone:this );
	        this._masking_utils = other._masking_utils?.Clone( genData_ofClone:this );
	        this.isBeingDisposed = other.isBeingDisposed;
	    }



	    //private ctor, because only used internally, during Load(), when restoring our saved project.
	    GenData2D( StableProjectorz_SL spz, GenData2D_SL genSL, out bool success_ ){
	        success_ = true;
	        object parseKind;
	        success_ &= Enum.TryParse(typeof(GenerationData_Kind), genSL.kind, out parseKind);
	        this.kind = success_? (GenerationData_Kind)parseKind : GenerationData_Kind.Unknown;

	        this.total_GUID = new Guid(genSL.internal_GUID);
	        this._selected3dModel_pos = genSL.selected3dModel_pos;
	        this.povInfos = new CameraPovInfos( genSL.camerasPOVs );

	        //NOTICE: projector camera will be assigned later, during OnAfter_AllLoaded()

	        this.txt2img_req = genSL.txt2img_req;
	        this.img2img_req = genSL.img2img_req;
	        this.ext_req = genSL.ext_req;

	        if (genSL.byproductsOfRequest != null){
	            this._byproductsOfRequest = new SD_GenRequestArgs_byproducts();
	            this._byproductsOfRequest.Load(spz, genSL.byproductsOfRequest);
	        }

	        if (genSL.generatedTextures != null) { 
	            _generatedTextures = new GenData_ResultTextures( this, genSL.generatedTextures.use_many_icons);
	            _generatedTextures.Load(spz, genSL.generatedTextures);
	        }

	        if (genSL.masking_utils != null){
	            _masking_utils = new GenData_Masks(this);
	            _masking_utils.Load( spz,  genSL.masking_utils );
	        }
	        _genSL = genSL;
	    }


	    void InitVars( GenerationData_Kind kind,  Vector3 selected3dModel_pos,
	                  List<CameraPovInfo> cameraPOVs, ProjectorCamera projCamera,  
	                  SD_GenRequestArgs_byproducts byproducts=null ){
	        this.kind = kind;
	        this.total_GUID = Guid.NewGuid();
	        this._selected3dModel_pos = selected3dModel_pos;
	        this.povInfos = new CameraPovInfos(cameraPOVs);
	        //don't init the ProjCamera yet. The Art List 2D needs to create an icon, assigned to the ProjCamera
	        this._projCamera = projCamera;
	        this._byproductsOfRequest = byproducts;
	    }


	    public GenData_TextureRef GetTexture_ref(Guid textureGuid)
	        => _generatedTextures.GetTexture_ref(textureGuid);

	    public GenData_TextureRef GetTexture_ref0()
	        => _generatedTextures.GetTexture_ref(textureGuidsOrdered[0]);

	    //very expensive, might make a copy of all textures. Usually invoked when saving textures to disk
	    //destroyWhenDone_: tells if textures are a copy and have to be destroyed, or are a reference.
	    public Dictionary<Texture2D,UDIM_Sector> GetTextures2D_expensive(out bool destroyWhenDone_)
	        => _generatedTextures.GetTextures2D_expensive(out destroyWhenDone_);

    
	    //to be notified every time the texture gets refreshed (useful if we are still generating)
	    public void Subscribe_for_TextureUpdates( IReadOnlyList<Guid> textureGuids,  Action<GenData_TextureRef> cb )
	        => textureGuids.ForEach(tguid => _generatedTextures.Subscribe_for_TextureUpdates(tguid, cb));
    
	    public void Unsubscribe_from_textureUpdates( IReadOnlyList<Guid> textureGuids,  Action<GenData_TextureRef> cb )
	        => textureGuids.ForEach(tguid => _generatedTextures.Unsubscribe_from_textureUpdates(tguid, cb));


	    public void Update_PendingImages(int iterIx, string combinedTex_base64)//textures in a specific iteration.
	        => _generatedTextures.Update_PendingImages(iterIx, batch_size, combinedTex_base64);

	    public void Complete_PendingImages( string[] base64_images )//textures in all iterations.
	        => _generatedTextures.Finish_PendingImages(base64_images);


	    public void AssignTextures_Manual( RenderTexture textureArray_withoutOwner, 
	                                       IReadOnlyList<UDIM_Sector> udims=null ){
	        List<Guid> slicesGuids;
	        Dictionary<Guid,UDIM_Sector> udimsDict;
	        make_textures_guids(out slicesGuids, out udimsDict, textureArray_withoutOwner.volumeDepth, udims);
	        _generatedTextures.Assign_TextureArray_Direct(textureArray_withoutOwner, slicesGuids, udimsDict);
	    }


	    public void AssignTextures_Manual( List<Texture2D> textures_withoutOwner,
	                                       IReadOnlyList<UDIM_Sector> udims=null ){
	        bool cleanupTextures;
	        if (is_use_TextureArray(kind)){
	            RenderTexture texArray = null;
	            TextureTools_SPZ.Texture2DList_to_TextureArray(ref texArray, textures_withoutOwner);
	            AssignTextures_Manual(texArray, udims);
	            cleanupTextures = true; //because will use the texArray.
	        }else {
	            Dictionary<Guid,Texture2D> texturesDict;
	            Dictionary<Guid,UDIM_Sector> udimsDict;
	            make_textures_guids(out texturesDict, out udimsDict, textures_withoutOwner, udims);
	            _generatedTextures.AssignTextures_Direct(clearAllExisting:true, texturesDict, udimsDict );
	            cleanupTextures = false;//dont't destroy the textures, they are owned by the genData now.
	        }
	        if(cleanupTextures){  textures_withoutOwner.ForEach( t=>Texture.DestroyImmediate(t) ); }
	    }


	    public void AssignTextures_Manual( bool clearAllExisting, 
	                                       IReadOnlyList<Guid> orderedGuids,
	                                       Dictionary<Guid,Texture2D> textures_withoutOwner,
	                                       Dictionary<Guid,UDIM_Sector> udims ){ 
	        bool cleanupTextures;
	        if (is_use_TextureArray(kind)){
	            RenderTexture texArray = null;
	            TextureTools_SPZ.Texture2DList_to_TextureArray(ref texArray, textures_withoutOwner.Values.ToList());
	            _generatedTextures.Assign_TextureArray_Direct(texArray, orderedGuids, udims);
	            cleanupTextures = true; //because will use the texArray.
	        }else {
	            _generatedTextures.AssignTextures_Direct(clearAllExisting, textures_withoutOwner, udims );
	            cleanupTextures = false;//dont't destroy the textures, they are owned by the genData now.
	        }
	        if(cleanupTextures){  foreach(var kvp in textures_withoutOwner){ Texture.DestroyImmediate(kvp.Value); } }
	    }


	    // Usually invoked at the end of loading custom images (from file), not when generating.
	    // Causes any kind of listeners to realize that this class has textures,
	    // and that no more texture-render-updates are expected.
	    public void ForceEvent_OnGenerationCompleted() => _generatedTextures.ForceEvent_OnGenerationCompleted();
    

	    public void DisposeTextures( IReadOnlyList<Guid> textureGuids)
	        => textureGuids.ForEach( tguid=>_generatedTextures?.DisposeTexture(tguid) );


	    public void ClearAllTextures_ToColor(Color color){
	        IReadOnlyList<Guid> texGuids = _generatedTextures.orderedGuids;
	        var texturesDict = new Dictionary<Guid,Texture2D>();

	        RenderTexture rt = new RenderTexture(4,4,0, GraphicsFormat.R8G8B8A8_UNorm);
	        TextureTools_SPZ.ClearRenderTexture(rt, color);

	        for(int i=0; i<texGuids.Count; ++i){
	            Guid texGuid  = texGuids[i];
	            Texture2D tex = TextureTools_SPZ.RenderTextureToTexture2D(rt);
	            texturesDict.Add(texGuid, TextureTools_SPZ.RenderTextureToTexture2D(rt));
	        }
	        _generatedTextures.AssignTextures_Direct(clearAllExisting:false,  texturesDict,  udims:null );
	        Texture.DestroyImmediate(rt);
	        //NOTICE: don't destroy the texture2D objects, now they are owned by _generatedTexture.
	    }


	    // True: the textures be arranged into a RenderTexture
	    //       that is a TextureArray (contains slices of images, like a stack).
	    // False: your images should be in a List<>.
	    static bool is_use_TextureArray(GenerationData_Kind kind){
	        bool isTextureArray =  kind!=GenerationData_Kind.SD_ProjTextures 
	                            && kind!=GenerationData_Kind.SD_Backgrounds && kind!=GenerationData_Kind.BgNormals_FromFile;
	        return isTextureArray;
	    }


	    static void make_textures_guids( out Dictionary<Guid,Texture2D> texs_, //outputs this
	                                     out Dictionary<Guid,UDIM_Sector> udims_,//and this (but can remain null)
	                                     IReadOnlyList<Texture2D> texs,  IReadOnlyList<UDIM_Sector> udims=null ){
	        texs_  =  new Dictionary<Guid,Texture2D>();
	        udims_ =  udims!=null ?  new Dictionary<Guid,UDIM_Sector>() : null;
	        for(int i=0; i<texs.Count; ++i){
	            Guid g = Guid.NewGuid();
	            texs_.Add(g, texs[i]);
	            udims_?.Add(g, udims[i]);
	        }
	    }

	    static void make_textures_guids( out List<Guid> sliceGuids_, //outputs this
	                                     out Dictionary<Guid,UDIM_Sector> udims_,//and this (but can remain null)
	                                     int num,  IReadOnlyList<UDIM_Sector> udims=null ){
	        sliceGuids_=  new List<Guid>();//list preserves order.
	        udims_     =  udims!=null ?  new Dictionary<Guid,UDIM_Sector>() : null;
	        for(int i=0; i<num; ++i){
	            Guid g = Guid.NewGuid();
	            sliceGuids_.Add(g);
	            udims_?.Add(g, udims[i]);
	        }
	    }


    
	    public void BeforeDispose_internal() => isBeingDisposed = true;

	    public void Dispose_internal(){
	        if(ProjectorCameras_MGR.instance != null){  ProjectorCameras_MGR.instance.Destroy_ProjCamera(_projCamera);  }
	        _byproductsOfRequest?.Dispose();
	        _generatedTextures?.Dispose();
	        _masking_utils?.Dispose();
	    }

	    public GenData2D_SL Save(StableProjectorz_SL spz){
	        var genData = new GenData2D_SL();
	        genData.internal_GUID = total_GUID.ToString();
	        genData.kind = kind.ToString();
	        genData.selected3dModel_pos = _selected3dModel_pos;
	        genData.camerasPOVs = povInfos.povs.ToList();
	        genData._projCamera_ix = ProjectorCameras_MGR.instance.projCameraIx(_projCamera);
	        genData.byproductsOfRequest = _byproductsOfRequest?.Save(spz, total_GUID);
	        genData.txt2img_req = txt2img_req;
	        genData.img2img_req = img2img_req;
	        genData.ext_req = ext_req;
	        genData.generatedTextures = _generatedTextures?.Save(spz);
	        genData.masking_utils =  _masking_utils?.Save(spz);
	        genData.n_iter = n_iter;
	        genData.batch_size = batch_size;
	        return genData;
	    }

	    public static GenData2D Make_via_Load( StableProjectorz_SL spz, GenData2D_SL genSL ){
	        var genData = new GenData2D(spz, genSL, out bool isSuccess);
	        if (!isSuccess){
	            genData.BeforeDispose_internal();
	            genData.Dispose_internal();
	        }
	        return isSuccess? genData : null;
	    }

	    public void OnAfter_AllLoaded( StableProjectorz_SL spz ){
	        _projCamera = ProjectorCameras_MGR.instance.ix_toProjCam( _genSL._projCamera_ix );
	        _genSL = null;
	    }
	}
}//end namespace
