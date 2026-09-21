using System;
using UnityEngine;

namespace OllamaVoice
{
    public class MicRecorder : MonoBehaviour
    {
        [Header("Configuración de Grabación")]
        [Tooltip("Frecuencia de muestreo (16000 Hz es óptimo para Whisper)")]
        [SerializeField] private int sampleRate = 16000;

        [Tooltip("Duración máxima de grabación en segundos")]
        [SerializeField] private int maxDurationSeconds = 30;

        private string selectedDevice;
        private AudioClip recordingClip;
        private bool isRecording = false;
        private float recordingStartTime = 0f;

        public bool IsRecording => isRecording;
        public float RecordingDuration => isRecording ? (Time.time - recordingStartTime) : 0f;
        public string SelectedDevice => selectedDevice;

        private void Start()
        {
            if (Microphone.devices.Length > 0)
            {
                selectedDevice = Microphone.devices[0];
                Debug.Log($"[MicRecorder] Micrófono seleccionado: {selectedDevice}");
            }
            else
            {
                Debug.LogWarning("[MicRecorder] No se detectó ningún micrófono conectado en el sistema.");
            }
        }

        /// <summary>
        /// Inicia la grabación del micrófono.
        /// </summary>
        public bool StartRecording()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("[MicRecorder] No se puede grabar: no hay micrófonos disponibles.");
                return false;
            }

            if (isRecording)
            {
                Debug.LogWarning("[MicRecorder] Ya se está grabando.");
                return false;
            }

            selectedDevice = Microphone.devices[0];
            recordingClip = Microphone.Start(selectedDevice, false, maxDurationSeconds, sampleRate);
            recordingStartTime = Time.time;
            isRecording = true;

            Debug.Log($"[MicRecorder] Grabando audio desde '{selectedDevice}' a {sampleRate} Hz (máx: {maxDurationSeconds}s)...");
            return true;
        }

        /// <summary>
        /// Detiene la grabación y devuelve el AudioClip recortado al tiempo exacto hablado.
        /// </summary>
        public AudioClip StopRecording()
        {
            if (!isRecording)
            {
                Debug.LogWarning("[MicRecorder] No hay ninguna grabación activa para detener.");
                return null;
            }

            int lastPosition = Microphone.GetPosition(selectedDevice);
            Microphone.End(selectedDevice);
            isRecording = false;

            if (recordingClip == null)
            {
                Debug.LogError("[MicRecorder] El clip de grabación es nulo.");
                return null;
            }

            if (lastPosition <= 0)
            {
                Debug.LogWarning("[MicRecorder] La grabación fue demasiado corta o no se detectaron muestras.");
                return null;
            }

            // Recortar el AudioClip para incluir únicamente las muestras capturadas
            AudioClip trimmedClip = TrimClip(recordingClip, lastPosition);
            Debug.Log($"[MicRecorder] Grabación finalizada. Duración: {trimmedClip.length:F2}s ({lastPosition} muestras).");
            return trimmedClip;
        }

        /// <summary>
        /// Obtiene el nivel actual de volumen del micrófono (0 a 1) para animaciones o indicadores.
        /// </summary>
        public float GetMicLevel(int sampleCount = 128)
        {
            if (!isRecording || recordingClip == null) return 0f;

            int micPosition = Microphone.GetPosition(selectedDevice);
            if (micPosition < sampleCount) return 0f;

            float[] waveData = new float[sampleCount];
            recordingClip.GetData(waveData, micPosition - sampleCount);

            float sum = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                sum += Mathf.Abs(waveData[i]);
            }

            return sum / sampleCount;
        }

        private AudioClip TrimClip(AudioClip original, int samplesRecorded)
        {
            float[] data = new float[samplesRecorded * original.channels];
            original.GetData(data, 0);

            AudioClip trimmed = AudioClip.Create("Recorded_Voice", samplesRecorded, original.channels, original.frequency, false);
            trimmed.SetData(data, 0);
            return trimmed;
        }

        private void OnDisable()
        {
            if (isRecording)
            {
                StopRecording();
            }
        }
    }
}
