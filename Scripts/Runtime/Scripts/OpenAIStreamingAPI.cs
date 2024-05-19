using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using DoubTech.ThirdParty.AI.Common;
using DoubTech.ThirdParty.AI.Common.Data;
using DoubTech.ThirdParty.OpenAI.Scripts.Data;
using Newtonsoft.Json;
using UnityEditor;

namespace DoubTech.ThirdParty.OpenAI
{
    public class OpenAIStreamingAPI : BaseAIStreamingAPI
    {
        protected override object OnPrepareData(Request requestData)
        {
            return new CompletionRequest
                        {
                            model = Model,
                            messages = MessageHistory,
                            stream = Stream
                        };
        }

        protected override string[] OnGetRequestPath()
        {
            throw new NotImplementedException();
        }

        protected override Response OnHandleStreamedResponse(string blob, Response currentResponse)
        {
            Response resultResponse = null;
            ChatCompletionChunk completion;
            if (blob.StartsWith("data: "))
            {
                Debug.Log(blob);
                var json = blob.Substring(6);
                if (json == "[DONE]")
                {
                    Debug.Log("TODO: Handle [Done]");
                    return null;
                }

                completion = JsonConvert.DeserializeObject<ChatCompletionChunk>(json);
            }
            else
            {
                completion = JsonConvert.DeserializeObject<ChatCompletionChunk>(blob);
            }

            if (null != completion && null != completion.Choices && completion.Choices.Count > 0 &&
                null != completion.Choices[0] && null != completion.Choices[0].Message)
            {
                currentResponse.response += completion.Choices[0].Message.content;
                resultResponse = currentResponse;
            }
            
            return resultResponse;
        }

        protected override Response OnHandleResponse(string blob, Response currentResponse)
        {
            Response resultResponse = null;
            currentResponse.rawResponse = blob;
            var json = blob;
            if (blob.StartsWith("data: "))
            {
                Debug.Log(blob);
                json = blob.Substring(6);
            }
            
            if (blob.Contains("\"choices\""))
            {
                try
                {
                    var completion = currentResponse.ParseResponse<ChatCompletionChunk>(json);

                    if (null != completion && null != completion.Choices && completion.Choices.Count > 0 && null != completion.Choices[0] && null != completion.Choices[0].Message)
                    {
                        currentResponse.response = completion.Choices[0].Message.content;
                        resultResponse = currentResponse;
                    }
                }
                catch (JsonReaderException e)
                {
                    Debug.LogError($"Error parsing response. {e.Message}\n\n{blob}");
                }
            }
            else
            {
                try
                {
                    Completion completion = JsonConvert.DeserializeObject<Completion>(json);

                    if (null != completion)
                    {
                        currentResponse.response += completion.Message.content;
                        resultResponse = currentResponse;
                    }
                }
                catch (JsonReaderException)
                {
                    // Ignore incomplete JSON blobs
                }
            }
            
            return resultResponse;
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(OpenAIStreamingAPI))]
    public class OpenAIStreamingAPIEditor : UnityEditor.Editor
    {
        private string _prompt;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!Application.isPlaying) return;
            GUILayout.Space(16);
            // Create a text area prompt field
            _prompt = EditorGUILayout.TextArea(_prompt);
            var streamingAPI = target as OpenAIStreamingAPI;
            if (GUILayout.Button("Submit Prompt"))
            {
                streamingAPI.Prompt(_prompt);
            }
            
            GUILayout.Space(16);
            EditorGUILayout.LabelField("Conversation History", EditorStyles.boldLabel);
            // Display text areas for all of the messages in the conversation history
            foreach (var message in streamingAPI.MessageHistory)
            {
                GUILayout.Label(message.role.ToString());
                EditorGUILayout.TextArea(message.content);
            }
        }
    }
    #endif
}