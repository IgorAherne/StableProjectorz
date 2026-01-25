using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace spz {

	//makes textures that show what texels are visibile to the camera when in some of its point-of-views (POVs).
	public class ProjCam_HelpTextures_Init : MonoBehaviour
	{
	    [SerializeField] ProjectorCamera _projectionCamera;
	    [SerializeField] Camera _cam;
	    [SerializeField] Depth_Contrast_Helper _depthContrastHelper;
	    [SerializeField] ScreenDepth_EdgesDetector _depthEdgesDetector;
	    [Space(10)]
	    [SerializeField] Shader _proj_visibility_Shader;
	    [Space(10)]
	    [SerializeField] Shader _ProjectionsAlignment_shader;//calculates dot products of camera POVs and stores into texture
	    [SerializeField] ComputeShader _AlignmentToVisibil_shader;//modifies the visibility texture based on dot products.

	    HelperTextures _helps = null;
    
	    Material _proj_visibility_mat = null;
	    Material _proj_Alignment_mat = null;

	    //MeshRenderer _debugQuad;
	    //MeshRenderer _debugQuad2;


	    public void Make_Visibilities_and_Alignments( IReadOnlyList<SD_3D_Mesh> myMeshes,  ProjBlendingParams blendParams,
	                                                  Func<int,RenderUdims> onWillRenderPov ){
	        GenData2D genData = _projectionCamera._myGenData;
	        List<Renderer> myRenderers = myMeshes.Select(m=>m._meshRenderer).ToList();

	        var prevParams = new ParamsBeforeRender(_cam);
	            int numPovs = genData.povInfos.numEnabled;
	            RenderUdims rendUdims0 = onWillRenderPov(0);
	            CameraTools.TempEnable_POVs_Keywords_GLOBAL( numPovs, DoStuff );
	        prevParams.RestoreCam(_cam);

	        void DoStuff(){
	            Material visMat = _proj_visibility_mat;

	            Objects_Renderer_MGR.instance.EquipMaterial_on_Specific(myMeshes, visMat);

	            //Could be null. If so, shader will use white texture, by default:
	            visMat.SetTexture("_ScreenMaskTexture", genData._byproductsOfRequest.screenSpaceMask_WE_disposableTex);

	            _helps?.Dispose();
	            _helps = new HelperTextures(genData, rendUdims0.widthHeight.x, rendUdims0.widthHeight.y);

	            PrepareDepth(genData, blendParams);

	            RenderVisibility_POVs(myRenderers, genData, visMat, blendParams, onWillRenderPov);

	            ImproveVisibility_byAlignments(myRenderers, genData, blendParams, onWillRenderPov);
	            _helps.Dispose();
	            _helps = null; 
	        }
	    }


	    void ImproveVisibility_byAlignments( IReadOnlyList<Renderer> myRenderers,  GenData2D genData,  ProjBlendingParams blendParams,
	                                         Func<int,RenderUdims> onWillRenderPov ){
	        bool isMultiView = genData.povInfos.numEnabled > 1;
	        if(!isMultiView){ return;}
	        //Else, multiprojection. See which polygons look where, and adjust visibility texture by that:
	        _helps.Init_AlignmentHelpTextures();
	        RenderAlignments_POVs_Dots(genData, _helps, onWillRenderPov,  myRenderers );

	        Alignments_AntiSeam(_helps);
	        RenderAlignments_POVs_apply(genData, _helps, onWillRenderPov, blendParams);
	    }


	    void PrepareDepth( GenData2D genData, ProjBlendingParams blendParams ){

	        Get_BlackWhiteDepth_Contrasted(_helps, genData);

	        bool isMultiView =  genData.povInfos.numEnabled > 1;

	        if(isMultiView || blendParams.edgeBlurStride_01==0){//ensuring full visibility of projection (without faded-rim)
	            Make_NoEdges_ofDepth(_helps);
	        }else {//single-pov will rely on blurred edges of depth (to conceal any ugly borders) so make it:
	            Get_DepthBlurredEdges(_helps, genData, blendParams);
	        }
	    }


	    void Get_BlackWhiteDepth_Contrasted( HelperTextures h, GenData2D genData ){
	        _depthContrastHelper.Get_BlackWhiteDepth_POVs(_cam,  genData.povInfos.povs,  _helps.screenA_r16);
	        //improve contrast of depth(make tiny differences into prominent differences)
	        var depthArg = new Depth_Contrast_Helper.DepthContrast_arg(h.screenA_r16, Depth_Contrast_Helper.ContrastMode.ApplyExact, 1, 1);
	        depthArg.warn_ifTooFrequent = false;
	        depthArg.overrideNumIters = 3;
	        _depthContrastHelper.Improve_DepthContrast(depthArg);
	    } 


	    void Make_NoEdges_ofDepth( HelperTextures h ){
	        TextureTools_SPZ.ClearRenderTexture( h.screenB_r16,  new Color(0,0,0,0) );
	    }


	    void Get_DepthBlurredEdges(HelperTextures h, GenData2D genData, ProjBlendingParams blendParams){

	        var tArgs = new ScreenDepth_EdgesDetector.TexArgs { 
	            depthNonLinear_contrast_R16 = h.screenA_r16,
	            result_edges_R16 = h.screenB_r16,
	            screenBrushMask_R  = genData._byproductsOfRequest.screenSpaceMask_WE_disposableTex,
	        };

	        var bArgs = new ScreenDepth_EdgesDetector.BlurArgs{
	            edgeBlurStride_01 = blendParams.edgeBlurStride_01,
	            edgeBlurPow_01    = blendParams.edgeBlurPow_01,
	        };
	        _depthEdgesDetector.DetectEdges_ByDepth(tArgs);
	        _depthEdgesDetector.BlurEdges_ofDepth(tArgs, bArgs);
	    }


	    void RenderVisibility_POVs( IReadOnlyList<Renderer> myRenderers,  GenData2D genData,  Material mat,
	                                ProjBlendingParams b, Func<int, RenderUdims> onWillRenderPov){
	        _cam.allowMSAA = false;
	        _cam.targetTexture = _helps.screenB_r16;
	        _cam.clearFlags = CameraClearFlags.Nothing;

	        Color clearingColor = new Color(0, 0, 0, 0);
	        mat.SetTexture("_BlurredDepthEdges", _helps.screenB_r16);
	        mat.SetFloat("_Dot_EdgesFade", b.edgeBlurStride_01 > 0 ? 0.3f : 0.0f);

	        var cmd = new CommandBufferScope("Render to TextureArray");

	        for (int i=0; i<genData.povInfos.numAll; ++i){
	            CameraPovInfo pov = genData.povInfos.povs[i];
	            if (!pov.wasEnabled){ continue; }

	            RenderUdims renderHere_square = onWillRenderPov?.Invoke(i);
	            renderHere_square.ClearTheTextures(clearingColor);
	            Debug.Assert(renderHere_square.graphicsFormat == GraphicsFormat.R8G8_UNorm);
	            RenderUdims.SetNumUdims(renderHere_square, mat);

	            _cam.transform.SetPositionAndRotation(pov.camera_pos, pov.camera_rot);
	            _cam.fieldOfView = pov.camera_fov;
	            CameraTools.ShiftViewportCenter_ofProjMat(_cam, pov.perspectiveCenter01);
	            mat.SetVector("_CameraWorldPos", pov.camera_pos.toVec3());

	            cmd.RenderIntoTextureArray( _cam,  myRenderers,  mat,  useClearingColor:true,  clearTheDepth:true, 
	                                        Color.clear,  renderHere_square.texArray, -1 );

	            DilateVisibility(renderHere_square);
	        }
	    }



	    void DilateVisibility( RenderUdims renderHere ){
	        var dArg = new DilationArg(renderHere.texArray, numberOfTexelsExpand:2, DilateByChannel.G, null, isRunInstantly:true);
	        TextureDilation_MGR.instance.Dillate(dArg);
	    }
     
    
	    void RenderAlignments_POVs_Dots( GenData2D genData,  HelperTextures h,  Func<int,RenderUdims> onWillRenderPov,  
	                                     IReadOnlyList<Renderer> myRenderers ){

	        int povTotalCount = genData.povInfos.numAll;
	        int actualNumPov = genData.povInfos.numEnabled;

	        RenderUdims.SetNumUdims( h.alignDots_rgba8, _proj_Alignment_mat );
	        CameraTools.Toggle_numPOVs_Keywords(_proj_Alignment_mat, actualNumPov);

	        int ixInShader = 0;
	        for(int i=0; i<povTotalCount; ++i){
	            CameraPovInfo pov = genData.povInfos.povs[i];
	            if(!pov.wasEnabled){ continue; }

	            RenderUdims visibilRT = onWillRenderPov(i);//i, not ixInShader.
	            _proj_Alignment_mat.SetTexture( $"_POV{ixInShader}_ProjVisibility", //ixInshader, not i
	                                            visibilRT.texArray );
	            ixInShader++;
	            RenderUdims.assertSameSize(visibilRT, h.alignDots_rgba8);
	        }

	        var destin_texArrays = new List<RenderTexture>{ h.alignDots_rgba8.texArray,  h.alignIxs_rgba8.texArray};

	        //MODIF maybe we don't need to clear here? Already cleared during their init.
	        var cmd = new CommandBufferScope("Render Alignments POVs Dots");
	        cmd.RenderIntoTextureArrays( _cam,  myRenderers,  _proj_Alignment_mat, 
	                                     useClearingColor:true,  clearTheDepth:true,  Color.clear,  destin_texArrays, -1 );
	    }



	    // Before we use the rendered alignments to modified the visibilities, need to prevent seams.
	    // Without those, some pixels are missed on the edges of triangles.
	    // Those missed pixels will incorrectly have dots 0 and indexes -1, leading to bright
	    // seams inside MultiProjection shader. So, we need to dilate them outwards.
	    void Alignments_AntiSeam( HelperTextures h ){
	        var dArg = new DilationArg(null, numberOfTexelsExpand: 2, DilateByChannel.Use_Separate_UVChunksR8,
	                                   null, isRunInstantly: true);
	        dArg.rule = DilationRule.NineSamplesAveraged;

	        RenderTextureDescriptor descriptor = h.alignIxs_rgba8.descriptorRT;
	        descriptor.graphicsFormat = GraphicsFormat.R8_UNorm;
	        dArg.separate_UVchunks_R8_WillAlter = RenderTexture.GetTemporary(descriptor);
	        dArg.separate_UVchunks_R8_WillAlter.filterMode =  FilterMode.Point;

	        //NOTICE: blitting Ixs (4 channel) into R8 (single channel texture).
	        // Not blitting dots because had seams with them, unlike by blitting ixs.
	        // Also, ixs are packed into [0,1], and a '-1' index is represented by 0, very convenient for this usecase.
	        // (because 0 represents empty space).
	        TextureTools_SPZ.Blit( h.alignIxs_rgba8.texArray,  dArg.separate_UVchunks_R8_WillAlter );//clear by indexes (first channel)
	        dArg.this_and_intoHere = h.alignDots_rgba8.texArray; //dilate dots
	        TextureDilation_MGR.instance.Dillate(dArg);

	        TextureTools_SPZ.Blit( h.alignIxs_rgba8.texArray,  dArg.separate_UVchunks_R8_WillAlter );//clear by indexes again.
	        dArg.this_and_intoHere = h.alignIxs_rgba8.texArray; //dilate ixs
	        TextureDilation_MGR.instance.Dillate(dArg);

	        TextureTools_SPZ.Dispose_RT(ref dArg.separate_UVchunks_R8_WillAlter, true);
	    }


	    void RenderAlignments_POVs_apply( GenData2D genData, HelperTextures h,
	                                      Func<int,RenderUdims> onWillRenderPov,
	                                      ProjBlendingParams b ){
	        int povTotalCount = genData.povInfos.numAll;
	        int actualNumPov  = genData.povInfos.numEnabled;

	        ComputeShader aShader = _AlignmentToVisibil_shader;
	        CameraTools.Toggle_numPOVs_Keywords(aShader, actualNumPov);

	        // NOTICE: no need to set Count of Udims,
	        // because CalcGroups_for_ComputeShader() will dispatch for all slices.

	        int kernel = aShader.FindKernel("CSMain");
	        aShader.SetFloat("_BestPov_DotMinRange", b.edgeBlurStride_01);
	        aShader.SetFloat("_DotFadePow", b.edgeBlurPow_01);

	        aShader.SetTexture(kernel, "_PovDots", h.alignDots_rgba8.texArray);
	        aShader.SetTexture(kernel, "_PovIxs", h.alignIxs_rgba8.texArray);

	        int ixInShader = 0;
	        for(int i=0; i<povTotalCount; ++i){
	            CameraPovInfo pov = genData.povInfos.povs[i];
	            if(!pov.wasEnabled){ continue; }

	            RenderUdims visibilRT = onWillRenderPov(i);//i, not ixInShader.
	            aShader.SetTexture( kernel, $"_POV{ixInShader}_ProjVisibility", //ixInshader, not i
	                                visibilRT.texArray );
	            ixInShader++;
	            RenderUdims.assertSameSize(visibilRT, h.alignDots_rgba8);
	        }

	        Vector3Int groups = h.alignDots_rgba8.CalcGroups_for_ComputeShader();
	        aShader.Dispatch(kernel, groups.x, groups.y, groups.z);
	    }


	    void Awake(){
	        _proj_visibility_mat = new Material(_proj_visibility_Shader);
	        _proj_Alignment_mat = new Material(_ProjectionsAlignment_shader);
	        //_debugQuad = GameObject.Find("DEBUG_QUAD").GetComponent<MeshRenderer>();
	        //_debugQuad2 = GameObject.Find("DEBUG_QUAD2").GetComponent<MeshRenderer>(); //for debugging textures, showing them on a quad.
	    }

	    void OnDestroy(){
	        DestroyImmediate(_proj_visibility_mat);
	        DestroyImmediate(_proj_Alignment_mat);
	        _proj_visibility_mat = null;
	        _proj_Alignment_mat = null;
	    }


    
	    class HelperTextures{
	        Vector2Int _maskSize;
	        public RenderTexture screenA_r16 =null;//same dimensions as projection image. For example 1024x678. will work with depth (edge-detection), but 16 works fine.
	        public RenderTexture screenB_r16 =null;//cheap texture, R8 format.
	        public RenderUdims alignDots_rgba8 = null;//used during Alignment stage, 4 channels. Dot products towards POV positions (no more than four)
	        public RenderUdims alignIxs_rgba8 = null;//used during Alignment stage, 4 channels. Ixs of POV that are most perpendicular (ix 0-5 but no more than four)

	        public HelperTextures( GenData2D genData,  int maskWidth, int maskHeight){
            
	            Vector3Int screen = genData.textureSize(withUpscale:true);
	            float aspect = screen.x/(float)screen.y;
	            if(screen.x<screen.y){ 
	                if(screen.x<2048){  screen.x=2048;  screen.y=Mathf.RoundToInt(screen.x/aspect); }
	            }else { 
	                if(screen.y<2048){ screen.y=2048;  screen.x=Mathf.RoundToInt(screen.y*aspect); }
	            }
	            _maskSize = new Vector2Int(maskWidth, maskHeight);

	            //cheap format for the black white depth, but edge detection works fine with 16:
	            RenderTextureDescriptor descA = new RenderTextureDescriptor(screen.x, screen.y, GraphicsFormat.R16_UNorm, depthBufferBits:0, mipCount:0);
	            descA.enableRandomWrite = true;
	            screenA_r16 =  new RenderTexture(descA);//cheap format.
	            screenB_r16 =  new RenderTexture(descA);
	            finishInit(screenA_r16);
	            finishInit(screenB_r16);
	        }  

	        void finishInit( RenderTexture rt ){  
	            rt.enableRandomWrite = true;
	            rt.filterMode = FilterMode.Point;
	            rt.anisoLevel = 0;
	            // VERY IMPORTANT TO CLEAR! July 2024 had issues: editor was ok but .exe had flickering
	            // when using FAR & SFT slider inside the icon.
	            // That's because the depth shader is ADDITIVE, and textures had garbage values originally.
	            TextureTools_SPZ.ClearRenderTexture(screenA_r16, Color.clear);
	            TextureTools_SPZ.ClearRenderTexture(screenB_r16, Color.clear);
	        }

	        public void Init_AlignmentHelpTextures(){
	            Debug.Assert(alignDots_rgba8 == null, $"already have images, can't invoke {nameof(Init_AlignmentHelpTextures)} twice");
	            IReadOnlyList<UDIM_Sector> allUdims =  ModelsHandler_3D.instance._allKnownUdims;//all udims, even from currently inactive/nonselected meshes.
	            alignDots_rgba8 = new RenderUdims(allUdims, _maskSize, GraphicsFormat.R8G8B8A8_UNorm, FilterMode.Point, Color.clear);
	            alignIxs_rgba8  = new RenderUdims(allUdims, _maskSize, GraphicsFormat.R8G8B8A8_UNorm, FilterMode.Point, Color.clear);
	            finishInit(alignDots_rgba8.texArray);
	            finishInit(alignIxs_rgba8.texArray);
	        }
	        public void Dispose(){
	            TextureTools_SPZ.Dispose_RT(ref screenA_r16, false);
	            TextureTools_SPZ.Dispose_RT(ref screenB_r16, false);
	            alignDots_rgba8?.Dispose();
	            alignIxs_rgba8?.Dispose();
	            alignDots_rgba8 = alignIxs_rgba8 = null;
	        }
	    }

	}
}//end namespace
