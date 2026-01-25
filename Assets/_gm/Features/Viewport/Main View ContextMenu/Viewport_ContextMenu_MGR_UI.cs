using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace spz {

	public class Viewport_ContextMenu_MGR_UI : MonoBehaviour
	{
	    [SerializeField] GraphicRaycaster _raycaster;
	    [SerializeField] ViewportContextMenu_Art_UI _viewport_contextMenu_ArtAndBG;
	    [SerializeField] ViewportContextMenu_AO_UI _viewport_contextMenu_AO;

	    float _time_StartedShowMenu;
	    ViewportContextMenu_UI _currentlyActiveMenu = null;//which one is active inside this holder.

	    enum Status{  Showing, Shown, Hiding, Hidden, }
	    Status _status = Status.Hidden;
    
	    public bool isShowing => (_status==Status.Showing || _status==Status.Shown);


	    public void Show(){
	        if (_currentlyActiveMenu != null){ 
	            _currentlyActiveMenu.ToggleVisibility(isShow:false, affectThisIcon:null, isInstant:true); 
	        }
	        IconUI affectThisIcon = null;
	        Decide_Icon_if_ArtPanel(ref affectThisIcon);

	        if(affectThisIcon==null){ return; }

	        _status = Status.Showing;
	        _time_StartedShowMenu = Time.time;
	        _currentlyActiveMenu.ToggleVisibility( isShow:true,  affectThisIcon, isInstant:false, onComplete:()=>_status=Status.Shown );
	    }


	    void Decide_Icon_if_ArtPanel(ref IconUI affectThisIcon){
        
	        if(affectThisIcon != null){ return; }//already decided.

	        bool isArtPanel  =  CommandRibbon_UI.instance._currentPanel == Panel.Art;
	             isArtPanel |=  CommandRibbon_UI.instance._currentPanel == Panel.ArtBG;
        
	        if(!isArtPanel){ return; }//no suitable panel active.

	        affectThisIcon = Art2D_IconsUI_List.instance._mainSelectedIcon;
	        if(affectThisIcon == null){ return; }//no icon in the panel yet.

	        //decide which context menu to show
	        if(affectThisIcon._genData.kind == GenerationData_Kind.AmbientOcclusion){
	            _currentlyActiveMenu = _viewport_contextMenu_AO;
	        }else{ 
	            _currentlyActiveMenu = _viewport_contextMenu_ArtAndBG;
	        }
	    }



	    void Hide(){
	        if(_currentlyActiveMenu == null){ _status = Status.Hidden; return; }
        
	        void onComplete(){
	            _currentlyActiveMenu = null;
	            _status=Status.Hidden;
	        }
	        _currentlyActiveMenu.ToggleVisibility( isShow:false, affectThisIcon:null, isInstant:false,  onComplete:onComplete);
	    }


	    void Update(){
	        if(_status == Status.Hiding || _status == Status.Hidden){ return; }
	        if(_currentlyActiveMenu == null){ return; }
	        if(Time.time <= _time_StartedShowMenu){ return; }//did it this frame, give it time to show up.
        
	        PointerEventData p = new PointerEventData(EventSystem.current);
	        p.position = KeyMousePenInput.cursorScreenPos();
	        List<RaycastResult> appendResultsHere = new List<RaycastResult>(20);
	        _raycaster.Raycast(p, appendResultsHere);
	        bool foundMyMenu = appendResultsHere.Any( r=>r.gameObject.GetComponent<ViewportContextMenu_UI>()==_currentlyActiveMenu );
	        if(foundMyMenu){ return; }//still hovering our region
	        Hide();
	    }

	}
}//end namespace
