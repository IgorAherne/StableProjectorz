using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	public class LoadIntroScreen_Panel_UI : MonoBehaviour{


	    [SerializeField] Animator _animator;
	    public static bool isShowing { get; private set; } = false;

	    void Awake(){
	        isShowing = true;
	        _animator.SetTrigger("playIntro");
	        _animator.speed = 0;
	    }

	    int numFrames = 0;
	    void Update(){
	        numFrames++;
	        if(numFrames < 3){ return; }//to avoid massive spike at start, while everything loads.
	        if (numFrames == 3){ _animator.speed=1; }

	            var info = _animator.GetCurrentAnimatorStateInfo(0);
	        if (info.IsName("Completed")){ 
	            Destroy(this.gameObject);//faded away, destroy self and our whole canvas.
	            isShowing = false;
	        }
	    }

	}

}//end namespace
