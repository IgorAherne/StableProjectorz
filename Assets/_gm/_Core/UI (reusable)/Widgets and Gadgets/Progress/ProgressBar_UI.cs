using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace spz {

	public class ProgressBar_UI : MonoBehaviour
	{
	    [SerializeField] TextMeshProUGUI _progressText;
	    [SerializeField] RectTransform _scaleMe;

	    public void SetProgress(float val01, string prefix="Downloading"){
	        _scaleMe.localScale = new Vector3(val01, 1, 1);
	        int pcnt = Mathf.RoundToInt(val01 * 100);
	        _progressText.text = $"{prefix} {pcnt.ToString() }%";
	    }
	}
}//end namespace
