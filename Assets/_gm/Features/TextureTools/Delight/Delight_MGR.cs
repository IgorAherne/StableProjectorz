using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace spz {

	public class Delight_MGR : MonoBehaviour{
	    public static Delight_MGR instance { get; private set; } = null;

	    [SerializeField] ComputeShader _patternAwareDelight_sh;

	    [SerializeField] MeshRenderer _debugRender_art;
	    [SerializeField] MeshRenderer _debugRender_mask;

	    [Space(10)]
	    [SerializeField] ShadowR_PythonRunner _shadowR_pythonRunner;

	    bool _isDelighiting = false;
    

	    public void ReduceShadows_ShadowR(GenData2D fromThis){
	        _shadowR_pythonRunner.ReduceShadows_ShadowR(fromThis);
	    }

	    public void Delight(GenData2D genData_from, Guid textureGuid){
	        if (_isDelighiting) { return; }

	        if(genData_from.GetTexture_ref(textureGuid).texturePreference != TexturePreference.Tex2D){
	            string msg = "Delighter can't work with stacks of texture, only single texture2D at a time";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 4, false);
	            return;
	        }
	        StartCoroutine( Delight_crtn(genData_from, textureGuid) );
	    }

	    public RenderTexture art_delit;
	    public RenderTexture out_mask;
	    [Space(10)]
	    [Range(0, 10.0f)]
	    public float LuminanceWeight = 0.7f;
	    [Range(0, 10.0f)]
	    public float ColorWeight = 0.3f;
	    [Range(0, 10.0f)]
	    public float GradCoherenceWeight = 0.3f;
	    [Range(0, 10.0f)]
	    public float PeriodicityWeight = 0.25f;
	    [Range(0, 10.0f)]
	    public float PatternWeight = 0.1f;
	    [Range(0, 10.0f)]
	    public float Sensitivity = 1.2f;
	    [Range(0, 10.0f)]
	    public float TextureThreshold = 0.15f;

	    public IEnumerator Delight_crtn(GenData2D genData_from, Guid textureGuid){
	        //MODIF _isDelighiting = true;

	        GenData_TextureRef texRef = genData_from.GetTexture_ref(textureGuid);
	        Vector2Int wh = texRef.widthHeight();

	        if (art_delit == null || art_delit.width != wh.x){ //MODIF
	            if (art_delit != null) { DestroyImmediate(art_delit); }//MODIF
	            art_delit = new RenderTexture(wh.x, wh.y, depth:0, GraphicsFormat.R8G8B8A8_UNorm, mipCount:1);
	        }
	        if(out_mask == null || out_mask.width != wh.x)//MODIF
	        {
	            if (out_mask != null) { DestroyImmediate(out_mask); }//MODIF
	            out_mask  = new RenderTexture(wh.x, wh.y, depth:0, GraphicsFormat.R8_UNorm, mipCount:1);
	        }
	        art_delit.enableRandomWrite = true;
	        out_mask.enableRandomWrite = true;

	        int kernel = _patternAwareDelight_sh.FindKernel("CSMain");

	        _patternAwareDelight_sh.SetTexture(kernel, "ArtTexture", texRef.tex2D);
	        _patternAwareDelight_sh.SetTexture(kernel, "ArtTexture_Delit", art_delit);
	        _patternAwareDelight_sh.SetTexture(kernel, "OutputMask", out_mask);

	        _patternAwareDelight_sh.SetInt("TextureWidth", wh.x);
	        _patternAwareDelight_sh.SetInt("TextureHeight", wh.y);

	        _patternAwareDelight_sh.SetFloat("LuminanceWeight", LuminanceWeight);
	        _patternAwareDelight_sh.SetFloat("ColorWeight", ColorWeight);
	        _patternAwareDelight_sh.SetFloat("GradCoherenceWeight", GradCoherenceWeight);
	        _patternAwareDelight_sh.SetFloat("PeriodicityWeight", PeriodicityWeight);
	        _patternAwareDelight_sh.SetFloat("PatternWeight", PatternWeight);
	        _patternAwareDelight_sh.SetFloat("Sensitivity", Sensitivity);
	        _patternAwareDelight_sh.SetFloat("TextureThreshold", TextureThreshold);

	        Vector3Int numGroups = ComputeShaders_MGR.calcNumGroups(wh.x, wh.y);
	        _patternAwareDelight_sh.Dispatch(kernel, numGroups.x, numGroups.y, numGroups.z);

	        ////  StableDiffusion_Hub.instance.ManualImg2Img();
	        ////  yield return ; //keep waiting until the result is returned.
	        //.;
        
	        //yield return null;//MODIF

	        _debugRender_art.material.mainTexture = art_delit;//MODIF
	        _debugRender_mask.material.mainTexture = out_mask;//MODIF
	        //DestroyImmediate(art_delit); //MODIF
	        //DestroyImmediate(out_mask); //MODIF
        
        
	        //MODIF _isDelighiting = false;
	        yield break;//MODIF
	    }


	    void Update(){ //MODIF (this Update is only used for debugging/testing)
	        return;
	        Guid genGuid = GenData2D_Archive.instance.latestGeneration_GUID;
	        GenData2D genData = GenData2D_Archive.instance.GenerationGUID_toData(genGuid);
	        if (genData!=null){//MODIF
	            Delight(genData, genData.GetTexture_ref0().guid);
	        }
	    }

	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	    }
	}
}//end namespace
