using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace spz {

	public class Art3D_IconsUI_List : MonoBehaviour{

	    [SerializeField] ArtBG_IconsUI_List _bgList;
	    [SerializeField] protected DraggableItems_Grid_UI _draggableItemsGrid;
	    [Space(10)]
	    [SerializeField] Icon3D_UI _icon3d_PREFAB;

	    Dictionary<IconUI,Icon3D_UI> _artIconTo3DIcon =  new Dictionary<IconUI,Icon3D_UI>(256);
	    List<IconUI> _iconsToRemove = new List<IconUI>(32);
	    List<IconUI> _iconsToAdd = new List<IconUI>(32);


	    void Start(){
	        //remove placeholder icons that were used during Unity editor:
	        int numChildren = _draggableItemsGrid.transform.childCount;
	        for(int i=0; i<numChildren; ++i){
	            var icon = _draggableItemsGrid.transform.GetChild(i).GetComponent<Icon3D_UI>();
	            if(icon == null){ continue; }
	            icon.DestroySelf(); 
	        }
	    }

	    void Update(){
	        Ensure_1_icon_per_1_bgIcon();
	    }


	    void Ensure_1_icon_per_1_bgIcon(){
	        List<IconUI> currentIcons = _bgList.allMyIcons();

	        // Clear the temporary lists
	        _iconsToRemove.Clear();
	        _iconsToAdd.Clear();

	        // Find icons to remove
	        foreach (var kvp in _artIconTo3DIcon){
	            if (!currentIcons.Contains(kvp.Key)){
	                _iconsToRemove.Add(kvp.Key);
	            }
	        }
	        // Find icons to add
	        foreach (var icon in currentIcons){
	            if (!_artIconTo3DIcon.ContainsKey(icon)){
	                _iconsToAdd.Add(icon);
	            }
	        }
	        // Remove old icons
	        foreach (var oldIcon in _iconsToRemove){
	            if (_artIconTo3DIcon.TryGetValue(oldIcon, out Icon3D_UI icon3D)){
	                icon3D.DestroySelf();
	                _artIconTo3DIcon.Remove(oldIcon);
	            }
	        }
	        // Add new icons
	        foreach (var newIcon in _iconsToAdd){
	            // Create a new 3D icon
	            Icon3D_UI new3DIcon = CreateNew_3DIcon(newIcon);
	            _artIconTo3DIcon.Add(newIcon, new3DIcon);
	        }
	    }


	    Icon3D_UI CreateNew_3DIcon(IconUI iconBG){
	        GenData2D genData = iconBG._genData;

	        List<DraggableItem_GridSquare_UI> squares = _draggableItemsGrid.SpawnSquares(1);
	        List<DraggableItem_UI> dragItems = new List<DraggableItem_UI>();

	        Vector2 cellSize_px = _draggableItemsGrid.cellSize_px / (_draggableItemsGrid.cellSize_pcntOfGrid / 0.5f);
	        cellSize_px +=  Vector2.one * 2; //2 pixels wider (this helps to cover gaps/imprecisions between icons)
	                                         //icon prefab has -2 pixels on visual-element so it cancel-out visually 
        
	        var icon3d = GameObject.Instantiate(_icon3d_PREFAB);
	        var iconRectTransf = icon3d.transform as RectTransform;
	        iconRectTransf.anchorMin = Vector2.one * 0.5f;
	        iconRectTransf.anchorMax = Vector2.one * 0.5f;
	        iconRectTransf.sizeDelta = cellSize_px;
	        icon3d.transform.SetParent(squares[0].transform);//AFTER the rectTranfs vals were updated (while parent was null)

	        var itm = icon3d.GetComponent<DraggableItem_UI>();
	        dragItems.Add(itm);

	        itm.Init(squares[0], dragItems, squares);

	        List<Guid> textureGuids = genData.use_many_icons ? new List<Guid>{ genData.textureGuidsOrdered[0] }
	                                                         : genData.textureGuidsOrdered.ToList();
	        icon3d.OnAfterInstantiated(iconBG);
	        return icon3d;
	    }
	}
}//end namespace
