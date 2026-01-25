using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	public class SP_Comment : MonoBehaviour
	{
	    [TextArea(3, 10)]  // Makes the field multi-line in the inspector
	    public string comment = "";
	}
}//end namespace
