using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace spz {

	// If repo folder exists but we are not connected,
	// will pulsate a 'Restart Trellis' button.
	// This hints the user they can press it to launch a CMD server-window
	public class RestartTrellis_Attention_UI : ServerRestartButton_Attention_UI{
	    [SerializeField] protected RestartTheWebui _restartServer_button;

	    protected void OnRestartServer_Clicked(){
	        _recent_pressTime = Time.time;
	    }

	    protected override bool isAttentionAnim(){
	        if(DimensionMode_MGR.instance._dimensionMode != DimensionMode.dim_gen_3d){ return false; }
	        if(Connection_MGR.is_3d_connected){ return false; }//already established connection to Trellis Server.
	        return isAttentionAnim();
	    } 

	    protected void Update(){
	        _restartServer_button.KeepPlaying_attention_anim( isAttentionAnim() );
	    }

	    protected void Start(){
	        _restartServer_button.OnClicked += OnRestartServer_Clicked;
	    }
	}
}//end namespace
