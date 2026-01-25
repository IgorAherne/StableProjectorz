using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	public class ExitTheProgram_MGR : MonoBehaviour
	{
	    bool _quitPopupConfirmed = false;

	    void Awake(){
	        Application.wantsToQuit += WantsToQuit;
	    }

	    bool WantsToQuit(){
	        if(_quitPopupConfirmed){ return true; }
        
	        if(ConfirmPopup_UI.instance==null){ 
	            OnExitConfirm(); 
	            return true; 
	        }
	        ConfirmPopup_UI.instance.Show("Close the program? Make sure to save progress first (Ctrl+S)", OnExitConfirm, OnExitCanceled, "Close", "Don't Close");
	        return false;
	    }


	    void OnExitConfirm(){
	        _quitPopupConfirmed = true;
	        Application.Quit();
	    }

	    void OnExitCanceled(){
	        //do nothing.
	    }
	}
}//end namespace
