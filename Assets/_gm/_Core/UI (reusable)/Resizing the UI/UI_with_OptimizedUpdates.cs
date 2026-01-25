using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace spz {

	// During Editor, invokes updates freely.
	// During Runtime, you can specify how soon to unsubscribe from receiving updates.
	// This saves perfromance during runtime, significantly, because there are many of UI elements 
	// that don't need to continiously re-resize.
	[ExecuteInEditMode]
	public abstract class UI_with_OptimizedUpdates : UIBehaviour {

		// if true, we assume the parent will never stretch during the game.
	    // Therefore, when playing the game, adjustment will happen only a few times, 
	    // during early game (in play mode).  This saves peformance and can be quite significant.
	    // You need several frames, because nested elements might take several frames to catch-up.
	    //
	    // Use -1 to never hibernate and to always update (but is less performant)
	    [SerializeField] int _stopUpdates_afterNumFrames = 4;
	    public int stopUpdates_afterNumFrames{  get{ return _stopUpdates_afterNumFrames; }  }


	    bool _init_invoked = false;


	#region init
	    //[ExecuteInEditMode]
	    protected override void Awake(){   
	        if(CanInvoke_Init()==false){  return;  }
	        Init();
	    }


	    bool CanInvoke_Init(){
	        #if UNITY_EDITOR
	        if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode == false){ return false; }
	        #endif

	        if(_init_invoked){ return false; }//we might have already been explicitly initialized previously.
	        _init_invoked = true;
	        return true;
	    }

	    //NOTICE:  called either during Awake()  OR during  Monobehoover::Start_FirstUpdate()
	    void Init(){
	        if(_stopUpdates_afterNumFrames > 0){
	            // user wants to update only a few times 
	            // (to save performance) during init.
	            OnUpdate();
	            return;
	        }
	    }

	    // carefull, OnDestroy() might also be called during editor [ExecuteInEditMode], not just during the gameplay.
	    // So (just in case), make it explicit that no serializations is to be done:
	    [System.NonSerialized] bool _destroyCalled = false;
    
	    //[ExecuteInEditMode]
	    protected sealed override void OnDestroy(){
	        if(_destroyCalled){ return; }
	        _destroyCalled = true;
	        OnDestroyCallback();
	    }

	    //[ExecuteInEditMode]
	    protected virtual void OnDestroyCallback(){  }
	#endregion



	    // using instead of OnUpdate(), only called a few times during the early game.
	    // Used if user wishes to save performance.
	    public int optimizedUpdatesRan{ get; private set; } = 0;
   
	    // [ExecuteInEditMode]
	    private void Update(){
	        if( optimizedUpdatesRan >= _stopUpdates_afterNumFrames){  return;  }

	        bool b = true;
	#if UNITY_EDITOR
	        if(UnityEditor.EditorApplication.isPlaying==false){ b = false; }
	#endif
	        if(b){  optimizedUpdatesRan++;  }
	        OnUpdate();
	        var fitters = GetComponentsInChildren<ContentSizeFitter>();
	        foreach(var f in fitters){
	            f.enabled = false;
	            f.enabled = true;
	        }
	    }


	    // NOTICE: either called by THIS class  manually during Runtime,  or via our [ExecuteInEditMode] if in editor
	    protected abstract void OnUpdate();


	    //force update our sizes imediatelly.
	    public void ManuallyUpdate(){  OnUpdate();  }
	}
}//end namespace
