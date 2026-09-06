using UnityEngine;
using UnityEngine.Android;

public class AndroidVoiceRecognizer : MonoBehaviour
{
    public static AndroidVoiceRecognizer Instance;
    public System.Action<string> OnCommandRecognized;

    private AndroidJavaObject speechRecognizer;
    private bool isListening = false;
    private bool escuchaContinua = true; // ← nuevo

    void Awake()
    {
        Instance = this;
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
    }

    void Start()
    {
        // Iniciar escucha automática al arrancar
        Invoke("IniciarEscuchaContinua", 2f);
    }

    public void IniciarEscuchaContinua()
    {
        escuchaContinua = true;
        StartListening();
    }

    public void DetenerEscuchaContinua()
    {
        escuchaContinua = false;
        speechRecognizer?.Call("stopListening");
        speechRecognizer?.Call("destroy");
        speechRecognizer = null;
        isListening = false;
    }

    public void StartListening()
    {
        if (isListening) return;
        isListening = true;
        Debug.Log("=== INICIANDO ESCUCHA ===");

        AndroidJavaClass unityPlayer =
            new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity =
            unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            try
            {
                // Destruir instancia anterior
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
                Debug.Log("Escuchando...");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error: " + e.Message);
                isListening = false;
                // Reintentar si es continua
                if (escuchaContinua)
                    UnityMainThreadDispatcher.Instance()
                        .Enqueue(() => Invoke("StartListening", 1f));
            }
        }));
    }

    public void OnResultReceived(string text)
    {
        isListening = false;
        Debug.Log("✅ Comando: " + text);
        OnCommandRecognized?.Invoke(text.ToLower().Trim());

        // Reiniciar escucha automáticamente
        if (escuchaContinua)
            UnityMainThreadDispatcher.Instance()
                .Enqueue(() => Invoke("StartListening", 0.8f));
    }

    public void OnErrorReceived(int errorCode)
    {
        isListening = false;
        Debug.LogWarning("Error: " + errorCode);

        // Reiniciar en errores recuperables
        if (escuchaContinua && errorCode != 9) // 9 = sin permiso
            UnityMainThreadDispatcher.Instance()
                .Enqueue(() => Invoke("StartListening", 1f));
    }

    // El botón ahora solo es indicador visual
    public void BotonMicrofono()
    {
        if (isListening)
        {
            Debug.Log("Ya estoy escuchando...");
            return;
        }
        StartListening();
    }

    void OnDestroy()
    {
        escuchaContinua = false;
        speechRecognizer?.Call("destroy");
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            escuchaContinua = false;
            speechRecognizer?.Call("stopListening");
        }
        else
        {
            escuchaContinua = true;
            Invoke("StartListening", 1.5f);
        }
    }
}

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

    public void onReadyForSpeech(AndroidJavaObject p)
    {
        Debug.Log("Listo para escuchar");
    }
    public void onBeginningOfSpeech() { }
    public void onRmsChanged(float r) { }
    public void onBufferReceived(AndroidJavaObject b) { }
    public void onEndOfSpeech() { }
    public void onPartialResults(AndroidJavaObject b) { }
    public void onEvent(int t, AndroidJavaObject b) { }
}