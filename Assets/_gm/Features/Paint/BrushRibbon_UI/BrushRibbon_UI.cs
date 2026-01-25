using System;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// Owns the UI controls that affect brushing.
	// Doesn't actually deal with textures etc, only with the UI controls.
	public class BrushRibbon_UI : MonoBehaviour{
	    public static BrushRibbon_UI instance { get; private set; } = null;

	    [Space(10)]
	    [SerializeField] BrushRibbon_UI_Colors _colors;
	    [SerializeField] BrushRibbon_UI_Opacity _opacity;
	    [SerializeField] BrushRibbon_UI_Hardness _hardness;
	    [SerializeField] BrushRibbon_UI_PressureMode _pressureTabletMode;
	    [Space(10)]
	    [SerializeField] BrushRibbon_UI_Size _size;
	    [SerializeField] BrushRibbon_UI_BucketFill _bucketFill;
	    [SerializeField] BrushRibbon_UI_InvertMask _invertMask;
	    [SerializeField] BrushRibbon_UI_DeleteButton _deleteColorsButton;
	    [SerializeField] Toggle _eyeDropperToggle;

	    public BrushRibbon_UI_Hardness brushHardnessUI => _hardness;

	    void Awake(){
	        if (instance != null){ DestroyImmediate(this); return; }
	        instance = this;

	        EventsBinder.Bind_Clickable_to_event("BrushRibbon_UI:ColorsButton", _colors);
	        EventsBinder.Bind_Clickable_to_event("BrushRibbon_UI:OpacityButton", _opacity);
	        EventsBinder.Bind_Clickable_to_event("BrushRibbon_UI:HardnessButton", _hardness);
	        EventsBinder.Bind_Clickable_to_event("BrushRibbon_UI:PressureButton", _pressureTabletMode);
	        EventsBinder.Bind_Clickable_to_event("BrushRibbon_UI:SizeSlider", _size);
	        EventsBinder.Bind_Clickable_to_event("BrushRibbon_UI:BucketFillButton", _bucketFill);
	        EventsBinder.Bind_Clickable_to_event("BrushRibbon_UI:InvertMaskButton", _invertMask);
	        EventsBinder.Bind_Clickable_to_event("BrushRibbon_UI:DeleteColorsButton", _deleteColorsButton);
	        EventsBinder.Bind_Clickable_to_event("BrushRibbon_UI:EyeDropperToggle", _eyeDropperToggle);
	    }

	    public void Save( StableProjectorz_SL spz){
	        var trSL = new BrushRibbon_UI_SL();
	        _hardness.Save(trSL);
	        _colors.Save(trSL);
	        _size.Save(trSL);
	        _opacity.Save(trSL);
	    }

	    public void Load(StableProjectorz_SL spz){
	        BrushRibbon_UI_SL trSL = spz.brush_MGR;
	        _hardness.Load(trSL);
	        _colors.Load(trSL);
	        _size.Load(trSL);
	        _opacity.Load(trSL);
	    }
	}
}//end namespace
