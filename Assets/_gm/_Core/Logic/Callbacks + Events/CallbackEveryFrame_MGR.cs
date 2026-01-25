using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// even if your Monobehavior is not enabled, you can keep
	// getting updates from here, every frame
	public class CallbackEveryFrame_MGR : MonoBehaviour{
	    public static System.Action onUpdate { get; set; } = null;

	    void Update(){
	        onUpdate?.Invoke();
	    }
	}
}//end namespace
