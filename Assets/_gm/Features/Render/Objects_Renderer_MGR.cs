using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace spz {

	// Schedules the order of rendering, such as applying uv texture onto 3d model.
	// Asks the projection manager to apply projections onto 3d model,
	// then applies ambient occlusion from the AO-manager, etc.
	public class Objects_Renderer_MGR : MonoBehaviour {
	    public static Objects_Renderer_MGR instance { get; private set; }

	    [SerializeField] VisualizeFinalMat_Helper _finalMat_Helper;
    
	    [Space(10)]
	    [SerializeField] Shader _blit_UV_tex_shader;//with the UV-mask, allowing users to conceal certain parts of the texture.
	    [SerializeField] Shader _blit_UV_brush_shader;//with the UV-mask, but uses a different blending (additive) that brushed colors need.
	    Material _blit_UV_tex_mat;
	    Material _blit_UV_brush_mat;

	    [SerializeField] Shader _color_UV_chunks_shader;//ran at the begining of frame, to give "base" coloring to UV islands/chunks
	    Material _colorUVchunks_mat = null;


	    public bool _skip_AO_blit { get; set; } = false;

	    // One texture per UDIM.
	    // We clear then colect results of projections into this texture. 
	    // It's a texture that's "wrapped around the objects", kinda like lightmap or a usual uv-texture.
	    // NOTICE: render texture will have greater resolution than genData's screenspace-2D-art resolution.
	    // That's because this render texture is wrapped around object, and will receive result of projections.
	    // It must also be large enough to maintain any preceding projections from other cameras, even if their res is small.
	    // Also, this texture might be saved to file eventually.
	    RenderUdims _accumulation_uv_RT = new RenderUdims();
	    RenderUdims _uvChunks_RT = new RenderUdims();
	    bool _render_uvChunks_asap = false;//if game just started, singletones might be be ready for rendering. So we use this flag.

	    int _numObjectsEverImported = 0;

	    public void ReRenderAll_soon() => _renderAll_ASAP = true;
	    bool _renderAll_ASAP = false;

	    public void EquipMaterial_on_ALL(Material matBelongsToSomeone)
	        => _finalMat_Helper.EquipMaterial_on_ALL(matBelongsToSomeone);

	    public void EquipMaterial_on_Specific( IReadOnlyList<SD_3D_Mesh> onTheseMeshes,  Material mat )
	        => _finalMat_Helper.EquipMaterial_on_Specific(onTheseMeshes, mat);

	    public void ShowFinalMat_on_ALL(RenderUdims finalTextureColor)
	        => _finalMat_Helper.ShowFinalMat_on_ALL(finalTextureColor);

	    public void TemporaryPreventWireframe_onSelected(bool isPreventWireframe)
	        => _finalMat_Helper.TemporaryPreventWireframe_onSelected(isPreventWireframe);


	    // You can only reference this texture because it doesn't belong to you.
	    // It might be destroyed at any moment so verify every frame if you keep using it.
	    public RenderUdims accumulationTextures_ref() => _accumulation_uv_RT;
	    public RenderUdims chunksTexture_ref(){ return _uvChunks_RT; }


	    public void Resize_AccumulationTexture(int texSize=1024){
        
	        if(ModelsHandler_3D.instance == null){ return; }//can happen when the program just launched.

	        _accumulation_uv_RT.Dispose();
	        _uvChunks_RT.Dispose();
	        IReadOnlyList<UDIM_Sector> allUdims = ModelsHandler_3D.instance._allKnownUdims;

	        _accumulation_uv_RT = new RenderUdims(allUdims, Vector2Int.one*texSize, 
	                                              GraphicsFormat.R8G8B8A8_UNorm, FilterMode.Bilinear, Color.clear);
	        _uvChunks_RT = new RenderUdims(allUdims, Vector2Int.one*texSize, 
	                                       GraphicsFormat.R8_UNorm, FilterMode.Point, Color.clear);
	        _render_uvChunks_asap = true;
	        ReRenderAll_soon();
	    }

        
	    void OnUpdate(){
	        if(ModelsHandler_3D.instance==null){ return; }//scenes are probably still loading.
	        if(AmbientOcclusion_Baker.instance==null){ return; }

	        //baker will be visualizing material while baking. Also, not projecting to save performance, while baking.
	        if(AmbientOcclusion_Baker.instance.isGeneratingAO){  OnUpdateComplete(); return; }
	        if(ModelsHandler_3D.instance._isImportingModel){  OnUpdateComplete(); return;  }
	        if(ModelsHandler_3D.instance._allKnownUdims.Count==0){ OnUpdateComplete(); return; }

	        // proceeding only if the UV texture is outtdated and everything needs to be re-rendered.
	        // Or if we are in the editing mode (to show the multiprojection brush preview, etc)
	        bool renderNow =  _renderAll_ASAP;
	             renderNow |= MultiView_Ribbon_UI.instance._isEditingMode;

	        if(!renderNow){ OnUpdateComplete(); return; }
	        _renderAll_ASAP = false;

	        _uvChunks_RT.Set_FilterMode( SceneResolution_MGR.resultTexFilterMode );

	        ModelsHandler_3D.instance.DoForAllMeshes_EvenIfHidden(()=>{ 
	            RenderUVChunksTex_maybe();
	            ApplyStartingColor();
	        });
	        ProcessMeshes();
	        AntiSeams_maybe();
	        OnUpdateComplete();
	    }

    
	    void OnUpdateComplete(){
	        ProjectorCameras_MGR.instance.HighlightProjCamera(null);
	        _finalMat_Helper.ShowFinalMat_on_ALL(_accumulation_uv_RT);
	    }


	    // Renders UV-texture (8-bit, single channel format), where chunks are 1, and empty space between them is 0.
	    // Will render only if '_render_uvChunks_asap' is true. (when we imported 3d model, or when resized the uv-chunks texture).
	    // Otherwise, this texture remains the same. 
	    void RenderUVChunksTex_maybe(){
	        if(_render_uvChunks_asap == false){ return; }
	        _render_uvChunks_asap = false;

	        _uvChunks_RT.ClearTheTextures(new Color(0,0,0,0));//Alpha is 0 (space between uv chunks needs alpha zero).
	        _colorUVchunks_mat.SetColor("_COL_UVCH_Color", new Color(1,1,1,1));//1 important for dilation of these uv chunks later on
	        _colorUVchunks_mat.DisableKeyword("VERTEX_COLORS");

	        RenderUdims.SetNumUdims(_uvChunks_RT, _colorUVchunks_mat);
	        _finalMat_Helper.EquipMaterial_on_ALL(_colorUVchunks_mat);

	        // it's important NOT to frustum cull: even if camera is looking at the object, remember that we are going to render into UVs.
	        // This would likely cause the camera to ignore the object.
	        UserCameras_MGR.instance._curr_viewCamera.RenderImmediate_Arr( _uvChunks_RT.texArray, ignore_nonSelected_meshes:false,
	                                                                       _colorUVchunks_mat,  useClearingColor:false,  Color.clear,  
	                                                                       dontFrustumCull: true);
	    }


	    void ApplyStartingColor(){
	        _accumulation_uv_RT.ClearTheTextures(Color.clear);

	        Color startingColor =Color.clear; // alpha zero (users might want no black background during export)

	        bool hasAO = null != GenData2D_Archive.instance.Find_GenData_ofKind(GenerationData_Kind.AmbientOcclusion, search_lastToFirst: true);
	        if (hasAO && !_skip_AO_blit && ProjectorCameras_MGR.instance._showOrderOfProjections == false){
	            startingColor = Color.white;
	            startingColor.a = 1;//Notice, the uv islands/chunks will start with alpha 1.
	        }
	        _colorUVchunks_mat.SetColor("_COL_UVCH_Color", startingColor);

	        ToggleVertexColors();

	        RenderUdims.SetNumUdims(_accumulation_uv_RT, _colorUVchunks_mat);

	        // it's important NOT to frustum cull: even if camera is looking at the object, remember that we are going to render into UVs.
	        // This would likely cause the camera to ignore the object.
	        UserCameras_MGR.instance._curr_viewCamera.RenderImmediate_Arr( _accumulation_uv_RT.texArray,  ignore_nonSelected_meshes: false,
	                                                                       _colorUVchunks_mat,  useClearingColor:false,  Color.clear,
	                                                                       dontFrustumCull:true);
	    }


	    void ToggleVertexColors(bool forceOff=false){
	        bool showVertColors =  ModelsHandler_3D_UI.instance._showVertexColors_on3d;
	             showVertColors &= _numObjectsEverImported > 1;//for some reason default Dungeon door shows as White vertex colors, though only after Build.
	                                                           //Even though the door has no vertex colors. So, need to disable it manually.
	             showVertColors &= !forceOff;
	        TextureTools_SPZ.SetKeyword_Material(_colorUVchunks_mat, "VERTEX_COLORS", showVertColors);
	    }


	    void ProcessMeshes(){//hide all meshes, every camera will unhide/hide those it needs, when rendering:
	        List<SD_3D_Mesh> nothing = new List<SD_3D_Mesh>();
	        ModelsHandler_3D.instance.DoForIsolatedMeshes( isolateAndEnable:nothing,  DoStuff );

	        void DoStuff(){ 
	            IconUI latest_AO_icon = null;

	            cycle_through_generations(onlyProjections:false, out latest_AO_icon);

	            ProjectorCameras_MGR.instance.HighlightProjCamera_maybe(_accumulation_uv_RT);
            
	            Apply_InpaintSketch_ColorLayer();
	            ApplyAmbientOcclusion(latest_AO_icon);
	        }
	    }


	    void cycle_through_generations(bool onlyProjections, out IconUI latest_AO_icon_){
	        latest_AO_icon_ = null;
	        //obtain generation ids as they appear in the ui-grid (maybe user did drag-and-drop rearrangements)
	        List<Guid> guidsOrdered_ref = Art2D_IconsUI_List.instance.guid_ordered_in_grid_refDontAlter();
	        bool anyIconGroup_asSolo    = Art2D_IconsUI_List.instance.Any_IconGroup_withSoloFlag();

	        int cnt = guidsOrdered_ref.Count;
	        for(int ix=0; ix<cnt; ++ix){
	            Guid guid   = guidsOrdered_ref[ix];
	            IconUI icon = Art2D_IconsUI_List.instance.GetIcon_of_GenerationGroup(guid, ix_in_generation:-1, justGetChosenIcon:true);
	            GenData2D genData =  icon._genData;

	            //Maybe skip this icon's projection, if it's not from that group which wans to be shown 'on its own':
	            if(anyIconGroup_asSolo  &&  icon._myIconGroup.showMyIcons_as_solo==false){ continue;}
	            if(icon._myIconGroup.hideMyIcons_please){ continue; }

	            if(onlyProjections  &&  genData.kind != GenerationData_Kind.SD_ProjTextures){ continue; }

	            switch (genData.kind){
	                case GenerationData_Kind.UvTextures_FromFile: ApplyUvTexture(genData, _blit_UV_tex_mat);  break;
	                case GenerationData_Kind.UvPaintedBrush:     ApplyUvTexture(genData, _blit_UV_brush_mat);  break;
	                case GenerationData_Kind.SD_ProjTextures:   RenderProjection(genData._projCamera, ix);  break;
	                case GenerationData_Kind.AmbientOcclusion:  latest_AO_icon_ = icon;  break;
	            }
	        }//end for
	    }


	    void ApplyUvTexture( GenData2D genData, Material blit_mat ){
	        GenData_TextureRef texRef = genData.GetTexture_ref0();
	        GenData_Masks genMasks    = genData._masking_utils;

	        Debug.Assert( texRef.texArray != null  &&  texRef.texArray.dimension == TextureDimension.Tex2DArray,
	                     "expected array for UV texture, (for udim support)");
	        blit_mat.SetTexture("_uvMask", genMasks._ObjectUV_brushedMaskR8[0].texArray );

	        RenderUdims.SetNumUdims(true, _accumulation_uv_RT.texArray.volumeDepth,  blit_mat,  sh:null);
	        TextureTools_SPZ.Blit(texRef.texArray, _accumulation_uv_RT.texArray, blit_mat);
	    }


	    void RenderProjection( ProjectorCamera projCam, int guidsSorted_ix){
	        ProjectorCameras_MGR.instance.RenderProjCamera( projCam, guidsSorted_ix, _accumulation_uv_RT);
	    }


	    void Apply_InpaintSketch_ColorLayer(){
	        if(MainViewport_UI.instance.showing != MainViewport_UI.Showing.UsualView){ return; }
	        if(WorkflowRibbon_UI.instance.allowed_to_showBrushMask() == false){ return; }
	        Inpaint_MaskPainter.instance.ApplyColorLayer_To_UV_Textures( _accumulation_uv_RT );
	    }


	    void ApplyAmbientOcclusion(IconUI fromIcon){
	        if(_skip_AO_blit){ return; }
	        if(fromIcon == null){ return; }
	        AmbientOcclusion_Baker.instance.Blend_AO_into_some_RT( fromIcon, _accumulation_uv_RT );
	    }

	    void AntiSeams_maybe(){
	        if(TextureDilation_MGR.instance ==null){ return; }

	        int numUDIMsTotal = _accumulation_uv_RT.UdimsCount;
	        if(numUDIMsTotal==0){ return; }
	        // we will be hiding seams by dilating the final texture (once all projections were accumulated during a frame).
	        // This allows us to hide places where the uv-chunks/island
	        var dilationArg = new DilationArg(_accumulation_uv_RT.texArray, 5, DilateByChannel.Use_Separate_UVChunksR8, null);//used to be 2px, requested to be 5, by user 'obsc', discord 25/04/2024

	        RenderUdims chunksCpy = _uvChunks_RT.Clone();
	        dilationArg.separate_UVchunks_R8_WillAlter = chunksCpy.texArray;
	        dilationArg.isRunInstantly = true;
	        dilationArg.findUVchunkBorders_thenBlurThem = false;
	        TextureDilation_MGR.instance.Dillate(dilationArg);
	        chunksCpy.Dispose();
	    }

    

	    void On_3dObject_Imported(GameObject rootGO){
	        _render_uvChunks_asap = true;
	        _numObjectsEverImported++;
	        Resize_AccumulationTexture( SceneResolution_MGR.resultTexQuality);//num udims might have changed.
	    }


	    void Awake(){
	        if(instance != null){  DestroyImmediate(this.gameObject); return;  }
	        instance = this;
	        _colorUVchunks_mat = new Material(_color_UV_chunks_shader);
	        _blit_UV_tex_mat   = new Material(_blit_UV_tex_shader);
	        _blit_UV_brush_mat = new Material(_blit_UV_brush_shader);
	        ModelsHandler_3D.Act_onImported += On_3dObject_Imported;
	        Update_callbacks_MGR.objectsRender += OnUpdate;
	        ReRenderAll_soon();
	    }

	    void Start(){
	        Resize_AccumulationTexture( SceneResolution_MGR.resultTexQuality );
	    }


	    void OnDestroy(){
	        if(_colorUVchunks_mat != null){ DestroyImmediate(_colorUVchunks_mat); }
	        if(_blit_UV_tex_mat  != null){ DestroyImmediate(_blit_UV_tex_mat); }
	        if(_blit_UV_brush_mat != null){ DestroyImmediate(_blit_UV_brush_mat); }

	        _accumulation_uv_RT.Dispose();
	        _uvChunks_RT.Dispose();

	        ModelsHandler_3D.Act_onImported -= On_3dObject_Imported;
	        Update_callbacks_MGR.objectsRender -= OnUpdate;
	    }
	}
}//end namespace
