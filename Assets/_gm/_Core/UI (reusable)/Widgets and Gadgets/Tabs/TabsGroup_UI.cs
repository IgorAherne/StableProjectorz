using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace spz {

	// Controls a list of tags, which user can click.
	// We will disable other tags.
	// Disables dividers of the tabs, when they are next to a clicked tab.
	// Dividers are a separation-line between inactive tabs.
	public class TabsGroup_UI : MonoBehaviour{
	    [Space(10)]
	    [SerializeField] string _default_activeTab = "";//if non "", we'll activate it.
	    [SerializeField] List<TabsGroupElem_UI> _tabs;
	    bool _tabsSwitched_atLeastOnce = false;

	    public void SubscribeForTab(string tabName, Action<TabsGroupElem_UI> act){
	        string nameLower = tabName.ToLower();
	        var tab = _tabs.FirstOrDefault(t=>t.title.ToLower()==nameLower);
	        if(tab == null){ return; }
	        tab.onClicked += act;
	    }

	    public void SwitchTab(string tabName){
	        _tabsSwitched_atLeastOnce = true;
	        string nameLower = tabName.ToLower();
	        var tab = _tabs.FirstOrDefault(t=>t.title.ToLower()==nameLower);
	        if(tab == null){ return; }
	        tab.Toggle(true);
	    }

	    void OnTabClicked(TabsGroupElem_UI elem){
	        int ixOfClicked = -1;
	        for(int i=0; i<_tabs.Count; i++){
	            if(_tabs[i] == elem){ 
	                ixOfClicked = i;
	                elem.Toggle(true);
	                continue; 
	            }
	            _tabs[i].Toggle(false);
	        }//end for

	        //make sure the neighboring tabs have their adjacent divider-lines hidden:
	        if(ixOfClicked > 0){
	            _tabs[ixOfClicked-1].DisableDivider(isLeft:false);
	        }
	        if(ixOfClicked < _tabs.Count - 1){
	            _tabs[ixOfClicked+1].DisableDivider(isLeft:true);
	        }
	    }


	    void Awake(){
	        for (int i=0; i<_tabs.Count; ++i){
	            _tabs[i].onClicked += OnTabClicked;
	        }
	    }


	    void Update(){
	        if (!_tabsSwitched_atLeastOnce && !string.IsNullOrEmpty(_default_activeTab)){
	            SwitchTab(_default_activeTab);
	        }
	    }
	}
}//end namespace
