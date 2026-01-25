using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace spz {

	public class MultiProj_DummyTextMaker : MonoBehaviour{
	    public static MultiProj_DummyTextMaker instance { get; private set; } = null;
	    [SerializeField] List<TextMeshProUGUI> _text; // shows text such as "Back Side"
	    [SerializeField] List<TextMeshProUGUI> _camPovIx_text; //shows index of currently edited camera.
	    [SerializeField] Camera _cam;
	    [Space(10)]
	    [SerializeField] Color _positiveBrush_bgCol;//what the camera will be Cleared with, 
	    [SerializeField] Color _negativeBrush_bgCol;//depending on the active mask-brush.
	    [Space(10)]
	    [SerializeField] Color _positiveBrush_txtCol;
	    [SerializeField] Color _negativeBrush_txtCol;
	    [Space(10)]
	    [SerializeField] Color _positiveBrush_num_txtCol;
	    [SerializeField] Color _negativeBrush_num_txtCol;


	    RenderTexture _renderTex;
	    public RenderTexture GetRenderTex_ref(){ return _renderTex; }

	    void Update(){
	        var oRib = SD_WorkflowOptionsRibbon_UI.instance;
	        _cam.enabled = MainViewport_UI.instance.showing == MainViewport_UI.Showing.UsualView;
	        _cam.backgroundColor =  oRib.isPositive?  _positiveBrush_bgCol : _negativeBrush_bgCol;
	        Color textCol    = oRib.isPositive?  _positiveBrush_txtCol : _negativeBrush_txtCol;
	        Color numTextCol = oRib.isPositive? _positiveBrush_num_txtCol : _negativeBrush_num_txtCol;
	        _text.ForEach(t=>t.color=textCol);
	        _camPovIx_text.ForEach(t=>t.color= numTextCol);

	        string curPovIx = (MultiView_Ribbon_UI.instance.currentPovIx + 1).ToString();
	        _camPovIx_text.ForEach( t=>t.text=curPovIx );
	    }

	    void Awake(){
	        if(instance !=null){ DestroyImmediate(this); return; }
	        instance = this;
	        _renderTex = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32, 0);
	        TextureTools_SPZ.ClearRenderTexture(_renderTex, Color.black);
	        _renderTex.wrapMode = TextureWrapMode.Repeat;

	        _cam.targetTexture = _renderTex;
	        _cam.clearFlags = CameraClearFlags.SolidColor;
	        _cam.backgroundColor = Color.black;
	        Shader.SetGlobalTexture("_MultiProj_WrongSide_Tex", _renderTex);
	    }

	    void OnDestroy(){
	        _cam.targetTexture = null;
	        TextureTools_SPZ.Dispose_RT(ref _renderTex, isTemporary:false);
	    }
	}
}//end namespace
