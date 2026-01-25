using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace spz {

	// UI button, changes number of rows in a Art-list grid.
	// Used kinda like a setting/preference
	public class NumIconsPerRow_Button :MonoBehaviour{
	    [SerializeField] Button _button;
	    [SerializeField] TextMeshProUGUI _text;
	    public int _num { get; private set; } = 2;
	    public System.Action<int> onNumPerRow_changed { get; set; } = null;

	    void OnButtonPressed(){
	        _num++;
	        if(_num > 4){ _num=2; }
	        _text.text = "x"+_num;
	        onNumPerRow_changed?.Invoke(_num);
	    }

	    public void Press_Manually(int forceThisNum){
	        _num = forceThisNum-1;
	        OnButtonPressed();
	    }

	    void Awake(){ 
	        _button.onClick.AddListener( OnButtonPressed );
	    }
	}
}//end namespace
