using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OllamaIntegration;

namespace OllamaUI
{
    public class ChatUIController : MonoBehaviour
    {
        [Header("Referencias Principales")]
        [SerializeField] private OllamaChatManager chatManager;
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button clearButton;

        [Header("Entrada y Envío")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;

        [Header("Visualización del Chat")]
        [SerializeField] private TMP_Text chatLogText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Estilos de Mensajes")]
        [SerializeField] private string userPrefix = "<color=#4CAF50><b>Tú:</b></color>";
        [SerializeField] private string assistantPrefix = "<color=#2196F3><b>Gemma:</b></color>";
        [SerializeField] private string errorPrefix = "<color=#F44336><b>Error:</b></color>";
        [SerializeField] private string systemPrefix = "<color=#FF9800><b>Sistema:</b></color>";

        private readonly StringBuilder chatHistoryBuffer = new StringBuilder();

        private void Awake()
        {
            if (chatManager == null)
            {
                chatManager = FindFirstObjectByType<OllamaChatManager>();
            }
        }

        private void OnEnable()
        {
            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendButtonClicked);

            if (clearButton != null)
                clearButton.onClick.AddListener(OnClearButtonClicked);

            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleChatPanel);

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseChatPanel);

            if (inputField != null)
                inputField.onSubmit.AddListener(OnInputSubmitted);

            SubscribeChatManager();
        }

        private void SubscribeChatManager()
        {
            if (chatManager == null)
            {
                chatManager = FindFirstObjectByType<OllamaChatManager>();
            }

            if (chatManager != null)
            {
                chatManager.MessageSent -= HandleMessageSent;
                chatManager.ResponseReceived -= HandleResponseReceived;
                chatManager.ErrorReceived -= HandleErrorReceived;
                chatManager.BusyStateChanged -= HandleBusyStateChanged;

                chatManager.MessageSent += HandleMessageSent;
                chatManager.ResponseReceived += HandleResponseReceived;
                chatManager.ErrorReceived += HandleErrorReceived;
                chatManager.BusyStateChanged += HandleBusyStateChanged;
            }
        }

        private void OnDisable()
        {
            if (sendButton != null)
                sendButton.onClick.RemoveListener(OnSendButtonClicked);

            if (clearButton != null)
                clearButton.onClick.RemoveListener(OnClearButtonClicked);

            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(ToggleChatPanel);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(CloseChatPanel);

            if (inputField != null)
                inputField.onSubmit.RemoveListener(OnInputSubmitted);

            if (chatManager != null)
            {
                chatManager.MessageSent -= HandleMessageSent;
                chatManager.ResponseReceived -= HandleResponseReceived;
                chatManager.ErrorReceived -= HandleErrorReceived;
                chatManager.BusyStateChanged -= HandleBusyStateChanged;
            }
        }

        private void Start()
        {
            UpdateStatus("Listo para conversar");
            AppendMessage(systemPrefix, "Conectado a Ollama (" + (chatManager != null ? chatManager.ModelName : "Gemma 3:4b") + "). ¡Haz tu pregunta!");
        }

        public void ToggleChatPanel()
        {
            if (chatPanel != null)
            {
                bool newState = !chatPanel.activeSelf;
                chatPanel.SetActive(newState);
                if (newState && inputField != null)
                {
                    inputField.ActivateInputField();
                }
            }
        }

        public void OpenChatPanel()
        {
            if (chatPanel != null)
            {
                chatPanel.SetActive(true);
                if (inputField != null)
                {
                    inputField.ActivateInputField();
                }
            }
        }

        public void CloseChatPanel()
        {
            if (chatPanel != null)
            {
                chatPanel.SetActive(false);
            }
        }

        public void OnSendButtonClicked()
        {
            SendMessageFromInput();
        }

        private void OnInputSubmitted(string text)
        {
            // Enviar con Enter a menos que se mantenga Shift
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                SendMessageFromInput();
            }
        }

        private void SendMessageFromInput()
        {
            if (inputField == null) return;

            string text = inputField.text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            if (chatManager == null)
            {
                chatManager = FindFirstObjectByType<OllamaChatManager>();
                if (chatManager == null)
                {
                    AppendMessage(errorPrefix, "No se encontró ningún OllamaChatManager en la escena.");
                    return;
                }
            }

            inputField.text = string.Empty;
            inputField.ActivateInputField();

            chatManager.SendUserMessage(text);
        }

        public void OnClearButtonClicked()
        {
            if (chatManager != null)
            {
                chatManager.ClearHistory();
            }
            chatHistoryBuffer.Clear();
            if (chatLogText != null)
            {
                chatLogText.text = string.Empty;
            }
            AppendMessage(systemPrefix, "Historial reiniciado.");
        }

        private void HandleMessageSent(string userText)
        {
            AppendMessage(userPrefix, userText);
        }

        private void HandleResponseReceived(string responseText)
        {
            AppendMessage(assistantPrefix, responseText);
        }

        private void HandleErrorReceived(string errorText)
        {
            AppendMessage(errorPrefix, errorText);
            UpdateStatus("Error de conexión");
        }

        private void HandleBusyStateChanged(bool isBusy)
        {
            if (sendButton != null)
                sendButton.interactable = !isBusy;

            if (inputField != null)
                inputField.interactable = !isBusy;

            if (loadingIndicator != null)
                loadingIndicator.SetActive(isBusy);

            UpdateStatus(isBusy ? "Gemma está pensando..." : "Listo");
        }

        private void AppendMessage(string prefix, string content)
        {
            if (chatHistoryBuffer.Length > 0)
            {
                chatHistoryBuffer.AppendLine("\n");
            }

            chatHistoryBuffer.Append($"{prefix} {content.Trim()}");

            if (chatLogText != null)
            {
                chatLogText.text = chatHistoryBuffer.ToString();
            }

            if (scrollRect != null)
            {
                StartCoroutine(ScrollToBottomCoroutine());
            }
        }

        private System.Collections.IEnumerator ScrollToBottomCoroutine()
        {
            yield return new WaitForEndOfFrame();
            try
            {
                if (scrollRect != null && scrollRect.content != null)
                {
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
                    scrollRect.verticalNormalizedPosition = 0f;
                }
            }
            catch { }
        }

        public void SetStatusText(string status)
        {
            UpdateStatus(status);
        }

        private void UpdateStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
        }

        public void SetupReferences(
            OllamaChatManager manager,
            GameObject panel,
            Button toggleBtn,
            Button closeBtn,
            Button clearBtn,
            TMP_InputField input,
            Button sendBtn,
            TMP_Text logText,
            ScrollRect scroll,
            TMP_Text status,
            GameObject loading)
        {
            chatManager = manager;
            chatPanel = panel;
            toggleButton = toggleBtn;
            closeButton = closeBtn;
            clearButton = clearBtn;
            inputField = input;
            sendButton = sendBtn;
            chatLogText = logText;
            scrollRect = scroll;
            statusText = status;
            loadingIndicator = loading;
        }
    }
}
