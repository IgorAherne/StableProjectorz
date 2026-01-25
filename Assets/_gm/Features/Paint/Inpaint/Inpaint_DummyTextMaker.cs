using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace spz {

	// Prepares a 2d texture that contains text.
	// Describes which inpainting type we are using (Original, Latent Nothing, etc).
	// This texture can then be presented inside the viewport, as additional info.
	public class Inpaint_DummyTextMaker : MonoBehaviour{
	    public static Inpaint_DummyTextMaker instance { get; private set; } = null;
	    [SerializeField] List<TextMeshProUGUI> _text; // Drag your text element here
	    [SerializeField] List<TextMeshProUGUI> _number; // Drag your text element here
	    [SerializeField] Camera _cam;

	    RenderTexture _renderTex;
	    public RenderTexture GetRenderTex_ref(){ return _renderTex; }

	    string _words = "Re-Do";


	    void Update(){
	        _cam.enabled = MainViewport_UI.instance.showing == MainViewport_UI.Showing.UsualView
	                       && WorkflowRibbon_UI.instance.isMode_using_img2img();
        
	        float decimalValue = SD_WorkflowOptionsRibbon_UI.instance.denoisingStrength;
	        int intVal = Mathf.RoundToInt(100*decimalValue);

	        _text.ForEach(t=>t.text = "");
	        _number.ForEach( n=>n.text = intVal.ToString() );
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
	    }

	    void OnDestroy(){
	        _cam.targetTexture = null;
	        TextureTools_SPZ.Dispose_RT(ref _renderTex, isTemporary:false);
	    }
	}
}//end namespace
