using System.Collections.Generic;
using DoubTech.ThirdParty.OpenAI.Data;
using Newtonsoft.Json;

namespace DoubTech.ThirdParty.OpenAI.Scripts.Data
{
    public class CompletionRequest
    {
        public string model;
        public Message[] messages;
        public bool stream = true;
        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } = 512;
    }

    public class Completion
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }

        [JsonProperty("message")]
        public Message Message { get; set; }
    }

    public class Usage
    {
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }

    public class ChatCompletion
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("choices")]
        public List<Completion> Choices { get; set; }

        [JsonProperty("usage")]
        public Usage Usage { get; set; }
    }
    
    public class Delta
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public class Choice
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; } // Note: Changed type to string to handle null values

        [JsonProperty("message")]
        public Message Message { get; set; }

        [JsonProperty("delta")]
        public Delta Delta { get; set; }
    }

    public class ChatCompletionChunk
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("choices")]
        public List<Choice> Choices { get; set; }
    }
}