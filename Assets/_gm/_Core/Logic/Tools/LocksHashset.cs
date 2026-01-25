using System.Collections.Generic;
using UnityEngine;

namespace spz {

	//allows for many lock-owners. When none remain, it's considered "unlocked"
	public class LocksHashset : MonoBehaviour
	{
	    HashSet<object> lockers = new HashSet<object>();
	    public System.Action act_was0_now1;
	    public System.Action act_was1_now0;

	    public bool isLocked(){ return lockers.Count > 0; }

	    public void Lock(object requestor){
	        int prevCount = lockers.Count;
	        lockers.Add(requestor);
	        if(prevCount==0 && lockers.Count==1){ OnLocked();   act_was0_now1?.Invoke(); }
	    }
	    public void Unlock(object originalRequestor){
	        int prevCount = lockers.Count;
	        lockers.Remove(originalRequestor);
	        if(prevCount==1 && lockers.Count==0){ OnUnlocked(); act_was1_now0?.Invoke();}
	    }
    
	    protected virtual void OnLocked(){}
	    protected virtual void OnUnlocked(){}
	}




	public class LocksHashset_OBJ{ //a variant that doesn't inherit from monobehavior
  
	    #if UNITY_EDITOR
	    public HashSet<object> lockers_editorOnly => lockers;//used for visualization and debug.
	  #endif
	    HashSet<object> lockers = new HashSet<object>();
	    bool _keep_pretending_isLocked = false;

	    public System.Action<bool> onLockStatusChanged;//true: became locked   false: became unlocked


	    public void keep_pretending_isLocked(bool isPretend){

	        bool wasPretending = _keep_pretending_isLocked;
	        bool notLocked_butPretend     =  !isLocked() && isPretend;
	        bool notLocked_stoppedPretend =  !isLocked() && wasPretending && !isPretend;
	        bool doCallback =  notLocked_butPretend || notLocked_stoppedPretend;

	        _keep_pretending_isLocked = isPretend;//AFTER the comparison.

	        if(!doCallback){ return; }
	        onLockStatusChanged?.Invoke(isPretend);
	    }
    
	    public bool isLocked(){ return lockers.Count>0 || _keep_pretending_isLocked; }

	    public void LockOrUnlock(object requestor, bool isLock){
	        if(isLock){ Lock(requestor); }
	        else{ Unlock(requestor); }
	    }

	    public void Lock(object requestor){
	        int prevCount = lockers.Count;
	        lockers.Add(requestor);
	        if(prevCount==0 && lockers.Count==1){ 
	            OnLocked();   
	            onLockStatusChanged?.Invoke(true);
	        }
	    }
	    public void Unlock(object originalRequestor){
	        int prevCount = lockers.Count;
	        lockers.Remove(originalRequestor);
	        if(prevCount==1 && lockers.Count==0){ 
	            OnUnlocked(); 
	            onLockStatusChanged?.Invoke(false);
	        }
	    }

	    public void Clear(){ lockers.Clear(); }

	    protected virtual void OnLocked(){}
	    protected virtual void OnUnlocked(){}
	}
}//end namespace
