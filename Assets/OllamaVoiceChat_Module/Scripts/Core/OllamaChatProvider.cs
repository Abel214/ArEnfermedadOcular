using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace OllamaIntegration
{
    /// <summary>
    /// Adaptador del proveedor local Ollama. Reutiliza OllamaClient.
    /// </summary>
    public class OllamaChatProvider : IChatProvider
    {
        public string ProviderId => "ollama";
        public string DisplayName => "Ollama · Local";

        private readonly string baseUrl;
        private readonly string model;
        private readonly int timeoutSeconds;
        private readonly int numCtx;
        private readonly int numPredict;
        private readonly int numThread;
        private readonly float temperature;
        private readonly bool forceCpu;

        public OllamaChatProvider(
            string baseUrl,
            string model,
            int timeoutSeconds,
            int numCtx,
            int numPredict,
            int numThread,
            float temperature,
            bool forceCpu)
        {
            this.baseUrl = baseUrl;
            this.model = model;
            this.timeoutSeconds = timeoutSeconds;
            this.numCtx = numCtx;
            this.numPredict = numPredict;
            this.numThread = numThread;
            this.temperature = temperature;
            this.forceCpu = forceCpu;
        }

        public async Task<string> SendChatAsync(IReadOnlyList<OllamaMessage> messages, CancellationToken ct)
        {
            var options = new OllamaChatOptions
            {
                NumCtx = numCtx,
                Temperature = temperature,
                NumPredict = numPredict,
                NumThread = numThread
            };

            if (forceCpu)
            {
                options.NumGpu = 0;
            }

            var request = new OllamaChatRequest
            {
                Model = model,
                Messages = new List<OllamaMessage>(messages),
                Stream = false,
                Options = options
            };

            var response = await OllamaClient.SendChatAsync(baseUrl, request, timeoutSeconds, ct);
            return response?.Message?.Content ?? string.Empty;
        }

        public async Task<string> TestConnectionAsync(CancellationToken ct)
        {
            var tags = await OllamaClient.GetTagsAsync(baseUrl, 10, ct);
            if (tags?.Models == null || tags.Models.Count == 0)
            {
                return "Sin modelos instalados";
            }

            return string.Join(", ", tags.Models.ConvertAll(m => m.Name));
        }
    }
}
