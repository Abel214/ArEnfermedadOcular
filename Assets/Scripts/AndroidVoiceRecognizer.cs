using UnityEngine;
using UnityEngine.Android;

public class AndroidVoiceRecognizer : MonoBehaviour
{
    public static AndroidVoiceRecognizer Instance;
    public System.Action<string> OnCommandRecognized;

    private AndroidJavaObject speechRecognizer;
    private bool isListening = false;
    private bool sesionActiva = false; // Controla si el usuario activó la escucha manualmente

    void Awake()
    {
        Instance = this;
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
    }

    void Start()
    {
        // El micrófono arranca apagado por defecto, esperando la interacción del usuario.
        Debug.Log("=== AndroidVoiceRecognizer en espera de interacción del usuario ===");
    }

    public void IniciarEscuchaManual()
    {
        sesionActiva = true;
        StartListening();
    }

    public void DetenerEscuchaManual()
    {
        sesionActiva = false;
        StopListeningNative();
    }

    private void StopListeningNative()
    {
        speechRecognizer?.Call("stopListening");
        speechRecognizer?.Call("destroy");
        speechRecognizer = null;
        isListening = false;
    }

    public void StartListening()
    {
        if (!sesionActiva || isListening) return;
        isListening = true;
        Debug.Log("=== INICIANDO ESCUCHA MANUAL ===");

        AndroidJavaClass unityPlayer =
            new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity =
            unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            try
            {
                speechRecognizer?.Call("destroy");
                speechRecognizer = null;

                AndroidJavaClass srClass =
                    new AndroidJavaClass("android.speech.SpeechRecognizer");
                speechRecognizer = srClass.CallStatic<AndroidJavaObject>(
                    "createSpeechRecognizer", activity);

                speechRecognizer.Call("setRecognitionListener",
                    new SpeechListener(this));

                AndroidJavaObject intent = new AndroidJavaObject(
                    "android.content.Intent",
                    "android.speech.action.RECOGNIZE_SPEECH");
                intent.Call<AndroidJavaObject>("putExtra",
                    "android.speech.extra.LANGUAGE_MODEL", "free_form");
                intent.Call<AndroidJavaObject>("putExtra",
                    "android.speech.extra.LANGUAGE", "es");
                intent.Call<AndroidJavaObject>("putExtra",
                    "android.speech.extra.MAX_RESULTS", 3);
                intent.Call<AndroidJavaObject>("putExtra",
                    "android.speech.extra.SPEECH_INPUT_COMPLETE_SILENCE_LENGTH_MILLIS", 1500);
                intent.Call<AndroidJavaObject>("putExtra",
                    "android.speech.extra.SPEECH_INPUT_POSSIBLY_COMPLETE_SILENCE_LENGTH_MILLIS", 1500);

                speechRecognizer.Call("startListening", intent);
                Debug.Log("Escuchando activamente...");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error: " + e.Message);
                isListening = false;
                if (sesionActiva)
                    UnityMainThreadDispatcher.Instance()
                        .Enqueue(() => Invoke("StartListening", 1f));
            }
        }));
    }

    public void OnResultReceived(string text)
    {
        isListening = false;
        Debug.Log("✅ Comando reconocido: " + text);
        OnCommandRecognized?.Invoke(text.ToLower().Trim());

        // Si la sesión sigue activa por el usuario, vuelve a escuchar para el siguiente comando
        if (sesionActiva)
            UnityMainThreadDispatcher.Instance()
                .Enqueue(() => Invoke("StartListening", 0.5f));
    }

    public void OnErrorReceived(int errorCode)
    {
        isListening = false;
        Debug.LogWarning("Error de reconocimiento: " + errorCode);

        // Reintentar si el usuario mantiene la sesión activa y no es error de permisos
        if (sesionActiva && errorCode != 9)
            UnityMainThreadDispatcher.Instance()
                .Enqueue(() => Invoke("StartListening", 1f));
    }

    void OnDestroy()
    {
        sesionActiva = false;
        StopListeningNative();
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            StopListeningNative();
        }
    }
}

// ==========================================
// CLASE AUXILIAR PARA ESCUCHAR EVENTOS NATIVOS
// ==========================================
public class SpeechListener : AndroidJavaProxy
{
    private AndroidVoiceRecognizer recognizer;

    public SpeechListener(AndroidVoiceRecognizer r)
        : base("android.speech.RecognitionListener") => recognizer = r;

    public void onResults(AndroidJavaObject bundle)
    {
        try
        {
            AndroidJavaObject results = bundle.Call<AndroidJavaObject>(
                "getStringArrayList", "results_recognition");
            string text = results.Call<string>("get", 0);
            UnityMainThreadDispatcher.Instance()
                .Enqueue(() => recognizer.OnResultReceived(text));
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error: " + e.Message);
        }
    }

    public void onError(int error)
    {
        UnityMainThreadDispatcher.Instance()
            .Enqueue(() => recognizer.OnErrorReceived(error));
    }

    public void onReadyForSpeech(AndroidJavaObject p) { Debug.Log("Listo para escuchar"); }
    public void onBeginningOfSpeech() { }
    public void onRmsChanged(float r) { }
    public void onBufferReceived(AndroidJavaObject b) { }
    public void onEndOfSpeech() { }
    public void onPartialResults(AndroidJavaObject b) { }
    public void onEvent(int t, AndroidJavaObject b) { }
}