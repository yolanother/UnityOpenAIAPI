using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DoubTech.ThirdParty.OpenAI.Data;
using DoubTech.ThirdParty.OpenAI.Scripts.Data;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine.Events;

namespace DoubTech.ThirdParty.OpenAI
{
    public class OpenAIStreamingAPI : MonoBehaviour
    {
        [Header("Prompt Config")] [SerializeField]
        private BasePrompt basePrompt;

        [SerializeField] private Message[] messages;

        [Header("Server Config")] [Models(nameof(serverConfig))] [SerializeField]
        private string model;

        [SerializeField] private OpenAIServerConfig serverConfig;

        [Header("Events")]
        public UnityEvent<string> onPartialResponseReceived = new UnityEvent<string>();
        public UnityEvent<string> onFullResponseReceived = new UnityEvent<string>();

        private string _currentResponse;

        private List<Message> _messageHistory = new List<Message>();
        private CompletionRequest _requestData;

        public Message[] MessageHistory
        {
            get
            {
                
                // Combine base prompt messages, messages, and a new message for prompt
                var allMessages = new List<Message>();
                if (basePrompt != null)
                {
                    allMessages.AddRange(basePrompt.messages);
                }

                if (messages != null)
                {
                    allMessages.AddRange(messages);
                }

                allMessages.AddRange(_messageHistory);
                return allMessages.ToArray();
            }
        }

        public void Prompt(string prompt)
        {
            _messageHistory.Add(new Message
            {
                role = "user",
                content = prompt
            });
            
            _requestData = new CompletionRequest
            {
                model = model,
                messages = MessageHistory
            };
            var postData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_requestData));

            var request = new UnityWebRequest(serverConfig.GetUrl("/v1/chat/completions"), "POST")
            {
                uploadHandler = new UploadHandlerRaw(postData),
                downloadHandler = new StreamingDownloadHandler(OnDataReceived),
                method = UnityWebRequest.kHttpVerbPOST
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + serverConfig.apiKey);

            StartCoroutine(SendRequest(request));
        }

        IEnumerator SendRequest(UnityWebRequest request)
        {
            _currentResponse = "";
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
            }
            
            while(!request.isDone)
            {
                yield return null;
            }
            _messageHistory.Add(new Message
            {
                role = "assistant",
                content = _currentResponse
            });
            onFullResponseReceived?.Invoke(_currentResponse);
        }

        private void OnDataReceived(byte[] data)
        {
            var text = Encoding.UTF8.GetString(data);
            // Assuming the API sends newline-delimited JSON blobs
            var jsonBlobs = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var blob in jsonBlobs)
            {
                if (!_requestData.stream) HandleFullData(blob);
                else HandleStreamedData(blob);
            }
        }

        private void HandleStreamedData(string blob)
        {
            try
            {
                ChatCompletionChunk completion;
                if (blob.StartsWith("data: "))
                {
                    var json = blob.Substring(6);
                    completion = JsonConvert.DeserializeObject<ChatCompletionChunk>(json);
                }
                else
                {
                    completion = JsonConvert.DeserializeObject<ChatCompletionChunk>(blob);
                }

                if (null != completion)
                {
                    _currentResponse += completion.Choices[0].Message.content;
                    onPartialResponseReceived?.Invoke(_currentResponse);
                    Debug.Log(_currentResponse);
                }
            }
            catch (JsonReaderException)
            {
                // Ignore incomplete JSON blobs
            }
        }

        private void HandleFullData(string blob)
        {
            try
            {
                Completion completion;
                if (blob.StartsWith("data: "))
                {
                    var json = blob.Substring(6);
                    completion = JsonConvert.DeserializeObject<Completion>(json);
                }
                else
                {
                    completion = JsonConvert.DeserializeObject<Completion>(blob);
                }

                if (null != completion)
                {
                    _currentResponse += completion.Message.content;
                    onPartialResponseReceived?.Invoke(_currentResponse);
                    Debug.Log(_currentResponse);
                }
            }
            catch (JsonReaderException)
            {
                // Ignore incomplete JSON blobs
            }
        }

        private class StreamingDownloadHandler : DownloadHandlerScript
        {
            private Action<byte[]> onDataReceived;

            public StreamingDownloadHandler(Action<byte[]> onDataReceivedCallback) : base(new byte[1024])
            {
                onDataReceived = onDataReceivedCallback;
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || dataLength == 0)
                {
                    return false;
                }

                var dataCopy = new byte[dataLength];
                Buffer.BlockCopy(data, 0, dataCopy, 0, dataLength);
                onDataReceived?.Invoke(dataCopy);

                return true;
            }
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
                GUILayout.Label(message.role);
                EditorGUILayout.TextArea(message.content);
            }
        }
    }
    #endif
}