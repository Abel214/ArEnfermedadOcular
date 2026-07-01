using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ARMedicalAI : MonoBehaviour
{
    [Header("API")]
    public string geminiApiKey = "";
    private const string GEMINI_URL = 
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

    private Item currentItem;
    private GameObject currentModel;
    private bool isProcessing = false;

    void Start()
    {
        GameManager.instance.OnMainMenu += HideAIPanel;
    }

    public void SetCurrentItem(Item item, GameObject modelInstance)
    {
        currentItem = item;
        currentModel = modelInstance;
        Debug.Log($"IA configurada para: {item.itemName}");

        // Dar bienvenida por voz al colocar el modelo
        SpeakText($"Modelo de {item.itemName} colocado. " +
                  $"Di 'explica', 'síntomas' o 'tratamiento' para más información.");
    }

    // Llamado desde VoiceCommandManager
    public void AskAboutCurrentItem(string pregunta = "")
    {
        if (currentItem == null)
        {
            SpeakText("Primero coloca un modelo médico en la escena.");
            return;
        }
        if (isProcessing)
        {
            SpeakText("Un momento, procesando tu consulta anterior.");
            return;
        }

        string prompt = string.IsNullOrEmpty(pregunta)
            ? $"Explica brevemente en 2 oraciones qué es {currentItem.itemName} " +
              $"para un paciente. Contexto: {currentItem.medicalContext}"
            : $"Responde en máximo 3 oraciones esta pregunta sobre " +
              $"{currentItem.itemName}: {pregunta}. " +
              $"Contexto médico: {currentItem.medicalContext}";

        StartCoroutine(SendToGemini(prompt));
    }

    IEnumerator SendToGemini(string prompt)
    {
        isProcessing = true;
        SpeakText("Consultando información médica...");

        string jsonPayload = $@"{{
            ""contents"": [{{
                ""parts"": [{{
                    ""text"": ""{EscapeJson(prompt)}""
                }}]
            }}]
        }}";

        using (UnityWebRequest request = 
            new UnityWebRequest(GEMINI_URL + "?key=" + geminiApiKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string answer = ParseGeminiResponse(request.downloadHandler.text);
                Debug.Log("Respuesta Gemini: " + answer);
                SpeakText(answer);
            }
            else
            {
                Debug.LogError("Error Gemini: " + request.error);
                SpeakText("No se pudo obtener información. Verifica tu conexión.");
            }
        }

        isProcessing = false;
    }

    void SpeakText(string text)
    {
#if UNITY_ANDROID
        try
        {
            AndroidJavaClass unityPlayer = 
                new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = 
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject tts = new AndroidJavaObject(
                "android.speech.tts.TextToSpeech", activity, null);

            AndroidJavaObject locale = 
                new AndroidJavaObject("java.util.Locale", "es", "ES");
            tts.Call<int>("setLanguage", locale);
            tts.Call<int>("speak", text, 0, null, null);
        }
        catch (System.Exception e)
        {
            Debug.LogError("TTS Error: " + e.Message);
        }
#else
        // En el Editor solo muestra en consola
        Debug.Log("🔊 TTS: " + text);
#endif
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
            return json.Substring(start, end - start)
                       .Replace("\\n", " ")
                       .Replace("\\\"", "\"")
                       .Replace("\\t", " ");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Parse error: " + e.Message);
            return "Error al interpretar la respuesta.";
        }
    }

    void HideAIPanel()
    {
        currentItem = null;
        currentModel = null;
        isProcessing = false;
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