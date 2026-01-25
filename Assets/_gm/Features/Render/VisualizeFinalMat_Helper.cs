using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	//Helper of the Objects_Renderer_MGR
	//Presents the final material on all the objects.
	//
	//Deals with special cases like showing wireframe on objects
	//that are actually non-selected, etc.
	public class VisualizeFinalMat_Helper : MonoBehaviour{
	    [Space(10)]
	    [SerializeField] Material _finalMat;
	    [SerializeField] Material _finalMat_wireframe;
	    [SerializeField] Material _finalMat_wireframe_transpar;
	    [SerializeField] float _selectMode_wireSpeed = 1;

	    float _selectMode_wireOpacity01 = 0;

	    public void EquipMaterial_on_ALL(Material matBelongsToSomeone){
	        //notice, meshes includes the 'selected' ones too.
	        IReadOnlyList<SD_3D_Mesh> meshes = ModelsHandler_3D.instance.meshes;
	        for(int i=0; i<meshes.Count; ++i){
	            meshes[i].EquipMaterial(matBelongsToSomeone);
	        }
	    }
        
	    public void EquipMaterial_on_Specific( IReadOnlyList<SD_3D_Mesh> onTheseMeshes,  Material mat ){
	        foreach(var m in onTheseMeshes){ 
	            m.EquipMaterial(mat);
	        }
	    }

    
	    public void TemporaryPreventWireframe_onSelected(bool isPreventWireframe){

	        if(ModelsHandler_3D.instance == null) { return; } //scenes are probably still loading

	        IReadOnlyList<SD_3D_Mesh> sel = ModelsHandler_3D.instance.selectedMeshes;

	        bool useWire =  !isPreventWireframe;
	             useWire &=  ModelsHandler_3D_UI.instance._useWireframe_onSelected;

	        foreach (SD_3D_Mesh sm in sel){
	            Material mat =  useWire ? _finalMat_wireframe : _finalMat;
	            sm.EquipMaterial(mat);
	        }
	    }


	    //usually invoked at the end of the frame, after we performed all projections.
	    public void ShowFinalMat_on_ALL(RenderUdims finalTextureColor){

	        RenderUdims.SetNumUdims(finalTextureColor, _finalMat);
	        RenderUdims.SetNumUdims(finalTextureColor, _finalMat_wireframe);
	        RenderUdims.SetNumUdims(finalTextureColor, _finalMat_wireframe_transpar);

	        _finalMat.SetTexture("_MainTex", finalTextureColor.texArray);
	        _finalMat_wireframe.SetTexture("_MainTex", finalTextureColor.texArray);
	        _finalMat_wireframe_transpar.SetTexture("_MainTex", finalTextureColor.texArray);

	        bool isPointFilter = SceneResolution_MGR.resultTexFilterMode==FilterMode.Point;
	        TextureTools_SPZ.SetKeyword_Material(_finalMat, "SAMPLER_POINT", isPointFilter);
	        TextureTools_SPZ.SetKeyword_Material(_finalMat_wireframe, "SAMPLER_POINT", isPointFilter);
	        TextureTools_SPZ.SetKeyword_Material(_finalMat_wireframe_transpar, "SAMPLER_POINT", isPointFilter);

	        NonSelected_FadeTheWireframe();
	        Set_FinalVisibility();
	    }

    
	    void NonSelected_FadeTheWireframe(){
	        float fadeSpeed  = _selectMode_wireSpeed;
	              fadeSpeed *= CameraPanning._haveBeenPanningFor > 0 ?  0.3f : 1; //fade slower when panning.
	              fadeSpeed *= Time.deltaTime;
	        float wireFadeSign = isCanShow_NonSelected_asWireframe() ? 1 : -1;
	        _selectMode_wireOpacity01 += wireFadeSign * fadeSpeed;
	        _selectMode_wireOpacity01 = Mathf.Clamp01(_selectMode_wireOpacity01);
	        _finalMat_wireframe_transpar.SetFloat("_Fade_WireOpacity01", _selectMode_wireOpacity01);
	    }


	    void Set_FinalVisibility(){
	        IReadOnlyList<SD_3D_Mesh> sel    = ModelsHandler_3D.instance.selectedMeshes;
	        IReadOnlyList<SD_3D_Mesh> nonSel = ModelsHandler_3D.instance.nonSelectedMeshes;

	        bool nonSelected_asWire =  isCanShow_NonSelected_asWireframe() ||
	                                   _selectMode_wireOpacity01 > 0;//still fading them out, so keep showing.

	        bool wireOnSelected =  ModelsHandler_3D_UI.instance._useWireframe_onSelected;
	        Material mat =  wireOnSelected? _finalMat_wireframe : _finalMat;

	        if (nonSelected_asWire){
	            for(int i=0; i<nonSel.Count; ++i){  
	                nonSel[i].ToggleRender(true);
	                nonSel[i].EquipMaterial(_finalMat_wireframe_transpar);
	            }
	            for(int i=0; i<sel.Count; ++i){  
	                sel[i].ToggleRender(true);
	                sel[i].EquipMaterial(mat);
	            }
	        }else{
	            for(int i=0; i<nonSel.Count; ++i){  
	                nonSel[i].ToggleRender(false);
	            }
	            for(int i=0; i<sel.Count; ++i){
	                sel[i].ToggleRender(true);
	                sel[i].EquipMaterial(mat);
	            }
	        }
	    }


	    // Usually we keep the non-selected objects as hidden from user.
	    // But we might decide to show them as transparent + wireframe geometry.
	    bool isCanShow_NonSelected_asWireframe(){
	        bool canShow = ClickSelect_Meshes_MGR.instance._isSelectMode;//User activated regime to click on hidden surfaces, show them in wire.
	            canShow |= CameraPanning._haveBeenPanningFor > 0.25;//Panning the camera, keep showing. 0.25 to skip when setting a Click-orbit.
	            canShow &= StableDiffusion_Hub.instance._finalPreparations_beforeGen==false;//Avoid messing up the view image, if GenArt was just used.
	        return canShow;
	    }
    
           


	    void Awake(){
	        Settings_MGR._Act_onWireframeColor +=  (col) => _finalMat_wireframe.SetColor("_WireColor", col);
	        if (Settings_MGR.instance != null){ //init already ran, we missed the its first callback, so set manually:
	            _finalMat_wireframe.SetColor("_WireColor", Settings_MGR.instance.get_wireframeColor());
	        }
	    }

	}
}//end namespace
