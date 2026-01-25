using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Looks at how many View Camera are active in Cameras_MGR.
	// Able to restore perspective-centers of each camera, to default locations.
	// So it will notify the 'Cameras_MGR' to update them. This will alter how each camera renders
	// Depending on number of active cameras, different default locations are avaialble
	public class CamerasMGR_POVdefaults_UI : MonoBehaviour{

	    // Every RectTransform contains varaints. Each variant is a group of default placments
	    // (has children, which represent default placement of their pin. For example:
	    // 
	    //   _3_pinPlacementVariants:
	    //      - variant A
	    //          -pin0_pos
	    //          -pos1_pos
	    //          -pos2_pos
	    //      - variant B
	    //          -pin0_pos
	    //          -pin1_pos
	    //          -pin2_pos
	    //      - variant C
	    //          - etc
	    [SerializeField] RectTransform _1_pinPlacementVariants;
	    [SerializeField] RectTransform _2_pinPlacementVariants;
	    [SerializeField] RectTransform _3_pinPlacementVariants;
	    [SerializeField] RectTransform _4_pinPlacementVariants;
	    [SerializeField] RectTransform _5_pinPlacementVariants;
	    [SerializeField] RectTransform _6_pinPlacementVariants;

	    int _placementVariant_ix = 0;
	    Coroutine _lerpPins_toDefaultPos_crtn = null;


	    public void EnsureNotLerping(){
	        if(_lerpPins_toDefaultPos_crtn != null){  StopCoroutine(_lerpPins_toDefaultPos_crtn);  }
	        _lerpPins_toDefaultPos_crtn = null;
	    }


	    public void OnOrderPinsButton( List<CameraPovInfo> povInfos ){
	        _placementVariant_ix++;
	        EnsureNotLerping();
	        _lerpPins_toDefaultPos_crtn =  StartCoroutine( LerpPins_toDefaultPos_crtn(povInfos,0.3f) );
	    }


	    public void Lerp_to_SpecificDestinations(List<CameraPovInfo> wantedDestinations, List<int> ixs_to_instantly=null){
	        if(wantedDestinations.Count==0){ return;}
	        EnsureNotLerping();
	        _lerpPins_toDefaultPos_crtn =  StartCoroutine( Lerp_to_SpecificDestin_crtn(wantedDestinations, ixs_to_instantly, 0.3f) );
	    }


	    IEnumerator Lerp_to_SpecificDestin_crtn( List<CameraPovInfo> wantedDestinations,  List<int> ixs_to_instantly,  float dur ){

	        List<CameraPovInfo> fromPovs = UserCameras_MGR.instance?.get_viewCams_PovInfos();
	        List<Vector2> from_centers01 = fromPovs.Select(p=>p.perspectiveCenter01.toVec2()).ToList();
	        List<Vector2> to_centers01   = wantedDestinations.Select(p=>p.perspectiveCenter01.toVec2()).ToList();
	        if(ixs_to_instantly!=null){ //for those that have to be instantly at the end, make their 'from' to be equal to the destination:
	            ixs_to_instantly.ForEach( ix=>from_centers01[ix] = to_centers01[ix] );
	        }

	        float startTime = Time.time;
	        while(true){
	            float elapsed01 = Mathf.Clamp01( (Time.time-startTime)/dur );
	            float factor01 = Mathf.SmoothStep(0,1,elapsed01);

	            for(int i=0; i<wantedDestinations.Count; ++i){
	                if (wantedDestinations[i].wasEnabled == false){ continue; }
	                Vector2 viewportPos01 =  Vector3.Lerp( from_centers01[i],  to_centers01[i], factor01);
	                UserCameras_MGR.instance?.Set_ProjMatrixCenter_ofCamera( i, viewportPos01);
	            }
	            if(elapsed01 == 1.0f){ break; }
	            yield return null;
	        }
	        _lerpPins_toDefaultPos_crtn = null;
	    }


	    IEnumerator LerpPins_toDefaultPos_crtn( List<CameraPovInfo> povInfos, float dur ){
	        float startTime = Time.time;
	        Transform variant = Get_PinDefaultPos_Variant(povInfos);

	        while(true){
	            float elapsed01 = Mathf.Clamp01(  (Time.time-startTime)/dur  );
	            float factor01 = Mathf.SmoothStep(0, 1, elapsed01);
	            LerpPins(variant, povInfos, factor01);
	            if(elapsed01 == 1.0f){ break; }
	            yield return null;
	        }
	        _lerpPins_toDefaultPos_crtn = null;
	    }//end crtn()

    
	    void LerpPins( Transform variant, List<CameraPovInfo> povInfos, float factor01 ){
	        int numActive = povInfos.Count(p=>p.wasEnabled);
	        int destin_ix = 0;
	        for(int i=0; i<povInfos.Count; ++i){
	            if(povInfos[i].wasEnabled == false){ continue; } //don't increment 'destin_ix' here.

	            var rectTrsf = variant.GetChild(destin_ix) as RectTransform;
	            Vector2 viewportPos01 = rectTrsf.anchorMin;
	            viewportPos01 =  Vector3.Lerp( povInfos[i].perspectiveCenter01.toVec2(), viewportPos01, factor01 );
	            UserCameras_MGR.instance?.Set_ProjMatrixCenter_ofCamera( i, viewportPos01);
	            destin_ix++;
	        }
	    }

	    Transform Get_PinDefaultPos_Variant( List<CameraPovInfo> povInfos ){
	        int numActive = povInfos.Count(p=>p.wasEnabled);
	        RectTransform variantsHolder;
	        switch(numActive){
	            case 0:  variantsHolder = _1_pinPlacementVariants; break;
	            case 1:  variantsHolder = _1_pinPlacementVariants; break;
	            case 2:  variantsHolder = _2_pinPlacementVariants; break;
	            case 3:  variantsHolder = _3_pinPlacementVariants; break;
	            case 4:  variantsHolder = _4_pinPlacementVariants; break;
	            case 5:  variantsHolder = _5_pinPlacementVariants; break;
	            case 6:  variantsHolder = _6_pinPlacementVariants; break;
	            default: variantsHolder = _6_pinPlacementVariants; break;
	        }
	        int numVariants =  variantsHolder.childCount;
	        _placementVariant_ix  %= numVariants;
	        Transform variant =  variantsHolder.GetChild( _placementVariant_ix );
	        return variant;
	    }


	    void Awake(){
	        //deactivate images (those are just to preview default-pin coordinates in editor)
	        _1_pinPlacementVariants.GetComponentsInChildren<Image>().ToList().ForEach( i=>i.enabled=false );
	        _2_pinPlacementVariants.GetComponentsInChildren<Image>().ToList().ForEach( i=>i.enabled=false );
	        _3_pinPlacementVariants.GetComponentsInChildren<Image>().ToList().ForEach( i=>i.enabled=false );
	        _4_pinPlacementVariants.GetComponentsInChildren<Image>().ToList().ForEach( i=>i.enabled=false );
	        _5_pinPlacementVariants.GetComponentsInChildren<Image>().ToList().ForEach( i=>i.enabled=false );
	        _6_pinPlacementVariants.GetComponentsInChildren<Image>().ToList().ForEach( i=>i.enabled=false );
	    }

	    void Start(){
	        CamerasMGR_PinsZone_UI.OnStartInvoked -= InitPins_To_DefaultLocations;
	        CamerasMGR_PinsZone_UI.OnStartInvoked += InitPins_To_DefaultLocations;
	        InitPins_To_DefaultLocations();//invoking manually in case the 'CamerasMGR_PinsZone_UI' already exists.
	    }

	    void InitPins_To_DefaultLocations(){
	        if(UserCameras_MGR.instance == null){
	            // if UserCameras_MGR isn't loaded yet (parallel scenes), wait for it.
	            StartCoroutine( WaitForMgr_AndInit() );
	            return;
	        }
	        List<CameraPovInfo> povs = new List<CameraPovInfo>();
	        for(int i=0; i<UserCameras_MGR.MAX_NUM_VIEW_CAMERAS; ++i){
	            povs.Add( new CameraPovInfo(true,  Vector3.one, Quaternion.identity, 22, Vector2.one*0.5f) );
	        }
	        Transform pinsPlacementVariant =  Get_PinDefaultPos_Variant(povs);
	        LerpPins(pinsPlacementVariant, povs, factor01:1);
	        //first camera should always be in the middle:
	        UserCameras_MGR.instance?.Set_ProjMatrixCenter_ofCamera( 0,  Vector2.one*0.5f );
	    }

	    IEnumerator WaitForMgr_AndInit(){
	        while(UserCameras_MGR.instance == null){ yield return null; }
	        InitPins_To_DefaultLocations();
	    }
	}
}//end namespace
