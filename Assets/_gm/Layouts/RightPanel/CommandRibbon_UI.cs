using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace spz {

	public enum Panel{
	    Unknown, Input, Obj3D, CtrlNet, ArtBG, Art,
	}

	//has tab-buttons that allow us to flick between different panels (3d-objects, CTRLnets, Art, ArtBG panels).
	//It also has buttons such as Gen Art, etc.
	public class CommandRibbon_UI : MonoBehaviour{
	    public static CommandRibbon_UI instance { get; private set; } = null;

	    [SerializeField] TabsGroup_UI _tabGroup;
	    [Space(10)]
	    [SerializeField] RectTransform _SD_ArtList_Panel;
	    [SerializeField] RectTransform _SD_ArtBgList_Panel;
	    [SerializeField] RectTransform _SD_3D_Models_Panels;
	    [SerializeField] RectTransform _SD_ControlNets_List_Panel;
	    [Space(10)]
	    [SerializeField] Animation _ctrlNetButton_anim;

	    Coroutine _attention_toCtrlNetButton_crtn = null;


	    public Panel _currentPanel { get; private set; } = Panel.Unknown;


	    public void Attention_toCtrlNetButton(){
	        if(_attention_toCtrlNetButton_crtn != null){ StopCoroutine(_attention_toCtrlNetButton_crtn); }
	        _attention_toCtrlNetButton_crtn = StartCoroutine( Attention_toCtrlNetButton_crtn() );
	    }

	    public void clickArtList_toggle_manual() => _tabGroup.SwitchTab("art list");
	    public void clickArtBGList_toggle_manual() => _tabGroup.SwitchTab("art bg list");


	    IEnumerator Attention_toCtrlNetButton_crtn(){
	        int childCount = _ctrlNetButton_anim.transform.childCount;
	        _ctrlNetButton_anim.transform.GetChild(childCount-1).gameObject.SetActive(true);
	        _ctrlNetButton_anim.Stop();
	        _ctrlNetButton_anim.Rewind();
	        _ctrlNetButton_anim.Play();
	        yield return new WaitForSeconds(3);
	        _ctrlNetButton_anim.transform.GetChild(childCount-1).gameObject.SetActive(false);
	        _attention_toCtrlNetButton_crtn = null;
	    }


	    void ShowOnePanel(GameObject go){
	        go.SetActive(true);
	        if(go != _SD_ArtList_Panel.gameObject){ _SD_ArtList_Panel.gameObject.SetActive(false); }
	        if(go != _SD_ArtBgList_Panel.gameObject){ _SD_ArtBgList_Panel.gameObject.SetActive(false); }
	        if(go != _SD_3D_Models_Panels.gameObject){ _SD_3D_Models_Panels.gameObject.SetActive(false); }
	        if(go != _SD_ControlNets_List_Panel.gameObject){ _SD_ControlNets_List_Panel.gameObject.SetActive(false); }
	        if (KeyMousePenInput.isKey_Shift_pressed() == false){ //likely clicked on the tab
	            string msg = "Use Shift+1, Shift+2, etc to switch tabs faster :)";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 1.2f, false);
	        }
	    }

	    void OnArtList_Toggle(TabsGroupElem_UI tab){
	        ShowOnePanel( _SD_ArtList_Panel.gameObject );
	        _currentPanel = Panel.Art;
	    }

	    void OnArtBgList_Toggle(TabsGroupElem_UI tab){
	        ShowOnePanel( _SD_ArtBgList_Panel.gameObject );
	        _currentPanel = Panel.ArtBG;
	    }

	    void On_3D_Meshes_Toggle(TabsGroupElem_UI tab){
	        ShowOnePanel( _SD_3D_Models_Panels.gameObject );
	        _currentPanel = Panel.Obj3D;
	    }

	    void On_ControlNets_Toggle(TabsGroupElem_UI tab){
	        ShowOnePanel( _SD_ControlNets_List_Panel.gameObject );
	        _currentPanel = Panel.CtrlNet;
	    }


	    void Update(){
	        if(KeyMousePenInput.isSomeInputFieldActive()){ return;} //maybe typing some exclamation mark etc.
	        if (KeyMousePenInput.isKey_Shift_pressed() == false){ return; }
	        if (Input.GetKeyDown(KeyCode.Alpha1)){ _tabGroup.SwitchTab("art list"); }
	        if (Input.GetKeyDown(KeyCode.Alpha2)){ _tabGroup.SwitchTab("art bg list"); }
	        if (Input.GetKeyDown(KeyCode.Alpha3)){ _tabGroup.SwitchTab("mesh"); }
	        if (Input.GetKeyDown(KeyCode.Alpha4)){ _tabGroup.SwitchTab("controlnet"); }
	    }
    

	    void Awake(){
	        if(instance != null){  DestroyImmediate(this); return; }
	        instance = this;

	        _tabGroup.SubscribeForTab("art list", OnArtList_Toggle);
	        _tabGroup.SubscribeForTab("art bg list", OnArtBgList_Toggle);
	        _tabGroup.SubscribeForTab("mesh", On_3D_Meshes_Toggle);
	        _tabGroup.SubscribeForTab("controlnet", On_ControlNets_Toggle);

	        // allows Awake() of panels to run, to init as singletons:
	        // NOTICE: false --> true --> false.  Because if was enabled, its awake won't run if we SetActive(true)
	        Action<Transform> flip_on_off =  (tr)=>{ tr.gameObject.SetActive(false); tr.gameObject.SetActive(true); tr.gameObject.SetActive(false); };

	        // flip_on_off(_SD_ControlNets_List_Panel);
	        flip_on_off(_SD_ArtList_Panel);
	        flip_on_off(_SD_ArtBgList_Panel);
	        flip_on_off(_SD_3D_Models_Panels);
        
	        _SD_ControlNets_List_Panel.gameObject.SetActive(true);
	        _currentPanel = Panel.CtrlNet;
	    }


	}
}//end namespace
