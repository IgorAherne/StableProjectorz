using System;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// Allows someone to subscribe by ID, without caring who will invoke an ID later.
	// Works together with 'UIEventBinder'.
	public static class StaticEvents
	{
	    private static readonly Dictionary<string, Delegate> _actionsDict = new Dictionary<string, Delegate>();


	    // INVOKE

	    // Relates the component to this id.
	    // Also, subscribes the ui-component so that it auto-invokes our events when pressed.
	    public static void Bind_Clickable_to_event(string eventId, Component uiComponent){
	        EventsBinder.Bind_Clickable_to_event(eventId, uiComponent);
	    }
	    public static void Invoke(string id) {
	        if (_actionsDict.TryGetValue(id, out var d) && d is Action act) { act?.Invoke(); }
	    }
	    public static void Invoke<T1>(string id, T1 arg1) {
	        if (_actionsDict.TryGetValue(id, out var d) && d is Action<T1> act) { act?.Invoke(arg1); }
	    }
	    public static void Invoke<T1, T2>(string id, T1 arg1, T2 arg2) {
	        if (_actionsDict.TryGetValue(id, out var d) && d is Action<T1, T2> act) { act?.Invoke(arg1, arg2); }
	    }
	    public static void Invoke<T1, T2, T3>(string id, T1 arg1, T2 arg2, T3 arg3) {
	        if (_actionsDict.TryGetValue(id, out var d) && d is Action<T1, T2, T3> act) { act?.Invoke(arg1, arg2, arg3); }
	    }
	    public static void Invoke<T1, T2, T3, T4>(string id, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
	        if (_actionsDict.TryGetValue(id, out var d) && d is Action<T1, T2, T3, T4> act) { act?.Invoke(arg1, arg2, arg3, arg4); }
	    }
	    public static void Invoke<T1, T2, T3, T4, T5>(string id, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) {
	        if (_actionsDict.TryGetValue(id, out var d) && d is Action<T1, T2, T3, T4, T5> act) { act?.Invoke(arg1, arg2, arg3, arg4, arg5); }
	    }
	    public static void Invoke<T1, T2, T3, T4, T5, T6>(string id, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) {
	        if (_actionsDict.TryGetValue(id, out var d) && d is Action<T1, T2, T3, T4, T5, T6> act) { act?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6); }
	    }

	    // SUBSCRIBE UNIQUE

	    public static void SubscribeUnique(string id, Action act) {
	        if (!_actionsDict.ContainsKey(id)) { _actionsDict.Add(id, act); return; }
	        throw new Exception($"Event ID '{id}' is already used. Use SubscribeAppend or a different ID.");
	    }
	    public static void SubscribeUnique<T1>(string id, Action<T1> act) {
	        if (!_actionsDict.ContainsKey(id)) { _actionsDict.Add(id, act); return; }
	        throw new Exception($"Event ID '{id}' is already used. Use SubscribeAppend or a different ID.");
	    }
	    public static void SubscribeUnique<T1, T2>(string id, Action<T1, T2> act) {
	        if (!_actionsDict.ContainsKey(id)) { _actionsDict.Add(id, act); return; }
	        throw new Exception($"Event ID '{id}' is already used. Use SubscribeAppend or a different ID.");
	    }
	    public static void SubscribeUnique<T1, T2, T3>(string id, Action<T1, T2, T3> act) {
	        if (!_actionsDict.ContainsKey(id)) { _actionsDict.Add(id, act); return; }
	        throw new Exception($"Event ID '{id}' is already used. Use SubscribeAppend or a different ID.");
	    }
	    public static void SubscribeUnique<T1, T2, T3, T4>(string id, Action<T1, T2, T3, T4> act) {
	        if (!_actionsDict.ContainsKey(id)) { _actionsDict.Add(id, act); return; }
	        throw new Exception($"Event ID '{id}' is already used. Use SubscribeAppend or a different ID.");
	    }
	    public static void SubscribeUnique<T1, T2, T3, T4, T5>(string id, Action<T1, T2, T3, T4, T5> act) {
	        if (!_actionsDict.ContainsKey(id)) { _actionsDict.Add(id, act); return; }
	        throw new Exception($"Event ID '{id}' is already used. Use SubscribeAppend or a different ID.");
	    }
	    public static void SubscribeUnique<T1, T2, T3, T4, T5, T6>(string id, Action<T1, T2, T3, T4, T5, T6> act) {
	        if (!_actionsDict.ContainsKey(id)) { _actionsDict.Add(id, act); return; }
	        throw new Exception($"Event ID '{id}' is already used. Use SubscribeAppend or a different ID.");
	    }


	    // SUBSCRIBE APPEND

	    public static void SubscribeAppend(string id, Action act) {
	        if (!_actionsDict.TryGetValue(id, out var d)) { _actionsDict.Add(id, act); return; }
	        if (d is Action existing) { _actionsDict[id] = existing + act; return; }
	        throw new Exception($"Event ID '{id}' is already registered with a different signature.");
	    }
	    public static void SubscribeAppend<T1>(string id, Action<T1> act) {
	        if (!_actionsDict.TryGetValue(id, out var d)) { _actionsDict.Add(id, act); return; }
	        if (d is Action<T1> existing) { _actionsDict[id] = existing + act; return; }
	        throw new Exception($"Event ID '{id}' is already registered with a different signature.");
	    }
	    public static void SubscribeAppend<T1, T2>(string id, Action<T1, T2> act) {
	        if (!_actionsDict.TryGetValue(id, out var d)) { _actionsDict.Add(id, act); return; }
	        if (d is Action<T1, T2> existing) { _actionsDict[id] = existing + act; return; }
	        throw new Exception($"Event ID '{id}' is already registered with a different signature.");
	    }
	    public static void SubscribeAppend<T1, T2, T3>(string id, Action<T1, T2, T3> act) {
	        if (!_actionsDict.TryGetValue(id, out var d)) { _actionsDict.Add(id, act); return; }
	        if (d is Action<T1, T2, T3> existing) { _actionsDict[id] = existing + act; return; }
	        throw new Exception($"Event ID '{id}' is already registered with a different signature.");
	    }
	    public static void SubscribeAppend<T1, T2, T3, T4>(string id, Action<T1, T2, T3, T4> act) {
	        if (!_actionsDict.TryGetValue(id, out var d)) { _actionsDict.Add(id, act); return; }
	        if (d is Action<T1, T2, T3, T4> existing) { _actionsDict[id] = existing + act; return; }
	        throw new Exception($"Event ID '{id}' is already registered with a different signature.");
	    }
	    public static void SubscribeAppend<T1, T2, T3, T4, T5>(string id, Action<T1, T2, T3, T4, T5> act) {
	        if (!_actionsDict.TryGetValue(id, out var d)) { _actionsDict.Add(id, act); return; }
	        if (d is Action<T1, T2, T3, T4, T5> existing) { _actionsDict[id] = existing + act; return; }
	        throw new Exception($"Event ID '{id}' is already registered with a different signature.");
	    }
	    public static void SubscribeAppend<T1, T2, T3, T4, T5, T6>(string id, Action<T1, T2, T3, T4, T5, T6> act) {
	        if (!_actionsDict.TryGetValue(id, out var d)) { _actionsDict.Add(id, act); return; }
	        if (d is Action<T1, T2, T3, T4, T5, T6> existing) { _actionsDict[id] = existing + act; return; }
	        throw new Exception($"Event ID '{id}' is already registered with a different signature.");
	    }


	    // UNSUBSCRIBE

	    public static void Unsubscribe(string id, Action act) {
	        if (!_actionsDict.TryGetValue(id, out var d) || d is not Action existing) { return; }
	        var newAct = existing - act;
	        if (newAct == null) { _actionsDict.Remove(id); } else { _actionsDict[id] = newAct; }
	    }
	    public static void Unsubscribe<T1>(string id, Action<T1> act) {
	        if (!_actionsDict.TryGetValue(id, out var d) || d is not Action<T1> existing) { return; }
	        var newAct = existing - act;
	        if (newAct == null) { _actionsDict.Remove(id); } else { _actionsDict[id] = newAct; }
	    }
	    public static void Unsubscribe<T1, T2>(string id, Action<T1, T2> act) {
	        if (!_actionsDict.TryGetValue(id, out var d) || d is not Action<T1, T2> existing) { return; }
	        var newAct = existing - act;
	        if (newAct == null) { _actionsDict.Remove(id); } else { _actionsDict[id] = newAct; }
	    }
	    public static void Unsubscribe<T1, T2, T3>(string id, Action<T1, T2, T3> act) {
	        if (!_actionsDict.TryGetValue(id, out var d) || d is not Action<T1, T2, T3> existing) { return; }
	        var newAct = existing - act;
	        if (newAct == null) { _actionsDict.Remove(id); } else { _actionsDict[id] = newAct; }
	    }
	    public static void Unsubscribe<T1, T2, T3, T4>(string id, Action<T1, T2, T3, T4> act) {
	        if (!_actionsDict.TryGetValue(id, out var d) || d is not Action<T1, T2, T3, T4> existing) { return; }
	        var newAct = existing - act;
	        if (newAct == null) { _actionsDict.Remove(id); } else { _actionsDict[id] = newAct; }
	    }
	    public static void Unsubscribe<T1, T2, T3, T4, T5>(string id, Action<T1, T2, T3, T4, T5> act) {
	        if (!_actionsDict.TryGetValue(id, out var d) || d is not Action<T1, T2, T3, T4, T5> existing) { return; }
	        var newAct = existing - act;
	        if (newAct == null) { _actionsDict.Remove(id); } else { _actionsDict[id] = newAct; }
	    }
	    public static void Unsubscribe<T1, T2, T3, T4, T5, T6>(string id, Action<T1, T2, T3, T4, T5, T6> act) {
	        if (!_actionsDict.TryGetValue(id, out var d) || d is not Action<T1, T2, T3, T4, T5, T6> existing) { return; }
	        var newAct = existing - act;
	        if (newAct == null) { _actionsDict.Remove(id); } else { _actionsDict[id] = newAct; }
	    }
	}
}//end namespace
