using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace spz {

	// can either be for connecting to server that generates textures (A1111, Forge), 
	// or for connecting to server that generates 3d and meshes.
	// Knows about the connection button and the ui-panel with IP+Port inputs.
	// Will hide its panel and button depending on the 'DimensionMode_MGR' - either 3D, 2D or UV representations.
	public class ConnectionPanel_UI : MonoBehaviour{
    
	    [SerializeField] GraphicRaycaster _raycaster; //will raycast towards the help button
	    [SerializeField] NonDrawingGraphic _hoverSurface;
	    [SerializeField] Button _openPanel_button;
	    [SerializeField] RectTransform _panel; // Contains fields for IP, port, etc.
	    [Space(10)]
	    [SerializeField] TMP_InputField _ip_text;
	    [SerializeField] IntegerInputField _port_text; // Assuming this is a custom component
	    [SerializeField] Image _connectionIcon; // Shows status of connection (red/green)
	    [SerializeField] TextMeshProUGUI _dim_text; // Shows 2D or 3D (stable diffusion or Trellis)
	    [SerializeField] Button _resetToDefault_button;//we set ip and port to usual ones.
	    [Space(10)]
	    [SerializeField] string IP_PlayerPrefs_KEY = "StableDiffusionIP"; //player prefs (for saving settings to disk)
	    [SerializeField] string PORT_PlayerPrefs_KEY = "StableDiffusionPort";
	    [Space(10)]
	    [SerializeField] string _default_ip = "127.0.0.1";
	    [SerializeField] string _default_port = "7860";
	    enum ConnectionPanel_Kind{
	        StableDiffusion, /*for 2D generation*/
	        Trellis,/*for 3d generation*/
	    }
	    [Space(10)]
	    [SerializeField] ConnectionPanel_Kind _panelKind;

	    bool _did_init = false;
	    Coroutine _connectionCheckCoroutine;
	    float _time_clickedOpenPanelButton;
	    string _url_for_ping;

	    public string ip_text => _ip_text.text;
	    public int port => _port_text.recentVal;
	    public string ip_and_port => ip_text + ":" + port;
	    public Action<string,int> _connectDetailsChanged { get; set; } = null;

	    public bool isConnected { get; private set; } = false;


    
	    //after we launch stableProjectorz, there is some grace-period while the CMD webui window will activate and become ready.
	    bool isStill_warmingUp()=> Time.unscaledTime < 30;



	    void Update(){
	        ShowHide_ConnButton();
	        if(_panel.gameObject.activeSelf == false){ return; }
	        float elapsed = Time.time - _time_clickedOpenPanelButton;
	        if(elapsed < 0.1f){ return; }
	        if(!IsHovering_Panel()){ _panel.gameObject.SetActive(false); }
	    }


	    void ShowHide_ConnButton(){
	        // show or hide our connection button, if we are for StableDiffusion, but user is generating 3D, etc.
	        switch (DimensionMode_MGR.instance._dimensionMode){
	            case DimensionMode.dim_uv:
	            case DimensionMode.dim_sd:
	                _openPanel_button.gameObject.SetActive(_panelKind == ConnectionPanel_Kind.StableDiffusion);
	                break;
	            case DimensionMode.dim_gen_3d:
	                _openPanel_button.gameObject.SetActive(_panelKind == ConnectionPanel_Kind.Trellis);
	                break;
	        }
	    }

    
	    bool IsHovering_Panel(){
	        PointerEventData eventData = new PointerEventData( EventSystem.current );
	        eventData.position = Input.mousePosition;

	        List<RaycastResult> results = new List<RaycastResult>();
	        _raycaster.Raycast(eventData, results);

	        foreach (var result in results){
	            NonDrawingGraphic g = result.gameObject.GetComponent<NonDrawingGraphic>();
	            if (g==_hoverSurface){ return true; }
	        }
	        return false;
	    }


	    void OnOpenPanel_Button(){
	        _panel.gameObject.SetActive(true);
	        _time_clickedOpenPanelButton = Time.time;
	    }

    
	    IEnumerator CheckConnection(bool setColorToPending_once ){
	        Color pendingColor = new Color(1, 0.8f, 0, 1);//orange-yellow

	        float spacing = 0.5f;

	        while (true){
	            string url_for_ping = where_to_ping(this);

	            bool skip =  url_for_ping==""  ||  StableDiffusion_Hub.instance==null;
            
	            if (skip){
	                yield return new WaitForSeconds(spacing);
	                continue;
	            }

	            using (UnityWebRequest request = UnityWebRequest.Get(url_for_ping)){

	                bool noConn_butJustStarted =  !isConnected && isStill_warmingUp();

	                if (setColorToPending_once || noConn_butJustStarted ){
	                    setColorToPending_once = false;
	                    _dim_text.color = _connectionIcon.color = pendingColor;
	                }
	                // Increase threshold 'connected'/not to 20 during generation, or if already connected.
	                // Because people had disconnects during generation if just 4.
	                // So if not connected -> short timeout
	                // If generating or conencted -> longer timeout (trusting more that we're still connected)
	                request.timeout = 4;
	                request.timeout = isConnected? 12 : request.timeout; 
	                request.timeout = StableDiffusion_Hub.instance._generating? 25 : request.timeout;
	                yield return request.SendWebRequest();

	                if (request.result == UnityWebRequest.Result.Success){//connection successful:
	                    PlayerPrefs_SaveConnDetails();
	                    _dim_text.color = _connectionIcon.color = Color.green;
	                    isConnected = true;
	                }
	                else{//Connection failed:
	                    _dim_text.color =  _connectionIcon.color =  isStill_warmingUp()?  pendingColor : Color.red;
	                    isConnected = false;
	                }
	            }
	            yield return new WaitForSeconds(spacing); // Check every 0.5 seconds
	        }
	        //_connectionCheckCoroutine = null;
	    }

    
	    string where_to_ping(ConnectionPanel_UI panel){
	        switch (_panelKind){
	            case ConnectionPanel_Kind.StableDiffusion:
	                return Connection_MGR.A1111_INTERNAL_API_URL + "/ping";
	            case ConnectionPanel_Kind.Trellis:
	                return Connection_MGR.GEN3D_URL + "/ping";
	        }
	        return "";
	    }


	    public void Init_Maybe(){ 
	        if(_did_init){ return; }
	        _did_init = true;
	        //enable panel so that it can run Awake(). That way its text and input field manage to intialize its values.
	        //This is important for our IP_AND_PORT and other static variables that use its child components.
	        _panel.gameObject.SetActive(true);
	        _panel.gameObject.SetActive(false);

	        // Occasionally, those values are somehow not set (even after _panel.gameObject.SetActive(true).
	        // This especially happens in new users, during their first launch.
	        // It only connects after we manually click on the red-connection icon, which makes it green.
	        // So, I suspect these values are incorrect until the panel, opens. Let's manually set them here, just in case:
	        // Feb 2024
	        if (string.IsNullOrEmpty(_ip_text.text)){ _ip_text.text = "127.0.0.1"; }
	        if(_port_text.recentVal==0){ _port_text.SetValue( _panelKind==ConnectionPanel_Kind.StableDiffusion?"7860":"7960"); }
	        PlayerPrefs_LoadConnDetails();

	        // Add listeners for changes in the IP and port input fields:
	        _ip_text.onValueChanged.AddListener(s=>{
	            _connectDetailsChanged?.Invoke(ip_text, port);
	            PlayerPrefs_SaveConnDetails();
	        });
	        _port_text.onValidInput.AddListener(i=>{
	            _connectDetailsChanged?.Invoke(ip_text, port);
	            PlayerPrefs_SaveConnDetails();
	        });
	    }


	    void PlayerPrefs_LoadConnDetails(){
	        if (PlayerPrefs.HasKey(IP_PlayerPrefs_KEY)){
	            _ip_text.text =  PlayerPrefs.GetString(IP_PlayerPrefs_KEY);
	        }
	        if (PlayerPrefs.HasKey(PORT_PlayerPrefs_KEY)){
	            _port_text.SetValueWithoutNotify( PlayerPrefs.GetString(PORT_PlayerPrefs_KEY) );
	        }
	    }


	    void PlayerPrefs_SaveConnDetails(){
	        // Save the new IP and Port to PlayerPrefs
	        PlayerPrefs.SetString(IP_PlayerPrefs_KEY, _ip_text.text);
	        PlayerPrefs.SetString(PORT_PlayerPrefs_KEY, _port_text.recentVal.ToString() );
	        PlayerPrefs.Save();
	    }

    
	    void OnResetToDefault_button(){
	        _ip_text.SetTextWithoutNotify(_default_ip);
	        _port_text.SetValueWithoutNotify(_default_port);
	        _connectDetailsChanged?.Invoke(ip_text, port);
	        PlayerPrefs_SaveConnDetails();
	    }


	    public void Save( StableProjectorz_SL spz ){
	        spz.connectionPanel = new ConnectionPanel_SL();
	        //COMMENTED OUT, KEPT FOR PRECAUTION:  do NOT save IP and Port of people (can expose person if they share save file)
	        //          spz.connectionPanel.ip = _ip_text.text;
	        //          spz.connectionPanel.port = _port_text.recentVal;
	    }

	    public void Load( StableProjectorz_SL spz ){
	        Init_Maybe();
	        // _ip_text.text = spz.connectionPanel.ip;
	        // _port_text.SetValue( spz.connectionPanel.port.ToString() );
	    }

    
	    void Awake(){
	        _resetToDefault_button.onClick.AddListener( OnResetToDefault_button );
	        _openPanel_button.onClick.AddListener( OnOpenPanel_Button );
	    }

	    void Start(){
	        _connectionCheckCoroutine = Coroutines_MGR.instance.StartCoroutine( CheckConnection(setColorToPending_once:true) );
	    }
	}
}//end namespace
