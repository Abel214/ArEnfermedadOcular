using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using OllamaIntegration;
using OllamaUI;

namespace OllamaVoice
{
    public class VoiceChatController : MonoBehaviour
    {
        [Header("Servicios")]
        [SerializeField] private MicRecorder micRecorder;
        [SerializeField] private SpeechToTextService sttService;
        [SerializeField] private TextToSpeechService ttsService;
        [SerializeField] private OllamaChatManager chatManager;
        [SerializeField] private ChatUIController uiController;

        [Header("UI del Micrófono")]
        [SerializeField] private Button micButton;
        [SerializeField] private Image micButtonImage;
        [SerializeField] private TMPro.TMP_Text micButtonText;

        [Header("Iconos del Micrófono")]
        [SerializeField] private UnityEngine.UI.Image glyphImage;
        [SerializeField] private Sprite micIdleSprite;
        [SerializeField] private Sprite micRecordingSprite;

        [Header("Colores de Estado")]
        [SerializeField] private Color idleColor = new Color(0.23f, 0.49f, 0.95f, 1f); // Azul
        [SerializeField] private Color recordingColor = new Color(0.90f, 0.22f, 0.21f, 1f); // Rojo
        [SerializeField] private Color processingColor = new Color(0.98f, 0.60f, 0.08f, 1f); // Naranja

        private bool isProcessing = false;

        private void Awake()
        {
            if (micRecorder == null) micRecorder = GetComponent<MicRecorder>() ?? gameObject.AddComponent<MicRecorder>();
            if (sttService == null) sttService = GetComponent<SpeechToTextService>() ?? gameObject.AddComponent<SpeechToTextService>();
            if (ttsService == null) ttsService = GetComponent<TextToSpeechService>() ?? gameObject.AddComponent<TextToSpeechService>();
            if (chatManager == null) chatManager = FindAnyObjectByType<OllamaChatManager>(FindObjectsInactive.Include);
            if (uiController == null) uiController = FindAnyObjectByType<ChatUIController>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            // El botón de micrófono usa HoldToTalkButton (mantener presionado para hablar).
            if (chatManager != null)
            {
                chatManager.ResponseReceived += HandleAssistantResponse;
            }

            if (ttsService != null)
            {
                ttsService.OnSpeechStarted += HandleSpeechStarted;
                ttsService.OnSpeechFinished += HandleSpeechFinished;
            }

            SetButtonVisuals(idleColor, null);
        }

        private void Start()
        {
            if (chatManager == null)
            {
                chatManager = FindAnyObjectByType<OllamaChatManager>(FindObjectsInactive.Include);
                if (chatManager != null)
                {
                    chatManager.ResponseReceived -= HandleAssistantResponse;
                    chatManager.ResponseReceived += HandleAssistantResponse;
                }
            }

            if (ttsService == null)
            {
                ttsService = GetComponent<TextToSpeechService>() ?? gameObject.AddComponent<TextToSpeechService>();
            }

            Debug.Log("[VoiceChatController] Sistema de voz inicializado (STT Whisper + TTS Voz Nativa en Español).");
        }

        private void OnDisable()
        {
            if (chatManager != null)
            {
                chatManager.ResponseReceived -= HandleAssistantResponse;
            }

            if (ttsService != null)
            {
                ttsService.OnSpeechStarted -= HandleSpeechStarted;
                ttsService.OnSpeechFinished -= HandleSpeechFinished;
            }
        }

        private void Update()
        {
            // Efecto de pulso en el botón mientras graba
            if (micRecorder != null && micRecorder.IsRecording && micButtonImage != null)
            {
                float pingPong = Mathf.PingPong(Time.time * 3f, 0.3f);
                micButtonImage.color = Color.Lerp(recordingColor, Color.red, pingPong);
            }
        }

        /// <summary>
        /// Mantener presionado para hablar. Se llama al presionar el botón del micrófono.
        /// </summary>
        public void StartHoldRecording()
        {
            if (isProcessing)
            {
                Debug.LogWarning("[VoiceChatController] Proceso en curso, espera...");
                return;
            }

            if (micRecorder != null && micRecorder.IsRecording) return;

            // Liberar el micrófono: si la sesión de comandos de voz nativa
            // (SpeechRecognizer de Android) está activa, retiene el micrófono
            // y la grabación del chat saldría vacía.
            if (AndroidVoiceRecognizer.Instance != null)
                AndroidVoiceRecognizer.Instance.DetenerEscuchaManual();

            StartRecording();
        }

        /// <summary>
        /// Se llama al soltar el botón: detiene la grabación y envía a transcribir.
        /// </summary>
        public void ReleaseHoldAndSend()
        {
            if (micRecorder == null || !micRecorder.IsRecording)
            {
                ResetToIdleState("Listo");
                return;
            }

            // Toc (click muy corto): no procesar ni bloquear el botón,
            // avisar el gesto correcto y volver al estado normal.
            if (micRecorder.RecordingDuration < 0.5f)
            {
                micRecorder.StopRecording();
                ResetToIdleState();
                if (uiController != null)
                {
                    uiController.SetStatusText("Mantén presionado para hablar.");
                }
                return;
            }

            StopRecordingAndProcess();
        }

