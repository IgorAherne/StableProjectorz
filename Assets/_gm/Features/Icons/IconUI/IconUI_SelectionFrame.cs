using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	//allow to toggle a selection-effect around a UI element that represents a 2D icon.
	public class IconUI_SelectionFrame : MonoBehaviour{

	    [SerializeField] List<Image> _selection_Frames;//thin frame that indicates this Art icon is used in projection ("chosen" from its batch)
	    [SerializeField] Color _theChosen_color;
	    [SerializeField] Color _theChosen_andMainSelected_color;//icon which we are painting with mask-brush, etc. There can only be 1 main-selected icon simultaniously.
    
	    [Space(10)]
	    [SerializeField] IconUI _myIcon;
	    [SerializeField] Transform _wholeIcon_darkBorder;
   
	    LocksHashset_OBJ _preventShowing_lock = new LocksHashset_OBJ();
	    bool _deinit = false;//have we run deinit already. Prevents duplicate invocations.



	    public void PreventShowing(object requestor){
	        _preventShowing_lock.Lock(requestor);
	        _selection_Frames.ForEach(i=>i.color = new Color(i.color.r, i.color.g, i.color.b, 0));
	    }

	    public void AllowShowing(object originalRequestor){
	        _preventShowing_lock.Unlock(originalRequestor);
	        if(_preventShowing_lock.isLocked()){ return; }
	        _selection_Frames.ForEach(i=>i.color = new Color(i.color.r, i.color.g, i.color.b, 1));
	    }

        
	    class ReparentedTransformInfo{
	        public Transform its_original_parent;
	        public int its_original_siblingsIndex;
	        public ReparentedTransformInfo(Transform its_original_parent, int its_original_siblingsIndex){
	            this.its_original_parent = its_original_parent;
	            this.its_original_siblingsIndex = its_original_siblingsIndex;
	        }
	    }

	    public void OnAfterInstantiated(){
	        IconUI.Act_OnSomeIconClicked += OnSomeIconSelected;
	    }


	    public void OnWillBeDestroyed(){//manually invoked, because OnDestroy not invoked if we were inactive.
	        if(_deinit){ return; }
	        _deinit = true;
	        IconUI.Act_OnSomeIconClicked -= OnSomeIconSelected;
	    }


	    // Could be selected because some of sibling icons was selected
	    // (icons from same generation as ours).
	    void OnSomeIconSelected(IconUI selected, GenerationData_Kind kind){
	        GenData2D myGenData = _myIcon._genData;
	        if (_myIcon==selected){
	            ToggleFrame( FrameShow.Show_isMainSelected );
	            return;
	        }//else some other icon was selected, not mine:

	        if(myGenData == null){//can happen if we were just instantiated and our icon wasn't yet init.
	            ToggleFrame( FrameShow.Hide );
	            return;
	        }
	        bool isKind_uniqueEntries  = selected._genData.kind == GenerationData_Kind.AmbientOcclusion;
	              isKind_uniqueEntries |= selected._genData.kind == GenerationData_Kind.SD_Backgrounds;
	              isKind_uniqueEntries |= selected._genData.kind == GenerationData_Kind.UvTextures_FromFile;
        
	        if (isKind_uniqueEntries && myGenData.kind == kind){
	            // it's the same kind as ours, AND the kind is special.
	            // when there can be only one icon of this kind selected.
	            // All others must be deselected, even if they come from different generations.
	            // For example, there can be only one AO, only one UV-texture, only one Background selected.
	            ToggleFrame( FrameShow.Hide );
	            return;
	        }
	        //else non-unique kind (there can be different generations, and each has its own "used" icon).
	        //So we will only respond, if its the same generation as ours:
	        if(myGenData==selected._genData){
	            ToggleFrame( FrameShow.Show_sameBatchAsMainSelected );
	            return;
	        }
	        //else, not our generation. It's selection doesn't affect ours.
	        //Keep self as is, but ensure we are not "main selected"
	        ToggleFrame( FrameShow.KeepAsIs_isNotMainSelected );
	    }

	    enum FrameShow { 
	        Hide,
	        Show_isMainSelected,
	        Show_sameBatchAsMainSelected,
	        KeepAsIs_isNotMainSelected,
	    }


	    void ToggleFrame(FrameShow how){
	        Color usedFrameColor =  _theChosen_color;
	        switch (how){
	            case FrameShow.Hide:
	                _selection_Frames.ForEach(f=>f.enabled = false);
	                break;
	            case FrameShow.Show_isMainSelected:
	                _selection_Frames.ForEach(f=>f.enabled = true);
	                usedFrameColor = _theChosen_andMainSelected_color;
	                break;
	            case FrameShow.Show_sameBatchAsMainSelected:
	                _selection_Frames.ForEach(f=>f.enabled = false);
	                break;
	            case FrameShow.KeepAsIs_isNotMainSelected:
	                break;
	            default:
	                break;
	        }
	        //preserve the alpha of the icon!
	        Color wanted = usedFrameColor;
	        _selection_Frames.ForEach(f=>f.color = new Color(wanted.r, wanted.g, wanted.b, f.color.a));
	    }

	}
}//end namespace
