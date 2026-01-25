using System;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class BrushHardness_ChoiceButton_UI : MonoBehaviour{
	    [SerializeField] Button _but;
	    [SerializeField] Image _image;
	    [SerializeField] Color _bgCol_negative;
	    [SerializeField] Color _bgCol_positive;
   
	    public Action onClick { get; set; } = null;

	    //isPositive: are we adding or erasing color
	    public void SetMode(bool isPositive){
        
	    }

	    public void Assign(int hardness, Sprite sprite){
	        _image.sprite = sprite;
	        ScaleIcon();
	    }

	    public void ScaleIcon(){//softer brush icons need to be larger, to see them better
	        var extraScale = new Vector3[]{ 
	            new Vector3(1.9f, 1.5f, 1.5f), //stretched more in x, so that text is seen better.
	            Vector3.one*1.07f, 
	            Vector3.one*1.02f 
	        };
	    }

	    void Awake(){
	        _but.onClick.AddListener( ()=>onClick?.Invoke() );
	        _image.material = new Material(_image.material);
	    }

	    void OnDestroy(){
	        DestroyImmediate(_image.material);
	    }
	}
}//end namespace
