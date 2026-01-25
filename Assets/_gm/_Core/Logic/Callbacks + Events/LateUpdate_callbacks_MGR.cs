using UnityEngine;
using System.Collections;

namespace spz {

	//receives LateUpdate() after all other scripts. 
	//invokes it for other scripts.
	//This means they will be last to receive callback during  "LateUpdate" stage.
	public class LateUpdate_callbacks_MGR : MonoBehaviour{
	    public static LateUpdate_callbacks_MGR instance { get; private set; } = null;

	    public System.Action onLateUpdate { get; set; } = null;
	    public System.Action onLateUpdate_postRender { get; set; } = null;

	    // Update is called once per frame
	    void LateUpdate() => onLateUpdate?.Invoke();

	    void Awake(){
	        if(instance != null){  DestroyImmediate(this.gameObject); return; }
	        instance = this;
	        StartCoroutine( PostRender_crtn() );
	    }

	    IEnumerator PostRender_crtn(){
	        while (true){
	            yield return null;//wait until all rendering is complete
	            onLateUpdate_postRender?.Invoke();
	        }
	    }
	}
}//end namespace
