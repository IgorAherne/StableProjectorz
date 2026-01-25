using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Signleton; Sits next to the canvas.
	// We can temporarily parent the icons/thumbnails under it, while the are being dragged.
	public class UI_CurrentlyDraggedIcons_Canvas : MonoBehaviour
	{
	    public static UI_CurrentlyDraggedIcons_Canvas instance { get; private set; } = null;

	    [SerializeField] Canvas _canvas;
	               public Canvas canvas => _canvas;

	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return;}
	        instance = this;
	    }
	}
}//end namespace
