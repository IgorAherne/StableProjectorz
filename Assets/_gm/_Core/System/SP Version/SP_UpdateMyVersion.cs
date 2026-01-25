using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace spz {

	// After loads, asks the SP_Version to swap the special version <> tags with the correct value,
	// so that we don't forget to do it.
	public class SP_UpdateMyVersion : MonoBehaviour
	{
	    [SerializeField] TextMeshProUGUI _myText;
	    void Start(){
	        _myText.text = SP_Version.instance.update_version_inText(_myText.text, gameObject);
	    }
	}
}//end namespace
