using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class Icon3D_UI : MonoBehaviour{

	    [SerializeField] Icon3D_ContextMenu _contextMenu;
	    [SerializeField] MouseClickSensor_UI _wholeIcon_button;
	    [SerializeField] RawImage_with_aspect _icon;
	    [SerializeField] Image _stackSheets_lines;//enabled when our icon "carries" several textures at once, instead of a single texture.

	    //Icon from BG list that we "listen to". Assigned to us during OnAfterInstatiated().
	    IconUI _bgIcon_ref;

	    GenData2D _genData =>  _bgIcon_ref?._genData;

	    void OnMouseClick(int buttonIx){
	        if(buttonIx == 1){
	            _contextMenu.Toggle(!_contextMenu.isShowing);
	        }
	    }

	    public void OnAfterInstantiated(IconUI bgIcon_ref){
	        _bgIcon_ref = bgIcon_ref;
	        _wholeIcon_button._onMouseClick += OnMouseClick;
	        _contextMenu.onGenerateButton += OnGenerateButton;
	        _genData.Subscribe_for_TextureUpdates(_bgIcon_ref.texture_guids, OnTextureUpdated);

	        bool is_img_stack =  _genData.use_many_icons == false  &&  _genData.n_total > 1;
	        _stackSheets_lines.gameObject.SetActive(is_img_stack);

	        GenData_TextureRef texRef = _genData.GetTexture_ref0();
	        OnTextureUpdated(texRef);
	    }


	    void OnGenerateButton(){
	        var sdHub = StableDiffusion_Hub.instance;
	        if(sdHub._generating || Time.time < sdHub._generationCooldownUntil){
	            Viewport_StatusText.instance.ShowStatusText("Cant generate 3D while StableDiffusion is making images", false, 6, true);
	            return; 
	        }
	        GenData_TextureRef texRef = _genData.GetTexture_ref0();
	        Guid tex0_textureGuid = _genData.textureGuidsOrdered[0];
	        if(texRef.texturePreference != TexturePreference.Tex2D || texRef.tex2D==null){
	            Viewport_StatusText.instance.ShowStatusText("Cant generate 3D from a stacked-image. Must be a single texture.", false, 6, true);
	            return;
	        }
	        /*perform actual generation here*/
	    }

	    void OnTextureUpdated(GenData_TextureRef texRef){
	        _icon.ShowTexture_dontOwn( texRef.tex_by_preference(),  texRef.sliceIx,  isGenerated:false, 
	                                   CameraTexType.Unknown,  _genData.kind );
	    }

	    public void DestroySelf(){
	        Destroy(this.gameObject);
	        _bgIcon_ref = null;
	        _genData?.Unsubscribe_from_textureUpdates(_bgIcon_ref.texture_guids, OnTextureUpdated);
	    }
	}
}//end namespace
