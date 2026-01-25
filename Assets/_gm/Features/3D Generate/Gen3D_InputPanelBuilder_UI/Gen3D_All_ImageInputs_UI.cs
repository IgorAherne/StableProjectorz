using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public class Gen3D_All_ImageInputs_UI : MonoBehaviour{

	    [SerializeField] Toggle _singleImage_toggle;
	    [SerializeField] Toggle _multiImage_toggle;
	    [Space(10)]
	    [SerializeField] Gen3D_MultiImageInput_UI _multiImage;
	    [SerializeField] Gen3D_SingleImageInput_UI _singleImage;

	    public int numImages(){
	        //'_singleImage.gameObject' because script might be on separate go object (always active)
	        if (_singleImage.gameObject.activeSelf){
	            return _singleImage.NumImages();
	        }
	        return _multiImage.NumImages();
	    }

	    public List<string> get_images_asBase64(){
	        if (_singleImage.gameObject.activeSelf){
	            return _singleImage.get_images_asBase64();
	        }
	        return _multiImage.get_images_asBase64();
	    }

	    public bool OnDragAndDropImages(List<string> files, Vector2Int screenCoord){
	        var rectTransf = transform as RectTransform;
        
	        bool multi_on = _multiImage.gameObject.activeSelf;
	        bool single_on = _singleImage.gameObject.activeSelf;
	        if (!multi_on &&  !single_on){ return false; }

	        var textures = new List<Texture2D>();

	        Debug.Log("Gen3D_All_ImageInputs_UI checking if can consume");

	        if (multi_on && RectTransformUtility.RectangleContainsScreenPoint(_multiImage.transform as RectTransform, screenCoord)){
	            Debug.Log("multi can consume");
	            _multiImage.OnDragAndDroppedTextures(files);
	            return true;
	        }
	        else if (single_on && RectTransformUtility.RectangleContainsScreenPoint(_singleImage.transform as RectTransform, screenCoord)){
	            Debug.Log("single can consume");
	            _singleImage.OnDragAndDroppedTextures(files);
	            return true;
	        }
	        return false;
	    }

	    void OnTab_SingleImage(bool isOn){
	        if (!isOn) { return; }
	        _singleImage_toggle.SetIsOnWithoutNotify(true);
	        _singleImage.gameObject.SetActive(true);
	        _multiImage.gameObject.SetActive(false);
	    }

	    void OnTab_MultiImage(bool isOn){
	        if(!isOn){ return; }
	        _multiImage_toggle.SetIsOnWithoutNotify(true);
	        _singleImage.gameObject.SetActive(false);
	        _multiImage.gameObject.SetActive(true);
	    }

	    public void Save(Generate3D_Inputs_SL intoHere, string path_dataFolder){
	        //intoHere.singleImage = new Generate3D_Inputs_singleImage_SL();
	        //intoHere.multiImage  = new Generate3D_Inputs_multiImage_SL();
	        //_singleImage_inputs.Save(intoHere.singleImage, path_dataFolder);
	        //_multiImage_inputs.Save(intoHere.multiImage, path_dataFolder);
	    }

	    public void Load(Generate3D_Inputs_SL fromHere, string path_dataFolder){
	        //_singleImage_inputs.Load(fromHere.singleImage, path_dataFolder);
	        //_multiImage_inputs.Load(fromHere.multiImage, path_dataFolder);
	    }
    
	    void Awake(){
	        _singleImage_toggle.onValueChanged.AddListener(OnTab_SingleImage);
	        _multiImage_toggle.onValueChanged.AddListener(OnTab_MultiImage);
	        OnTab_MultiImage(true);
	    }

	}
}//end namespace
