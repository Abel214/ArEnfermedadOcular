using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace OllamaIntegration
{
    /// <summary>
    /// Adaptador del proveedor en nube Gemini (API REST generateContent).
    /// La API key NUNCA se serializa ni se registra en logs. Se resuelve en
    /// tiempo de ejecución, en este orden: clave escrita por el usuario en el
    /// dispositivo (botón "Clave" del chat), o variable de entorno cuyo NOMBRE
    /// se configura en el Inspector (por defecto GEMINI_API_KEY).
    /// </summary>
    public class GeminiChatProvider : IChatProvider
    {
        public string ProviderId => "gemini";
        public string DisplayName => "Gemini · Nube";

        private readonly string model;
        private readonly string apiKeyEnvVar;
        private readonly int timeoutSeconds;
        private readonly int maxOutputTokens;
        private readonly float temperature;

        public GeminiChatProvider(
            string model,
            string apiKeyEnvVar,
            int timeoutSeconds,
            int maxOutputTokens,
            float temperature)
        {
            this.model = string.IsNullOrWhiteSpace(model) ? "gemini-flash-latest" : model.Trim();
            this.apiKeyEnvVar = string.IsNullOrWhiteSpace(apiKeyEnvVar) ? "GEMINI_API_KEY" : apiKeyEnvVar.Trim();
            this.timeoutSeconds = Mathf.Max(10, timeoutSeconds);
            this.maxOutputTokens = Mathf.Max(1, maxOutputTokens);
            this.temperature = temperature;
        }

        /// <summary>
        /// Lee la API key desde la variable de entorno. Devuelve null si no existe.
        /// No registra ni expone el valor.
        /// </summary>
        public static string ReadApiKey(string envVarName)
        {
            try
            {
                string value = Environment.GetEnvironmentVariable(envVarName);
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
            catch
            {
                return null;
            }
        }

        public string EnvVarName => apiKeyEnvVar;
        public string Model => model;

        /// <summary>
        /// Resuelve la API key: primero la escrita por el usuario en el
        /// dispositivo (botón "Clave" del chat), luego la variable de entorno.
        /// Devuelve null si no hay ninguna. No registra el valor.
        /// </summary>
        private string ResolveApiKey()
        {
            string stored = GeminiKeyStore.Load();
            if (!string.IsNullOrEmpty(stored)) return stored;
            return ReadApiKey(apiKeyEnvVar);
        }

        public async Task<string> SendChatAsync(IReadOnlyList<OllamaMessage> messages, CancellationToken ct)
        {
            string apiKey = ResolveApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    throw new Exception(
                        "Falta tu clave de Gemini. Toca el botón 'Clave' en la cabecera del chat y escríbela.");
                }

                throw new Exception(
                    $"Falta la API key de Gemini. Escribe tu clave con el botón 'Clave' del chat o define la variable de entorno '{apiKeyEnvVar}'.");
            }

            string systemText = null;
            var contents = new List<GeminiContent>();
            foreach (var msg in messages)
            {
                if (msg == null || string.IsNullOrWhiteSpace(msg.Content)) continue;

                if (msg.Role == "system" && systemText == null)
                {
                    systemText = msg.Content;
                    continue;
                }

                string role = msg.Role == "assistant" ? "model" : "user";
                contents.Add(new GeminiContent
                {
                    Role = role,
                    Parts = new List<GeminiPart> { new GeminiPart { Text = msg.Content } }
                });
            }

            if (contents.Count == 0)
            {
                throw new Exception("Gemini no recibió ningún mensaje para procesar.");
            }

            var body = BuildRequestBody(systemText, contents);
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
            string jsonBody = JsonConvert.SerializeObject(body);

            // Reintento automático ante saturación (5xx) o cuota (429): picos suelen ser temporales.
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                string outcome = await TryDirectAttemptAsync(url, jsonBody, apiKey, ct);
                if (outcome != null) return outcome;

                if (attempt < maxAttempts)
                {
                    try { await Task.Delay(3000, ct); }
                    catch (OperationCanceledException)
                    {
                        throw new ChatRequestCancelledException("La petición a Gemini fue cancelada.");
                    }
                }
            }

            throw new Exception(
                "Gemini sigue saturado después de 3 intentos. Espera unos minutos o prueba con Ollama · Local.");
        }

        /// <summary>
        /// Un intento de petición directa. Devuelve el texto si tiene éxito,
        /// null si es un error transitorio (5xx/429) para reintentar,
        /// y lanza excepción en errores definitivos.
        /// </summary>
        private async Task<string> TryDirectAttemptAsync(string url, string jsonBody, string apiKey, CancellationToken ct)
        {
            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] raw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(raw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-goog-api-key", apiKey);
                request.timeout = timeoutSeconds;

                try
                {
                    await OllamaClient.SendWebRequestAsync(request, ct);
                }
                catch (OperationCanceledException)
                {
                    throw new ChatRequestCancelledException("La petición a Gemini fue cancelada.");
                }

                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    throw new Exception(
                        "Error de conexión con Gemini. Verifica tu conexión a internet.");
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string detail = request.downloadHandler?.text ?? request.error;
                    if (request.responseCode == 401 || request.responseCode == 403)
                    {
                        throw new Exception(
                            $"Gemini rechazó la credencial (HTTP {request.responseCode}). Revisa la variable de entorno '{apiKeyEnvVar}'.");
                    }
                    // 429 y 5xx: transitorios, el llamador reintenta
                    if (request.responseCode == 429 || (request.responseCode >= 500 && request.responseCode < 600))
                    {
                        return null;
                    }
                    throw new Exception($"Error de Gemini (HTTP {request.responseCode}): {detail}");
                }

                string responseText = request.downloadHandler.text;
                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("Gemini devolvió una respuesta vacía.");
                }

                GeminiGenerateResponse parsed;
                try
                {
                    parsed = JsonConvert.DeserializeObject<GeminiGenerateResponse>(responseText);
                }
                catch (Exception ex)
                {
                    throw new Exception($"No se pudo interpretar la respuesta de Gemini: {ex.Message}");
                }

                string text = ExtractText(parsed);
                if (!string.IsNullOrEmpty(text)) return text;

                string blocked = parsed?.PromptFeedback?.BlockReason;
                if (!string.IsNullOrEmpty(blocked))
                {
                    throw new Exception($"Gemini bloqueó la respuesta por seguridad ({blocked}). Reformula la pregunta.");
                }

                throw new Exception("Gemini devolvió una respuesta sin texto.");
            }
        }

        public async Task<string> TestConnectionAsync(CancellationToken ct)
        {
            string apiKey = ResolveApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    throw new Exception(
                        "Falta tu clave de Gemini. Toca el botón 'Clave' en la cabecera del chat y escríbela.");
                }

                throw new Exception($"Falta la variable de entorno '{apiKeyEnvVar}' o una clave escrita en el dispositivo.");
            }

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}";
            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("x-goog-api-key", apiKey);
                request.timeout = 15;

                await OllamaClient.SendWebRequestAsync(request, ct);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Gemini no responde (HTTP {request.responseCode}). Revisa modelo y credencial.");
                }

                try
                {
                    var info = JsonConvert.DeserializeObject<GeminiModelInfo>(request.downloadHandler.text);
                    return string.IsNullOrWhiteSpace(info?.DisplayName) ? model : info.DisplayName;
                }
                catch
                {
                    return model;
                }
            }
        }

        private GeminiGenerateRequest BuildRequestBody(string systemText, List<GeminiContent> contents)
        {
            var body = new GeminiGenerateRequest
            {
                Contents = contents,
                GenerationConfig = new GeminiGenerationConfig
                {
                    MaxOutputTokens = maxOutputTokens,
                    Temperature = temperature
                }
            };

            if (!string.IsNullOrWhiteSpace(systemText))
            {
                body.SystemInstruction = new GeminiContent
                {
                    Parts = new List<GeminiPart> { new GeminiPart { Text = systemText } }
                };
            }

            return body;
        }

        private static string ExtractText(GeminiGenerateResponse response)
        {
            if (response?.Candidates == null || response.Candidates.Count == 0) return null;

            var sb = new StringBuilder();
            var parts = response.Candidates[0]?.Content?.Parts;
            if (parts == null) return null;

            foreach (var part in parts)
            {
                if (!string.IsNullOrEmpty(part?.Text)) sb.Append(part.Text);
            }

            string result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }
    }

    [Serializable]
    public class GeminiGenerateRequest
    {
        [JsonProperty("contents")]
        public List<GeminiContent> Contents = new List<GeminiContent>();

        [JsonProperty("system_instruction", NullValueHandling = NullValueHandling.Ignore)]
        public GeminiContent SystemInstruction;

        [JsonProperty("generationConfig")]
        public GeminiGenerationConfig GenerationConfig;
    }

    [Serializable]
    public class GeminiContent
    {
        [JsonProperty("role", NullValueHandling = NullValueHandling.Ignore)]
        public string Role;

        [JsonProperty("parts")]
        public List<GeminiPart> Parts = new List<GeminiPart>();
    }

    [Serializable]
    public class GeminiPart
    {
        [JsonProperty("text")]
        public string Text;
    }

    [Serializable]
    public class GeminiGenerationConfig
    {
        [JsonProperty("maxOutputTokens")]
        public int MaxOutputTokens;

        [JsonProperty("temperature")]
        public float Temperature;
    }

    [Serializable]
    public class GeminiGenerateResponse
    {
        [JsonProperty("candidates")]
        public List<GeminiCandidate> Candidates;

        [JsonProperty("promptFeedback")]
        public GeminiPromptFeedback PromptFeedback;
    }

    [Serializable]
    public class GeminiCandidate
    {
        [JsonProperty("content")]
        public GeminiContent Content;

        [JsonProperty("finishReason")]
        public string FinishReason;
    }

    [Serializable]
    public class GeminiPromptFeedback
    {
        [JsonProperty("blockReason")]
        public string BlockReason;
    }

    [Serializable]
    public class GeminiModelInfo
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("displayName")]
        public string DisplayName;
    }
}
