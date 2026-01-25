using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;

namespace spz {

	public class Gen3D_VideoPreview_UI : MonoBehaviour{
	    [SerializeField] protected GameObject _videoPreview_go;
	    [SerializeField] protected VideoPlayer _videoPlayer_gauss;
	    [SerializeField] protected VideoPlayer _videoPlayer_mesh;
	    [SerializeField] protected VideoPlayer _videoPlayer_radiance;
	    [Space(10)]
	    [SerializeField] protected GameObject _decisionButtons_go;
	    [SerializeField] protected Button _video_OK_button;
	    [SerializeField] protected Button _video_NO_button;
	    [SerializeField] protected Button _video_retry_button;
	    [Space(10)]
	    [SerializeField] protected Toggle _gauss_toggle; //which video to show
	    [SerializeField] protected Toggle _mesh_toggle;
	    [SerializeField] protected Toggle _radiance_toggle;

	    RenderTexture _video_renderTex_gauss;
	    RenderTexture _video_renderTex_mesh;
	    RenderTexture _video_renderTex_radiance;

	    protected string _videoClip_gauss_fileURL = "";
	    protected string _videoClip_mesh_fileURL = "";
	    protected string _videoClip_radiance_fileURL = "";

	    public Action onVideoLiked { get; set; }
	    public Action onVideoDisliked { get; set; }
	    public Action onVideoRetry { get; set; }

	    protected virtual void Gen_OnVideoReady(string previewType, byte[] videoData){
	        if(previewType == "gaussian"){
	            _videoClip_gauss_fileURL = make_videoFile(previewType, videoData);
	            _videoPlayer_gauss.Prepare();
	        }
	        if(previewType == "mesh"){
	            _videoClip_mesh_fileURL = make_videoFile(previewType, videoData);
	            _videoPlayer_mesh.Prepare();
	        }
	        if(previewType == "radiance"){
	            _videoClip_radiance_fileURL = make_videoFile(previewType, videoData);
	            _videoPlayer_radiance.Prepare();
	        }
	        if(string.IsNullOrEmpty(_videoClip_gauss_fileURL) /*  || 
	           //for performance reasons we'll  only show/need gauss:
	           string.IsNullOrEmpty(_videoClip_mesh_fileURL) ||  
	           string.IsNullOrEmpty(_videoClip_radiance_fileURL)  */){ 
	            return; 
	        }//else, all video files are made, so playthem:
	        GenerateButtons_UI.OnConfirmed_GeneratePaused(true);

	        //for performance reasons we only show/need gauss:
	        ShowVideos(_videoPlayer_gauss, _videoClip_gauss_fileURL);
	        // ShowVideos(_videoPlayer_mesh, _videoClip_mesh_fileURL);
	        // ShowVideos(_videoPlayer_radiance, _videoClip_radiance_fileURL);
	    }

	    protected virtual void OnVideo_Liked(){
	        //var cbks = new Trellis3D_API.GenerationCallbacks{
	        //    onProgress       = Gen_OnProgress,
	        //    onPreviewReady   = Gen_OnVideoReady,
	        //    onMeshDownloaded = Gen_OnMeshReady,
	        //    onError    = Gen_OnError,
	        //    onComplete = Gen_OnComplete,
	        //};
	        //float meshSimplifyRatio = _mesh_simplifyRatio_slider.value;
	        //int textureSize = _textureSize.recentVal;
	        //Trellis3D_API.instance.Resume_after_preview( meshSimplifyRatio:meshSimplifyRatio, 
	        //                                             textureSize:textureSize,  cbks );
	        //_videoPreview_go.SetActive(false);
	        //GenerateButtons_UI.OnConfirmed_GeneratePaused(false);
	        onVideoLiked.Invoke();
	    }

	    protected virtual void OnVideo_Disliked(){
	        //_videoPreview_go.SetActive(false);
	        //GenerateButtons_UI.OnConfirmed_FinishedGenerate(canceled:true);
	        onVideoDisliked?.Invoke();
	    }

	    protected virtual void OnVideoRetry(){
	        //_videoPreview_go.SetActive(false);
	        //_seed_inputField.SetValue("-1");//-1 makes it random, so we can retry for another result.

	        //GenerateButtons_UI.OnConfirmed_FinishedGenerate(canceled:true);
	        //Gen_OnStart();
	        onVideoRetry?.Invoke();
	    }


