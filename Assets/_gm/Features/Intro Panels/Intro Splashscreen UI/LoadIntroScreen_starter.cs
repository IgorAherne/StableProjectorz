using UnityEngine;

namespace spz {

	//this one is always enabled during the start.
	//Will tell the LoadIntroScreen to activate now, which will show large canvas, and the "intro animations"
	public class LoadIntroScreen_starter : MonoBehaviour{
	    [SerializeField] LoadIntroScreen_Panel_UI _loadIntroScreen;
	    void Awake(){
	        //#if UNITY_EDITOR
	        //return;//keeps bothering when I constantly need to test something. So disabling in the editor
	        //#endif
	        _loadIntroScreen.gameObject.SetActive(true);
	    }
	}
}//end namespace
