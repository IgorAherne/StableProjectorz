using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace spz {

	public class Gen3D_MultiImageInput_UI : Gen3D_ImageInputs_UI{

	    [SerializeField] GridLayoutGroupContentResizer _grid;
	    [SerializeField] Trellis_ImageSlot _slot_PREFAB;
	    [SerializeField] TextMeshProUGUI _multiFiles_hint_text;

	    Trellis_ImageSlot _dummySlot;//always exists, can't be deleted. Used to import images.
	    List<Trellis_ImageSlot> _currentSlots = new List<Trellis_ImageSlot>();


	    public override int NumImages() => _currentSlots.Count();


	    public override List<string> get_images_asBase64()
	        => _currentSlots.Select(s=>s.image_as_base64()).ToList();


	    bool isMySlot(Trellis_ImageSlot slot)//if true - maybe it's be from SingleImage_inputs, so skip.
	    => slot.transform.parent == _grid.transform;


	    protected override void OnTakeScreenshotTexture(Vector2 screen_min01, Vector2 screen_max01, Texture2D tex2D_takeOwnership){ 
	        if(gameObject.activeSelf==false){ return; }
	        var new_slot = GameObject.Instantiate(_slot_PREFAB, _grid.transform);
	        new_slot.SwapWithNewImage(tex2D_takeOwnership);
	        //set as -2 (one before last), because last one should always be the clickable dummy slot:
	        new_slot.transform.SetSiblingIndex( new_slot.transform.parent.childCount-2 );
	        _multiFiles_hint_text.gameObject.SetActive(false);
	        _currentSlots.Add(new_slot);
	        Viewport_StatusText.instance.ShowStatusText("Screenshot added", false, 2, false);
	    }


	    protected override void OnImportedImage(Trellis_ImageSlot slot, Texture2D tex){
	        if (!isMySlot(slot)){ return; }
	        //check if we tried importing into the gray dummy-slot that is always visible:
	        bool isFromDummySlot =  _currentSlots.Contains(slot)==false;
        
	        if (isFromDummySlot){
	            var new_slot = GameObject.Instantiate(_slot_PREFAB, _grid.transform);
	            new_slot.SwapWithNewImage(tex);
	            //set as one before last, becasue last one should always be the clickable dummy slot:
	            new_slot.transform.SetAsLastSibling();
	            slot.transform.SetAsLastSibling();
	            _currentSlots.Add(new_slot);
	            _multiFiles_hint_text.gameObject.SetActive( false );
	        }else{
	            slot.SwapWithNewImage(tex);
	        }
	    }

	    protected override void OnImportedImage_Closed(Trellis_ImageSlot slot){
	        if (!isMySlot(slot)){ return; }
	        _currentSlots.Remove(slot);
	        slot.DisposeTheImage();
	        slot.DestroySelf();
	        //2 because the slot hasn't destroyed itself this frame yet:
	        _multiFiles_hint_text.gameObject.SetActive( _grid.get_enabledCells().Count == 2);
	    }

	    protected override void OnImage_Mirrored(Trellis_ImageSlot slot){
	        if (!isMySlot(slot)){ return; }
	        var mirrored_rt = new RenderTexture( slot.visibleTexture_ref.width, slot.visibleTexture_ref.height, 
	                                              depth:0,  GraphicsFormat.R8G8B8A8_UNorm,  mipCount:1 );
	        TextureTools_SPZ.Blit(slot.visibleTexture_ref, mirrored_rt, base._mirrorImage_mat);
	        Texture2D mirorred2D = TextureTools_SPZ.RenderTextureToTexture2D(mirrored_rt);
	        DestroyImmediate(mirrored_rt);

	        var new_slot = GameObject.Instantiate(_slot_PREFAB, _grid.transform);
	        new_slot.SwapWithNewImage(tex_takeOwnership: mirorred2D);
	        //set as -2 (one before last), because last one should always be the clickable dummy slot:
	        new_slot.transform.SetSiblingIndex(new_slot.transform.parent.childCount - 2);
	        _currentSlots.Add(new_slot);
	    }

	    public override void OnDragAndDroppedTextures(List<string> filepaths){
	        if (gameObject.activeSelf == false){ return; }
	        List<Texture2D> texList = TextureTools_SPZ.LoadTextures_FromFiles(filepaths);
	        foreach(Texture2D tex in texList){
	            if(tex == null){ continue; }
	            OnTakeScreenshotTexture(new Vector2(0,0), new Vector2(1,1), tex);//coords won't matter.
	        }
	    }


	    protected override void Awake(){
	        base.Awake();
	        _multiFiles_hint_text.gameObject.SetActive(true);
	    }

	    protected override void Start(){
	        base.Start(); 
	        _dummySlot = _grid.transform.GetComponentInChildren<Trellis_ImageSlot>();
	    }
	}
}//end namespace
