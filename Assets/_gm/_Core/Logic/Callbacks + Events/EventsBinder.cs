using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace spz {

	// Binds UI components or GameObjects to string IDs.
	// If component is pressable (button, slider, etc) - ensures it will also trigger any event associated to that string id.
	// Works together with 'StaticEvents'
	//
	// Automatically cleans-up references if the bound Object becomes destroyed.
	// Uses a cached key list for cleanup to avoid garbage collection every frame.
	public static class EventsBinder
	{
	    private static readonly Dictionary<string, UnityEngine.Object> _boundObjects = new Dictionary<string, UnityEngine.Object>();
	    private static readonly List<string> _keysToRemove = new List<string>();

	    // State for optimized, non-allocating cleanup
	    private static List<string> _cachedKeyList = new List<string>();
	    private static bool _isKeyCacheDirty = true; // Start dirty to force initial build.
	    private static int _cleanupIndex = 0;


	    // Retrieves a bound object by its event ID and casts it to the specified type.
	    // This can retrieve Components, GameObjects, or any other UnityEngine.Object.
	    public static T FindObj<T>(string eventID) where T : UnityEngine.Object
	    {
	        bool found = _boundObjects.TryGetValue(eventID, out UnityEngine.Object obj);
	        if (!found){ return null; }

	        if (obj == null){ // This uses Unity's custom null check for destroyed objects
	            _boundObjects.Remove(eventID);
	            _isKeyCacheDirty = true; // A removal dirties the cache.
	            return null;
	        }
	        // Handle casting for Components, GameObjects, etc.
	        if (obj is T result) return result;
	        if (obj is GameObject go) return go.GetComponent<T>();
	        if (obj is Component c) return c.GetComponent<T>();

	        return null;
	    }

    
	    // A convenient helper for the common case of retrieving a GameObject.
	    public static GameObject FindGameObj(string eventID){
	        bool found = _boundObjects.TryGetValue(eventID, out UnityEngine.Object obj);
	        if (!found){ return null; }

	        if (obj == null){
	            _boundObjects.Remove(eventID);
	            _isKeyCacheDirty = true;
	            return null;
	        }
	        if (obj is GameObject go) return go;
	        if (obj is Component c) return c.gameObject;
        
	        return null;
	    }


	    // A convenient helper for the common case of retrieving a Component.
	    public static T FindComponent<T>(string eventID) where T : UnityEngine.Component {
	        bool found = _boundObjects.TryGetValue(eventID, out UnityEngine.Object obj);
	        if (!found){ return null; }

	        if (obj == null){
	            _boundObjects.Remove(eventID);
	            _isKeyCacheDirty = true;
	            return null;
	        }
	        if (obj is T result) return result;
	        if (obj is GameObject go) return go.GetComponent<T>();
	        return null;
	    }


	    // Associates the object with an event-id. Also, searches for components that can be pressed etc.
	    // if found, ensures the component will trigger the event, of that eventId.
	    public static void Bind_Clickable_to_event(string eventID_to_invoke, UnityEngine.Object uiObject)
	    {
	        if (uiObject == null){
	            Debug.LogWarning($"UIEventBinder: Attempted to bind a null object to ID '{eventID_to_invoke}'.");
	            return;
	        }
	        _boundObjects[eventID_to_invoke] = uiObject;
	        _isKeyCacheDirty = true; // Any addition or modification dirties the cache.
        
	        // Find the GameObject to search for interactive components.
	        GameObject targetGameObject = (uiObject is GameObject go) ? go : (uiObject as Component)?.gameObject;
	        if (targetGameObject == null){ return; }

	        // Attempt to bind an event listener. It's okay if none is found;
	        // the object remains bound for retrieval via GetComponent.
	        if (targetGameObject.TryGetComponent<SliderUI_Snapping>(out var customSlider)){
	            customSlider.onValueChanged.AddListener(val => StaticEvents.Invoke<float>(eventID_to_invoke, val));
	            return;
	        }
	        if (targetGameObject.TryGetComponent<IntegerInputField>(out var intInput)){
	            intInput.onValidInput.AddListener(val => StaticEvents.Invoke<int>(eventID_to_invoke, val));
	            return;
	        }
	        if (targetGameObject.TryGetComponent<FloatInputField>(out var floatInput)){
	            floatInput.onValidInput.AddListener(val => StaticEvents.Invoke<float>(eventID_to_invoke, val));
	            return;
	        }
	        if (targetGameObject.TryGetComponent<Button>(out var button)){
	            button.onClick.AddListener(() => StaticEvents.Invoke(eventID_to_invoke));
	            return;
	        }
	        if (targetGameObject.TryGetComponent<Toggle>(out var toggle)){
	            toggle.onValueChanged.AddListener(val => StaticEvents.Invoke<bool>(eventID_to_invoke, val));
	            return;
	        }
	        if (targetGameObject.TryGetComponent<Slider>(out var slider)){
	            slider.onValueChanged.AddListener(val => StaticEvents.Invoke<float>(eventID_to_invoke, val));
	            return;
	        }
	        if (targetGameObject.TryGetComponent<TMP_InputField>(out var tmpInput)){
	            tmpInput.onValueChanged.AddListener(val => StaticEvents.Invoke<string>(eventID_to_invoke, val));
	            return;
	        }
	    }
    

	    // Called once per frame to incrementally clean up destroyed object references.
	    public static void OnUpdate()
	    {
	        if (_boundObjects.Count == 0) return;

	        // Rebuild the key cache only if the dictionary has been modified.
	        if (_isKeyCacheDirty){
	            _cachedKeyList.Clear();
	            _cachedKeyList.AddRange(_boundObjects.Keys);
	            _isKeyCacheDirty = false;
	            // Reset index if it's now out of bounds.
	            if (_cleanupIndex >= _cachedKeyList.Count) _cleanupIndex = 0;
	        }

	        if (_cachedKeyList.Count == 0) return;

	        int itemsChecked = 0;
	        const int maxItemsPerFrame = 100;

	        while (itemsChecked < maxItemsPerFrame && _cleanupIndex < _cachedKeyList.Count)
	        {
	            string key = _cachedKeyList[_cleanupIndex];
            
	            // If a key exists in our cache but not the dictionary, it was removed by GetComponent.
	            // Otherwise, check if the object itself has been destroyed.
	            if (!_boundObjects.ContainsKey(key) || _boundObjects[key] == null){
	                _keysToRemove.Add(key);
	            }
	            _cleanupIndex++;
	            itemsChecked++;
	        }
	        // If we found any keys to remove, process them now.
	        if (_keysToRemove.Count > 0){
	            foreach (var key in _keysToRemove){
	                _boundObjects.Remove(key);
	            }
	            _keysToRemove.Clear();
	            _isKeyCacheDirty = true; // The removals dirty the cache for the next frame.
	        }
        
	        if (_cleanupIndex >= _cachedKeyList.Count){
	            _cleanupIndex = 0;// Loop back to the start for the next cleanup cycle.
	        }
	    }
	}
}//end namespace
