using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace spz {

	// UI list that contains IconUI elments and can be scrolled.
	// 'Art-list' and 'BG-list' both inherit from this.
	public abstract class IconsUI_List : MonoBehaviour {
	    [Space(10)]
	    [SerializeField] protected GraphicRaycaster _graphicRaycaster;
	    [SerializeField] protected Art_IconList_Header _header;
	    [SerializeField] protected DraggableItems_Grid_UI _draggableItemsGrid;
	    [SerializeField] RectTransform _container;//icons are parented under this group.
	    [SerializeField] protected ScrollRect_ItemFocuser_UI _sr_itemFocuser;

	    IconsList_SL _iconsListSL = null;//used temporarily, during Load()


	    // These are icons that can be continuously updated while the generation is happening.
	    // Every generation request has array.
	    protected Dictionary<Guid,ArtIconsGroup> _genGUID_to_iconGroup =  new Dictionary<Guid,ArtIconsGroup>(256);
    
	    // order is important and corresponds to the order of draggable icons inside the grid.
	    //Recalculated here every time the icons are reoredered in the ui grid.
	    protected List<Guid> _genGuids_ordered_in_grid =  new List<Guid>();
	    public List<Guid> guid_ordered_in_grid_refDontAlter() => _genGuids_ordered_in_grid;


	    // It might contain generationIconsUI that have null entries.
	    // Those are that we are no longer using (if we have deleted their corresponding UI entries).
	    // But 'Generation_IconsUI' exists as long as there is at least one 'IconUI' remaining, inside of it.
	    // If there are no UI entries remaining, then Generation_Data gets disposed.
	    public bool hasSomeIcons() => _genGUID_to_iconGroup.Count > 0;
	    public virtual IconUI _mainSelectedIcon { get; protected set; } = null;
	    public IconUI _hovered_icon { get; protected set; } = null;


	    public List<IconUI> allMyIcons(){
	        List<IconUI> iconUIs = new List<IconUI>(256);
	        foreach(var kvp in _genGUID_to_iconGroup){
	            var grp = kvp.Value;
	            grp.DoForEachIcon(i=>iconUIs.Add(i));
	        }
	        return iconUIs;
	    }

	    public int num_all_my_icons(){
	        int num = 0;
	        foreach(var kvp in _genGUID_to_iconGroup){
	            var grp = kvp.Value;
	            num += grp.NumAlive;
	        }
	        return num;
	    }


	    //might return null if the requested IconUI was deleted/destroyed
	    public IconUI GetIcon_of_GenerationGroup(Guid generationGuid, int ix_in_generation, bool justGetChosenIcon=false){
	        ArtIconsGroup group;
	        _genGUID_to_iconGroup.TryGetValue(generationGuid, out group);
	        if(group == null){ return null; }
	        if(justGetChosenIcon){ return group.chosenIcon; }
	        return group.getIconUI(ix_in_generation); //might return null if ui icon was deleted/destroyed
	    }


	    public bool Any_IconGroup_withSoloFlag(){
	        foreach(var kvp in _genGUID_to_iconGroup){
	            if (kvp.Value.showMyIcons_as_solo){ return true; }
	        }
	        return false;
	    }

    
	    // See if user wants to make the group be shown on its own, or be hidden.
	    // This will be useful during rendering stage.
	    public void ChangeVisibility_of_IconGroup(IconUI iconOfGroup, ArtIconsGroup.GroupVisibility visibil){
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();

	        ArtIconsGroup iconGrp = iconOfGroup._myIconGroup;
	        switch (visibil) {
	            case ArtIconsGroup.GroupVisibility.Hide:{
	                iconGrp.hideMyIcons_please=true;
	                Action<IconUI> act =  (icon)=>{ icon.OnMyIconGroup_HiddenOrSolo( Any_IconGroup_withSoloFlag() ); };
	                iconGrp.DoForEachIcon(act);
	                break;
	            }
	            case ArtIconsGroup.GroupVisibility.DontHide:{
	                iconGrp.hideMyIcons_please = false;
	                Action<IconUI> act =  (icon)=>{ icon.OnMyIconGroup_HiddenOrSolo( Any_IconGroup_withSoloFlag() ); };
	                iconGrp.DoForEachIcon(act);
	                break;
	            }
	            case ArtIconsGroup.GroupVisibility.Solo:{
	                //ensure no icons are solo, if some were.  Skip that group so that we don't turn off its context menu, if it's open:
	                disable_IsSolo_inAllGroups(anotherGroupRemains_asSolo:true, sendEventsToIcons: true, skipThis:iconGrp);
	                iconGrp.hideMyIcons_please = false;
	                iconGrp.showMyIcons_as_solo = true; 
	                Action<IconUI> act =  (icon)=>{ icon.OnMyIconGroup_HiddenOrSolo(someGroupRemains_asSolo:false);  };
	                iconGrp?.DoForEachIcon(act);
	                break;
	            }
	            case ArtIconsGroup.GroupVisibility.DontSolo:{
	                //ensure no icons are solo, if some were.  Skip that group so that we don't turn off its context menu, if it's open:
	                disable_IsSolo_inAllGroups(anotherGroupRemains_asSolo:false, sendEventsToIcons:true, skipThis:iconGrp);
	                iconGrp.hideMyIcons_please = false;
	                iconGrp.showMyIcons_as_solo = false; 
	                // all the icons, in case group was Hidden while was Solo.
	                // Also, ensures it's context menu is shown if we unticked the 'Solo':
	                Action<IconUI> act =  (icon)=>{ icon.OnMyIconGroup_HiddenOrSolo(someGroupRemains_asSolo:false);  };
	                iconGrp.DoForEachIcon(act);
	                break;
	            }
	            default: Debug.Log("unknown parameter passed " + visibil); break;
	        }
	    }

	    //can ensure neither of existing groups are to be shown/rendered as 'solo' (on their own).
	    public void disable_IsSolo_inAllGroups(bool anotherGroupRemains_asSolo, bool sendEventsToIcons, ArtIconsGroup skipThis=null){
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();

	        foreach(var kvp in _genGUID_to_iconGroup){
	            ArtIconsGroup grp = kvp.Value;
	            if(grp==skipThis){ continue; }
	            grp.showMyIcons_as_solo=false;
	            if(sendEventsToIcons){  grp.DoForEachIcon( ForeachIconOfGroup ); }
	        }
	        void ForeachIconOfGroup(IconUI icon){
	            icon.OnMyIconGroup_HiddenOrSolo(anotherGroupRemains_asSolo); 
	        }
	    }


	    protected abstract bool OnWillGenerate_isMyKind(GenData2D genData, out IconUI prefab_);


	    void OnWillGenerate(GenData2D genData){
	        IconUI iconPrefab;
	        if(!OnWillGenerate_isMyKind(genData, out iconPrefab)){ return; }

	        var imgsOfGen = new ArtIconsGroup(this, genData, iconPrefab, _draggableItemsGrid);

	        var autoScroll = _container.GetComponentInParent<ScrollRect_AutoScroll>(includeInactive:true);
	        autoScroll.ScrollToEnd(0.4f, isScrollDown:true);

	        _genGUID_to_iconGroup.Add( genData.total_GUID,  imgsOfGen );
	        _genGuids_ordered_in_grid.Add( genData.total_GUID );
	    }


	    protected virtual void OnSomeIconSelfDestroy( IconUI toRemove, GenerationData_Kind kind ){
	        if(_mainSelectedIcon==toRemove){ _mainSelectedIcon=null; }
	    }


	    protected virtual void OnSomeIconCloneSelf( IconUI cloneSelf, GenerationData_Kind kind){
	        IconUI iconPrefab;
	        if(!OnWillGenerate_isMyKind(cloneSelf._genData, out iconPrefab)){  return; }
	        GenData2D genData_clone =  GenData2D_Maker.make_clonedGenData2D( cloneSelf._genData );
	        genData_clone.ForceEvent_OnGenerationCompleted();
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();
	    }


	    // It either got "selected" during generation.
	    // Or it got "selected" when left-clicked.
	    protected abstract void onIconUI_Selected(IconUI icon, GenerationData_Kind kind);


	    public virtual void OnIconGroup_WillDestroySelf( ArtIconsGroup group ){
	        Guid guid = group.myGenData.total_GUID;
        
	        int nextGroupIx = _genGuids_ordered_in_grid.IndexOf(guid);

	        _genGUID_to_iconGroup.Remove( guid );
	        _genGuids_ordered_in_grid.Remove( guid );
	        GenData2D_Archive.instance.DisposeGenerationData(guid);

	        int count = _genGUID_to_iconGroup.Count;
	        if(count> 0){//if list still has groups, select a new icon from a nearby generation-group
	            nextGroupIx = nextGroupIx>=count? count-1 : nextGroupIx;
	            Guid next_guid = _genGuids_ordered_in_grid[nextGroupIx];
	            _genGUID_to_iconGroup[next_guid].Ensure_IconSelectedAsMain();
	        }
	    }


	    protected virtual void Destroy_IconsGroup_ofGeneration(GenData2D genData){
	        Guid guid = genData.total_GUID;
	        ArtIconsGroup imgsOfGen = null;
	        _genGUID_to_iconGroup.TryGetValue(guid, out imgsOfGen);
	        if(imgsOfGen == null){ return; }
	        imgsOfGen.Dispose();
	        _genGUID_to_iconGroup.Remove(guid);
	        _genGuids_ordered_in_grid.Remove(guid);
	    }

	    void OnDell_AllIcons_Button(){
	        if(!isAllowed_deleteIcons()){ return; }
	        ConfirmPopup_UI.instance.Show("Delete <b><color=#FF5C66>ALL ICONS</color></b>.\nCareful, there is no CTRL+Z!", onYes, onNo:null);
	        void onYes(){ 
	            List<Guid> guidKeys = _genGUID_to_iconGroup.Keys.ToList();
	            foreach(Guid guid in guidKeys){
	                ArtIconsGroup iconsGrp = _genGUID_to_iconGroup[guid];
	                iconsGrp.DeleteAllIcons_ExceptChosen( deleteEvenSelected_maybe:true);
	                GenData2D_Archive.instance.DisposeGenerationData(guid);
	            }
	            _hovered_icon = null;
	        }
	    }

    
	    void OnDel_NonSelectedIcons_Button(){
	        if(!isAllowed_deleteIcons()){ return; }
	        void onYes(){ 
	            List<Guid> guidKeys = _genGUID_to_iconGroup.Keys.ToList();
	            foreach(Guid guid in guidKeys){
	                ArtIconsGroup iconsGrp = _genGUID_to_iconGroup[guid];
	                iconsGrp.DeleteAllIcons_ExceptChosen( deleteEvenSelected_maybe:true);
	                //NOTICE: don't dispose the group. It still contains 1 icon inside it, and is valid.
	            }
	            _hovered_icon = null;
	        }
	        ConfirmPopup_UI.instance.Show("Delete all non-selected icons.\nCareful, there is no CTRL+Z!", onYes, onNo:null);
	    }


	    void OnDel_HiddenIcons_Button(){
	        if(!isAllowed_deleteIcons()){ return; }
	        void onYes(){ 
	            List<Guid> guidKeys = _genGUID_to_iconGroup.Keys.ToList();
	            foreach(Guid guid in guidKeys){
	                ArtIconsGroup iconsGrp = _genGUID_to_iconGroup[guid];
	                if (iconsGrp.hideMyIcons_please == false){ continue; }//not hidden, skip
	                iconsGrp.DeleteAllIcons_ExceptChosen( deleteEvenSelected_maybe:true);
	                GenData2D_Archive.instance.DisposeGenerationData(guid);
	            }
	            _hovered_icon = null;
	        }
	        ConfirmPopup_UI.instance.Show("Delete all <b>Hidden</b> icons.\nCareful, there is no CTRL+Z!", onYes, onNo:null);
	    }


	    bool isAllowed_deleteIcons(){
	        if(StableDiffusion_Hub.instance._generating){
	            string msg = "Can't remove icons while stable diffusion is still generating and reports progres";
	            Viewport_StatusText.instance.ShowStatusText(msg, textIsETA_number: false, textVisibleDur:1, progressVisibility:true); 
	            return false; 
	        }
	        return true;
	    }


	    protected virtual void OnMergeAllIcons_Button(){
	    }


	    // creates texture (or list of them if several udims) from existing icons.
	    // Does NOT add them as a new GenData2D (you can do it afterwards, if you need so)
	    // NOTICE: old icons must surive!
	    public virtual void GetTextures_FromAllIcons(Action<List<Texture2D>> onTexturesReady){
	        onTexturesReady?.Invoke( new List<Texture2D>() );
	    }


	    void OnIconRearrangeDragging_Ended(){
	        //user finished dragging around the icons, now let's look at their order.
	        //We need to ensure the '_guid_ordered_inside_grid' is specifying them in the same order as visible in the ui-grid.
	        var indexGuidPairs = new List<KeyValuePair<Guid,int>>();

	        foreach(var kvp in _genGUID_to_iconGroup){
	            Guid guid = kvp.Key;
	            ArtIconsGroup grp = kvp.Value;
	            //can't get sibling index of IconUI becuase it might still be unparented from its square (and still flying towards it)
	            int siblingIx = grp.chosenIcon.myDraggableScript._mySquare.transform.GetSiblingIndex();
	            indexGuidPairs.Add(new KeyValuePair<Guid, int>(guid,siblingIx));
	        }
	        // Sort by transform-sibling-index
	        indexGuidPairs.Sort( (pair1,pair2) => pair1.Value.CompareTo(pair2.Value) );
	        _genGuids_ordered_in_grid =  indexGuidPairs.Select(pair => pair.Key).ToList();

	        Objects_Renderer_MGR.instance.ReRenderAll_soon();
	    }


	    public void ImportCustomImages( GenerationData_Kind kind,  List<Texture2D> textures ){
	        bool use_many_icons = GenData2D_Maker.ImportedFiles_use_several_icons(kind);
	        bool allow_multipleFiles = !use_many_icons;
	        Images_ImportHelper.instance.OnImport_ExistingImages( kind,  textures,
	                                                              OnImportCustomImage_OK, 
	                                                              OnImportCustomImage_Fail );
	    }


	    protected virtual void OnButton_ImportCustomImage( GenerationData_Kind kind ){
	        bool use_many_icons = GenData2D_Maker.ImportedFiles_use_several_icons(kind);
	        bool allow_multipleFiles = !use_many_icons;
	        Images_ImportHelper.instance.ImportCustomImageButton( kind,  allow_multipleFiles: allow_multipleFiles, 
	                                                              OnImportCustomImage_OK, 
	                                                              OnImportCustomImage_Fail );
	    }

	    protected void OnImportCustomImage_Fail( GenerationData_Kind kind, string msg ){
	        if(!string.IsNullOrEmpty(msg)){ 
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 5, false); 
	        }
	    }

	    protected void OnImportCustomImage_OK( GenerationData_Kind kind,  
	                                           Dictionary<Texture2D,UDIM_Sector> textures_withoutOwner ){
	        OnCustomImageImported_maybeChangeKind(ref kind);

	        var sortedEntries = textures_withoutOwner
	                                .OrderBy(entry => entry.Value.y) // Sort by UDIM y value
	                                .ThenBy(entry => entry.Value.x); // Then sort by UDIM x value
	        List<Texture2D> textures = sortedEntries.Select(entry => entry.Key).ToList();
	        List<UDIM_Sector> udims  = sortedEntries.Select(entry => entry.Value).ToList();

	        GenData2D_Maker.make_ImportedCustomImages(kind, textures, udims);
	        Objects_Renderer_MGR.instance.ReRenderAll_soon();

	        //check that user imported sufficient number of files at once, if the model has several udims.
	        bool allowUdims = GenData2D_Maker.CanUseUdims(kind);//Not all kinds (projection/background) work with udims.
	        int numUdims = UDIMs_Helper._allKnownUdims.Count;

	        if(allowUdims  &&  numUdims > 1  &&  textures_withoutOwner.Count==1){
	            string msg = $"Your model has {numUdims} UDIMs. You should import {numUdims} images together, one per udim sector."
	                        +$"\nFilenames need to have suffix  _1001  _1002  and so on.";
	            Viewport_StatusText.instance.ShowStatusText(msg, textIsETA_number:false, 12, false);
	        }
	    }


	    //called when our own header (of this list) did import some texture.
	    //The header might have imported it as uv-texture, but we might 'respecify' it
	    //to be treated as background, etc.
	    protected abstract void OnCustomImageImported_maybeChangeKind(ref GenerationData_Kind kind);


	    public abstract void Save(StableProjectorz_SL spz);//we will setup some variables of the SPZ.
	    public abstract void Load(StableProjectorz_SL spz);//we'll load from some variables of the SPZ.

	    protected void OnSaveCommonStuff(IconsList_SL saveHere){
        
	        saveHere.generationGUID_to_iconGrps = new List<GenGUID_and_ArtIconsGroup_SL>();
        
	        foreach(var kvp in _genGUID_to_iconGroup){
	            ArtIconsGroup group = kvp.Value;
	            var entry = new GenGUID_and_ArtIconsGroup_SL();
	            entry.guid = kvp.Key.ToString();
	            entry.groupSL = group.Save();
	            saveHere.generationGUID_to_iconGrps.Add(entry);
	        }
	        saveHere._mainSelectedIcon_groupGuid = _mainSelectedIcon?.generation_guid.ToString() ?? "";
	        //NOTICE: child class already assigned obj into SPZ, when we asked it earlier here.
	    }


	    protected void OnLoadCommonStuff(IconsList_SL sl){
	        _iconsListSL = sl;
	    }

	    public void OnAfter_AllLoaded(){
	        //remove previous icon groups:
	        foreach(var kvp in _genGUID_to_iconGroup){  Destroy_IconsGroup_ofGeneration(kvp.Value.myGenData);  }
	        _genGUID_to_iconGroup.Clear();

	        _draggableItemsGrid.OnLoad();//AFTER headers were loaded in child class. Recals dimensions, BEFORE we start spawning icons.

	        //create new icon groups:
	        foreach (var pair in _iconsListSL.generationGUID_to_iconGrps){
	            string guid = pair.guid;
	            ArtIconsGroup_SL grpSL = pair.groupSL;
	            GenData2D genData   = GenData2D_Archive.instance.GenerationGUID_toData(new Guid(guid));
	            ArtIconsGroup group = Load_Make_IconGroup(genData, grpSL);
	        }
	        var autoScroll = _container.GetComponentInParent<ScrollRect_AutoScroll>(includeInactive:true);
	        autoScroll.ScrollToEnd(0.4f, isScrollDown:true);
	        _iconsListSL = null;
	    }


	    ArtIconsGroup Load_Make_IconGroup(GenData2D genData, ArtIconsGroup_SL icon_group_SL){
	        IconUI iconPrefab;
	        if(!OnWillGenerate_isMyKind(genData, out iconPrefab)){ return null; }

	        var skip_these_icons = new List<bool>();

	        for(int i=0;  i<icon_group_SL.icons.Count;  ++i){
	            bool isSkipIcon =  icon_group_SL.icons[i] == null;
	            skip_these_icons.Add(isSkipIcon);
	        }

	        var iconsGroup = new ArtIconsGroup(this, genData, iconPrefab, _draggableItemsGrid, skip_these_icons);

	        _genGUID_to_iconGroup.Add( genData.total_GUID,  iconsGroup );
	        _genGuids_ordered_in_grid.Add( genData.total_GUID );

	        iconsGroup.Load_AfterSpawned(icon_group_SL);

	        if(genData.total_GUID == new Guid(_iconsListSL._mainSelectedIcon_groupGuid)){
	            _mainSelectedIcon =  iconsGroup.chosenIcon;
	        }
	        return iconsGroup;
	    }


    
	    void UpdateIsHovered_ofIcons(){
	        //important, if user clicked and is adjusting sliders.
	        //We want "keep hovering" the icon until mouse is unpressed, even if it's currently outside the square
	        if(KeyMousePenInput.isLMBpressed()){ return; }

	        IconUI hovered = GetHoveredIconUI(_graphicRaycaster);

	        //stops hovering if we were overing during previous frames:
	        if(Keyboard.current.altKey.isPressed){ hovered = null; }
        
	        if(hovered != _hovered_icon){  
	            _hovered_icon?.hovers.OnHoverStopped();  
	            _hovered_icon = hovered;
	            _hovered_icon?.hovers.StartedHover();
	        }
	        hovered?.hovers.OnHoveredThisFrame();
	    }

        
	    IconUI GetHoveredIconUI( GraphicRaycaster raycaster2D ){
	        PointerEventData eventData = new PointerEventData( EventSystem.current );
	        eventData.position = Input.mousePosition;

	        // Previously we would raycast from 'raycaster2D'.
	        // But each icon has its Canvas (I guessed it would be more performant),
	        // therefore raycast doesn't reach into them.
	        // So, We will do a slightly more expensive raycast via RaycastAll()
	        List<RaycastResult> results = new List<RaycastResult>();
	        EventSystem.current.RaycastAll(eventData, results);

	        foreach (var result in results){
	            if(result.gameObject.tag != IconUI.IconUI_tag){ continue; }

	            IconUI uiComponent = result.gameObject.GetComponentInParent<IconUI>();
	            if (uiComponent != null){
	                return uiComponent;
	            }
	        }
	        return null;
	    }


    
	    protected virtual void Update(){
	        UpdateIsHovered_ofIcons();
	    }


	    protected virtual void Awake(){
	        GenData2D_Archive.OnWillGenerate += OnWillGenerate;
	        GenData2D_Archive.OnWillDispose_GenerationData += Destroy_IconsGroup_ofGeneration;
	        IconUI.Act_OnSomeIconClicked += onIconUI_Selected;
	        IconUI.Act_OnSomeIconSelfDestroy += OnSomeIconSelfDestroy;
	        IconUI.Act_OnSomeIconCloneSelf += OnSomeIconCloneSelf;
	        _header.onImport_CustomImageButton += OnButton_ImportCustomImage;

	        _header.onMerge_all_icons += OnMergeAllIcons_Button;

	        _header.onDel_AllIcons += OnDell_AllIcons_Button;
	        _header.onDel_HiddenIcons += OnDel_HiddenIcons_Button;
	        _header.onDel_NonSelectedIcons += OnDel_NonSelectedIcons_Button;
	        _header.onNumIconsPerRow_button +=  _draggableItemsGrid.ChangeNumCells_perRow;
        
	        _draggableItemsGrid.onUserRearangingStop += OnIconRearrangeDragging_Ended;

	        var entries = _container.GetComponentsInChildren<IconUI>().ToList();
	        for(int i=0; i<entries.Count; ++i){  Destroy(entries[i].gameObject); }
	    }

	    protected virtual void Start() { }

	    protected virtual void OnDestroy(){
	        GenData2D_Archive.OnWillGenerate -= OnWillGenerate;
	        GenData2D_Archive.OnWillDispose_GenerationData -= Destroy_IconsGroup_ofGeneration;
	        IconUI.Act_OnSomeIconClicked -= onIconUI_Selected;
	        IconUI.Act_OnSomeIconSelfDestroy -= OnSomeIconSelfDestroy;
	        IconUI.Act_OnSomeIconCloneSelf -= OnSomeIconCloneSelf;
	        if(_header!=null){
	            _header.onImport_CustomImageButton -= OnButton_ImportCustomImage;
	            _header.onDel_NonSelectedIcons -= OnDel_NonSelectedIcons_Button;
	            _header.onDel_AllIcons -= OnDell_AllIcons_Button;
	            _header.onMerge_all_icons -= OnMergeAllIcons_Button;
	            _header.onNumIconsPerRow_button -= _draggableItemsGrid.ChangeNumCells_perRow;
	        }
	        if(_draggableItemsGrid != null){
	            _draggableItemsGrid.onUserRearangingStop -= OnIconRearrangeDragging_Ended;
	        }
	    }

	}
}//end namespace
