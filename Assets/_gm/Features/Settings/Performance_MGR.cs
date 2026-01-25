using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	public class Performance_MGR : MonoBehaviour{
	    public static Performance_MGR instance { get; private set; } = null;


	    public bool isThrottleFPS_whenGenerating()
	        => EventsBinder.FindObj<ReduceFPS_Toggle_UI>( nameof(ReduceFPS_Toggle_UI) )? .throttleFPS_whenGenerating?? false;


	    SceneResolution_MGR getSceneRes_ui()
	        => EventsBinder.FindObj<SceneResolution_MGR>( nameof(SceneResolution_MGR) );


	    void Update(){
	        Optimize_maybe();
	        RevertOptimizations_maybe();
	    }

	    //see if we should optimize stuff while SD is working:
	    void Optimize_maybe(){
	        var sceneResUI = getSceneRes_ui();
	        if(sceneResUI == null){ return; }

	        if(sceneResUI._isSavingProject_keepResolution4k){ return; }//the project file is being saved to disk.
	        if(sceneResUI._isWillGenArt_keepResolution5k){ return; }
	        if(isThrottleFPS_whenGenerating()){ return; }

	        if(GenerateButtons_UI.isGenerating==false){ return; }

	        // Important frames before we send of the render.
	        // So, we must maintain the quality high, to prevent sending low-res image to controlnets:
	        if(StableDiffusion_Hub.instance._finalPreparations_beforeGen){ return; }

	        if(sceneResUI.HasMemorizeRes()){ return; }//already captured the tex quality
	        sceneResUI.OnAdd_texResolutionQuality( increase:false, force_pickThisRes:256, memorize_before:true);
	        Resources.UnloadUnusedAssets();
	    }

	    void RevertOptimizations_maybe(){ 
	        var sceneResUI = getSceneRes_ui();
	        if(sceneResUI == null){ return; }

	        if(sceneResUI.HasMemorizeRes()==false){ return; }//nothing to revert (all was reverted in past)

	        bool doThrottle = isThrottleFPS_whenGenerating();
	        if(doThrottle && GenerateButtons_UI.isGenerating){ return; }//don't revert any optimizations yet.
	        //else revert the optimizations, restoring to prior resolution:
	        sceneResUI.RevertRes_from_Memorized();
	    }

	    public void Save(StableProjectorz_SL spz){
	        getSceneRes_ui().Save(spz);
	    }

	    public void Load(StableProjectorz_SL spz){
	        getSceneRes_ui().Load(spz);
	    }


	    void Awake(){
	        if (instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;
	    }
	}
}//end namespace
