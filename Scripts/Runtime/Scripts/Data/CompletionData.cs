using System.Collections.Generic;
using DoubTech.ThirdParty.OpenAI.Data;
using Unity.Plastic.Newtonsoft.Json;

namespace DoubTech.ThirdParty.OpenAI.Scripts.Data
{
    public class CompletionRequest
    {
        public string model;
        public Message[] messages;
        public bool stream = true;
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

}