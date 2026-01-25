using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace spz {

	public class Gen3D_API : MonoBehaviour{
	    public static Gen3D_API instance { get; private set; } = null;

	    Coroutine _gen_or_resume_crtn = null;
	    Coroutine _progress_crtn = null;
	    Coroutine _getSupportedOper_crtn = null;

	    TaskStatus _generateStatus = TaskStatus.COMPLETE;
	    GenerationResponse _generateResponse = null;


	    public bool IsServerAvailable => Connection_MGR.is_3d_connected;

	    // If you want to be notified when server availability changes:
	    public bool isBusy => _generateStatus!=TaskStatus.COMPLETE  && 
	                          _generateStatus!=TaskStatus.FAILED  &&
	                          _gen_or_resume_crtn!=null;

    
	    //uppercase, must ensure the capitalization exactly matches the one in python script.
	    public enum TaskStatus{
	        PROCESSING,
	        PREVIEW_READY,
	        COMPLETE,
	        FAILED
	    }

	    [Serializable]
	    public class PreviewUrls{
	        public string gaussian;
	        public string mesh;
	        public string radiance;
	    }

	    [Serializable]
	    public class GenerationResponse{
	        public TaskStatus status;
	        public int progress;
	        public string message;
	        public PreviewUrls preview_urls;
	        public string model_url;
	    }

	    [Serializable]
	    public class GenerationStatus{
	        // This matches the /status response from the single-generation approach:
	        public string status; // direct string from JSON
	        public int progress;
	        public string message;
	        public bool busy;
	    }


	    public enum GenerateWhat{
	        make_meshes_and_tex, //makes a mesh and its texture.
	        retexture, //prepares 2d textures for our existing mesh.
	    }

	    // other c# scripts that invoke our functions can supply these callbacks. We'll invoke them.
	    public class GenerationCallbacks{
	        public Action<float> onProgress;             // 0..1
	        public Action<string, byte[]> onPreviewReady;// (previewType, data)
	        public Action<byte[]> onDataDownloaded;
	        public Action<string> onError;
	        public Action onComplete;
	    }


	    public void CancelGeneration(){
	        if (_gen_or_resume_crtn == null){ return; }
	        StopCoroutine(_gen_or_resume_crtn);
	        _gen_or_resume_crtn = null;
	        // Also call the server's /interrupt endpoint:
	        StartCoroutine( InterruptOnServer() );
	    }


	    //<string,object>  object could be a dictionary, a list of base64 images, a float, etc.
	    public void StartGeneration( GenerateWhat what,  Dictionary<string,object> inputs,  GenerationCallbacks callbacks ){
	        if (!IsServerAvailable){
	            callbacks.onError?.Invoke("Server is not available");
	            return;
	        }
	        if(_gen_or_resume_crtn!=null){ StopCoroutine(_gen_or_resume_crtn);  }
	        _gen_or_resume_crtn = StartCoroutine( Generate_crtn(what, inputs, callbacks) );
	    }


	    public void GetSupportedOperations(Action<List<string>> onSuccess, Action<string> onError = null) {
	       if (_getSupportedOper_crtn != null){ StopCoroutine(_getSupportedOper_crtn); }
	        _getSupportedOper_crtn = StartCoroutine(GetSupportedOperations_crtn(onSuccess, onError));
	    }


	    IEnumerator GetSupportedOperations_crtn(Action<List<string>> onSuccess, Action<string> onError){
	        using (UnityWebRequest www = UnityWebRequest.Get($"{Connection_MGR.GEN3D_URL}/info/supported_operations")) {
	            yield return www.SendWebRequest();
	            try{
	                if (www.result == UnityWebRequest.Result.Success){
	                    List<string> operations = JsonConvert.DeserializeObject<List<string>>( www.downloadHandler.text );
	                    onSuccess?.Invoke(operations);
	                } else {
	                    onError?.Invoke($"Failed to get operations: {www.error}");
	                } 
	            }catch{
	                onError?.Invoke($"Failed to get operations: {www.error}");
	            }
	        }
	        _getSupportedOper_crtn = null;
	    }

	    IEnumerator Generate_crtn( GenerateWhat what,  Dictionary<string,object> inputs,  GenerationCallbacks callbacks ){
  
	        // Decide which endpoint:
	        string destin_url = $"{Connection_MGR.GEN3D_URL}/generate";

	        var jsonString = JsonConvert.SerializeObject(inputs);

	        // Start the generation, but don't yield yet:
	        _generateStatus = TaskStatus.PROCESSING;
	        _generateResponse = null;
	        StartCoroutine( GenerateSubmit_crtn(destin_url, jsonString) );

	        {//keep checking the progress:
	            if(_progress_crtn != null){  StopCoroutine(_progress_crtn); }
	            _progress_crtn = StartCoroutine( PollGenerationProgress(callbacks.onProgress) );
	            while(_generateStatus == TaskStatus.PROCESSING){ yield return null; }
        
	            if(_progress_crtn!=null){ StopCoroutine(_progress_crtn); }
	            _progress_crtn = null;
	        }

	        if (_generateResponse == null){
	            // We might have had an error or something else
	            callbacks.onError?.Invoke("No response from generation request");
	            _gen_or_resume_crtn = null;
	            yield break;
	        }

	        string download_endpoint = "";
	        switch (what){
	            case GenerateWhat.make_meshes_and_tex: download_endpoint = "/download/model"; break;
	            case GenerateWhat.retexture: download_endpoint = "/download/texture"; break;
	            default: Debug.LogError("unknown download endpoint"); break;
	        }

	        if (_generateStatus == TaskStatus.FAILED){// Show the error from the server (if any)
	            callbacks.onError?.Invoke($"Generation failed: {_generateResponse.message}");
	        }
	        else if (_generateStatus == TaskStatus.COMPLETE){// Download the final mesh
	            yield return StartCoroutine(Gen_downloadFinalData(callbacks, download_endpoint));
	        }
	        _gen_or_resume_crtn = null;
	    }


	    IEnumerator GenerateSubmit_crtn(string url, string jsonString){

	        using (UnityWebRequest www = UnityWebRequest.Post(url, jsonString, "application/json")){
	            yield return www.SendWebRequest();

	            if (www.result != UnityWebRequest.Result.Success){
	                Debug.LogError($"Generation request failed: {www.error}");
	                _generateResponse = null;
	                _generateStatus = TaskStatus.FAILED;
	                yield break;
	            }
	            try{
	                _generateResponse = JsonConvert.DeserializeObject<GenerationResponse>(www.downloadHandler.text);
	                _generateStatus = _generateResponse.status;
	            }catch (Exception e){
	                Debug.LogError($"JSON parse failed: {e.Message}\n{www.downloadHandler.text}");
	                _generateResponse = null;
	                _generateStatus = TaskStatus.FAILED;
	                _generateResponse = null;
	            }
	        }
	    }


	    // Poll /status (without trailing slash) until preview_ready, complete, or failed.
	    // Return the final TaskStatus via onStatusUpdate callback.
	    IEnumerator PollGenerationProgress(Action<float> onProgressUpdate){
	        float spacing_sec = 1f;

	        while (true){
	            string endpoint = $"{Connection_MGR.GEN3D_URL}/status"; 
	            using (UnityWebRequest www = UnityWebRequest.Get(endpoint))
	            {
	                yield return www.SendWebRequest();
	                if (www.result != UnityWebRequest.Result.Success){
	                    Debug.LogError($"PollGenerationProgress => WebRequest {www.result} ");
	                    break; 
	                }
	                GenerationStatus st = null;
	                try{
	                    st = JsonConvert.DeserializeObject<GenerationStatus>(www.downloadHandler.text);
	                }
	                catch (Exception e){
	                    Debug.LogError("PollGenerationProgress => JSON parse error: " + e.Message);
	                    break;
	                }

	                onProgressUpdate?.Invoke(st.progress / 100f);
                
	                //we should NOT set generation status ourselves, but we can monitor it anyway:
	                if(_generateStatus != TaskStatus.PROCESSING){ break; }
	            }
	            yield return new WaitForSeconds(spacing_sec);
	        }//end while
	        _progress_crtn = null;
	    }

    
	    IEnumerator ResumeAfterPreview_crtn(float meshSimplifyRatio, int textureSize, GenerationCallbacks callbacks){
	        //not yielding the coroutine, just starting and continuing
	        _generateStatus = TaskStatus.PROCESSING;
	        _generateResponse = null;
	        StartCoroutine( ResumeSubmit_crtn(meshSimplifyRatio, textureSize, callbacks) );
        
	        {//keep checking the progress:
	            if(_progress_crtn != null){  StopCoroutine(_progress_crtn); }
	            _progress_crtn = StartCoroutine( PollGenerationProgress(callbacks.onProgress) );
	            while(_generateStatus == TaskStatus.PROCESSING){ yield return null; }
        
	            if(_progress_crtn!=null){ StopCoroutine(_progress_crtn); }
	            _progress_crtn = null;
	        }

	        if (_generateStatus == TaskStatus.COMPLETE){
	            yield return StartCoroutine(Gen_downloadFinalData(callbacks));
	        }
	        else if (_generateStatus == TaskStatus.FAILED){
	            callbacks.onError?.Invoke("Resume generation => task failed");
	        }
	        _gen_or_resume_crtn = null;
	    }


	    IEnumerator ResumeSubmit_crtn(float meshSimplifyRatio, int textureSize, GenerationCallbacks callbacks){
	        string resumeUrl = $"{Connection_MGR.GEN3D_URL}/resume_from_preview" +
	            $"?mesh_simplify_ratio={meshSimplifyRatio}" +
	            $"&texture_size={textureSize}";

	        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(resumeUrl, "")){
	            yield return www.SendWebRequest();
	            if (www.result != UnityWebRequest.Result.Success){
	                callbacks.onError?.Invoke("Resume generation failed: " + www.error);
	                _generateStatus = TaskStatus.FAILED;
	                _gen_or_resume_crtn = null;
	            }

	            try{
	                _generateResponse = JsonConvert.DeserializeObject<GenerationResponse>(www.downloadHandler.text);
	                _generateStatus = _generateResponse.status;
	            }catch (Exception e){
	                Debug.LogError($"JSON parse failed: {e.Message}\n{www.downloadHandler.text}");
	                _generateResponse = null;
	                _generateStatus = TaskStatus.FAILED;
	                _generateResponse = null;
	            }
	        }
	    }


	    IEnumerator InterruptOnServer(){
	        using (UnityWebRequest www = UnityWebRequest.PostWwwForm($"{Connection_MGR.GEN3D_URL}/interrupt", "")){
	            yield return www.SendWebRequest();
	            if (www.result == UnityWebRequest.Result.Success){
	                Debug.Log("Server interrupt request sent.");
	            }else{
	                Debug.LogError("Failed to send interrupt request: " + www.error);
	            }
	        }
	    }


	    IEnumerator Gen_downloadPreviews(GenerationCallbacks callbacks)
	    {
	        if (_generateResponse.preview_urls == null) yield break;

	        // The simplified server endpoints: /download/preview/gaussian, etc.
	        // No extra ID here; we assume a single generation context

	        // NOTICE: we only generated gaussian to save performance, so skip downloading the other 2:
	        string[] previewTypes = { "gaussian"/*, "mesh", "radiance" */};
	        foreach (string previewType in previewTypes)
	        {
	            string previewUrl = $"{Connection_MGR.GEN3D_URL}/download/preview/{previewType}";
	            using (UnityWebRequest www = UnityWebRequest.Get(previewUrl))
	            {
	                yield return www.SendWebRequest();
	                if (www.result == UnityWebRequest.Result.Success){
	                    callbacks.onPreviewReady?.Invoke(previewType, www.downloadHandler.data);
	                }else{
	                    Debug.LogWarning($"Failed to download {previewType} preview: {www.error}");
	                }
	            }
	        }//end foreach
	    }


	    //mesh, textures, etc.
	    IEnumerator Gen_downloadFinalData(GenerationCallbacks callbacks, string download_endpoint="/download/model")
	    {
	        using (UnityWebRequest www = UnityWebRequest.Get($"{Connection_MGR.GEN3D_URL}{download_endpoint}")){
	            yield return www.SendWebRequest();

	            if (www.result == UnityWebRequest.Result.Success){
	                callbacks.onDataDownloaded?.Invoke(www.downloadHandler.data);
	                callbacks.onComplete?.Invoke();
	            }else{
	                callbacks.onError?.Invoke($"Failed to download data: {www.error}");
	            }
	        }
	    }//end()



	    void Awake(){
	        if (instance != null){ DestroyImmediate(this); return;}
	        instance = this;
	    }

	}
}//end namespace
