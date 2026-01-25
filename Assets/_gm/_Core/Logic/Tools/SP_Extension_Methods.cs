using System;
using System.Collections.Generic;
using UnityEngine;


namespace spz {

	public static class RectTransformExtensions
	{
	    // Copies the core layout properties from a source RectTransform to this one.
	    public static void CopyValsFrom(this RectTransform target, RectTransform source){
	        if (target == null || source == null){ return; }
	        target.anchorMin = source.anchorMin;
	        target.anchorMax = source.anchorMax;
	        target.anchoredPosition = source.anchoredPosition;
	        target.sizeDelta = source.sizeDelta;
	        target.pivot = source.pivot;
	    }
	}


	public static class DictionaryExtensions{
	    //obtains value if it exists. Otherwise uses your lambda to add new one.
	    public static TValue GetOrAddValue<TKey,TValue>( this Dictionary<TKey,TValue> dictionary, 
	                                                     TKey key,  Func<TValue> valueFactory ){
	        if (dictionary.TryGetValue(key, out TValue value)){ 
	            return value; 
	        }
	        value = valueFactory();
	        dictionary.Add(key,value);
	        return value;
	    }

	    public static void UpdateOrAddValue<TKey,TValue>( this Dictionary<TKey,TValue> dictionary, 
	                                                      TKey key,   TValue val ){
	        if (dictionary.ContainsKey(key)){
	            dictionary[key] = val;
	        }else { 
	            dictionary.Add(key, val);
	        }
	    }


	    public static void DestroyImmediateAll<TKey,TValue>(this Dictionary<TKey,TValue> dictionary) 
	                                                                        where TValue:UnityEngine.Object{
	        foreach(var kvp in dictionary){
	            if(kvp.Value==null){ continue; }
	            UnityEngine.Object.DestroyImmediate(kvp.Value);
	        }
	    }//end()
	}


	public static class ReadOnlyListExtensions{

	    public static void ForEach<T>(this IReadOnlyList<T> list, Action<T> action)
	    {
	        if(list == null){ throw new ArgumentNullException(nameof(list)); }
	        if(action == null){ throw new ArgumentNullException(nameof(action)); }
	        for(int i=0; i<list.Count; i++){  action(list[i]);  }
	    }
	}


	public static class Vector3Extensions{
	    public static Vector3 Multiply(this Vector3 vec, Vector3 other)
	        => new Vector3(vec.x*other.x, vec.y*other.y, vec.z*other.z);
	}
}//end namespace
