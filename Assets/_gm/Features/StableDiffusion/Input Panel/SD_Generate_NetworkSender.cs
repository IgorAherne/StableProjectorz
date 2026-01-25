using System;
using System.Linq;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace spz {

	public class SD_Generate_NetworkSender : MonoBehaviour{

	    Action<UnityWebRequest> _onProgress = null;
	    Action<UnityWebRequest> _onCompleted = null;

	    // Other serialized fields to capture input from the UI
	    public void Send_GenerateRequest( SD_txt2img_payload req,  Action<UnityWebRequest> onProgress,  Action<UnityWebRequest> onCompleted ){
	        _onProgress = onProgress;
	        _onCompleted = onCompleted;
	        string url = Connection_MGR.A1111_SD_API_URL + "/txt2img";
	        StartCoroutine( Send_GenerateRequest_crtn( url, req, req.width, req.height, withProgress:true) );
	    }

    
	    public void Send_GenerateRequest( SD_img2img_payload payload,  Action<UnityWebRequest> onProgress,  Action<UnityWebRequest> onCompleted ){
	        _onProgress = onProgress;
	        _onCompleted = onCompleted;
	        string url = Connection_MGR.A1111_SD_API_URL + "/img2img";
	        StartCoroutine( Send_GenerateRequest_crtn( url, payload, payload.width, payload.height, withProgress:true) );
	    }

    
	    public void Send_GenerateRequest( SD_img2extra_payload payload,  Action<UnityWebRequest> onProgress,  Action<UnityWebRequest> onCompleted ){
	        _onProgress = onProgress;
	        _onCompleted = onCompleted;
	        string url = Connection_MGR.A1111_SD_API_URL + "/extra-batch-images";
	        StartCoroutine( Send_GenerateRequest_crtn(url, payload, payload.rslt_imageWidths, payload.rslt_imageHeights, withProgress:true) );
	    }


	    public void Send_GenerateRequest(SD_ControlnetDetect_payload payload, Action<UnityWebRequest> onComplete){
	        _onProgress = null;
	        _onCompleted = onComplete;
	        string url = Connection_MGR.A1111_CTRLNET_API_URL + "/detect";
	        StartCoroutine( Send_GenerateRequest_crtn(url, payload, -1, -1, withProgress:false ));
	    }


	    public void Send_StopGenerateRequest(){
	        StopAllCoroutines();//stops any progress-tracking coroutines, etc.
	        string url = Connection_MGR.A1111_SD_API_URL + "/interrupt";
	        StartCoroutine( Send_GenerateRequest_crtn<object>(url, null, width:-1, height:-1, withProgress:false) );
	    }


	    IEnumerator Send_GenerateRequest_crtn<T>(string urlSuffix, T payloadStruct, int width, int height, bool withProgress)
	    {
	        Coroutine progressRoutine = null;
	        if (withProgress){
	            progressRoutine = StartCoroutine(CheckProgress_crtn(width, height));
	        }

	        using (UnityWebRequest request = new UnityWebRequest(urlSuffix, "POST")){
	            if (payloadStruct != null){
	                var settings = new JsonSerializerSettings{
	                    Formatting = Formatting.Indented,
	                    TypeNameHandling = TypeNameHandling.Auto //automatically resolve inheritance/abstract classes
	                };
	                string json = JsonConvert.SerializeObject(payloadStruct, settings);
	                byte[] jsonToSend = new UTF8Encoding().GetBytes(json);
	                request.uploadHandler = new UploadHandlerRaw(jsonToSend);
	                request.SetRequestHeader("Content-Type", "application/json");
	            }
	            request.downloadHandler = new DownloadHandlerBuffer();

	            yield return request.SendWebRequest();

	            if (progressRoutine != null){
	                StopCoroutine(progressRoutine);
	            }
	            _onCompleted?.Invoke(request);
	        }
	    }


	    IEnumerator CheckProgress_crtn(int width, int height){
	        string progressUrl = Connection_MGR.A1111_SD_API_URL + "/progress";
    
	        while (true){
	            yield return new WaitForSeconds( CalculateWaitTime(width,height) );

	             using (UnityWebRequest request = UnityWebRequest.Get(progressUrl)){
	                yield return request.SendWebRequest();
	                _onProgress?.Invoke(request);
	                if(request.result == UnityWebRequest.Result.ConnectionError){ yield break; }
	                if(request.result == UnityWebRequest.Result.ProtocolError){ yield break;  }
	             }
	        }//end while
	    }

	    float CalculateWaitTime(int width, int height) {
	        int totalPixels = width*height;
	        float spacing = CalculateSpacing(totalPixels);
	        return spacing;
	    }

	    float CalculateSpacing(int totalPixels) {
	        if (totalPixels <= 256*256){ return 0.5f; }
	        else if (totalPixels <= 512*512){ return 1f; }
	        else if (totalPixels <= 750*750){ return 3f; }
	        else if (totalPixels <= 1024*1024){ return 5f; }
	        else if (totalPixels <= 1600*1600){ return 6f; }
	        else if (totalPixels <= 2048*2048){ return 15f; }
	        else if (totalPixels <= 3000*3000){ return 15f; }
	        else if (totalPixels <= 3500*3500){ return 15f; }
	        return 12;
	    }
	}
}//end namespace
