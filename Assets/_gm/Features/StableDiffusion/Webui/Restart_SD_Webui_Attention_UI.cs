using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// will pulsate a 'Restart Webui' button.
	// This hints the user they can press it to launch a CMD server-window.
	public class Restart_SD_Webui_Attention_UI : ServerRestartButton_Attention_UI{
	   protected override bool isAttentionAnim(){
	        if(DimensionMode_MGR.instance._dimensionMode != DimensionMode.dim_sd){ return false; }
	        if(Connection_MGR.is_sd_connected){ return false; }//already established connection to Trellis Server.
	        if(base.isAttentionAnim()==false){ return false; }
	        if(Time.time < 25){ 
	            return false;//wait if at the very start (A1111 webui opens automatically, maybe still starting up).
	        }
	        return true;
	    } 
	}


	public class ServerRestartButton_Attention_UI : MonoBehaviour{
    
	    protected float _recent_pressTime = -999;

	    protected virtual bool isAttentionAnim(){
	        if (Time.frameCount < 5){ return false; }//wait, our panel might be temporarily on, during init.
	        if(Time.time - _recent_pressTime < 40){ return false; }//wait, server might already be starting.
	        return true;
	    }
	}
}//end namespace
