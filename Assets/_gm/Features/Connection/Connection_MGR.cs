using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// knows about 2 connection panels:  For stableDiffusion (a1111) and for 3d generation (Trellis, etc)
	// This allows us to connect to two different servers, one for generating textures, one for geometry.
	public class Connection_MGR : MonoBehaviour{
	    public static Connection_MGR instance { get; private set; } = null;

	    [SerializeField] RectTransform _placeOnTopOfMainView;
	    [SerializeField] GraphicRaycaster _raycaster; //will raycast towards the help button
	    [Space(10)]
	    [SerializeField] ConnectionPanel_UI _a1111_connPanel;
	    [SerializeField] ConnectionPanel_UI _3d_connPanel;

	    bool _did_init = false;
	    public static bool is_sd_connected =>  instance==null?false : instance._a1111_connPanel.isConnected;
	    public static bool is_3d_connected =>  instance==null?false : instance._3d_connPanel.isConnected;

	    public static string A1111_IP_AND_PORT => instance ==null? "" : "http://" + instance._a1111_connPanel.ip_and_port;
	    public static string A1111_SD_API_URL  => instance == null ? "" : "http://" + instance._a1111_connPanel.ip_and_port + "/sdapi/v1";
	    public static string A1111_CTRLNET_API_URL => instance==null? "" : "http://" + instance._a1111_connPanel.ip_and_port + "/controlnet";
	    public static string A1111_INTERNAL_API_URL => instance==null? "" : "http://" + instance._a1111_connPanel.ip_and_port + "/internal";

	    public static string GEN3D_URL =>  instance == null ? "" : "http://" + instance._3d_connPanel.ip_and_port;


	    public void Save( StableProjectorz_SL spz ){
	        spz.connectionPanel = new ConnectionPanel_SL();
	        _a1111_connPanel.Save(spz);
	        _3d_connPanel.Save(spz);
	    }

	    public void Load( StableProjectorz_SL spz ){
	        _a1111_connPanel.Load(spz);
	        _3d_connPanel.Load(spz);
	        Init_maybe();
	    }

	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	        Init_maybe();
	    }


	    void Update(){
	        Global_Skeleton_UI.instance?.Place_onto_MainViewport_between_ribbons(_placeOnTopOfMainView);
	    }
    
	    void Init_maybe(){
	        if(_did_init){ return; }
	        _did_init = true;
	        _a1111_connPanel.Init_Maybe();
	        _3d_connPanel.Init_Maybe();
	    }
	}
}//end namespace