        /// <summary>
        /// Se llama si el dedo sale del botón: descarta la grabación sin enviar.
        /// </summary>
        public void CancelHold()
        {
            if (micRecorder != null && micRecorder.IsRecording)
            {
                micRecorder.StopRecording();
            }
            ResetToIdleState("Grabación cancelada.");
        }

        /// <summary>
        /// Reproduce un texto por TTS (botón de re-escuchar en cada burbuja).
        /// </summary>
        public void SpeakText(string text)
        {
            if (ttsService == null || string.IsNullOrWhiteSpace(text)) return;
            if (ttsService.IsSpeaking)
            {
                ttsService.Stop();
            }
            ttsService.Speak(text);
        }

        private void StartRecording()
        {
            // Si el TTS está hablando, detenerlo para no grabarse a sí mismo
            if (ttsService != null && ttsService.IsSpeaking)
            {
                ttsService.Stop();
            }

            bool started = micRecorder.StartRecording();
            if (started)
            {
                SetButtonVisuals(recordingColor, null);
                if (uiController != null)
                {
                    uiController.SetStatusText("Escuchando… suelta para enviar");
                }
            }
        }

        private async void StopRecordingAndProcess()
        {
            isProcessing = true;
            SetButtonVisuals(processingColor, null);

            if (uiController != null)
            {
                uiController.SetStatusText("Transcribiendo voz con Whisper...");
            }

            AudioClip recordedClip = micRecorder.StopRecording();

            if (recordedClip == null)
            {
                Debug.LogWarning("[VoiceChatController] No se obtuvo audio grabado.");
                ResetToIdleState("No se detectó audio.");
                return;
            }

            string transcribedText = await sttService.TranscribeAsync(recordedClip);

            if (string.IsNullOrWhiteSpace(transcribedText))
            {
                Debug.LogWarning("[VoiceChatController] Transcripción vacía o inaudible.");
                ResetToIdleState("Voz no entendida, intenta de nuevo.");
                return;
            }

            ResetToIdleState("Enviando al asistente...");

            // Enviar mensaje transcrito al flujo del chat
            if (chatManager != null)
            {
                chatManager.SendUserMessage(transcribedText);
            }
        }

        private void HandleAssistantResponse(string responseText)
        {
            // Reproducir la respuesta del asistente automáticamente por TTS
            if (ttsService != null && !string.IsNullOrWhiteSpace(responseText))
            {
                ttsService.Speak(responseText);
            }
        }

        private void HandleSpeechStarted()
        {
            // Reactivar la sesión de comandos nativa para que el usuario pueda
            // interrumpir la voz del asistente diciendo "silencio" mientras
            // habla. VoiceComanderManage ignora el eco (solo acepta silenciar).
            if (AndroidVoiceRecognizer.Instance != null)
                AndroidVoiceRecognizer.Instance.IniciarEscuchaManual();

            if (uiController != null)
            {
                uiController.SetStatusText("El asistente está hablando... (di 'silencio' o toca PARAR)");
            }
        }

        private void HandleSpeechFinished()
        {
            if (uiController != null && !isProcessing && !micRecorder.IsRecording)
            {
                uiController.SetStatusText("Listo para conversar");
            }
        }

        private void ResetToIdleState(string statusMessage = null)
        {
            isProcessing = false;
            SetButtonVisuals(idleColor, null);

            if (uiController != null && !string.IsNullOrEmpty(statusMessage))
            {
                uiController.SetStatusText(statusMessage);
            }
        }

        private void SetButtonVisuals(Color color, string label)
        {
            if (micButtonImage != null)
            {
                micButtonImage.color = color;
            }

            if (glyphImage != null)
            {
                glyphImage.sprite = color == recordingColor ? micRecordingSprite : micIdleSprite;
            }

            if (micButtonText != null && label != null)
            {
                micButtonText.text = label;
            }
        }

        public void SetupReferences(
            MicRecorder recorder,
            SpeechToTextService stt,
            TextToSpeechService tts,
            OllamaChatManager chat,
            ChatUIController ui,
            Button btn,
            Image btnImg,
            TMPro.TMP_Text btnText,
            UnityEngine.UI.Image glyph = null,
            Sprite idleIcon = null,
            Sprite recordingIcon = null)
        {
            micRecorder = recorder;
            sttService = stt;
            ttsService = tts;
            chatManager = chat;
            uiController = ui;
            micButton = btn;
            micButtonImage = btnImg;
            micButtonText = btnText;
            glyphImage = glyph;
            micIdleSprite = idleIcon;
            micRecordingSprite = recordingIcon;
        }
    }
}
