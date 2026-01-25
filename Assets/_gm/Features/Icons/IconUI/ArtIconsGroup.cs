using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


namespace spz {

	// Holds a collection of IconUI elements, which came from the same stable-diffusion-generation.
	// Only one icon can be marked as 'chosen' by the user.
	public  class ArtIconsGroup{
	    public IconsUI_List myList { get; private set; }
	    public GenData2D myGenData { get; private set; }
	    public int NumAlive => icons.Count(i=>i!=null);
	    public IconUI chosenIcon { get; private set; } = null;//can only be one icon from this entire group.
	    //Notice, IconUI[] might contain null entries.
	    //Because we might be removing some iconsUI at some point (by presssing 'delete' buttons, etc).
	    //Not using a list to avoid issues with 'ix-in-generation'.
	    IconUI[] icons;

	    public bool showMyIcons_as_solo{ get; set; } = false;//A flag to show just the icons of this group. Can be assigned 
	    public bool hideMyIcons_please { get; set; } = false;//by the owner list who renders icon
	    public enum GroupVisibility { Hide, DontHide, Solo, DontSolo };

	    public IconUI getIconUI(int initial_ix){
	        if(initial_ix >= icons.Length){ return null; }
	        IconUI icon =  icons[initial_ix];
	        //might be null, if was already destroyed.
	        //Monobehaviour specifies override for '==' so returning 'null' manually.
	        if(icon == null){ return null; }; 
	        return icon;
	    }

	    public void DoForEachIcon( Action<IconUI> act ){
	        int cnt = icons.Length;
	        for(int i=0;i<cnt; ++i){
	            if(icons[i]==null){ continue; }
	            act(icons[i]);
	        }
	    }

    
	    //ctor:
	    public ArtIconsGroup(IconsUI_List myList, GenData2D genData, IconUI icon_PREFAB,  
	                         DraggableItems_Grid_UI grid_spawnHere,  List<bool> skip_these_icons=null){
	        this.myList = myList;
	        this.myGenData = genData;
	        int num_icons =  genData.use_many_icons?  genData.n_total : 1;
	        icons = new IconUI[num_icons];

	        //even though we could hold 8 icons, some might be 'true', meaning we'll keep them null.
	        int iconsCount = skip_these_icons?.Count(s=>s==false) ??  num_icons;
         
	        List<DraggableItem_GridSquare_UI> squares =  grid_spawnHere.SpawnSquares(iconsCount);
	        List<DraggableItem_UI> dragItems =  new List<DraggableItem_UI>();

	        int squareIX = 0;
	        for (int i=0; i<num_icons; ++i){
	            bool skip_icon =  skip_these_icons!=null  &&  skip_these_icons[i];
	            if(skip_icon){ continue; }//don't increment squareIX, just continue to next i.
            
	            var icon = GameObject.Instantiate(icon_PREFAB);
	            // NOTICE: DO NOT INSTANTIATE INTO THE PARENT SQUARE.
	            // Because if that square is disabled/inactive, the Awake() won't be invoked
	            // on sliders of the spawned icon.  Instead, set parent AFTER the instantiate:
	            icon.transform.SetParent(squares[squareIX].transform);

	            var iconRectTransf = icon.transform as RectTransform;
	            iconRectTransf.anchorMin = Vector2.zero;
	            iconRectTransf.anchorMax = Vector2.one;
	            iconRectTransf.offsetMin = iconRectTransf.offsetMin = Vector2.zero;

	            var itm = icon.GetComponent<DraggableItem_UI>();
	            dragItems.Add(itm);

	            // NOTICE: not i, but squareIX! Else there will be bug when loading save file,
	            // if batch has some icons skipped/deleted (null). (August 2024)
	            List<Guid> textureGuids   = genData.use_many_icons?  new List<Guid>{genData.textureGuidsOrdered[squareIX] } 
	                                                               : genData.textureGuidsOrdered.ToList();
	            icon.OnAfterInstantiated(this, textureGuids);
	            icons[i] = icon;
	            squareIX++;
	        }

	        for(int d=0; d<dragItems.Count; ++d){ 
	            dragItems[d].Init( squares[d], dragItems, squares );
	        }

	        IconUI.Act_OnSomeIconClicked += Act_OnSomeIconSelected;
	        IconUI.Act_OnSomeIconSelfDestroy += OnSomeIconSelfDestroy;
        
	        //select the first icon from the batch, to immediately have some kind of preview:
	        int first = Array.FindIndex(icons, icn=>icn!=null);
	        IconUI.Act_OnSomeIconClicked(icons[first], genData.kind);
	    }//end ctor


