using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ARMedicalAI : MonoBehaviour
{
    [Header("Referencias")]
    private ARInteractionManager arInteractionManager;

    [Header("UI")]
    public GameObject aiPanel;
    public Button microphoneButton;
    public Button closeAIButton;
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI answerText;
    public TextMeshProUGUI statusText;
    public GameObject loadingIndicator;
    private string simulatedQuestion;


    [Header("API")]
    public string geminiApiKey = ""; // Configura en Inspector
    private const string GEMINI_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

    private Item currentItem; // ScriptableObject del modelo actual
    private GameObject currentModel; // GameObject instanciado
    private bool isListening = false;

    void Start()
    {
        arInteractionManager = FindAnyObjectByType<ARInteractionManager>();

        // Inicializar UI
        aiPanel.SetActive(false);
        loadingIndicator.SetActive(false);

        // Botones
        microphoneButton.onClick.AddListener(StartVoiceRecognition);
        closeAIButton.onClick.AddListener(CloseAIPanel);

        // Suscribirse a eventos
        GameManager.instance.OnArPosition += ShowAIPanel;
        GameManager.instance.OnMainMenu += HideAIPanel;
    }

    // Método público para que ARInteractionManager pase el Item
    public void SetCurrentItem(Item item, GameObject modelInstance)
    {
        currentItem = item;
        currentModel = modelInstance;
        Debug.Log($"IA configurada para: {item.itemName}");
    }

    void ShowAIPanel()
    {
        if (currentItem != null)
        {
            aiPanel.SetActive(true);
            statusText.text = $"Pregunta sobre {currentItem.itemName}";
        }
    }

    void HideAIPanel()
    {
        aiPanel.SetActive(false);
        currentItem = null;
        currentModel = null;
    }

    void CloseAIPanel()
    {
        aiPanel.SetActive(false);
    }

    public void StartVoiceRecognition()
    {
        if (isListening || currentItem == null) return;

        isListening = true;
        statusText.text = "🎤 Escuchando...";
        microphoneButton.interactable = false;

#if UNITY_ANDROID
        StartAndroidSpeechRecognition();
#elif UNITY_IOS
        StartIOSSpeechRecognition();
#else
        // Simulación en Editor
        SimulateSpeechRecognition();
#endif
    }

    void StartAndroidSpeechRecognition()
    {
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent",
                "android.speech.action.RECOGNIZE_SPEECH");

            intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE_MODEL", "free_form");
            intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE", "es-ES");
            intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.PROMPT",
                $"Pregunta sobre {currentItem.itemName}");

            currentActivity.Call("startActivityForResult", intent, 100);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error en reconocimiento de voz: " + e.Message);
            OnSpeechRecognitionFailed();
        }
    }

    void StartIOSSpeechRecognition()
    {
        // Para iOS necesitarás un plugin
        SimulateSpeechRecognition();
    }

    void SimulateSpeechRecognition()
    {
        string viewType = currentItem.isAnatomyView ? "anatomía interna" : "vista externa";

        string[] questions = new string[]
        {
        $"¿Qué es {currentItem.itemName}?",
        $"¿Cuáles son los síntomas de {currentItem.itemName}?",
        $"¿Cómo se trata {currentItem.itemName}?",
        $"Explícame la {viewType} que estoy viendo",
        $"¿Qué partes del ojo afecta {currentItem.itemName}?"
        };

        simulatedQuestion = questions[UnityEngine.Random.Range(0, questions.Length)];
        Invoke(nameof(InvokeSimulatedQuestion), 1.5f);
    }


    public void OnSpeechRecognized(string recognizedText)
    {
        isListening = false;
        microphoneButton.interactable = true;

        questionText.text = "📝 " + recognizedText;
        statusText.text = "🤔 Analizando con IA...";
        loadingIndicator.SetActive(true);

        StartCoroutine(CaptureAndAskAI(recognizedText));
    }

    void OnSpeechRecognitionFailed()
    {
        isListening = false;
        microphoneButton.interactable = true;
        statusText.text = "❌ Error al escuchar. Intenta de nuevo.";
    }

    IEnumerator CaptureAndAskAI(string question)
    {
        yield return new WaitForEndOfFrame();

        // Capturar screenshot del modelo AR
        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        byte[] imageBytes = screenshot.EncodeToJPG(70);
        Destroy(screenshot);

        yield return StartCoroutine(SendToGemini(question, imageBytes));
    }
    void InvokeSimulatedQuestion()
    {
        OnSpeechRecognized(simulatedQuestion);
    }


    IEnumerator SendToGemini(string question, byte[] imageBytes)
    {
        string imageBase64 = System.Convert.ToBase64String(imageBytes);

        string viewContext = currentItem.isAnatomyView ?
            "vista INTERNA (anatomía)" :
            "vista EXTERNA";

        string fullPrompt = $@"
Eres un asistente médico educativo especializado en oftalmología.

CONTEXTO:
- Enfermedad: {currentItem.itemName}
- Tipo de vista: {viewContext}
- Descripción: {currentItem.itemDescription}

INFORMACIÓN MÉDICA DETALLADA:
{currentItem.medicalContext}

El usuario está viendo un modelo 3D en Realidad Aumentada.
Responde de forma clara, educativa y en máximo 3 párrafos.
Menciona las partes del ojo visibles en el modelo cuando sea relevante.

PREGUNTA DEL USUARIO: {question}
";

        string jsonPayload = @"{
            ""contents"": [{
                ""parts"": [
                    {""text"": """ + EscapeJson(fullPrompt) + @"""},
                    {""inline_data"": {
                        ""mime_type"": ""image/jpeg"",
                        ""data"": """ + imageBase64 + @"""
                    }}
                ]
            }]
        }";

        using (UnityWebRequest request = new UnityWebRequest(GEMINI_URL + "?key=" + geminiApiKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            loadingIndicator.SetActive(false);

            if (request.result == UnityWebRequest.Result.Success)
            {
                string answer = ParseGeminiResponse(request.downloadHandler.text);
                answerText.text = "💡 " + answer;
                statusText.text = "🔊 Reproduciendo respuesta...";

                SpeakText(answer);
            }
            else
            {
                Debug.LogError("Error Gemini: " + request.error);
                statusText.text = "❌ Error de conexión";
                answerText.text = "No se pudo obtener respuesta. Verifica tu conexión a internet.";
            }

            yield return new WaitForSeconds(2f);
            statusText.text = "Presiona el micrófono para otra pregunta";
        }
    }

    string ParseGeminiResponse(string json)
    {
        try
        {
            int textIndex = json.IndexOf("\"text\": \"");
            if (textIndex == -1) return "No se pudo procesar la respuesta.";

            int start = textIndex + 9;
            int end = json.IndexOf("\"", start);

            if (end == -1) return "Respuesta inválida.";

            string text = json.Substring(start, end - start);
            return text.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\t", " ");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Parse error: " + e.Message);
            return "Error al interpretar la respuesta.";
        }
    }

    void SpeakText(string text)
    {
#if UNITY_ANDROID
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            
            AndroidJavaObject tts = new AndroidJavaObject(
                "android.speech.tts.TextToSpeech", 
                currentActivity, 
                null
            );
            
            AndroidJavaObject locale = new AndroidJavaObject("java.util.Locale", "es", "ES");
            tts.Call<int>("setLanguage", locale);
            tts.Call<int>("speak", text, 0, null, null);
            
            Debug.Log("TTS reproducido");
        }
        catch (System.Exception e)
        {
            Debug.LogError("TTS Error: " + e.Message);
        }
#else
        Debug.Log("TTS: " + text);
#endif
    }

    string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        return text.Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("\n", "\\n")
                   .Replace("\r", "")
                   .Replace("\t", "\\t");
    }
}