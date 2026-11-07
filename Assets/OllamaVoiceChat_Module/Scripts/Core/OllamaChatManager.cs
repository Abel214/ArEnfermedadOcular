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
        [Header("Proveedor de Chat")]
        [Tooltip("Proveedor activo. El historial se conserva al cambiar entre Ollama y Gemini.")]
        [SerializeField] private ChatProviderType provider = ChatProviderType.Ollama;

        [Tooltip("Modelo de Gemini a usar cuando el proveedor es Gemini (ej. gemini-flash-latest)")]
        [SerializeField] private string geminiModel = "gemini-flash-latest";

        [Tooltip("Nombre de la variable de entorno que contiene la API key de Gemini. La clave nunca se guarda en el proyecto.")]
        [SerializeField] private string geminiApiKeyEnvVar = "GEMINI_API_KEY";

        [Tooltip("Máximo de tokens de salida por respuesta de Gemini")]
        [SerializeField] private int geminiMaxOutputTokens = 1024;

        [Header("Configuración del Servidor Ollama")]
        [Tooltip("URL base de Ollama (por defecto http://127.0.0.1:11434)")]
        [SerializeField] private string baseUrl = "http://127.0.0.1:11434";

        [Tooltip("Nombre del modelo a usar en Ollama")]
        [SerializeField] private string modelName = "gemma3:4b";

        [Tooltip("Tiempo de espera máximo en segundos para la respuesta (10 minutos)")]
        [SerializeField] private int timeoutSeconds = 600;

        [Header("Instrucciones y Límites")]
        [Tooltip("Instrucción inicial para guiar el comportamiento y formato del asistente")]
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
        [SerializeField] private string testPrompt = "Hola, confirma si estás funcionando correctamente.";

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
        public event Action<ChatProviderType> ProviderChanged;
        public event Action<string> ActiveModelChanged;

        private ARInteractionManager interactionManager;

        private readonly List<OllamaMessage> conversationHistory = new List<OllamaMessage>();
        private bool isBusy = false;
        private CancellationTokenSource activeCts;

        public bool IsBusy => isBusy;
        public IReadOnlyList<OllamaMessage> History => conversationHistory.AsReadOnly();
        public string ModelName => modelName;
        public ChatProviderType Provider => provider;
        public string ProviderDisplayName => provider == ChatProviderType.Gemini ? "Gemini" : "Ollama Gemma 4";
        public string ModelLabel => provider == ChatProviderType.Gemini ? geminiModel : modelName;

        /// <summary>
        /// Cambia el proveedor activo. Solo permitido sin petición en curso.
        /// El historial de la conversación se conserva.
        /// </summary>
        public bool SetProvider(ChatProviderType newProvider)
        {
            if (isBusy)
            {
                Debug.LogWarning("[OllamaChatManager] No se puede cambiar de proveedor durante una petición en curso.");
                return false;
            }

            if (provider == newProvider) return true;

            provider = newProvider;
            Debug.Log($"[OllamaChatManager] Proveedor activo: {ProviderDisplayName} ({ModelLabel}). Historial conservado ({conversationHistory.Count} mensajes).");
            ProviderChanged?.Invoke(provider);
            return true;
        }

        private void Start()
        {
            interactionManager = UnityEngine.Object.FindAnyObjectByType<ARInteractionManager>(FindObjectsInactive.Include);
            if (interactionManager != null)
            {
                interactionManager.ActiveItemChanged -= HandleActiveItemChanged;
                interactionManager.ActiveItemChanged += HandleActiveItemChanged;
            }

            if (testOnStart)
            {
                SendUserMessage(testPrompt);
            }
        }

        private void HandleActiveItemChanged(Item item)
        {
            ActiveModelChanged?.Invoke(item != null ? item.itemName : null);
        }

        /// <summary>
        /// Nombre del modelo ocular visible en AR (Glaucoma, Catarata…), o null si no hay ninguno.
        /// </summary>
        public string ActiveModelName
        {
            get
            {
                if (interactionManager == null)
                {
                    interactionManager = UnityEngine.Object.FindAnyObjectByType<ARInteractionManager>(FindObjectsInactive.Include);
                }
                return interactionManager != null && interactionManager.ActiveItem != null
                    ? interactionManager.ActiveItem.itemName
                    : null;
            }
        }

        private void OnDestroy()
        {
            CancelCurrentRequest();
            if (interactionManager != null)
            {
                interactionManager.ActiveItemChanged -= HandleActiveItemChanged;
            }
        }

        /// <summary>
        /// Envía un mensaje de usuario manteniendo el historial de la conversación.
        /// </summary>
        public void SendUserMessage(string userText)
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

            _ = SendUserMessageAsync(userText);
        }

        /// <summary>
        /// Envía el mensaje de forma asíncrona devolviendo el texto de respuesta.
        /// </summary>
        public async Task<string> SendUserMessageAsync(string userText)
        {
            if (string.IsNullOrWhiteSpace(userText)) return null;

            SetBusy(true);
            activeCts?.Dispose();
            var requestCts = new CancellationTokenSource();
            activeCts = requestCts;

            // Preparar lista con system prompt (contextual al modelo visible), historial y mensaje nuevo
            var messagesToSend = new List<OllamaMessage>();
            messagesToSend.Add(new OllamaMessage("system", BuildSystemPrompt()));

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

            IChatProvider activeProvider = BuildActiveProvider();

            try
            {
                Debug.Log($"[OllamaChatManager] Enviando mensaje a {ProviderDisplayName} ({ModelLabel})...");
                string responseText = await activeProvider.SendChatAsync(messagesToSend, requestCts.Token) ?? string.Empty;

                if (stripEmojis && !string.IsNullOrEmpty(responseText))
                {
                    responseText = StripEmojiText(responseText);
                }

                var assistantMsg = new OllamaMessage("assistant", responseText);
                conversationHistory.Add(assistantMsg);

                Debug.Log($"[OllamaChatManager] Respuesta recibida de {ModelLabel}:\n{responseText}");
                ResponseReceived?.Invoke(responseText);
                onResponseReceived?.Invoke(responseText);

                return responseText;
            }
            catch (Exception ex)
            {
                // Cancelaciones esperadas (petición reemplazada, app cerrada,
                // tiempo agotado) no se muestran como error al usuario.
                bool cancelled = ex is ChatRequestCancelledException
                    || ex is OperationCanceledException
                    || requestCts.IsCancellationRequested
                    || ex.Message.IndexOf("cancelada", StringComparison.OrdinalIgnoreCase) >= 0;

                // Si falló el envío, quitamos el último mensaje del usuario para no desfasar el historial
                if (conversationHistory.Count > 0 && conversationHistory[conversationHistory.Count - 1] == userMsg)
                {
                    conversationHistory.RemoveAt(conversationHistory.Count - 1);
                }

                if (cancelled)
                {
                    Debug.Log("[OllamaChatManager] Petición cancelada (sin mostrar mensaje al usuario).");
                    return null;
                }

                string errorMsg = ex.Message;
                Debug.LogError($"[OllamaChatManager] Error al comunicarse con {ProviderDisplayName}: {errorMsg}");

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
        /// System prompt base + contexto del modelo ocular visible en AR.
        /// Si el usuario pregunta qué está viendo, el asistente responde con ese nombre.
        /// </summary>
        private string BuildSystemPrompt()
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                sb.Append(systemPrompt);
            }

            Item activeItem = interactionManager != null ? interactionManager.ActiveItem : null;
            if (activeItem != null)
            {
                sb.Append($"\n\nContexto de la escena AR: el usuario está viendo el modelo '{activeItem.itemName}'.");
                if (!string.IsNullOrWhiteSpace(activeItem.medicalContext))
                {
                    sb.Append($"\nInformación médica del modelo visible: {activeItem.medicalContext}");
                }
                sb.Append("\nSi te preguntan qué modelo están viendo, responde con ese nombre.");
            }
            else
            {
                sb.Append("\n\nEl usuario aún no ha colocado ningún modelo en la escena. Si te preguntan qué están viendo, diles que elijan un modelo primero.");
            }

            return sb.ToString();
        }

        private IChatProvider BuildActiveProvider()
        {
            if (provider == ChatProviderType.Gemini)
            {
                return new GeminiChatProvider(
                    geminiModel,
                    geminiApiKeyEnvVar,
                    timeoutSeconds,
                    geminiMaxOutputTokens,
                    temperature);
            }

            int threadCount = Mathf.Clamp(SystemInfo.processorCount - 1, 2, 8);
            return new OllamaChatProvider(
                baseUrl,
                modelName,
                timeoutSeconds,
                contextLength,
                maxPredictTokens,
                threadCount,
                temperature,
                forceCpu);
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

        [ContextMenu("Probar Conexión con el Proveedor Activo")]
        public async void TestConnection()
        {
            Debug.Log($"[OllamaChatManager] Verificando conexión con {ProviderDisplayName}...");
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    string info = await BuildActiveProvider().TestConnectionAsync(cts.Token);
                    Debug.Log($"[OllamaChatManager] ¡Conexión exitosa! {ProviderDisplayName}: {info}");
                }
            }
            catch (ChatRequestCancelledException)
            {
                Debug.Log("[OllamaChatManager] Prueba de conexión cancelada.");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[OllamaChatManager] Prueba de conexión cancelada por tiempo de espera.");
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
