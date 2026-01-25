using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class VideoTutorial_Button : MonoBehaviour
	{
	    [SerializeField] Button _button;
	    [Header("Relative to StreamingAssets folder:")]
	    [SerializeField] string _videoClipNameWithExten;

	    //arg is the video name, relative to streaming assets. ("MyTutorial.mp4")
	    public System.Action<string> onPressed { get; set; } 

	    void OnButton(){
	        onPressed?.Invoke(_videoClipNameWithExten);
	    }

	    void Awake(){
	        _button.onClick.AddListener( OnButton );
	    }
	}
}//end namespace
