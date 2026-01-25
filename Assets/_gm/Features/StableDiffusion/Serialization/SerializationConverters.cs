using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;


namespace spz {

	//helps to serialize class SDUpscalerItem

	// NOTICE: important! Without this converter, the scale can be null for custom DAT upscalers,
	// resulting in null ref exception.  Aug 2024. 
	public class SDUpscalerItemConverter : JsonConverter<SDUpscalerItem>{
	    public override SDUpscalerItem ReadJson(JsonReader reader, Type objectType, SDUpscalerItem existingValue, bool hasExistingValue, JsonSerializer serializer)
	    {
	        var jsonObject = JObject.Load(reader);
	        var upscalerItem = new SDUpscalerItem
	        {
	            name = jsonObject["name"].ToString(),
	            model_name = jsonObject["model_name"]?.ToString(),
	            model_path = jsonObject["model_path"]?.ToString(),
	            model_url = jsonObject["model_url"]?.ToString(),
	            scale = jsonObject["scale"]?.ToObject<float?>() ?? 1f // Use ToObject<float?>() and null-coalescing operator
	        };
	        return upscalerItem;
	    }
	    public override void WriteJson(JsonWriter writer, SDUpscalerItem value, JsonSerializer serializer){
	        throw new NotImplementedException();
	    }
	}

}//end namespace
