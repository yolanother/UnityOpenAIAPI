using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DoubTech.ThirdParty.AI.Common.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace DoubTech.ThirdParty.OpenAI
{
    [CreateAssetMenu(fileName = "OpenAI Config", menuName = "DoubTech/AI APIs/Config/OpenAI", order = 0)]
    public class OpenAIServerConfig : ApiConfig, IBearerAuth
    {
        const string DEFAULT_HOST = "https://api.openai.com";
        const string ENDPOINT_MODELS = "/v1/models";
        const string ENDPOINT_INTERNAL_MODELS = "/v1/internal/model/list";
        
        [SerializeField] private string host = DEFAULT_HOST;
        [Password]
        [SerializeField] public string apiKey;
        [SerializeField] public string[] models;
        
        public string ApiURL => $"{host}/v1";

        public string GetUrl(string endpoint) => $"{host}/{endpoint}";

        public override string[] Models => models;

        public override string GetUrl(params string[] path) => string.Join("/", ApiURL, string.Join("/", path));

        public override async Task RefreshModels()
        {
            string[] modelEndpoints = new string[]
            {
                ENDPOINT_MODELS,
                ENDPOINT_INTERNAL_MODELS
            };
            
            List<string> modelNames = new List<string>();
            // Try to get the models
            foreach (var endpoint in modelEndpoints)
            {
                string response = await GetDataAsync(GetUrl(endpoint));
                if (!string.IsNullOrEmpty(response))
                {
                    var models = endpoint == ENDPOINT_MODELS
                        ? ModelData.GetModelNames(response)
                        : ModelCollection.ExtractModelNames(response);
                    if (null != models)
                    {
                        modelNames.AddRange(models);
                    }
                }
            }

            models = modelNames.ToArray();
        }
        
        public async Task<string> GetDataAsync(string url)
        {
            try
            {
                using (HttpClient _httpClient = new HttpClient())
                {
                    // Add the Authorization header with the Bearer token
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                    HttpResponseMessage response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
            }
            catch (HttpRequestException e)
            {
                Debug.LogError(e);
                return null;
            }
        }

        public string ApiKey => apiKey;
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(OpenAIServerConfig))]
    public class OpenAIServerConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            OpenAIServerConfig config = (OpenAIServerConfig) target;
            if (GUILayout.Button("Refresh Models"))
            {
                _ = config.RefreshModels();
            }
        }
    }
    #endif
}