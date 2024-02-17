using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;

namespace DoubTech.ThirdParty.OpenAI
{
    public class ModelData
    {
        public string id { get; set; }
        public string @object {
            get;
            set;
        }

        public long created { get; set; }
        public string owned_by { get; set; }
        
        public static List<string> GetModelNames(string jsonResponse)
        {
            var responseData = JsonConvert.DeserializeObject<ResponseData>(jsonResponse);
            List<string> modelNames = new List<string>();

            if (responseData != null && responseData.data != null)
            {
                foreach (var model in responseData.data)
                {
                    modelNames.Add(model.id);
                }
            }

            return modelNames;
        }
    }

    public class ResponseData
    {
        public string @object { get; set; }
        public List<ModelData> data { get; set; }
    }
    
    public class ModelCollection
    {
        [JsonProperty("model_names")]
        public List<string> ModelNames { get; set; }
        public static List<string> ExtractModelNames(string jsonResponse)
        {
            var modelCollection = JsonConvert.DeserializeObject<ModelCollection>(jsonResponse);
            return modelCollection?.ModelNames ?? new List<string>();
        }
    }
}