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
            if (chatManager == null) chatManager = FindFirstObjectByType<OllamaChatManager>();
            if (uiController == null) uiController = FindFirstObjectByType<ChatUIController>();
        }

        private void OnEnable()
        {
            if (micButton != null)
            {
                micButton.onClick.AddListener(ToggleRecording);
            }

            if (chatManager != null)
            {
                chatManager.ResponseReceived += HandleAssistantResponse;
            }

            if (ttsService != null)
            {
                ttsService.OnSpeechStarted += HandleSpeechStarted;
                ttsService.OnSpeechFinished += HandleSpeechFinished;
            }

            SetButtonVisuals(idleColor, "Voz");
        }

        private void Start()
        {
            if (chatManager == null)
            {
                chatManager = FindFirstObjectByType<OllamaChatManager>();
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
            if (micButton != null)
            {
                micButton.onClick.RemoveListener(ToggleRecording);
            }

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
        /// Inicia o detiene la grabación de voz por micrófono.
        /// </summary>
        public void ToggleRecording()
        {
            if (isProcessing)
            {
                Debug.LogWarning("[VoiceChatController] Proceso en curso, espera...");
                return;
            }

            if (micRecorder.IsRecording)
            {
                StopRecordingAndProcess();
            }
            else
            {
                StartRecording();
            }
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
                SetButtonVisuals(recordingColor, "Stop");
                if (uiController != null)
                {
                    uiController.SetStatusText("Escuchando... Habla ahora (Click para enviar)");
                }
            }
        }

        private async void StopRecordingAndProcess()
        {
            isProcessing = true;
            SetButtonVisuals(processingColor, "...");

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

            ResetToIdleState("Enviando a Gemma...");

            // Enviar mensaje transcrito al flujo del chat
            if (chatManager != null)
            {
                chatManager.SendUserMessage(transcribedText);
            }
        }

        private void HandleAssistantResponse(string responseText)
        {
            // Reproducir la respuesta de Gemma automáticamente por TTS
            if (ttsService != null && !string.IsNullOrWhiteSpace(responseText))
            {
                ttsService.Speak(responseText);
            }
        }

        private void HandleSpeechStarted()
        {
            if (uiController != null)
            {
                uiController.SetStatusText("Gemma está hablando...");
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
            SetButtonVisuals(idleColor, "Voz");

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

            if (micButtonText != null)
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
            TMPro.TMP_Text btnText)
        {
            micRecorder = recorder;
            sttService = stt;
            ttsService = tts;
            chatManager = chat;
            uiController = ui;
            micButton = btn;
            micButtonImage = btnImg;
            micButtonText = btnText;
        }
    }
}
