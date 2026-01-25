using UnityEngine;
using System.Collections.Generic;

namespace spz {

	// Prevents you from accidentally painting / adjusting masks or clicking somewhere.
	// For example, while showing some ui-popup or while saving your work in a file-browser.
	public class GlobalClickBlocker : MonoBehaviour
	{
	    static List<GlobalClickBlocker> _list = new List<GlobalClickBlocker>();
	    static LocksHashset _lock = new LocksHashset();

	    public static void Lock(object who_is_requesting){
	        _lock.Lock(who_is_requesting);
	        _list.ForEach(b => b.gameObject.SetActive(true)); //enable (blocks clicks)
	    }

	    public static void Unlock_if_can(object who_is_requesting){
	        _lock.Unlock(who_is_requesting);
	        if (_lock.isLocked()) { return; }
	        _list.ForEach(b => b.gameObject.SetActive(false)); //disable
	    }

	    public static bool isLocked(){
	        return _lock.isLocked();
	    }

	    void Awake(){
	        _list.Add(this);
	        gameObject.AddComponent<NonDrawingGraphic>();
	        RectTransform rectTransform = transform as RectTransform;
	        rectTransform.anchorMin = Vector3.zero;
	        rectTransform.anchorMax = Vector3.one;
	        rectTransform.offsetMin = Vector3.zero;
	        rectTransform.offsetMax = Vector3.zero;
	    }

	    private void Start(){
	        gameObject.SetActive(false);
	    }

	    void OnDestroy(){
	        _list.Remove(this);
	    }
	}
}//end namespace
