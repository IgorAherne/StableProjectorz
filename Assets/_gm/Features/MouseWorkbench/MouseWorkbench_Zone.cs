using System;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// Singleton
	// An area spanning accross entire screen.
	// Can spawn a small panel with color-palette mini-prompts and mini-generate buttons.
	// Spawns it at the requested screen coordinate and ensures theis panel remains visible, by clamping its position.
	public class MouseWorkbench_Zone : MonoBehaviour{
	    public static MouseWorkbench_Zone instance { get; private set; }

	    [SerializeField] Canvas _myCanvas;//helps us deduce the scaling factor, to place ui elements accurately.
	    [SerializeField] GameObject _workbenchGO;//holds color-panel and other controls
	    [SerializeField] ColorPalette_Panel_UI _colorPanel;
	    [SerializeField] List<RectTransform> _panelsToRaycast;

	    public bool isShowing => _colorPanel._isShowing;

	    public enum ShowPreference{//flags, which you can combine.
	        CenterOnCursor = 0, AboveCursor = 1, BelowCursor =2, LeftOfCursor=4, RightOfCursor=8,
	    }

	    public void ShowAtScreenCoord( Vector2 screenCoord, Color startingCol, Action<Color> onColorUpdated,
	                                   ShowPreference pref = ShowPreference.CenterOnCursor ){

	        var panelRTF = _workbenchGO.transform as RectTransform;

	        Vector2 position = screenCoord;
	        Vector2 panelSize = panelRTF.rect.size * _myCanvas.scaleFactor;
	        Vector2 screenSize = (transform as RectTransform).rect.size * _myCanvas.scaleFactor;
	        position += CalculateOffset(pref, panelSize);
        
	        Vector2 positionClamped = position;
	        ClampPosition(ref positionClamped, panelSize, screenSize);

	        panelRTF.position = positionClamped;
	        _workbenchGO.SetActive(true);
	        _colorPanel.Show(startingCol, onColorUpdated);
	    }


	    Vector2 CalculateOffset(ShowPreference pref, Vector2 panelSize){
	        Vector2 offset = Vector2.zero;
	        if(pref.HasFlag(ShowPreference.LeftOfCursor)){  offset += new Vector2(-panelSize.x*0.45f, 0); }
	        if(pref.HasFlag(ShowPreference.RightOfCursor)){ offset += new Vector2(panelSize.x*0.45f, 0);  }
	        if(pref.HasFlag(ShowPreference.BelowCursor)){   offset += new Vector2(0, -panelSize.y*0.41f);  }
	        if(pref.HasFlag(ShowPreference.AboveCursor)){   offset += new Vector2(0, panelSize.y*0.41f);  }
	        return offset;
	    }

	    void ClampPosition(ref Vector2 position, Vector2 panelSize, Vector2 screenSize){
	        position.x =  Mathf.Clamp(position.x, panelSize.x*0.5f,  screenSize.x - panelSize.x*0.5f);
	        position.y =  Mathf.Clamp(position.y, panelSize.y*0.5f,  screenSize.y - panelSize.y*0.5f);
	    }


	    void Update(){
	        if(!_colorPanel._isShowing){ return; }
	        if(KeyMousePenInput.isLMBpressed()){ return; }//possibly still dragging the knob

	        Vector2 cursorPos  = KeyMousePenInput.cursorScreenPos();
	        bool contains = false;

	        for(int i=0;i< _panelsToRaycast.Count;i++){
	            RectTransform area = _panelsToRaycast[i];
	            contains = RectTransformUtility.RectangleContainsScreenPoint(area, cursorPos);
	            if(contains){ break; }
	        }
	        if(!contains){ 
	            _colorPanel.Hide();
	            _workbenchGO.SetActive(false);
	        }
	    }


	    void Awake(){
	        if(instance != null){  DestroyImmediate(this); return; }
	        instance = this;
	    }

	}

}//end namespace
