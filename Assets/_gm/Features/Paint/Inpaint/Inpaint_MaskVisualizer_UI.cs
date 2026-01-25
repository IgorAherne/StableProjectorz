using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace spz {

	// Helper class of the 'Inpaint_MainPanel_UI'.
	// Visually fades the mask in and out (but doesn't create it).
	public class Inpaint_MaskVisualizer_UI : MonoBehaviour
	{
	    [SerializeField] RawImage _show_ScreenMask_ui_image; //for visualizing the masked region inside large ui rectangle.
	    [SerializeField] AspectRatioFitter _image_aspectRatioFitter;
	    [Space(10)]
	    [SerializeField] float _maskFadeSpeed = 6.7f;

	    float _maskOpacity = 0;


	    public bool CanVisualize_ScreenMask(){ 
	        bool isUsualView =  MainViewport_UI.instance?.showing == MainViewport_UI.Showing.UsualView;
	        if(!isUsualView){ return false; }

	        var tRib = WorkflowRibbon_UI.instance;
	        if(!tRib){ return false; }

	        bool genArtHover  = GenerateButtons_Main_UI.instance?.isHovering_GenArtButton?? false;
	        bool mGenArtHover = GenerateButtons_Mini_UI.instance?.isHovering_GenArtButton?? false;

	        bool isHovered  = tRib.isMode_using_img2img() && (tRib.isHoveredByCursor || tRib.isPressedByCursor);
	             isHovered |= tRib.isMode_using_img2img() && (genArtHover || mGenArtHover);

	        bool isWhereEmpty = tRib.currentMode()==WorkflowRibbon_CurrMode.WhereEmpty 
	                             && KeyMousePenInput.isKey_alt_pressed();//orbiting etc

	        bool isTotalObject = tRib.currentMode()==WorkflowRibbon_CurrMode.TotalObject
	                             && KeyMousePenInput.isKey_alt_pressed();//orbiting etc

	        return isHovered || isWhereEmpty || isTotalObject;
	    }


	    void VisualizeScreenMask(){
	        if(MaskOpacity_FadeInOut() == false){ return; } //return if invisible
	        //else, mask is still visible, so assign the remaining properties:
	        var inp = Inpaint_MaskPainter.instance;
	        RenderTexture screenMask_ref = inp.ScreenMask_ContentRT_ref(withAntiEdge:true);

	        if (screenMask_ref != null){//the aspectRatioFitter uses "FitInParent", so just update the ratio:
	            _image_aspectRatioFitter.aspectRatio =  screenMask_ref.width / (float)screenMask_ref.height;
	        }
	        Material mat = _show_ScreenMask_ui_image.materialForRendering;
	        mat.SetTexture("_MainTex", screenMask_ref);
	        mat.SetTexture("_InfoTex", Inpaint_DummyTextMaker.instance.GetRenderTex_ref() );
	        mat.SetVector("_InfoTex_ST", new Vector4(7, 7, 0, 0));

	        Vector2 size = _show_ScreenMask_ui_image.rectTransform.rect.size;
	        mat.SetFloat("_ScreenAspectRatio", size.x/size.y);
        
	        _show_ScreenMask_ui_image.SetMaterialDirty();//if the render texture contents change.
	    }

    
	    bool MaskOpacity_FadeInOut(){
	        bool isShow = CanVisualize_ScreenMask();
	        _maskOpacity +=  Time.deltaTime*_maskFadeSpeed*(isShow? 1 : -1);
	        _maskOpacity  =  Mathf.Clamp01(_maskOpacity);

	        Material mat = _show_ScreenMask_ui_image.materialForRendering;
	        Color c =  mat.GetColor("_TintColor");
	            c.a = _maskOpacity;
	        mat.SetColor("_TintColor", c);

	        _show_ScreenMask_ui_image.gameObject.SetActive( _maskOpacity>0 );
	        return c.a>0;
	    }//end()



	    void OnUpdate() => VisualizeScreenMask();

	    void Start(){
	        Update_callbacks_MGR.show_inpaintScreenMask += OnUpdate;
	    }
	}
}//end namespace
