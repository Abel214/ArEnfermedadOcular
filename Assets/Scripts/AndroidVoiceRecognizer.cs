using UnityEngine;
using UnityEngine.Android;

public class AndroidVoiceRecognizer : MonoBehaviour
{
    public static AndroidVoiceRecognizer Instance;
    public System.Action<string> OnCommandRecognized;

    private AndroidJavaObject speechRecognizer;
    private bool isListening = false;

    void Awake()
    {
        Instance = this;
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
    }

    public void StartListening()
    {
        if (isListening) return;
        isListening = true;
        Debug.Log("=== INICIANDO ESCUCHA DIRECTA ===");

        AndroidJavaClass unityPlayer =
            new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity =
            unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        // Crear SpeechRecognizer en el hilo principal de Android
        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            try
            {
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
                    "android.speech.extra.LANGUAGE", "es-EC");
                intent.Call<AndroidJavaObject>("putExtra",
                    "android.speech.extra.MAX_RESULTS", 3);

                speechRecognizer.Call("startListening", intent);
                Debug.Log("SpeechRecognizer escuchando...");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error SpeechRecognizer: " + e.Message);
                isListening = false;
            }
        }));
    }

    public void OnResultReceived(string text)
    {
        isListening = false;
        Debug.Log("✅ Comando: " + text);
        OnCommandRecognized?.Invoke(text.ToLower().Trim());

        // Destruir recognizer para liberar recursos
        speechRecognizer?.Call("destroy");
        speechRecognizer = null;
    }

    public void OnErrorReceived(int errorCode)
    {
        isListening = false;
        Debug.LogWarning("Error reconocimiento: " + errorCode);
        speechRecognizer?.Call("destroy");
        speechRecognizer = null;
    }

    void OnDestroy()
    {
        speechRecognizer?.Call("destroy");
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
            Debug.LogError("Error parseando resultado: " + e.Message);
        }
    }

    public void onError(int error)
    {
        UnityMainThreadDispatcher.Instance()
            .Enqueue(() => recognizer.OnErrorReceived(error));
    }

    public void onReadyForSpeech(AndroidJavaObject p) { }
    public void onBeginningOfSpeech() { }
    public void onRmsChanged(float r) { }
    public void onBufferReceived(AndroidJavaObject b) { }
    public void onEndOfSpeech() { }
    public void onPartialResults(AndroidJavaObject b) { }
    public void onEvent(int t, AndroidJavaObject b) { }
}