	    public void Dispose(){
	        for(int i=0; i<icons.Length; ++i){
	            IconUI icn = icons[i];
	            if(icn == null){ continue; }
	            icn.DestroySelf();
	            icons[i] = null;
	        }
	        IconUI.Act_OnSomeIconClicked -= Act_OnSomeIconSelected;
	        IconUI.Act_OnSomeIconSelfDestroy -= OnSomeIconSelfDestroy;
	        chosenIcon = null;
	        myGenData = null;
	        myList = null;
	    }

	    public void DeleteAllIcons_ExceptChosen(bool deleteEvenSelected_maybe){
	        for(int i=0; i<icons.Length; ++i){
	            if(icons[i]==null){ continue; }
	            if(icons[i]==chosenIcon){ continue; }
	            icons[i].DestroySelf();
	            icons[i] = null;
	        }
	        // for some kinds of generations, there can only be 1 icon PER ALL GENERATIONS.
	        // So some generations (maybe ours) isn't selected, and we need to actually destroy everything:
	        if(deleteEvenSelected_maybe == false){ return; }
	        bool deleteEvenSelected = false;
	        switch (myGenData.kind){
	            case GenerationData_Kind.Unknown: break;
	            case GenerationData_Kind.SD_ProjTextures: break;
	            case GenerationData_Kind.SD_Backgrounds:
	                deleteEvenSelected =  SkyboxBackground_MGR.instance.isObserving_IconUI(chosenIcon)==false;
	                break;
	            default:
	                break;
	        }
	        if(!deleteEvenSelected){  return;}
	        chosenIcon.DestroySelf();
	        myList.OnIconGroup_WillDestroySelf(this);
	        Dispose();
	    }

    
	    public void Ensure_IconSelectedAsMain(){
	        IconUI.Act_OnSomeIconClicked( chosenIcon, myGenData.kind );
	    }


	    void Act_OnSomeIconSelected(IconUI selected,  GenerationData_Kind kind){
	        if(selected._genData != myGenData){ return; }
	        chosenIcon = selected;
	    }


	    void OnSomeIconSelfDestroy( IconUI icon,  GenerationData_Kind kind ){
	        if(icon._myIconGroup != this){ return; }
        
	        int ix=Array.FindIndex(icons, i=>i==icon);//make sure to forget, else Load() might still save 
	        if(ix>=0){ icons[ix]=null; }              //it even if appears as 'null' because Destroyed.

	        if(icon == null){ return; }

	        // IMPORTANT: ONLY continue if chosenIcon has changed!
	        // Don't touch otherwise! else deleting All-Non-selected
	        // will mess up icons, keeping wrong ones
	        if(chosenIcon!=icon && chosenIcon!=null){ return; }
                                                           
	        //try get another non-null icon and make it selected:
	        chosenIcon =  icons.FirstOrDefault( i =>i!=icon && i!=null );

	        if(chosenIcon != null){
	            IconUI.Act_OnSomeIconClicked(chosenIcon, kind);
	            return;
	        }

	        myList.OnIconGroup_WillDestroySelf(this);
	        Dispose();
	    }


	    public ArtIconsGroup_SL Save(){
	        var grpSL = new ArtIconsGroup_SL();
	        grpSL.chosenIconIx = -1;
	        if (chosenIcon != null){ grpSL.chosenIconIx = Array.IndexOf(icons, chosenIcon); }
	        //  //Notice, IconUI[] might contain null entries.
	        //  //Because we might be removing some iconsUI at some point (by presssing 'delete' buttons, etc).
	        //  //Not using a list to avoid issues with 'ix-in-generation'.
	        grpSL.icons = new List<IconUI_SL>( icons.Length );
	        for(int i=0; i<icons.Length; ++i){
	            IconUI_SL iconSL = icons[i]?.Save();
	            grpSL.icons.Add(iconSL);
	        }
	        grpSL.showMyIcons_as_solo = showMyIcons_as_solo;
	        grpSL.hideMyIcons_please = hideMyIcons_please;
	        return grpSL;
	    }


	    public void Load_AfterSpawned( ArtIconsGroup_SL grpSL ){
	        showMyIcons_as_solo = grpSL.showMyIcons_as_solo;
	        hideMyIcons_please  = grpSL.hideMyIcons_please;

	        chosenIcon = grpSL.chosenIconIx==-1?  icons[0]  :  icons[grpSL.chosenIconIx];

	        for(int i=0; i<grpSL.icons.Count; ++i) {
	            IconUI_SL iconSL = grpSL.icons[i];
	            if(iconSL == null){ icons[i]=null; continue; }

	            bool isChosen = grpSL.chosenIconIx == i;
	            icons[i].Load_AfterSpawned( iconSL, isChosen );
	        }
	    }

	}//end class
}//end namespace
