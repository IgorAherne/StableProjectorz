using UnityEngine;

namespace spz {

	//receives Update() before all other scripts. 
	//invokes update for other scripts.
	public class EarlyUpdate_callbacks_MGR : MonoBehaviour{
	    public static EarlyUpdate_callbacks_MGR instance { get; private set; } = null;

	    public System.Action onEarlyUpdate0 { get; set; } = null;//earliest of them all
	    public System.Action onEarlyUpdate1 { get; set; } = null;
	    public System.Action onEarlyUpdate2 { get; set; } = null;
	    public System.Action onEarlyUpdate3 { get; set; } = null;//latest, but still before the Update() of all others.

	    // Update is called once per frame
	    void Update(){
	        onEarlyUpdate0?.Invoke();
	        onEarlyUpdate1?.Invoke();
	        onEarlyUpdate2?.Invoke();
	        onEarlyUpdate3?.Invoke();
	    }

	    void Awake(){
	        if(instance != null){  DestroyImmediate(this.gameObject); return; }
	        instance = this;
	    }
	}
}//end namespace
