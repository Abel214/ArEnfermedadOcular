using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OllamaVoice
{
    public class TextToSpeechService : MonoBehaviour
    {
        [Header("Configuración de Voz (TTS)")]
        [Tooltip("Velocidad de habla (-10 a 10)")]
        [Range(-10, 10)]
        [SerializeField] private int speechRate = 1;

        [Tooltip("Volumen de habla (0 a 100)")]
        [Range(0, 100)]
        [SerializeField] private int speechVolume = 100;

        [Tooltip("Habilitar o silenciar la voz")]
        [SerializeField] private bool enableVoiceOutput = true;

        public event Action OnSpeechStarted;
        public event Action OnSpeechFinished;

        private bool isSpeaking = false;
        private Process currentProcess;
        private CancellationTokenSource cts;

        public bool IsSpeaking => isSpeaking;
        public bool EnableVoiceOutput
        {
            get => enableVoiceOutput;
            set => enableVoiceOutput = value;
        }

        /// <summary>
        /// Sintetiza y reproduce el texto como audio hablado en español.
        /// </summary>
        public async void Speak(string rawText)
        {
            if (!enableVoiceOutput) return;
            if (string.IsNullOrWhiteSpace(rawText)) return;

            string cleanedText = CleanTextForSpeech(rawText);
            if (string.IsNullOrWhiteSpace(cleanedText)) return;

            Stop();

            isSpeaking = true;
            OnSpeechStarted?.Invoke();
            cts = new CancellationTokenSource();

            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                SpeakAndroid(cleanedText);
#else
                await SpeakWindowsAsync(cleanedText, cts.Token);
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TextToSpeechService] Error al sintetizar voz: {ex.Message}");
            }
            finally
            {
                isSpeaking = false;
                OnSpeechFinished?.Invoke();
            }
        }

        /// <summary>
        /// Detiene cualquier reproducción de voz activa.
        /// </summary>
        public void Stop()
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }

            if (currentProcess != null && !currentProcess.HasExited)
            {
                try
                {
                    currentProcess.Kill();
                }
                catch { }
                currentProcess.Dispose();
                currentProcess = null;
            }

            if (isSpeaking)
            {
                isSpeaking = false;
                OnSpeechFinished?.Invoke();
            }
        }

        private Task SpeakWindowsAsync(string text, CancellationToken token)
        {
            return Task.Run(() =>
            {
                try
                {
                    // Escapar comillas simples para el script de PowerShell
                    string escapedText = text.Replace("'", "''");

                    // Script en PowerShell que auto-selecciona voz en español (ej: Microsoft Sabina)
                    string psScript = 
                        "Add-Type -AssemblyName System.Speech; " +
                        "$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                        $"$synth.Rate = {speechRate}; " +
                        $"$synth.Volume = {speechVolume}; " +
                        "$esVoice = $synth.GetInstalledVoices() | Where-Object { $_.VoiceInfo.Culture.Name -like 'es*' -and $_.Enabled } | Select-Object -First 1; " +
                        "if ($esVoice) { $synth.SelectVoice($esVoice.VoiceInfo.Name); } " +
                        $"$synth.Speak('{escapedText}');";

                    // Codificar en Base64 UTF-16LE para -EncodedCommand (inmune a problemas de escape)
                    byte[] scriptBytes = Encoding.Unicode.GetBytes(psScript);
                    string base64Script = Convert.ToBase64String(scriptBytes);

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {base64Script}",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    currentProcess = Process.Start(psi);
                    if (currentProcess == null) return;

                    while (!currentProcess.HasExited)
                    {
                        if (token.IsCancellationRequested)
                        {
                            try { currentProcess.Kill(); } catch { }
                            break;
                        }
                        Thread.Sleep(50);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TextToSpeechService] Excepción en TTS Windows: {ex.Message}");
                }
            }, token);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject ttsObject;

        private void Start()
        {
            InitAndroidTTS();
        }

        private void InitAndroidTTS()
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    ttsObject = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TextToSpeechService] Error inicializando Android TTS: {ex.Message}");
            }
        }

        private void SpeakAndroid(string text)
        {
            if (ttsObject != null)
            {
                ttsObject.Call<int>("speak", text, 0, null, "OllamaTTS");
            }
        }
#endif

        private string CleanTextForSpeech(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Eliminar etiquetas HTML / Rich Text como <color=...> o <b>
            string noTags = Regex.Replace(input, "<.*?>", string.Empty);
            // Eliminar markdown como asteriscos, numerales, backticks
            string noMarkdown = Regex.Replace(noTags, @"[*#`_~]", string.Empty);
            // Reemplazar saltos de línea por pausas con punto
            string cleaned = noMarkdown.Replace("\r\n", ". ").Replace("\n", ". ");

            return cleaned.Trim();
        }

        private void OnDestroy()
        {
            Stop();
        }
    }
}
