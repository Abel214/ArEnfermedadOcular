using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OllamaIntegration
{
    [Serializable]
    public class OllamaMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        public OllamaMessage() { }

        public OllamaMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    [Serializable]
    public class OllamaChatOptions
    {
        [JsonProperty("num_ctx", NullValueHandling = NullValueHandling.Ignore)]
        public int? NumCtx { get; set; }

        [JsonProperty("num_gpu", NullValueHandling = NullValueHandling.Ignore)]
        public int? NumGpu { get; set; }

        [JsonProperty("num_thread", NullValueHandling = NullValueHandling.Ignore)]
        public int? NumThread { get; set; }

        [JsonProperty("num_predict", NullValueHandling = NullValueHandling.Ignore)]
        public int? NumPredict { get; set; }

        [JsonProperty("temperature", NullValueHandling = NullValueHandling.Ignore)]
        public float? Temperature { get; set; }

        [JsonProperty("top_p", NullValueHandling = NullValueHandling.Ignore)]
        public float? TopP { get; set; }
    }

    [Serializable]
    public class OllamaChatRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("messages")]
        public List<OllamaMessage> Messages { get; set; } = new List<OllamaMessage>();

        [JsonProperty("stream")]
        public bool Stream { get; set; } = false;

        [JsonProperty("options", NullValueHandling = NullValueHandling.Ignore)]
        public OllamaChatOptions Options { get; set; }
    }

    [Serializable]
    public class OllamaChatResponse
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("message")]
        public OllamaMessage Message { get; set; }

        [JsonProperty("done")]
        public bool Done { get; set; }

        [JsonProperty("done_reason")]
        public string DoneReason { get; set; }

        [JsonProperty("total_duration")]
        public long? TotalDuration { get; set; }

        [JsonProperty("load_duration")]
        public long? LoadDuration { get; set; }

        [JsonProperty("prompt_eval_count")]
        public int? PromptEvalCount { get; set; }

        [JsonProperty("eval_count")]
        public int? EvalCount { get; set; }
    }

    [Serializable]
    public class OllamaTagsResponse
    {
        [JsonProperty("models")]
        public List<OllamaModelDetails> Models { get; set; }
    }

    [Serializable]
    public class OllamaModelDetails
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }
    }
}
