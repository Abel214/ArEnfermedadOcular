using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace OllamaIntegration
{
    public static class OllamaClient
    {
        /// <summary>
        /// Sends a chat request to Ollama (/api/chat) asynchronously.
        /// </summary>
        public static async Task<OllamaChatResponse> SendChatAsync(
            string baseUrl,
            OllamaChatRequest chatRequest,
            int timeoutSeconds = 600,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));
            }

            baseUrl = baseUrl.TrimEnd('/');
            string endpoint = $"{baseUrl}/api/chat";
            string jsonBody = JsonConvert.SerializeObject(chatRequest);

            using (var request = new UnityWebRequest(endpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = timeoutSeconds;

                try
                {
                    await SendWebRequestAsync(request, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw new Exception("La petición a Ollama fue cancelada.");
                }

                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    throw new Exception($"Error de conexión con Ollama en '{baseUrl}'. Verifica que Ollama esté en ejecución ('ollama serve'). Detalle: {request.error}");
                }

                if (request.result == UnityWebRequest.Result.ProtocolError)
                {
                    string serverError = request.downloadHandler?.text;
                    throw new Exception($"Error del servidor Ollama (HTTP {request.responseCode}): {serverError ?? request.error}");
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Error en la petición a Ollama: {request.error}");
                }

                string responseText = request.downloadHandler.text;
                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("Ollama devolvió una respuesta vacía.");
                }

                try
                {
                    var chatResponse = JsonConvert.DeserializeObject<OllamaChatResponse>(responseText);
                    return chatResponse;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error al deserializar la respuesta de Ollama: {ex.Message}\nRespuesta recibida: {responseText}", ex);
                }
            }
        }

        /// <summary>
        /// Checks if Ollama service is reachable and lists installed models (/api/tags).
        /// </summary>
        public static async Task<OllamaTagsResponse> GetTagsAsync(
            string baseUrl,
            int timeoutSeconds = 10,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));
            }

            baseUrl = baseUrl.TrimEnd('/');
            string endpoint = $"{baseUrl}/api/tags";

            using (var request = UnityWebRequest.Get(endpoint))
            {
                request.timeout = timeoutSeconds;

                await SendWebRequestAsync(request, cancellationToken);

                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    throw new Exception($"No se pudo conectar a Ollama en '{baseUrl}'.");
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Error al consultar tags de Ollama: {request.error}");
                }

                string responseText = request.downloadHandler.text;
                return JsonConvert.DeserializeObject<OllamaTagsResponse>(responseText);
            }
        }

        /// <summary>
        /// Helper to await UnityWebRequest without blocking the main Unity thread.
        /// </summary>
        public static Task<UnityWebRequest> SendWebRequestAsync(
            UnityWebRequest request,
            CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<UnityWebRequest>();
            var operation = request.SendWebRequest();

            if (operation.isDone)
            {
                tcs.SetResult(request);
                return tcs.Task;
            }

            operation.completed += _ =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }
                else
                {
                    tcs.TrySetResult(request);
                }
            };

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    if (!operation.isDone)
                    {
                        request.Abort();
                        tcs.TrySetCanceled(cancellationToken);
                    }
                });
            }

            return tcs.Task;
        }
    }
}
