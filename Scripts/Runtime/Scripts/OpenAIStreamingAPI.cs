using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DoubTech.ThirdParty.OpenAI.Data;
using DoubTech.ThirdParty.OpenAI.Scripts.Data;
using Newtonsoft.Json;
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

        [SerializeField] private bool stream;

        [SerializeField] private OpenAIServerConfig serverConfig;

        [Header("Events")]
        public UnityEvent<string> onPartialResponseReceived = new UnityEvent<string>();
        public UnityEvent<string> onFullResponseReceived = new UnityEvent<string>();

        private string _currentResponse;

        private List<Message> _messageHistory = new List<Message>();
        private CompletionRequest _requestData;
        private Message _partialPrompt;

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

        public void PartialPrompt(string prompt)
        {
            if (string.IsNullOrEmpty(prompt)) return;
            if (null == _partialPrompt)
            {
                _partialPrompt = new Message()
                {
                    role = "user",
                    content = prompt
                };
                _messageHistory.Add(_partialPrompt);
            }

            _partialPrompt.content = prompt;
            Submit();
        }

        public void Prompt(string prompt)
        {
            if (null != _partialPrompt && prompt == _partialPrompt.content)
            {
                _partialPrompt = null;
                return;
            }

            if (null == _partialPrompt)
            {
                _messageHistory.Add(new Message
                {
                    role = "user",
                    content = prompt
                });
            }
            else
            {
                _partialPrompt = null;
            }

            Submit();
        }

        private void Submit()
        {
            _requestData = new CompletionRequest
            {
                model = model,
                messages = MessageHistory,
                stream = stream
            };
            var postData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_requestData));

            var request = new UnityWebRequest(serverConfig.GetUrl("/v1/chat/completions"), "POST")
            {
                uploadHandler = new UploadHandlerRaw(postData),
                downloadHandler = new StreamingDownloadHandler(OnDataReceived, stream),
                method = UnityWebRequest.kHttpVerbPOST
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + serverConfig.apiKey);

            StopAllCoroutines();
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
            Debug.Log(_currentResponse);
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
                    Debug.Log(blob);
                    var json = blob.Substring(6);
                    completion = JsonConvert.DeserializeObject<ChatCompletionChunk>(json);
                }
                else
                {
                    completion = JsonConvert.DeserializeObject<ChatCompletionChunk>(blob);
                }

                if (null != completion && null != completion.Choices && completion.Choices.Count > 0 && null != completion.Choices[0] && null != completion.Choices[0].Message)
                {
                    _currentResponse += completion.Choices[0].Message.content;
                    onPartialResponseReceived?.Invoke(_currentResponse);
                }
            }
            catch (JsonReaderException)
            {
                // Ignore incomplete JSON blobs
            }
        }

        private void HandleFullData(string blob)
        {
            if (blob.Contains("\"choices\""))
            {
                ChatCompletionChunk completion;
                try
                {
                    if (blob.StartsWith("data: "))
                    {
                        Debug.Log(blob);
                        var json = blob.Substring(6);
                        completion = JsonConvert.DeserializeObject<ChatCompletionChunk>(json);
                    }
                    else
                    {
                        completion = JsonConvert.DeserializeObject<ChatCompletionChunk>(blob);
                    }

                    if (null != completion && null != completion.Choices && completion.Choices.Count > 0 && null != completion.Choices[0] && null != completion.Choices[0].Message)
                    {
                        _currentResponse = completion.Choices[0].Message.content;
                        onPartialResponseReceived?.Invoke(_currentResponse);
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
                        onFullResponseReceived?.Invoke(_currentResponse);
                    }
                }
                catch (JsonReaderException)
                {
                    // Ignore incomplete JSON blobs
                }
            }
        }

        private class StreamingDownloadHandler : DownloadHandlerScript
        {
            private Action<byte[]> onDataReceived;
            private MemoryStream _buffer;

            public StreamingDownloadHandler(Action<byte[]> onDataReceivedCallback, bool chunk) : base(new byte[1024])
            {
                onDataReceived = onDataReceivedCallback;
                if (!chunk)
                {
                    _buffer = new MemoryStream();
                }
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || dataLength == 0)
                {
                    return false;
                }

                if (null != _buffer)
                {
                    _buffer.Write(data, 0, dataLength);
                }
                else
                {
                    // TODO: Handle chunked data that is > one receive.
                    var dataCopy = new byte[dataLength];
                    Buffer.BlockCopy(data, 0, dataCopy, 0, dataLength);
                    onDataReceived?.Invoke(dataCopy);
                }

                return true;
            }

            protected override void CompleteContent()
            {
                base.CompleteContent();
                if (null != _buffer)
                {
                    onDataReceived?.Invoke(_buffer.GetBuffer());
                }
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