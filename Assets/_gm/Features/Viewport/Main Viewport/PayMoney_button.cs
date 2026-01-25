using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class PayMoney_button : MonoBehaviour{
	    [SerializeField] Button _button;
    
	    void Awake(){
	        _button.onClick.AddListener(OnButtonPressed);
	    }

	    void OnButtonPressed(){
	        Application.OpenURL("https://stableprojectorz.com/thanks/");
	    }
	}
}//end namespace
