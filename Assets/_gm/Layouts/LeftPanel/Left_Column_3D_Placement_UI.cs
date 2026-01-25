using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace spz {

	// positions the rect-transform on top of the Left-Column of the global UI-skeleton.
	// For the panel that holds ui elements of 3d-generation.
	// Also fades this panel depending on the Mode we are in (preview-UV, StableDiffusion, 3D)
	//
	// There is also a very similar script, `Left_Column_SD_Placement_UI`
	public class Left_Column_3D_Placement_UI : MonoBehaviour
	{
	    [SerializeField] RectTransform _place_me;
	    [SerializeField] CanvasGroup _canvGrp;
	    [SerializeField] float _fadeSpeed = 5;

	    void Update(){
	        Global_Skeleton_UI.instance?.Place_onto_LeftColumn( _place_me );
	        DimensionMode currMode = DimensionMode_MGR.instance._dimensionMode;
	        switch (currMode){
	            case DimensionMode.dim_uv:
	                FadePanel(_canvGrp, 0);
	                break;
	            case DimensionMode.dim_gen_3d:
	                FadePanel(_canvGrp, 1);
	                break;
	            case DimensionMode.dim_sd:
	            default:
	                FadePanel(_canvGrp, 0);
	                break;
	        }
	    }

	    void FadePanel(CanvasGroup canvGrp, float destin){
	        canvGrp.alpha = Mathf.MoveTowards(canvGrp.alpha, destin, Time.deltaTime*_fadeSpeed);
	        if(destin!=1 && canvGrp.alpha<=0.0001f){ canvGrp.gameObject.SetActive(false); }
	        else{
	            canvGrp.gameObject.SetActive(true);
	        }
	    }//end()
	}
}//end namespace
