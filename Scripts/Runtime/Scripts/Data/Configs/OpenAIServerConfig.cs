using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DoubTech.ThirdParty.AI.Common.Data;
using Newtonsoft.Json;
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
        
        [Header("Endpoint Configuration")]
        [SerializeField] private string host = DEFAULT_HOST;
        [SerializeField] private string apiEndpoint = "/v1";
        [SerializeField] private string modelsEndpoint = "/models";
        [SerializeField] private string internalModelsEndpoint = "/internal/model/list";
        
        [Password]
        [SerializeField] public string apiKey;
        [SerializeField] public string[] models;
        
        public string ApiURL => $"{host}/{apiEndpoint}";

        public string GetUrl(string endpoint) => $"{host.Trim('/')}/{apiEndpoint.Trim('/')}/{endpoint.Trim('/')}";

        public override string[] Models => models;

        public override string GetUrl(params string[] path) => string.Join("/", ApiURL, string.Join("/", path));

        public override async Task RefreshModels()
        {
            try
            {
                string[] modelEndpoints = new string[]
                {
                    modelsEndpoint,
                    internalModelsEndpoint
                };

                List<string> modelNames = new List<string>();
                // Try to get the models
                foreach (var endpoint in modelEndpoints)
                {
                    var url = GetUrl(endpoint);
                    Debug.Log("AARON: url: " + url);
                    string response = await GetDataAsync(url);
                    if (!string.IsNullOrEmpty(response))
                    {
                        try
                        {
                            var models = endpoint == modelsEndpoint
                                ? ModelData.GetModelNames(response)
                                : ModelCollection.ExtractModelNames(response);
                            if (null != models)
                            {
                                modelNames.AddRange(models);
                            }
                        }
                        catch (JsonReaderException e)
                        {
                            Debug.LogWarning(e);
                            Debug.LogWarning(response);
                        }
                    }
                }

                models = modelNames.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
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