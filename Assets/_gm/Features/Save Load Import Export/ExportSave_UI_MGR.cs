using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace spz {

	public class ExportSave_UI_MGR : MonoBehaviour{
	    public static ExportSave_UI_MGR instance { get; private set; } = null;
    
	    [Space(10)]
	    [SerializeField] Button _save_button;
	    [SerializeField] Button _export_finalTex_dilate_button;//with dilation (anti-seam)
	    [SerializeField] Button _export_finalTex_NoDilate_button;//with dilation (anti-seam)
	    [SerializeField] Button _export_views_button; //whatever the camera is observing (view,depth,normals,etc)
	    [SerializeField] Button _export_all_art_icons; //whatever the camera is observing (view,depth,normals,etc)
	    [SerializeField] Button _export_all_artBG_icons; //whatever the camera is observing (view,depth,normals,etc)
	    [SerializeField] Button _export_3d_button;
	    [Space(10)]
	    [SerializeField] Button _save_project_button;
	    [SerializeField] Button _load_project_button;
	    [Space(10)]
	    [SerializeField] SlideOut_Widget_UI _options_slideOut;//contains save,load, export, filtering etc.
	    [SerializeField] List<MouseHoverSensor_UI> _optionSlideOut_hoverAreas;

	    public static Action OnExportFinalTex_Button { get; set; } = null;//with texture-dilation (anti-seam).
	    public static Action OnExportFinalTex_NoDilate_Button { get; set; } = null;//without texture-dilation
	    public static Action OnExportViews_Button { get; set; } = null; //export whatever camera is obsering (view,depth,normals,etc)
	    public static Action OnExportAllArt_Icons_Button { get; set; } = null; 
	    public static Action OnExportAllArtBG_Icons_Button { get; set; } = null;
	    public static Action OnExport3D_Button { get; set; } = null;
	    public static Action OnSaveProject_Button { get; set; } = null;
	    public static Action OnLoadProject_Button { get; set; } = null;
    

	    //when we can expand the options slide-out (save/load, export, etc)
	    void OnOptionsHoverStart( PointerEventData p )
	        => _options_slideOut.Toggle_if_Different(true);


	    void Awake(){
	        if (instance != null){ DestroyImmediate(this.gameObject); return; }
	        instance = this;

	        _save_button.onClick.AddListener( ()=>OnExportFinalTex_Button?.Invoke() );
	        _export_finalTex_dilate_button.onClick.AddListener( ()=>OnExportFinalTex_Button?.Invoke() );
	        _export_finalTex_NoDilate_button.onClick.AddListener( ()=>OnExportFinalTex_NoDilate_Button?.Invoke() );
	        _export_views_button.onClick.AddListener( ()=>OnExportViews_Button?.Invoke() );

	        _export_all_art_icons.onClick.AddListener( ()=>OnExportAllArt_Icons_Button?.Invoke() );
	        _export_all_artBG_icons.onClick.AddListener( ()=>OnExportAllArtBG_Icons_Button?.Invoke() );

	        _export_3d_button.onClick.AddListener( ()=>OnExport3D_Button?.Invoke() );

	        _save_project_button.onClick.AddListener( ()=>OnSaveProject_Button?.Invoke() );
	        _load_project_button.onClick.AddListener( ()=>OnLoadProject_Button?.Invoke() );
	        _optionSlideOut_hoverAreas.ForEach( h=> h.onSurfaceEnter += OnOptionsHoverStart );
	    }
	}
}//end namespace
