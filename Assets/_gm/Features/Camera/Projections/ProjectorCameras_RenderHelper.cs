using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// helps its 'ProjectorCameras_MGR' to render the cameras.
	// Assigns properties into material, then tells projector camera to render.
	public class ProjectorCameras_RenderHelper : MonoBehaviour{

	    [SerializeField] ProjectorCameras_MGR _ProjCamsMGR;
	    [Space(10)]
	    [SerializeField] Texture _checkerTexture;
	    [SerializeField] float _checkTexture_opacity = 0.4f;
	    [Space(10)]
	    [SerializeField] Shader _projectionShader;
	    [SerializeField] Shader _multiProjectionShader;
	    [SerializeField] Color _mainSelected_color;

	    Material _projMat = null;
	    Material _multiProjMat = null;
    
	    //we will draw black to white colors instead of projections, to show which projection is render when.
	    public bool _showOrderOfProjections { get; set; } = false;


	    // pcamIx: needed if you want to display grayscale projections, to visualize drawing order of cameras.
	    // isHighlight: can show checker-pattern that's drawn on top of all other projections.
	    // Useful to show where some selected projectorCamera is shining, even if its overlapped by some other projection
	    public void RenderProjCamera( ProjectorCamera pcam,  RenderUdims intoHereRT,
	                                  int pcamIx=-1,  bool isHighlight=false ){
	        bool isMultiPOV = pcam.numPOV > 1;
	        GenData_Masks masks =  pcam._myGenData._masking_utils;
	        Material pMat =  Choose_Material(masks);

	        if (isMultiPOV){
	            MultiPOV_Set_UvMasks(pMat, masks);
	            MultiPOV_Set_CursorMask(pMat, pcam);
	        }else {
	            SinglePOV_Set_UvMasks(pMat, masks);
	        }

	        Set_ScreenArt_and_Mask(pMat, pcam, isHighlight);
	        Set_HSVC_vars(pMat, pcam, pcamIx, isHighlight);

	        Objects_Renderer_MGR.instance.EquipMaterial_on_ALL( pMat );

	        var renderArg = new ProjectorCamera.RenderProj_arg(intoHereRT);
	            renderArg.materialOnGeometry = pMat;
	        pcam.RenderProj_into(renderArg);

	        ShowSpecificPov_if_multipov_maybe(pMat, pcam, renderArg);
	    }


	    Material Choose_Material( GenData_Masks masks ){
	        if(masks.numPOV<=1){ return _projMat; }
	        //else, using multiprojection shader, ensure it uses correct #defines:
	        CameraTools.Toggle_numPOVs_Keywords(_multiProjMat, masks.numPOV);
	        return _multiProjMat;
	    }


	    void SinglePOV_Set_UvMasks( Material mat,  GenData_Masks masks ){
	        RenderUdims.SetNumUdims(masks._ObjectUV_visibilityR8G8[0], intoHere:mat);
	        mat.SetTexture("_uvMask", masks._ObjectUV_brushedMaskR8[0].texArray );
	        mat.SetTexture("_ProjVisibility", masks._ObjectUV_visibilityR8G8[0].texArray );
	    }

	    void MultiPOV_Set_UvMasks( Material mat,  GenData_Masks masks ){
	        int maskIx = 0;
	        bool udimsSet = false;

	        for (int i=0; i<masks.numPOV; ++i){
	            if(!udimsSet){ 
	                RenderUdims.SetNumUdims(masks._ObjectUV_visibilityR8G8[i], intoHere: mat);
	                udimsSet=true; 
	            }
	            RenderTexture uvMask   = masks._ObjectUV_brushedMaskR8[i].texArray;
	            RenderTexture pVisibil = masks._ObjectUV_visibilityR8G8[i].texArray;
	            if(uvMask == null){ continue; }
	            mat.SetTexture( $"_POV{maskIx}_additive_uvMask", uvMask);
	            mat.SetTexture( $"_POV{maskIx}_ProjVisibility", pVisibil);
	            maskIx++;
	        }
	    }


	    //For for previewing some POV through cursor-texture on screen.
	    void MultiPOV_Set_CursorMask(Material putHere, ProjectorCamera pCam)
	    {
	        View_UserCamera currViewCam = UserCameras_MGR.instance._curr_viewCamera;
	        Matrix4x4 currViewport_P_Matrix =  currViewCam.ExpandFov_Match_ContentCamFov( with_ShiftPerspectiveCenter:true );
	        Matrix4x4 currViewport_V_Matrix =  currViewCam.myCamera.worldToCameraMatrix;
	                  currViewport_P_Matrix =  GL.GetGPUProjectionMatrix(currViewport_P_Matrix, true);
	        putHere.SetMatrix("_CurrViewport_VP_matrix",  currViewport_P_Matrix*currViewport_V_Matrix );

	        var oRib  = SD_WorkflowOptionsRibbon_UI.instance;
	        var mvRib = MultiView_Ribbon_UI.instance;
	        var p = Projections_MaskPainter.instance;

	        Vector2 pointInViewport01 = p.getViewportCursorPos01();
	        putHere.SetVector("_PrevNewBrushScreenCoord", new Vector4(pointInViewport01.x, pointInViewport01.y, 
	                                                                  pointInViewport01.x, pointInViewport01.y));
	        putHere.SetVector("_BrushSize_andFirstFrameFlag", new Vector4(p.visibleBrushSize(), p.visibleBrushSize(), 0,0));
	        Vector2 viewportSize = p.getViewportSize();
	        putHere.SetFloat("_ScreenAspectRatio", viewportSize.x / viewportSize.y);

	        bool isPaintingMyIcon = p.isPainting_in_Generation(pCam._myGenData);
	            bool showPreview  = mvRib._isEditingMode  &&  isPaintingMyIcon;  
	                 showPreview &= !Save_MGR.instance._isSaving;//<--to avoid baking-in the brush-peek-preview.
	                 showPreview &= WorkflowRibbon_UI.instance.isMode_using_img2img()==false;
        
	        Texture brushStamp = showPreview ? oRib._brushHardnessTex : Texture2D.blackTexture;
	                int povIx  = showPreview ?  mvRib.currentPovIx : -1;

	        putHere.SetTexture("_BrushStamp", brushStamp);
	        putHere.SetInteger("_Cursor_for_POV_ix", povIx);

	        TextureTools_SPZ.SetKeyword_Material(putHere, "CURSOR_COLOR_WHITE", oRib.isPositive);
	    }

    
	    void Set_ScreenArt_and_Mask(Material mat, ProjectorCamera pcam, bool isHighlight){
	        var byproducts = pcam._myGenData._byproductsOfRequest;

	        Texture screenMaskInpaint = null;
	        if (pcam._myGenData._byproductsOfRequest != null){
	            screenMaskInpaint = byproducts.screenSpaceMask_WE_disposableTex;
	        }

	        mat.SetTexture("_ScreenMaskTexture", screenMaskInpaint);
	        mat.SetFloat("_Force_Mask0_as_white", 0);

	        Texture art2D =  isHighlight? _checkerTexture  :  pcam.myIconUI?.texture0()?.tex2D ?? null;
	                art2D =  _showOrderOfProjections? Texture2D.whiteTexture  :  art2D;
	        mat.SetTexture("_ScreenArtTexture", art2D);
	    }



	    void Set_HSVC_vars( Material mat, ProjectorCamera pcam,  int pcamIx, bool isHighlight){
	        if (isHighlight){
	            SetupMatVars_HSVC_forHighlight(mat, pcam);
	        }
	        else if(_showOrderOfProjections){//show projections in black and white, instead of 2D arts (brighter = later)
	            SetupMatVars_HSVC_forOrder(mat, pcam, pcamIx);
	        }
	        else{ 
	            SetupMatVars_HSVC_Usual(mat, pcam);
	        }
	    }
    
	    void SetupMatVars_HSVC_forHighlight(Material pMat, ProjectorCamera pcam){
	        float hueShift = 0;
	        float saturation = 1;
	        float value = pcam.myIconUI.hsvc().value;
	        float contrast = 1;
	        pMat.SetVector("_HSV_and_Contrast", new Vector4(hueShift, saturation, value, contrast));

	        bool isMainSelected = pcam.myIconUI != null && pcam.myIconUI == Art2D_IconsUI_List.instance._mainSelectedIcon;
	        Color whiteColor =  Color.white;
	        Color tintCol    =  isMainSelected ? _mainSelected_color : whiteColor;
	              tintCol.a  = _checkTexture_opacity;
	        pMat.SetColor("_TintColorCurrProjection", tintCol);
	    }

	    void SetupMatVars_HSVC_forOrder( Material mat, ProjectorCamera pcam, int pcamIx){
	        float hueShift = 0;
	        float saturation = 1;
	        float coeff01 = Mathf.InverseLerp( 0, _ProjCamsMGR.num_projCameras-1,  pcamIx );
	        float value = Mathf.Lerp(0.25f, 0.8f, coeff01); //[0.25, 0.8] so that it's not too black no too bright
	        float contrast = 1;
	        mat.SetVector("_HSV_and_Contrast", new Vector4(hueShift, saturation, value, contrast));

	        bool isMainSelected =  pcam.myIconUI!=null  &&  pcam.myIconUI == Art2D_IconsUI_List.instance._mainSelectedIcon;
	        mat.SetColor("_TintColorCurrProjection", isMainSelected?_mainSelected_color : Color.white);
	    }

	    void SetupMatVars_HSVC_Usual( Material mat, ProjectorCamera pcam ){
	        mat.SetVector("_HSV_and_Contrast", pcam.myIconUI.hsvc().toVector4());
	        Color tintCol = Color.white;
	              tintCol.a = pcam.myIconUI.projBends().totalVisibility_01;
	        mat.SetColor("_TintColorCurrProjection", tintCol);
	    }


    
	    //if we are in the Editing mode and hovered some specific POV-toggle in the UI pannel,
	    //ensure we render it on top of other povs, to highlight:
	    void ShowSpecificPov_if_multipov_maybe( Material pMat,  ProjectorCamera pcam, 
	                                            ProjectorCamera.RenderProj_arg renderArg ){

	        bool isPaiting_in_myGeneration = Projections_MaskPainter.instance.isPainting_in_Generation(pcam._myGenData);
	        int hoveredPovIx = MultiView_Ribbon_UI.instance.hoveredPovIx;

	        if(isPaiting_in_myGeneration && hoveredPovIx>=0){
	            Texture2D art2D =  pcam.myIconUI?.texture0()?.tex2D ?? null;
	            pMat.SetTexture("_ScreenArtTexture", art2D);
	            pMat.SetTexture("_ObjectUVmasks_AtlasTexture", Texture2D.whiteTexture);
	            pMat.SetColor("_TintColorCurrProjection", Color.white);//make hoveredPOV fully visible.
	            pMat.SetFloat("_Force_Mask0_as_white", 1);

	            renderArg.onlySpecificPov = hoveredPovIx;
	            pcam.RenderProj_into( renderArg ); 
	        }
	    }



	    void Awake(){
	        _projMat =  new Material( _projectionShader );
	        _multiProjMat = new Material( _multiProjectionShader );
	    }

	    void OnDestroy(){
	        if(_projMat != null){ DestroyImmediate(_projMat); }
	        if(_multiProjMat!=null){ DestroyImmediate(_multiProjMat);  }
	    }
	}
}//end namespace
