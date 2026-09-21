using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace OllamaIntegration
{
    public class OllamaChatManager : MonoBehaviour
    {
        [Header("Configuración del Servidor Ollama")]
        [Tooltip("URL base de Ollama (por defecto http://127.0.0.1:11434)")]
        [SerializeField] private string baseUrl = "http://127.0.0.1:11434";

        [Tooltip("Nombre del modelo a usar en Ollama")]
        [SerializeField] private string modelName = "gemma3:4b";

        [Tooltip("Tiempo de espera máximo en segundos para la respuesta (10 minutos)")]
        [SerializeField] private int timeoutSeconds = 600;

        [Header("Instrucciones y Límites")]
        [Tooltip("Instrucción inicial para guiar el comportamiento y formato de Gemma")]
        [TextArea(3, 5)]
        [SerializeField] private string systemPrompt = "Eres un asistente médico experto en salud visual y anatomía ocular para una app educativa de Realidad Aumentada. Responde de forma concisa, clara, directa y estructurada (máximo 2 a 3 párrafos breves o viñetas), en español y sin usar emojis.";

        [Tooltip("Límite máximo de tokens a generar por respuesta (evita que el modelo se cuelgue generando textos infinitos)")]
        [SerializeField] private int maxPredictTokens = 350;

        [Tooltip("Número máximo de turnos previos a incluir en la petición para no sobrecargar el procesamiento")]
        [SerializeField] private int maxHistoryTurns = 6;

        [Header("Parámetros del Modelo")]
        [Tooltip("Si está activo, fuerza la ejecución en CPU para evitar errores de memoria o CUDA en la GPU")]
        [SerializeField] private bool forceCpu = true;

        [Tooltip("Elimina emojis de las respuestas del modelo")]
        [SerializeField] private bool stripEmojis = true;

        [Tooltip("Tamaño máximo de contexto (num_ctx)")]
        [SerializeField] private int contextLength = 2048;

        [Range(0f, 2f)]
        [Tooltip("Temperatura para controlar la creatividad")]
        [SerializeField] private float temperature = 0.7f;

        [Header("Prueba Rápida")]
        [SerializeField] private bool testOnStart = false;
        [SerializeField] private string testPrompt = "Hola Gemma, confirma si estás funcionando correctamente.";

        [Header("Eventos")]
        public UnityEvent<string> onMessageSent;
        public UnityEvent<string> onResponseReceived;
        public UnityEvent<string> onErrorReceived;
        public UnityEvent<bool> onBusyStateChanged;

        // C# Actions para suscripción por código
        public event Action<string> MessageSent;
        public event Action<string> ResponseReceived;
        public event Action<string> ErrorReceived;
        public event Action<bool> BusyStateChanged;

        private readonly List<OllamaMessage> conversationHistory = new List<OllamaMessage>();
        private bool isBusy = false;
        private CancellationTokenSource activeCts;

        public bool IsBusy => isBusy;
        public IReadOnlyList<OllamaMessage> History => conversationHistory.AsReadOnly();
        public string ModelName => modelName;

        private void Start()
        {
            if (testOnStart)
            {
                SendUserMessage(testPrompt);
            }
        }

        private void OnDestroy()
        {
            CancelCurrentRequest();
        }

        /// <summary>
        /// Envía un mensaje de usuario a Ollama manteniendo el historial de la conversación.
        /// </summary>
        public async void SendUserMessage(string userText)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                Debug.LogWarning("[OllamaChatManager] El mensaje no puede estar vacío.");
                return;
            }

            if (isBusy)
            {
                Debug.LogWarning("[OllamaChatManager] Ya hay una petición en curso. Espera a que termine.");
                return;
            }

            await SendUserMessageAsync(userText);
        }

        /// <summary>
        /// Envía el mensaje de forma asíncrona devolviendo el texto de respuesta.
        /// </summary>
        public async Task<string> SendUserMessageAsync(string userText)
        {
            if (string.IsNullOrWhiteSpace(userText)) return null;

            SetBusy(true);
            activeCts?.Dispose();
            activeCts = new CancellationTokenSource();

            // Preparar lista con system prompt, historial reciente y el nuevo mensaje del usuario
            var messagesToSend = new List<OllamaMessage>();

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messagesToSend.Add(new OllamaMessage("system", systemPrompt));
            }

            // Incluir historial limitado a los últimos turnos para mantener alta velocidad
            int historyCount = conversationHistory.Count;
            int maxHistoryMessages = maxHistoryTurns * 2;
            int startIndex = Mathf.Max(0, historyCount - maxHistoryMessages);
            for (int i = startIndex; i < historyCount; i++)
            {
                messagesToSend.Add(conversationHistory[i]);
            }

            var userMsg = new OllamaMessage("user", userText);
            messagesToSend.Add(userMsg);

            // Notificar que se envió el mensaje
            conversationHistory.Add(userMsg);
            MessageSent?.Invoke(userText);
            onMessageSent?.Invoke(userText);

            int threadCount = Mathf.Clamp(SystemInfo.processorCount - 1, 2, 8);

            var options = new OllamaChatOptions
            {
                NumCtx = contextLength,
                Temperature = temperature,
                NumPredict = maxPredictTokens,
                NumThread = threadCount
            };

            if (forceCpu)
            {
                options.NumGpu = 0;
            }

            var request = new OllamaChatRequest
            {
                Model = modelName,
                Messages = messagesToSend,
                Stream = false,
                Options = options
            };

            try
            {
                Debug.Log($"[OllamaChatManager] Enviando mensaje a Ollama ({modelName})...");
                var response = await OllamaClient.SendChatAsync(baseUrl, request, timeoutSeconds, activeCts.Token);

                string responseText = response?.Message?.Content ?? string.Empty;
                if (stripEmojis && !string.IsNullOrEmpty(responseText))
                {
                    responseText = StripEmojiText(responseText);
                }

                var assistantMsg = new OllamaMessage("assistant", responseText);
                conversationHistory.Add(assistantMsg);

                Debug.Log($"[OllamaChatManager] Respuesta recibida de {modelName}:\n{responseText}");
                ResponseReceived?.Invoke(responseText);
                onResponseReceived?.Invoke(responseText);

                return responseText;
            }
            catch (Exception ex)
            {
                string errorMsg = ex.Message;
                Debug.LogError($"[OllamaChatManager] Error al comunicarse con Ollama: {errorMsg}");

                // Si falló el envío, quitamos el último mensaje del usuario para no desfasar el historial
                if (conversationHistory.Count > 0 && conversationHistory[conversationHistory.Count - 1] == userMsg)
                {
                    conversationHistory.RemoveAt(conversationHistory.Count - 1);
                }

                ErrorReceived?.Invoke(errorMsg);
                onErrorReceived?.Invoke(errorMsg);
                return null;
            }
            finally
            {
                SetBusy(false);
            }
        }

        /// <summary>
        /// Borra el historial de la conversación actual.
        /// </summary>
        public void ClearHistory()
        {
            conversationHistory.Clear();
            Debug.Log("[OllamaChatManager] Historial de conversación reiniciado.");
        }


        /// <summary>
        /// Cancela la petición en curso si existe.
        /// </summary>
        public void CancelCurrentRequest()
        {
            if (activeCts != null && !activeCts.IsCancellationRequested)
            {
                activeCts.Cancel();
                activeCts.Dispose();
                activeCts = null;
            }
        }

        private void SetBusy(bool busy)
        {
            isBusy = busy;
            BusyStateChanged?.Invoke(isBusy);
            onBusyStateChanged?.Invoke(isBusy);
        }

        [ContextMenu("Probar Conexión con Ollama")]
        public async void TestConnection()
        {
            Debug.Log($"[OllamaChatManager] Verificando conexión con Ollama en {baseUrl}...");
            try
            {
                var tags = await OllamaClient.GetTagsAsync(baseUrl);
                string modelList = tags?.Models != null
                    ? string.Join(", ", tags.Models.ConvertAll(m => m.Name))
                    : "Ninguno";
                Debug.Log($"[OllamaChatManager] ¡Conexión exitosa! Modelos disponibles: {modelList}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OllamaChatManager] Error de conexión: {ex.Message}");
            }
        }

        [ContextMenu("Enviar Prompt de Prueba")]
        public void SendTestPrompt()
        {
            SendUserMessage(testPrompt);
        }

        /// <summary>
        /// Filtra y elimina emojis y caracteres pictográficos que puedan causar problemas de renderizado con TextMeshPro.
        /// </summary>
        private static string StripEmojiText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Rango de emojis Unicode: Emoticons, Miscellaneous Symbols, Dingbats, Supplemental Symbols, etc.
            string pattern = @"[\uD83C-\uDBFF\uDC00-\uDFFF]|[\u2600-\u27BF]|[\u2300-\u23FF]|[\u2B50-\u2B55]|[\u200D\uFE0F]";
            string cleaned = Regex.Replace(text, pattern, string.Empty);

            // Limpiar espacios dobles que hayan quedado tras borrar emojis
            cleaned = Regex.Replace(cleaned, @"[ ]{2,}", " ");
            return cleaned.Trim();
        }
    }
}
