using UnityEngine;
using UnityEngine.SceneManagement;

namespace spz {

	// Copies RectTransform layout values from a source RectTransform in another scene.
	// This component is ideal for synchronizing UI element layouts at runtime.
	// It operates in LateUpdate to ensure it captures the final position of the source from the current frame.
	public class RectTransform_CopyVals : MonoBehaviour
	{
	    [SerializeField] RectTransform _thisRectTransform;

	    private void Awake()
	    {
	        _thisRectTransform = GetComponent<RectTransform>();
	    }

	    private void Start(){
	        // If the source isn't already assigned, try to find it.
	    }

	    private void LateUpdate(){
        
	        //if (sourceRectTransform == null || _thisRectTransform == null){
	        //    return;
	        //}
	        //// Copy the essential layout properties from the source.
	        //// World position, rotation, and scale are not copied directly,
	        //// as they are a result of these layout properties within a Canvas.
	        //_thisRectTransform.anchorMin = sourceRectTransform.anchorMin;
	        //_thisRectTransform.anchorMax = sourceRectTransform.anchorMax;
	        //_thisRectTransform.anchoredPosition = sourceRectTransform.anchoredPosition;
	        //_thisRectTransform.sizeDelta = sourceRectTransform.sizeDelta;
	        //_thisRectTransform.pivot = sourceRectTransform.pivot;
	    }
	}
}//end namespace
