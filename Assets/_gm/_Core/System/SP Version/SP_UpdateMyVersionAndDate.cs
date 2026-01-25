using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace spz {

	// After loads, asks the SP_Version to swap the date <> tags with the correct value,
	// so that we don't forget to do it.
	public class SP_UpdateMyVersionAndDate : MonoBehaviour
	{
	    [SerializeField] TextMeshProUGUI _myText;

	    void Start(){
	        _myText.text = SP_Version.instance.update_versionAndDate_inText(_myText.text, gameObject);
	    }

	}
}//end namespace
