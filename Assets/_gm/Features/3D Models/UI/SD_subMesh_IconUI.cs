using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace spz {

	public class SD_subMesh_IconUI : MonoBehaviour{
	    [SerializeField] Image _background;
	    [SerializeField] Color _color_Selected;
	    [SerializeField] Color _color_NotSelected;
	    [SerializeField] Button _wholeIcon_button;
	    [Space(10)]
	    [SerializeField] TextMeshProUGUI _name;
	    [SerializeField] Button _rmvButton;

	    bool _destroyed = false;
	    bool _sendEvent_duringDestroy = true;//for example, if we are placeholder and have to be removed dur game start.

	    public SD_3D_Mesh myMesh { get; private set; } = null;
	    public bool isSelected => myMesh._isSelected;
	    public static System.Action<SD_subMesh_IconUI> Act_OnWillDestroy_Icon { get; set; }  = null;


	    public void Init( SD_3D_Mesh myMesh ){
	        this.myMesh = myMesh;
	        _name.text = myMesh.gameObject.name;
        
	        //doing it all here, because Start() might not be invoked until entire panel becomes active:
	        _wholeIcon_button.onClick.AddListener(OnWholeIcon_button);
	        _rmvButton.onClick.AddListener(OnRemoveButton);
        
	        SD_3D_Mesh.Act_OnWillDestroyMesh += OnWillDestroyMesh;
	        SD_3D_Mesh.Act_OnMeshSelected += OnSomeMesh_Selected;
	        SD_3D_Mesh.Act_OnMeshDeselected += OnSomeMesh_Deselected;

	        OnSomeMesh_Selected(myMesh);
	    }


	    public void DestroySelf(bool sendEvents=true){
	        if(_destroyed){ return; } 
	        _sendEvent_duringDestroy = sendEvents;
	        Cleanup();
	        if(this!=null && this.gameObject!=null){ DestroyImmediate(this.gameObject); }
	    }
    
	    void OnDestroy(){
	        if(_destroyed){ return; }
	        Cleanup();
	    }

	    void Cleanup(){
	        _destroyed = true;
	        if (_sendEvent_duringDestroy){  Act_OnWillDestroy_Icon?.Invoke(this);  }
	        SD_3D_Mesh.Act_OnWillDestroyMesh -= OnWillDestroyMesh;
	        SD_3D_Mesh.Act_OnMeshSelected -= OnSomeMesh_Selected;
	        SD_3D_Mesh.Act_OnMeshDeselected -= OnSomeMesh_Deselected;
	    }


	    void OnWillDestroyMesh(SD_3D_Mesh mesh){
	        if(mesh != myMesh){ return; }
	        DestroySelf();
	    }

	    void OnRemoveButton(){
	        ConfirmPopup_UI.instance.Show("Remove this mesh? There is no CTRL+Z yet.", onYes, null);
	        void onYes(){
	            DestroySelf();
	        }
	    }


	    void OnWholeIcon_button(){
	        bool ctrlOrShift =  KeyMousePenInput.isKey_CtrlOrCommand_pressed() || KeyMousePenInput.isKey_Shift_pressed();
	        bool isSelect = true;
	        if(!myMesh._isSelected){  isSelect=true;  }
	        if(ctrlOrShift && myMesh._isSelected){  isSelect = false; }
        
	        bool isSucces;
	        myMesh.TryChange_SelectionStatus(isSelect, out isSucces, isDeselectOthers:ctrlOrShift==false);
	    }


	    void OnSomeMesh_Selected(SD_3D_Mesh mesh){
	        if(mesh == myMesh){  ToggleBG(true);  }
	    }


	    void OnSomeMesh_Deselected(SD_3D_Mesh mesh){
	        if(mesh == myMesh){  ToggleBG(false);  }
	    }

	    void ToggleBG(bool isEnable)
	        => _background.color =  isEnable ? _color_Selected : _color_NotSelected;
	}
}//end namespace
