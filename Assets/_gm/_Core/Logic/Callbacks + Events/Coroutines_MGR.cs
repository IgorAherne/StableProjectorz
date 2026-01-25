using UnityEngine;

namespace spz {

	// Always exists, give it your coroutine and it will run
	// even if its Monobehavior becomes disabled.
	public class Coroutines_MGR : MonoBehaviour{
	    public static Coroutines_MGR instance { get; private set; } = null;
	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	    }
	}
}//end namespace
