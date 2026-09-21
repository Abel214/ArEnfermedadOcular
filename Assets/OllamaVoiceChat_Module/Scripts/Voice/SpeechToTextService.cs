using System;
using System.Threading.Tasks;
using UnityEngine;
using Whisper;

namespace OllamaVoice
{
    public class SpeechToTextService : MonoBehaviour
    {
        [Header("Configuración Whisper STT")]
        [SerializeField] private WhisperManager whisperManager;
        [SerializeField] private string language = "es";
        [SerializeField] private bool translateToEnglish = false;

        public bool IsReady => whisperManager != null && whisperManager.IsLoaded;

        private async void Awake()
        {
            if (whisperManager == null)
            {
                whisperManager = GetComponent<WhisperManager>();
                if (whisperManager == null)
                {
                    whisperManager = gameObject.AddComponent<WhisperManager>();
                    whisperManager.ModelPath = "whisper/ggml-tiny.bin";
                    whisperManager.IsModelPathInStreamingAssets = true;
                }
            }

            whisperManager.language = language;
            whisperManager.translateToEnglish = translateToEnglish;

            if (!whisperManager.IsLoaded && !whisperManager.IsLoading)
            {
                Debug.Log("[SpeechToTextService] Inicializando modelo Whisper...");
                await whisperManager.InitModel();
                Debug.Log("[SpeechToTextService] ¡Modelo Whisper cargado y listo!");
            }
        }

        /// <summary>
        /// Transcribe el AudioClip grabado a texto en español usando Whisper.
        /// </summary>
        public async Task<string> TranscribeAsync(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[SpeechToTextService] El AudioClip proporcionado es nulo.");
                return string.Empty;
            }

            if (whisperManager == null)
            {
                Debug.LogError("[SpeechToTextService] WhisperManager no está disponible.");
                return string.Empty;
            }

            if (!whisperManager.IsLoaded)
            {
                Debug.Log("[SpeechToTextService] Esperando que cargue el modelo Whisper...");
                await whisperManager.InitModel();
            }

            whisperManager.language = language;

            try
            {
                Debug.Log("[SpeechToTextService] Transcribiendo audio...");
                var result = await whisperManager.GetTextAsync(clip);

                string transcribedText = result?.Result?.Trim() ?? string.Empty;
                Debug.Log($"[SpeechToTextService] Transcripción completada: \"{transcribedText}\"");
                return transcribedText;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpeechToTextService] Error durante la transcripción: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
