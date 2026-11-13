using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OllamaIntegration;
using Nobi.UiRoundedCorners;

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

        [Header("Selector de Proveedor")]
        [Tooltip("Dropdown con las opciones Ollama · Local y Gemini · Nube.")]
        [SerializeField] private TMP_Dropdown providerDropdown;
        [SerializeField] private TMP_Text titleText;

        [Header("Entrada y Envío")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;

        [Header("Visualización del Chat")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Iconos (los asigna el builder)")]
        [SerializeField] private Sprite replayIcon;
        [SerializeField] private TMP_FontAsset appFont;

        [Header("Panel de Clave Gemini")]
        [Tooltip("Si está en OFF, el botón 'Clave' del chat se oculta y la app usa la clave de fábrica de GeminiKeyStore.")]
        [SerializeField] private bool showKeyButton = false;

        [Header("Botón PARAR Voz (TTS)")]
        [Tooltip("Si está en OFF, el botón PARAR no se crea; la voz se silencia por comando de voz ('silencio'/'callar'/'detener'/'basta').")]
        [SerializeField] private bool showTtsStopButton = false;

        // Botón PARAR/VOZ: interrumpe la voz del asistente (TTS). Se crea por código.
        private OllamaVoice.TextToSpeechService ttsService;
        private GameObject ttsStopButton;
        private TMP_Text ttsStopLabel;

        // Botón de parada dentro de cada burbuja del asistente (junto al de
        // reproducir). Solo se muestra en la burbuja que se está reproduciendo.
        private readonly Dictionary<GameObject, GameObject> bubbleStopButtons = new Dictionary<GameObject, GameObject>();
        private GameObject speakingBubble;

        // Panel de clave Gemini (se crea en tiempo de ejecución, no en la escena)
        private GameObject keyPanel;
        private TMP_InputField keyInput;
        private TMP_Text keyStatusText;
        private bool keyVisible = false;

        // Burbujas: colores de la app
        private static readonly Color UserBubbleColor = new Color(0.0f, 0.45f, 0.55f, 1f);
        private static readonly Color AssistantBubbleColor = new Color(0.07f, 0.12f, 0.18f, 1f);

        private const float BubblePadX = 28f;
        private const float BubblePadY = 20f;
        private const float ReplaySpace = 74f;

        private readonly List<GameObject> bubbles = new List<GameObject>();
        private bool suppressDropdownCallback = false;
        private bool clearArmed = false;
        private Coroutine clearDisarmRoutine;
        private Coroutine typingRoutine;
        private int typingDots;

        private void Awake()
        {
            if (chatManager == null)
            {
                chatManager = FindAnyObjectByType<OllamaChatManager>(FindObjectsInactive.Include);
            }

            // Estado inicial: solo el botón flotante visible.
            if (chatPanel != null)
            {
                chatPanel.SetActive(false);
            }

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
            }

            if (chatManager != null)
            {
                chatManager.ProviderChanged -= HandleProviderChanged;
                chatManager.ProviderChanged += HandleProviderChanged;
                chatManager.ActiveModelChanged -= HandleActiveModelChanged;
                chatManager.ActiveModelChanged += HandleActiveModelChanged;
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

            if (providerDropdown != null)
                providerDropdown.onValueChanged.AddListener(OnProviderDropdownChanged);

            SubscribeChatManager();
        }

        private void SubscribeChatManager()
        {
            if (chatManager == null)
            {
                chatManager = FindAnyObjectByType<OllamaChatManager>(FindObjectsInactive.Include);
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

            if (providerDropdown != null)
                providerDropdown.onValueChanged.RemoveListener(OnProviderDropdownChanged);

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
            SetupProviderDropdown();
            BuildKeyUI();
            ClearSerializedBubbles();
            RefreshTitle();
            UpdateStatus("Listo");
            AddAssistantBubble("Hola, ¿en qué te ayudo?");
            EnsureTtsService();
            CreateTtsStopButton();
        }

        /// <summary>
        /// Elimina burbujas que hayan quedado guardadas en la escena para que
        /// no se dupliquen ni reaparezcan mensajes viejos al abrir el chat.
        /// </summary>
        private void ClearSerializedBubbles()
        {
            if (contentRoot == null) return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = contentRoot.GetChild(i);
                if (child.name == "AssistantRow" || child.name == "UserRow")
                {
                    Destroy(child.gameObject);
                }
            }

            bubbles.Clear();
            bubbleStopButtons.Clear();
            speakingBubble = null;
        }

        private void OnDestroy()
        {
            if (chatManager != null)
            {
                chatManager.ProviderChanged -= HandleProviderChanged;
                chatManager.ActiveModelChanged -= HandleActiveModelChanged;
            }

            if (ttsService != null)
            {
                ttsService.OnSpeechStarted -= HandleTtsStateChanged;
                ttsService.OnSpeechFinished -= HandleTtsStateChanged;
            }
        }

        // ── Botón PARAR/VOZ (interrumpir al asistente) ─────────
        private void EnsureTtsService()
        {
            if (ttsService == null)
            {
                ttsService = FindAnyObjectByType<OllamaVoice.TextToSpeechService>(FindObjectsInactive.Include);
            }

            if (ttsService != null)
            {
                ttsService.OnSpeechStarted -= HandleTtsStateChanged;
                ttsService.OnSpeechStarted += HandleTtsStateChanged;
                ttsService.OnSpeechFinished -= HandleTtsStateChanged;
                ttsService.OnSpeechFinished += HandleTtsStateChanged;
            }
        }

        private void HandleTtsStateChanged()
        {
            UpdateTtsButtonVisual();
            UpdateBubbleStopButtons();
        }

        private void CreateTtsStopButton()
        {
            if (!showTtsStopButton) return;
            if (chatPanel == null || ttsStopButton != null) return;

            GameObject go = new GameObject("TtsStopButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(chatPanel.transform, false);
            go.transform.SetAsLastSibling();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(14f, 134f);
            rect.sizeDelta = new Vector2(110f, 92f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.10f, 0.16f, 0.22f, 0.95f);
            Round(go, 18f);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(OnTtsStopButtonClicked);

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(go.transform, false);
            var label = labelObj.GetComponent<TextMeshProUGUI>();
            Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 34;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            if (appFont != null) label.font = appFont;

            ttsStopLabel = label;
            ttsStopButton = go;
            UpdateTtsButtonVisual();
        }

        private void UpdateTtsButtonVisual()
        {
            bool speaking = ttsService != null && ttsService.IsSpeaking;

            if (ttsStopLabel != null)
            {
                ttsStopLabel.text = speaking ? "PARAR" : "VOZ";
                ttsStopLabel.color = speaking ? new Color(1f, 0.75f, 0.7f, 1f) : Color.white;
            }

            if (ttsStopButton != null)
            {
                ttsStopButton.GetComponent<Image>().color = speaking
                    ? new Color(0.55f, 0.13f, 0.13f, 0.95f)
                    : new Color(0.10f, 0.16f, 0.22f, 0.95f);
            }
        }

        private void OnTtsStopButtonClicked()
        {
            EnsureTtsService();
            if (ttsService == null)
            {
                UpdateStatus("Servicio de voz no disponible.");
                return;
            }

            if (ttsService.IsSpeaking)
            {
                ttsService.Stop();
                UpdateStatus("Voz silenciada");
            }
        }

        // ── Panel ──────────────────────────────────────────────
        public void ToggleChatPanel()
        {
            if (chatPanel != null)
            {
                bool newState = !chatPanel.activeSelf;
                chatPanel.SetActive(newState);
                if (newState)
                {
                    if (inputField != null)
                    {
                        inputField.ActivateInputField();
                    }
                    HintKeyIfNeeded();
                }
            }
        }

        /// <summary>
        /// Abre el panel del chat. activateInput=false evita abrir el teclado
        /// en pantalla (útil al abrir por comando de voz: el teclado roba el
        /// foco de audio y mata la sesión de reconocimiento nativa).
        /// </summary>
        public void OpenChatPanel(bool activateInput = true)
        {
            if (chatPanel != null)
            {
                chatPanel.SetActive(true);
                if (activateInput && inputField != null)
                {
                    inputField.ActivateInputField();
                }
                HintKeyIfNeeded();
            }
        }

        public void CloseChatPanel()
        {
            if (keyPanel != null)
            {
                keyPanel.SetActive(false);
            }

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
            // En móvil no hay Shift: enviar siempre al pulsar "Enviar/Intro" del teclado.
            SendMessageFromInput();
        }

        private void SendMessageFromInput()
        {
            if (inputField == null) return;

            string text = inputField.text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            if (chatManager == null)
            {
                chatManager = FindAnyObjectByType<OllamaChatManager>(FindObjectsInactive.Include);
                if (chatManager == null)
                {
                    AddErrorBubble("No se encontró ningún gestor de chat en la escena.");
                    return;
                }
            }

            inputField.text = string.Empty;
            inputField.ActivateInputField();

            chatManager.SendUserMessage(text);
        }

        // ── Borrado con confirmación en dos pasos ──────────────
        public void OnClearButtonClicked()
        {
            if (!clearArmed)
            {
                clearArmed = true;
                SetClearLabel("¿Seguro?");
                if (clearDisarmRoutine != null) StopCoroutine(clearDisarmRoutine);
                clearDisarmRoutine = StartCoroutine(DisarmClearAfterSeconds(3f));
                return;
            }

            clearArmed = false;
            if (clearDisarmRoutine != null) StopCoroutine(clearDisarmRoutine);
            SetClearLabel("Borrar");

            if (chatManager != null)
            {
                chatManager.ClearHistory();
            }
            foreach (var b in bubbles)
            {
                if (b != null) Destroy(b);
            }
            bubbles.Clear();
            bubbleStopButtons.Clear();
            speakingBubble = null;
            AddAssistantBubble("Historial reiniciado. ¿En qué te ayudo?");
        }

        private IEnumerator DisarmClearAfterSeconds(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            clearArmed = false;
            SetClearLabel("Borrar");
        }

        private void SetClearLabel(string label)
        {
            if (clearButton == null) return;
            var tmp = clearButton.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = label;
        }

        // ── Clave de Gemini escrita en el dispositivo ──────────
        private void BuildKeyUI()
        {
            if (chatPanel == null || keyPanel != null) return;

            // Con showKeyButton OFF (clave de fábrica) no se crea ni el botón
            // ni el panel de configuración de la clave.
            if (!showKeyButton) return;

            Transform header = chatPanel.transform.Find("Header");
            if (header != null)
            {
                GameObject btn = CreateLabelButton(header, "ApiKeyButton", "Clave",
                    new Color(0.10f, 0.22f, 0.30f, 1f), 18);
                var br = btn.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(1f, 0.5f);
                br.anchorMax = new Vector2(1f, 0.5f);
                br.pivot = new Vector2(1f, 0.5f);
                br.anchoredPosition = new Vector2(-224f, 0f);
                br.sizeDelta = new Vector2(100f, 65f);
                btn.GetComponent<Button>().onClick.AddListener(OpenKeyPanel);
            }

            keyPanel = new GameObject("ApiKeyPanel", typeof(RectTransform), typeof(Image));
            keyPanel.transform.SetParent(chatPanel.transform, false);
            var pr = keyPanel.GetComponent<RectTransform>();
            pr.anchorMin = Vector2.zero;
            pr.anchorMax = Vector2.one;
            pr.offsetMin = new Vector2(26f, 26f);
            pr.offsetMax = new Vector2(-26f, -26f);
            keyPanel.GetComponent<Image>().color = new Color(0.035f, 0.063f, 0.094f, 1f);
            Round(keyPanel, 26f);

            var title = CreateLabel(keyPanel.transform, "Title", "Clave de Gemini", 36,
                Color.white, TextAlignmentOptions.MidlineLeft);
            PlaceTop(title.rectTransform, 40f, 56f, 44f, -44f);

            var desc = CreateLabel(keyPanel.transform, "Help",
                "Pega tu API key de Google AI Studio. Se guarda solo en este teléfono: no va dentro del APK ni al repositorio. Bórrala al terminar la demostración.",
                21, new Color(0.62f, 0.72f, 0.78f, 1f), TextAlignmentOptions.TopLeft);
            PlaceTop(desc.rectTransform, 110f, 120f, 44f, -44f);

            GameObject inputObj = new GameObject("KeyInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputObj.transform.SetParent(keyPanel.transform, false);
            PlaceTop(inputObj.GetComponent<RectTransform>(), 250f, 96f, 44f, -190f);
            inputObj.GetComponent<Image>().color = new Color(0.05f, 0.09f, 0.13f, 1f);
            Round(inputObj, 18f);

            keyInput = inputObj.GetComponent<TMP_InputField>();

            GameObject areaObj = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            areaObj.transform.SetParent(inputObj.transform, false);
            var areaRect = areaObj.GetComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(22f, 10f);
            areaRect.offsetMax = new Vector2(-22f, -10f);
            keyInput.textViewport = areaRect;

            var ph = CreateLabel(areaObj.transform, "Placeholder", "Pega aquí tu API key (AIza…)", 24,
                new Color(0.55f, 0.65f, 0.72f, 0.8f), TextAlignmentOptions.MidlineLeft);
            ph.fontStyle = FontStyles.Italic;
            Stretch(ph.rectTransform);
            keyInput.placeholder = ph;

            var txt = CreateLabel(areaObj.transform, "Text", string.Empty, 24, Color.white,
                TextAlignmentOptions.MidlineLeft);
            Stretch(txt.rectTransform);
            keyInput.textComponent = txt;

            keyInput.contentType = TMP_InputField.ContentType.Password;
            keyInput.lineType = TMP_InputField.LineType.SingleLine;
            keyInput.characterLimit = 200;
            keyInput.onSubmit.AddListener(_ => SaveKey());

            var viewBtn = CreateLabelButton(keyPanel.transform, "ShowButton", "Ver",
                new Color(0.14f, 0.22f, 0.28f, 1f), 22);
            var vr = viewBtn.GetComponent<RectTransform>();
            vr.anchorMin = new Vector2(1f, 1f);
            vr.anchorMax = new Vector2(1f, 1f);
            vr.pivot = new Vector2(1f, 1f);
            vr.anchoredPosition = new Vector2(-44f, -250f);
            vr.sizeDelta = new Vector2(130f, 96f);
            viewBtn.GetComponent<Button>().onClick.AddListener(ToggleKeyVisibility);

            keyStatusText = CreateLabel(keyPanel.transform, "KeyStatus", string.Empty, 21,
                new Color(0.35f, 0.85f, 0.9f, 1f), TextAlignmentOptions.MidlineLeft);
            PlaceTop(keyStatusText.rectTransform, 372f, 40f, 44f, -44f);

            var saveBtn = CreateLabelButton(keyPanel.transform, "SaveKeyButton", "Guardar clave",
                new Color(0.0f, 0.55f, 0.62f, 1f), 28);
            PlaceTop(saveBtn.GetComponent<RectTransform>(), 436f, 100f, 44f, -44f);
            saveBtn.GetComponent<Button>().onClick.AddListener(SaveKey);

            var clearKeyBtn = CreateLabelButton(keyPanel.transform, "ClearKeyButton", "Quitar clave del teléfono",
                new Color(0.16f, 0.22f, 0.28f, 1f), 24);
            PlaceTop(clearKeyBtn.GetComponent<RectTransform>(), 556f, 90f, 44f, -44f);
            clearKeyBtn.GetComponent<Button>().onClick.AddListener(ClearStoredKey);

            var closeKeyBtn = CreateLabelButton(keyPanel.transform, "CloseKeyButton", "Cerrar",
                new Color(0.12f, 0.18f, 0.24f, 1f), 26);
            var cr = closeKeyBtn.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0.5f, 0f);
            cr.anchorMax = new Vector2(0.5f, 0f);
            cr.pivot = new Vector2(0.5f, 0f);
            cr.anchoredPosition = new Vector2(0f, 46f);
            cr.sizeDelta = new Vector2(320f, 90f);
            closeKeyBtn.GetComponent<Button>().onClick.AddListener(CloseKeyPanel);

            keyPanel.SetActive(false);
        }

        public void OpenKeyPanel()
        {
            if (keyPanel == null) BuildKeyUI();
            if (keyPanel == null) return;

            keyPanel.transform.SetAsLastSibling();
            keyPanel.SetActive(true);
            UpdateKeyStatus();

            if (keyInput != null)
            {
                keyInput.text = string.Empty;
                keyInput.ActivateInputField();
            }
        }

        public void CloseKeyPanel()
        {
            if (keyPanel != null) keyPanel.SetActive(false);
            UpdateStatus("Listo");
        }

        public void SaveKey()
        {
            string key = keyInput != null ? keyInput.text?.Trim() : null;
            if (string.IsNullOrEmpty(key))
            {
                if (keyStatusText != null)
                    keyStatusText.text = "Escribe o pega una clave antes de guardar.";
                return;
            }

            if (key.Length < 20)
            {
                if (keyStatusText != null)
                    keyStatusText.text = "Esa clave parece incompleta. Cópiala de nuevo desde Google AI Studio.";
                return;
            }

            GeminiKeyStore.Save(key);
            keyInput.text = string.Empty;

            if (chatManager != null && chatManager.Provider != ChatProviderType.Gemini)
            {
                chatManager.SetProvider(ChatProviderType.Gemini);
            }

            UpdateKeyStatus();
            if (keyPanel != null) keyPanel.SetActive(false);
            UpdateStatus("Gemini listo");
        }

        public void ClearStoredKey()
        {
            GeminiKeyStore.Clear();
            if (keyInput != null) keyInput.text = string.Empty;
            UpdateKeyStatus();
            UpdateStatus("Clave eliminada");
        }

        private void ToggleKeyVisibility()
        {
            keyVisible = !keyVisible;
            if (keyInput == null) return;

            keyInput.contentType = keyVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            keyInput.ForceLabelUpdate();
        }

        private void UpdateKeyStatus()
        {
            if (keyStatusText == null) return;

            if (GeminiKeyStore.HasKey)
            {
                keyStatusText.text = $"Clave guardada en este teléfono ({GeminiKeyStore.Hint()}).";
            }
            else
            {
                keyStatusText.text = "No hay clave guardada.";
            }
        }

        private void HintKeyIfNeeded()
        {
            if (chatManager == null || chatManager.IsBusy) return;
            if (chatManager.Provider != ChatProviderType.Gemini) return;
            if (GeminiKeyStore.HasKey) return;

            if (showKeyButton)
                UpdateStatus("Toca 'Clave' para configurar Gemini.");
            else
                UpdateStatus("Gemini no está configurado en esta instalación.");
        }

        private TMP_Text CreateLabel(Transform parent, string name, string text, float fontSize,
            Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            if (appFont != null) tmp.font = appFont;
            return tmp;
        }

        private GameObject CreateLabelButton(Transform parent, string name, string label, Color bg, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bg;
            Round(go, 16f);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var text = CreateLabel(go.transform, "Label", label, fontSize, Color.white,
                TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            return go;
        }

        private static void PlaceTop(RectTransform r, float top, float height, float left, float right)
        {
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.offsetMin = new Vector2(left, -top - height);
            r.offsetMax = new Vector2(right, -top);
        }

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        // ── Proveedor ──────────────────────────────────────────
        private void SetupProviderDropdown()
        {
            if (providerDropdown == null || chatManager == null) return;

            suppressDropdownCallback = true;
            try
            {
                providerDropdown.ClearOptions();
                providerDropdown.AddOptions(new List<string>
                {
                    "Ollama Gemma 4",
                    "Gemini"
                });

                // Asegura que el elemento seleccionado muestre una marca visible,
                // no solo el cuadro celeste vacío del template.
                 var checkTransform = providerDropdown.transform.Find("Template/Viewport/Content/Item/Item Checkmark");
                 if (checkTransform != null)
                 {
                     var existingText = checkTransform.GetComponent<TMP_Text>();
                     if (existingText != null)
                     {
                         existingText.text = "\u2713";
                         existingText.fontSize = 24;
                         existingText.color = new Color(0.0f, 0.85f, 0.9f, 1f);
                         existingText.alignment = TextAlignmentOptions.Center;
                         if (appFont != null) existingText.font = appFont;
                     }
                     else
                     {
                         var checkImage = checkTransform.GetComponent<Image>();
                         if (checkImage != null)
                         {
                             checkImage.color = new Color(0f, 0f, 0f, 0f);
                             checkImage.raycastTarget = false;
                         }

                         if (checkTransform.Find("Check") == null)
                         {
                             var checkTextObj = new GameObject("Check", typeof(RectTransform), typeof(TextMeshProUGUI));
                             checkTextObj.transform.SetParent(checkTransform, false);
                             var checkTextRect = checkTextObj.GetComponent<RectTransform>();
                             checkTextRect.anchorMin = Vector2.zero;
                             checkTextRect.anchorMax = Vector2.one;
                             checkTextRect.offsetMin = Vector2.zero;
                             checkTextRect.offsetMax = Vector2.zero;
                             var checkText = checkTextObj.GetComponent<TextMeshProUGUI>();
                             checkText.text = "\u2713";
                             checkText.fontSize = 24;
                             checkText.color = new Color(0.0f, 0.85f, 0.9f, 1f);
                             checkText.alignment = TextAlignmentOptions.Center;
                             if (appFont != null) checkText.font = appFont;

                             var itemToggle = checkTransform.parent.GetComponent<Toggle>();
                             if (itemToggle != null) itemToggle.graphic = checkText;
                         }
                     }

                     var itemToggle2 = checkTransform.parent.GetComponent<Toggle>();
                     if (itemToggle2 != null)
                     {
                         var tmpText = checkTransform.GetComponent<TMP_Text>();
                         if (tmpText != null) itemToggle2.graphic = tmpText;
                     }
                 }

providerDropdown.SetValueWithoutNotify((int)chatManager.Provider);
                providerDropdown.RefreshShownValue();
            }
            finally
            {
                suppressDropdownCallback = false;
            }
        }

        private void OnProviderDropdownChanged(int index)
        {
            if (suppressDropdownCallback || chatManager == null) return;

            var selected = (ChatProviderType)Mathf.Clamp(index, 0, 1);
            if (!chatManager.SetProvider(selected))
            {
                suppressDropdownCallback = true;
                try
                {
                    providerDropdown.SetValueWithoutNotify((int)chatManager.Provider);
                    providerDropdown.RefreshShownValue();
                }
                finally
                {
                    suppressDropdownCallback = false;
                }
                UpdateStatus("Espera a que termine la respuesta para cambiar.");
                return;
            }

            if (selected == ChatProviderType.Gemini && !GeminiKeyStore.HasKey)
            {
                OpenKeyPanel();
            }
        }

        private void HandleProviderChanged(ChatProviderType newProvider)
        {
            RefreshTitle();

            if (providerDropdown != null)
            {
                suppressDropdownCallback = true;
                try
                {
                    providerDropdown.SetValueWithoutNotify((int)newProvider);
                    providerDropdown.RefreshShownValue();
                }
                finally
                {
                    suppressDropdownCallback = false;
                }
            }

            UpdateStatus("Listo");
        }

        private void HandleActiveModelChanged(string modelName)
        {
            RefreshTitle();
        }

        private void RefreshTitle()
        {
            if (titleText == null) return;
            titleText.text = "<b>Asistencia ocular</b>";
        }

        // ── Burbujas (ancho según texto, máx 80% del contenido) ──
        private GameObject CreateBubbleRow(bool isUser, out GameObject bubble)
        {
            bubble = null;
            if (contentRoot == null) return null;

            GameObject row = new GameObject(isUser ? "UserRow" : "AssistantRow", typeof(RectTransform));
            row.transform.SetParent(contentRoot, false);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 60f;

            bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            bubble.transform.SetParent(row.transform, false);
            var bubbleRect = bubble.GetComponent<RectTransform>();
            if (isUser)
            {
                bubbleRect.anchorMin = new Vector2(1f, 1f);
                bubbleRect.anchorMax = new Vector2(1f, 1f);
                bubbleRect.pivot = new Vector2(1f, 1f);
            }
            else
            {
                bubbleRect.anchorMin = new Vector2(0f, 1f);
                bubbleRect.anchorMax = new Vector2(0f, 1f);
                bubbleRect.pivot = new Vector2(0f, 1f);
            }
            bubbleRect.anchoredPosition = Vector2.zero;
            bubbleRect.sizeDelta = new Vector2(300f, 60f);
            bubble.GetComponent<Image>().color = isUser ? UserBubbleColor : AssistantBubbleColor;
            Round(bubble, 24f);

            bubbles.Add(row);
            return row;
        }

        private TMP_Text AddBubbleText(GameObject bubble, string text, TextAlignmentOptions alignment)
        {
            var textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(bubble.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(BubblePadX, BubblePadY);
            textRect.offsetMax = new Vector2(-BubblePadX, -BubblePadY);
            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 26;
            tmp.color = Color.white;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            if (appFont != null) tmp.font = appFont;
            return tmp;
        }

        public void AddUserBubble(string text)
        {
            var row = CreateBubbleRow(true, out var bubble);
            if (row == null) return;
            AddBubbleText(bubble, text, TextAlignmentOptions.TopRight);
            StartCoroutine(FitBubbleNextFrame(row, bubble, false));
            ScrollToBottom();
        }

        public void AddAssistantBubble(string text)
        {
            var row = CreateBubbleRow(false, out var bubble);
            if (row == null) return;
            AddBubbleText(bubble, text, TextAlignmentOptions.TopLeft);
            AddReplayButton(bubble, text);
            AddStopVoiceButton(bubble);
            // La respuesta recién creada es la que el asistente va a leer en voz
            // alta a continuación: su botón de parada es el que debe aparecer.
            speakingBubble = bubble;
            StartCoroutine(FitBubbleNextFrame(row, bubble, true));
            ScrollToBottom();
        }

        public void AddErrorBubble(string text)
        {
            var row = CreateBubbleRow(false, out var bubble);
            if (row == null) return;
            var tmp = AddBubbleText(bubble, text, TextAlignmentOptions.TopLeft);
            tmp.color = new Color(1f, 0.65f, 0.6f, 1f);
            StartCoroutine(FitBubbleNextFrame(row, bubble, false));
            ScrollToBottom();
        }

        private void AddReplayButton(GameObject bubble, string text)
        {
            GameObject replayObj = new GameObject("Replay", typeof(RectTransform), typeof(Image), typeof(Button));
            replayObj.transform.SetParent(bubble.transform, false);
            var replayRect = replayObj.GetComponent<RectTransform>();
            replayRect.anchorMin = new Vector2(0f, 0f);
            replayRect.anchorMax = new Vector2(0f, 0f);
            replayRect.pivot = new Vector2(0f, 0f);
            replayRect.anchoredPosition = new Vector2(BubblePadX, BubblePadY);
            replayRect.sizeDelta = new Vector2(64f, 64f);
            replayObj.GetComponent<Image>().color = new Color(0.0f, 0.55f, 0.62f, 1f);
            Round(replayObj, 32f);

            var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            iconObj.transform.SetParent(replayObj.transform, false);
            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(36f, 36f);
            var iconImg = iconObj.GetComponent<UnityEngine.UI.Image>();
            if (replayIcon != null) iconImg.sprite = replayIcon;
            iconImg.color = Color.white;

            string captured = text;
            replayObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                // Si el usuario re-reproduce una burbuja concreta, el botón de
                // parada se muestra en esa burbuja.
                speakingBubble = bubble;
                var voice = FindAnyObjectByType<OllamaVoice.VoiceChatController>(FindObjectsInactive.Include);
                if (voice != null) voice.SpeakText(captured);
            });
        }

        // ── Botón de parada de voz dentro de la burbuja ─────────
        private void AddStopVoiceButton(GameObject bubble)
        {
            GameObject stopObj = new GameObject("StopVoice", typeof(RectTransform), typeof(Image), typeof(Button));
            stopObj.transform.SetParent(bubble.transform, false);

            var rect = stopObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            // A la derecha del botón de reproducir (64 + separación).
            rect.anchoredPosition = new Vector2(BubblePadX + 76f, BubblePadY);
            rect.sizeDelta = new Vector2(64f, 64f);

            var img = stopObj.GetComponent<Image>();
            img.color = new Color(0.62f, 0.16f, 0.16f, 1f);
            Round(stopObj, 32f);

            // Icono de pausa: dos barras blancas (sin depender de sprites/fuentes).
            CreatePauseBar(stopObj.transform, -9f);
            CreatePauseBar(stopObj.transform, 9f);

            var btn = stopObj.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(OnBubbleStopVoiceClicked);

            stopObj.SetActive(false);
            bubbleStopButtons[bubble] = stopObj;
        }

        private void CreatePauseBar(Transform parent, float xOffset)
        {
            GameObject bar = new GameObject("PauseBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);

            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(xOffset, 0f);
            rect.sizeDelta = new Vector2(7f, 26f);

            bar.GetComponent<Image>().color = Color.white;
        }

        private void OnBubbleStopVoiceClicked()
        {
            EnsureTtsService();
            if (ttsService != null && ttsService.IsSpeaking)
            {
                ttsService.Stop();
                UpdateStatus("Voz silenciada");
            }
            else
            {
                UpdateStatus("Listo");
            }

            UpdateBubbleStopButtons();
        }

        /// <summary>
        /// Muestra el botón de parada solo en la burbuja que se está
        /// reproduciendo y lo oculta en el resto (y cuando no hay voz).
        /// </summary>
        private void UpdateBubbleStopButtons()
        {
            bool speaking = ttsService != null && ttsService.IsSpeaking;

            List<GameObject> stale = null;
            foreach (var kv in bubbleStopButtons)
            {
                if (kv.Key == null || kv.Value == null)
                {
                    if (stale == null) stale = new List<GameObject>();
                    stale.Add(kv.Key);
                    continue;
                }

                bool visible = speaking && kv.Key == speakingBubble;
                if (kv.Value.activeSelf != visible)
                    kv.Value.SetActive(visible);
            }

            if (stale != null)
            {
                foreach (var key in stale)
                    bubbleStopButtons.Remove(key);
            }
        }

        private IEnumerator FitBubbleNextFrame(GameObject row, GameObject bubble, bool hasReplay)
        {
            yield return new WaitForEndOfFrame();
            if (row == null || bubble == null) yield break;

            var contentRect = contentRoot as RectTransform;
            float contentW = contentRect != null && contentRect.rect.width > 0 ? contentRect.rect.width : 900f;
            float maxW = Mathf.Max(160f, contentW * 0.8f);

            var tmp = bubble.GetComponentInChildren<TMP_Text>();
            if (tmp == null) yield break;

            var bubbleRect = bubble.GetComponent<RectTransform>();

            // 1. Medir el ancho real del texto sin salto de línea.
            //    (Si se mide con el texto ya estirado, preferredWidth devuelve
            //    el ancho de la burbuja y todas salen del mismo tamaño.)
            bubbleRect.sizeDelta = new Vector2(maxW, 2000f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.ForceMeshUpdate();
            float textW = tmp.preferredWidth;

            // 2. Fijar el ancho justo (texto + márgenes) y recalcular el alto
            //    dejando que haga salto de línea solo si supera el 80%.
            tmp.textWrappingMode = TextWrappingModes.Normal;
            float bubbleW = Mathf.Clamp(Mathf.Ceil(textW) + BubblePadX * 2f + 4f, 110f, maxW);
            bubbleRect.sizeDelta = new Vector2(bubbleW, 2000f);
            tmp.ForceMeshUpdate();
            float textH = tmp.preferredHeight;

            float bubbleH = textH + BubblePadY * 2f + (hasReplay ? ReplaySpace : 0f);
            bubbleH = Mathf.Max(bubbleH, 68f);

            bubbleRect.sizeDelta = new Vector2(bubbleW, bubbleH);

            var rowRect = row.transform as RectTransform;
            rowRect.sizeDelta = new Vector2(0f, bubbleH);
            var rowLE = row.GetComponent<LayoutElement>();
            if (rowLE != null) rowLE.preferredHeight = bubbleH;

            ScrollToBottom();
        }

        // ── Eventos del chat ───────────────────────────────────
        private void HandleMessageSent(string userText)
        {
            AddUserBubble(userText);
        }

        private void HandleResponseReceived(string responseText)
        {
            AddAssistantBubble(responseText);
        }

        private void HandleErrorReceived(string errorText)
        {
            AddErrorBubble(errorText);
            UpdateStatus("Error de conexión");
        }

        private void HandleBusyStateChanged(bool isBusy)
        {
            if (sendButton != null)
                sendButton.interactable = !isBusy;

            if (inputField != null)
                inputField.interactable = !isBusy;

            if (providerDropdown != null)
                providerDropdown.interactable = !isBusy;

            if (loadingIndicator != null)
                loadingIndicator.SetActive(isBusy);

            if (isBusy)
            {
                if (typingRoutine != null) StopCoroutine(typingRoutine);
                typingDots = 0;
                typingRoutine = StartCoroutine(TypingDotsRoutine());
            }
            else
            {
                if (typingRoutine != null) StopCoroutine(typingRoutine);
                typingRoutine = null;
                UpdateStatus("Listo");
            }
        }

        private IEnumerator TypingDotsRoutine()
        {
            while (true)
            {
                typingDots = (typingDots + 1) % 4;
                UpdateStatus("El asistente está escribiendo" + new string('.', typingDots));
                yield return new WaitForSeconds(0.4f);
            }
        }

        private void ScrollToBottom()
        {
            if (scrollRect != null)
            {
                StartCoroutine(ScrollToBottomCoroutine());
            }
        }

        private IEnumerator ScrollToBottomCoroutine()
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

        private static void Round(GameObject go, float radius)
        {
            if (go == null || go.GetComponent<ImageWithIndependentRoundedCorners>() != null) return;
            var rounded = go.AddComponent<ImageWithIndependentRoundedCorners>();
            rounded.r = new Vector4(radius, radius, radius, radius);
        }

        public void SetupReferences(
            OllamaChatManager manager,
            GameObject panel,
            Button toggleBtn,
            Button closeBtn,
            Button clearBtn,
            TMP_InputField input,
            Button sendBtn,
            ScrollRect scroll,
            Transform content,
            TMP_Text status,
            GameObject loading,
            TMP_Dropdown provider = null,
            TMP_Text title = null,
            Sprite replay = null,
            TMP_FontAsset font = null)
        {
            chatManager = manager;
            chatPanel = panel;
            toggleButton = toggleBtn;
            closeButton = closeBtn;
            clearButton = clearBtn;
            inputField = input;
            sendButton = sendBtn;
            scrollRect = scroll;
            contentRoot = content;
            statusText = status;
            loadingIndicator = loading;
            providerDropdown = provider;
            titleText = title;
            replayIcon = replay;
            appFont = font;
        }
    }
}
