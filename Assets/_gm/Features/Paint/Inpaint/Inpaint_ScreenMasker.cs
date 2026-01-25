using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// controls similar 2 scripts which can create screenmask:
	// Over the painted areas
	// Over non-painted areas ("Where empty").
	// Those masks can be then sent to StableDiffusion, to tell where to generate.
	public class Inpaint_ScreenMasker : MonoBehaviour
	{
	    [SerializeField] Inpaint_ScreenMasker_Original _masker_original;
	    [SerializeField] Inpaint_ScreenMasker_EmptyNothing _masker_emptyNothing;

	    public RenderTexture ScreenMask_ContentRT_ref(bool withAntiEdge)
	    {
	        if (WorkflowRibbon_UI.instance.currentMode()==WorkflowRibbon_CurrMode.WhereEmpty){
	            return withAntiEdge ? _masker_emptyNothing._screenMaskRT_fixedRes_antiEdge
	                                : _masker_emptyNothing._screenMaskRT_skipAntiEdge; 
	        }
	        return withAntiEdge ? _masker_original._screenMaskRT_fixedRes_antiEdge
	                            : _masker_original._screenMaskRT_skipAntiEdge;
	    }

    
	    bool canSkipRenderingMask(){
	        var tRib = WorkflowRibbon_UI.instance;

	        if(tRib.isMode_using_img2img()==false){ return true; }
	        if(MainViewport_UI.instance.showing != MainViewport_UI.Showing.UsualView){ return true; }

	        bool isHovered =  tRib.isHoveredByCursor ||
	                          tRib.isPressedByCursor || 
	                          (GenerateButtons_Main_UI.instance?.isHovering_GenArtButton??false) ||
	                          (GenerateButtons_Mini_UI.instance?.isHovering_GenArtButton??false);

	        bool isWhereEmpty = tRib.currentMode()==WorkflowRibbon_CurrMode.WhereEmpty
	                            && KeyMousePenInput.isKey_alt_pressed();//orbiting etc

	        bool isTotalObj = tRib.currentMode() == WorkflowRibbon_CurrMode.TotalObject
	                            && KeyMousePenInput.isKey_alt_pressed();//orbiting etc

	        if(!isHovered && !isWhereEmpty && !isTotalObj){ return true; }
	        return false;
	    }


	    // invoked every frame by our update,
	    // but can be also called if user wants to generate with StableDiffusion.
	    public void RenderScreenMask_maybe( RenderUdims objectUV_brushedColorRGBA, bool mustRender ){
	        if (!mustRender && canSkipRenderingMask()){ return; }
	        if (WorkflowRibbon_UI.instance.currentMode()==WorkflowRibbon_CurrMode.WhereEmpty){
	            _masker_emptyNothing.RenderScreenMask( objectUV_brushedColorRGBA );
	            return; 
	        }
	        if (objectUV_brushedColorRGBA == null) { return; }
	        _masker_original.RenderScreenMask( objectUV_brushedColorRGBA );
	    }//end()


	    void OnUpdate() => RenderScreenMask_maybe( Inpaint_MaskPainter.instance._ObjectUV_brushedColorRGBA, mustRender:false );

	    void Start() => Update_callbacks_MGR.calc_inpaintScreenMask += OnUpdate;
	    void OnDestroy() => Update_callbacks_MGR.calc_inpaintScreenMask -= OnUpdate;
	}
}//end namespace