	    void OnVideoTypeToggle(Toggle tog, bool isOn){
	        if(!isOn){ return; }
	        // Keep 1 videoPlayer image and disable other 2 images.
	        // Don't disable game objects/component, to avoid mesisng up the playback. Only their color:
	        _videoPlayer_gauss.GetComponent<RawImage>().color = Color.clear;
	        _videoPlayer_mesh.GetComponent<RawImage>().color = Color.clear;
	        _videoPlayer_radiance.GetComponent<RawImage>().color = Color.clear;
	        if(tog == _gauss_toggle){ _videoPlayer_gauss.GetComponent <RawImage>().color = Color.white; }
	        if(tog == _mesh_toggle){  _videoPlayer_mesh.GetComponent<RawImage>().color = Color.white;  }
	        if(tog == _radiance_toggle){ _videoPlayer_radiance.GetComponent<RawImage>().color = Color.white; }
	    }


	    protected string make_videoFile(string previewType, byte[] videoData){
	        // Create temp path for video
	        try { 
	            string tempPath = Path.Combine(Application.temporaryCachePath, $"preview_{previewType}.mp4");
	            if (File.Exists(tempPath)){  File.Delete(tempPath); }// Clean up temp file

	            _videoPreview_go.SetActive(true);
	            File.WriteAllBytes(tempPath, videoData);
	            // Create URL for video player
	            string videoUrl = "file://" + tempPath;
	            return videoUrl;
	        }catch{
	            return "";
	        }
	    }

	    protected virtual void ShowVideos(VideoPlayer player, string fileURL){
	        _videoPreview_go.SetActive(true);
	        try {
	            // Load and play video
	            player.url = fileURL;
	            player.Play();
	        }
	        catch (Exception e){
	            _videoPreview_go.SetActive(false);
	            Debug.LogError($"Failed to save or play preview video: {e.Message}");
	        }
	    }

	    protected void CleanupPreviewVideos(){
	        if (File.Exists(_videoClip_gauss_fileURL)){ File.Delete(_videoClip_gauss_fileURL); }
	        if (File.Exists(_videoClip_mesh_fileURL)){ File.Delete(_videoClip_mesh_fileURL); }
	        if (File.Exists(_videoClip_radiance_fileURL)){ File.Delete(_videoClip_radiance_fileURL); }
	        _videoClip_gauss_fileURL = "";
	        _videoClip_mesh_fileURL = "";
	        _videoClip_radiance_fileURL = "";
	    }

    
	    protected virtual void Awake(){
	        _gauss_toggle.onValueChanged.AddListener( isOn => OnVideoTypeToggle(_gauss_toggle, isOn) );
	        _mesh_toggle.onValueChanged.AddListener( isOn => OnVideoTypeToggle(_mesh_toggle, isOn) );
	        _radiance_toggle.onValueChanged.AddListener( isOn => OnVideoTypeToggle(_radiance_toggle, isOn) );
	        _gauss_toggle.isOn = true;
	    }

	    protected virtual void Start(){
	        _video_OK_button.onClick.AddListener( OnVideo_Liked );
	        _video_NO_button.onClick.AddListener( OnVideo_Disliked );
	        _video_retry_button.onClick.AddListener( OnVideoRetry );
	        _videoPreview_go.gameObject.SetActive(false);
	    }

	       protected virtual void OnEnable(){
	        Init_MakeVideoTexture(ref _videoPlayer_gauss, ref _video_renderTex_gauss, _videoPlayer_gauss.GetComponent<RawImage>());
	        Init_MakeVideoTexture(ref _videoPlayer_mesh, ref _video_renderTex_mesh,  _videoPlayer_mesh.GetComponent<RawImage>());
	        Init_MakeVideoTexture(ref _videoPlayer_radiance, ref _video_renderTex_radiance,  _videoPlayer_radiance.GetComponent<RawImage>());
	    }
    
	    protected virtual void OnDisable(){
	        TextureTools_SPZ.Dispose_RT(ref _video_renderTex_gauss, false);
	        TextureTools_SPZ.Dispose_RT(ref _video_renderTex_mesh, false);
	        TextureTools_SPZ.Dispose_RT(ref _video_renderTex_radiance, false);
	    }
    
	    void Init_MakeVideoTexture(ref VideoPlayer player_, ref RenderTexture rt_, RawImage raw_img){
	        rt_ = new RenderTexture(512, 512, depth: 0, GraphicsFormat.R8G8B8A8_UNorm, mipCount: 1);
	        player_.targetTexture = rt_;
	        raw_img.texture = rt_;
	    }

	    void OnDestroy(){
	        TextureTools_SPZ.Dispose_RT(ref _video_renderTex_gauss, false);
	        TextureTools_SPZ.Dispose_RT(ref _video_renderTex_mesh, false);
	        TextureTools_SPZ.Dispose_RT(ref _video_renderTex_radiance, false);
	        CleanupPreviewVideos();
	    }

	}
}//end namespace
