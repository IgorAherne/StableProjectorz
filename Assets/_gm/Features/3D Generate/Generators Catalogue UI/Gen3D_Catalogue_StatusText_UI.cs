using TMPro;
using UnityEngine;

namespace spz {

	// Belongs to the Gen3D_Catalogue_UI, which is for showing 3d-generators, available for download.
	// Able to show messages, while the usual 'Viewport_StatusText' is obscured.
	public class Gen3D_Catalogue_StatusText_UI : MonoBehaviour
	{
	    [SerializeField] CanvasGroup _canvasGroup;
	    [SerializeField] TextMeshProUGUI _text;
	    float _timer;

	    public void ShowStatusText(string msg, float duration){
	        _text.text = msg;
	        _timer = duration + 1; //+1 for fading-out
	    }
    
	    void Update(){
	        if (_timer > 0){
	            _timer -= Time.deltaTime;
	            _canvasGroup.alpha = Mathf.Clamp(_timer, 0, 1);
	        }
	    }

	    void Start(){
	        _canvasGroup.alpha = 0;
	        _text.text = "";
	    }
	} 
}//end namespace
