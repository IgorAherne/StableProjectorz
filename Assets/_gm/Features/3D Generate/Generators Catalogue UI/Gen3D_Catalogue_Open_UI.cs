using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace spz {

	//sits on the UI, has blutton which opens the catalog panel.
	public class Gen3D_Catalogue_Open_UI : MonoBehaviour
	{
	    [SerializeField] Button _openCatalogue_button;
	    [SerializeField] GameObject _catalogueInvitation_go;

	    void Awake(){
	        _catalogueInvitation_go.SetActive(true);

	        EventsBinder.Bind_Clickable_to_event("Gen3D_Catalogue:Open", _openCatalogue_button);
	    }

	    void LateUpdate(){
	        //hide open-catalog-button as soon as we know about some sliders / inputs that a 3D generator receives via JSON.
	        bool anyInputs_known = false;
	        if (Gen3D_MGR.instance != null){ anyInputs_known = Gen3D_MGR.instance.any_known_inputs;}
	        _catalogueInvitation_go.SetActive(!anyInputs_known);
	    }
	}
}//end namespace
