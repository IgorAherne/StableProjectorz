using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class SD_Seed_Input_UI : MonoBehaviour{
	    [SerializeField] IntegerInputField _intInput;
	    [SerializeField] Button _randomize_button;

	    public int recentVal => _intInput.recentVal;

	    private void Start(){
	        _randomize_button.onClick.AddListener(OnRandomizeButton);
	    }

	    void OnRandomizeButton(){
	        _intInput.SetValueWithoutNotify("-1");
	    }
	}
}//end namespace
