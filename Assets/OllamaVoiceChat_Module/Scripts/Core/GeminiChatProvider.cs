using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.AI;
using UnityEngine;

namespace OllamaIntegration
{
    /// <summary>
    /// Proveedor Gemini vía Firebase AI Logic (sin API key en el cliente).
    /// </summary>
    public class GeminiChatProvider : IChatProvider
    {
        const string ModelName = "gemini-3.5-flash-lite";
        const int BootstrapTimeoutMs = 15000;

        public string ProviderId => "gemini";
        public string DisplayName => "Gemini (nube)";

        GenerativeModel cachedModel;
        string cachedSystem;

        public async Task<string> SendChatAsync(IReadOnlyList<OllamaMessage> messages, CancellationToken ct)
        {
            try
            {
                await WaitForFirebase(ct);

                string system = null;
                var contents = new List<ModelContent>();

                foreach (var m in messages)
                {
                    if (string.IsNullOrWhiteSpace(m.Content)) continue;

                    switch (m.Role)
                    {
                        case "system":
                            system = m.Content;
                            break;
                        case "assistant":
                            contents.Add(ModelContent.Text(m.Content) /* rol model, ver nota */);
                            break;
                        default: // "user"
                            contents.Add(ModelContent.Text(m.Content));
                            break;
                    }
                }

                if (contents.Count == 0)
                    throw new InvalidOperationException("No hay mensaje para enviar.");

                var model = GetModel(system);
                var response = await model.GenerateContentAsync(contents).WithCancellation(ct);
                return response.Text ?? string.Empty;
            }
            catch (OperationCanceledException)
            {
                throw new ChatRequestCancelledException("Petición cancelada.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Gemini (Firebase AI Logic): {e}");
                throw new Exception(MapError(e), e);
            }
        }

        public async Task<string> TestConnectionAsync(CancellationToken ct)
        {
            try
            {
                await WaitForFirebase(ct);
                var model = GetModel(null);
                var r = await model.GenerateContentAsync("Responde solo: OK").WithCancellation(ct);
                return $"Gemini OK: {r.Text}";
            }
            catch (OperationCanceledException)
            {
                throw new ChatRequestCancelledException("Prueba cancelada.");
            }
            catch (Exception e)
            {
                throw new Exception(MapError(e), e);
            }
        }

        GenerativeModel GetModel(string system)
        {
            if (cachedModel != null && cachedSystem == system) return cachedModel;

            cachedModel = string.IsNullOrEmpty(system)
                ? FirebaseBootstrap.AI.GetGenerativeModel(ModelName)
                : FirebaseBootstrap.AI.GetGenerativeModel(
                    modelName: ModelName,
                    systemInstruction: ModelContent.Text(system));
            cachedSystem = system;
            return cachedModel;
        }

        static async Task WaitForFirebase(CancellationToken ct)
        {
            int waited = 0;
            while (!FirebaseBootstrap.Ready)
            {
                ct.ThrowIfCancellationRequested();
                if (waited >= BootstrapTimeoutMs)
                    throw new Exception("Firebase no se inicializó. Revisa la consola de Unity.");
                await Task.Delay(100);
                waited += 100;
            }
        }

        static string MapError(Exception e)
        {
            string msg = e.Message ?? string.Empty;
            if (msg.Contains("403") || msg.Contains("App Check") || msg.Contains("PERMISSION_DENIED"))
                return "Acceso denegado (403). Falta registrar el token debug de App Check o AI Logic no está activo.";
            if (msg.Contains("429") || msg.Contains("quota") || msg.Contains("RESOURCE_EXHAUSTED"))
                return "Cuota de Gemini agotada (429). Espera un momento o cambia de modelo.";
            if (msg.Contains("404") || msg.Contains("not found") || msg.Contains("NOT_FOUND"))
                return "Modelo no encontrado (404). Revisa el nombre del modelo.";
            return "Error al consultar Gemini: " + msg;
        }
    }

    static class TaskCancellationExtensions
    {
        /// <summary>Permite cancelar la espera aunque el SDK no acepte CancellationToken.</summary>
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (ct.Register(() => tcs.TrySetResult(true)))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                    throw new OperationCanceledException(ct);
            }
            return await task;
        }
    }
}