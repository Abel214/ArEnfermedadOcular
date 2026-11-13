using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class VoiceComanderManage : MonoBehaviour
{
    [Header("Menús")]
    public GameObject itemsMenuCanvas;
    public GameObject arPositionCanvas;
    public GameObject micIcon;

    void Start()
    {
        if (AndroidVoiceRecognizer.Instance == null)
            Debug.LogError("AndroidVoiceRecognizer no encontrado!");
        else
        {
            AndroidVoiceRecognizer.Instance.OnCommandRecognized += ProcesarComando;
            Debug.Log("=== VoiceCommandManager iniciado ===");
        }
    }

    void ProcesarComando(string comando)
    {
        Debug.Log("=== COMANDO RECIBIDO: " + comando + " ===");

        bool silenciarVoz = comando.Contains("silencio") || comando.Contains("callar")
                            || comando.Contains("detener") || comando.Contains("basta");

        var tts = FindAnyObjectByType<OllamaVoice.TextToSpeechService>(FindObjectsInactive.Include);
        bool ttsHablando = tts != null && tts.IsSpeaking;

        // Mientras el asistente esté hablando, solo se acepta el comando de
        // silencio. Todo lo demás que escuche el micrófono es eco de la propia
        // voz del asistente y debe ignorarse para no disparar comandos falsos.
        if (ttsHablando)
        {
            if (silenciarVoz) SilenciarVoz();
            return;
        }

        if (silenciarVoz)
        {
            SilenciarVoz();
            return;
        }

        if (comando.Contains("colocar") || comando.Contains("modelo")
            || comando.Contains("objeto") || comando.Contains("mostrar"))
        {
            GameManager.instance.ItemsMenu();
        }
        else if (comando.Contains("catarata"))
            ColocarModeloPorNombre("catarata");
        else if (comando.Contains("conjuntivitis"))
            ColocarModeloPorNombre("conjuntivitis");
        else if (comando.Contains("glaucoma"))
            ColocarModeloPorNombre("glaucoma");
        else if (comando.Contains("sano") || comando.Contains("normal"))
            ColocarModeloPorNombre("sano");

        // NUEVOS COMANDOS DE ROTACIÓN POR VOZ (Izquierda / Derecha)
        else if (comando.Contains("girar izquierda") || comando.Contains("izquierda"))
        {
            FindAnyObjectByType<ARInteractionManager>()?.RotarModeloPorVoz(-1f); // Negativo para girar a la izquierda
        }
        else if (comando.Contains("girar derecha") || comando.Contains("derecha"))
        {
            FindAnyObjectByType<ARInteractionManager>()?.RotarModeloPorVoz(1f);  // Positivo para girar a la derecha
        }
        // FIN DE NUEVOS COMANDOS

        else if (comando.Contains("volver") || comando.Contains("salir")
                 || comando.Contains("menu principal"))
        {
            StartCoroutine(VolverAlMenu());
        }
        else if (comando.Contains("explotar") ||
         comando.Contains("separar") ||
         comando.Contains("capas"))
        {
            FindAnyObjectByType<OjoExplodedView>()?.ExplotarVista();
        }
        else if (comando.Contains("reiniciar") ||
                 comando.Contains("vista completa") ||
                 comando.Contains("ver completo") ||
                 comando.Contains("mostrar todo"))
        {
            FindAnyObjectByType<OjoExplodedView>()?.ReiniciarModeloCompleto();
        }
        else if (comando.Contains("unir") ||
                 comando.Contains("cerrar capas"))
        {
            FindAnyObjectByType<OjoExplodedView>()?.UnirVista();
        }
        else if (comando.Contains("siguiente capa") ||
                 comando.Contains("siguiente"))
        {
            FindAnyObjectByType<OjoExplodedView>()?.MostrarSiguienteCapa();
        }
        else if (comando.Contains("capa anterior") ||
                 comando.Contains("anterior"))
        {
            FindAnyObjectByType<OjoExplodedView>()?.MostrarCapaAnterior();
        }
        else if (comando.Contains("cerrar chat"))
        {
            CerrarChat();
        }
        else if (comando.Contains("abrir chat") || comando.Contains("chat")
                 || comando.Contains("asistente"))
        {
            AbrirChat();
        }
        else if (comando.Contains("cerrar") || comando.Contains("menu"))
        {
            GameManager.instance.MainMenu();
        }
        else if (comando.Contains("eliminar") || comando.Contains("borrar"))
        {
            FindAnyObjectByType<ARInteractionManager>()?.DeleteItem();
            return;
        }
        else if (comando.Contains("explica") || comando.Contains("información")
                 || comando.Contains("que es") || comando.Contains("qué es"))
            FindAnyObjectByType<ARMedicalAI>().AskAboutCurrentItem();
        else if (comando.Contains("síntomas") || comando.Contains("sintomas"))
            FindAnyObjectByType<ARMedicalAI>()
                .AskAboutCurrentItem("¿Cuáles son los síntomas?");
        else if (comando.Contains("tratamiento"))
            FindAnyObjectByType<ARMedicalAI>()
                .AskAboutCurrentItem("¿Cuál es el tratamiento?");
    }

    // ═══════════════════════════════════════════════════════
    // COMANDO DE VOZ: SILENCIAR LA VOZ DEL ASISTENTE (TTS)
    // ═══════════════════════════════════════════════════════
    void SilenciarVoz()
    {
        var tts = FindAnyObjectByType<OllamaVoice.TextToSpeechService>(FindObjectsInactive.Include);
        if (tts != null && tts.IsSpeaking)
        {
            tts.Stop();
            Debug.Log("Voz del asistente silenciada por comando de voz.");
        }
    }

    // ═══════════════════════════════════════════════════════
    // COMANDO DE VOZ: ABRIR / CERRAR EL CHAT DEL ASISTENTE
    // (Ollama_Chat_Canvas / Ollama_Chat_Panel)
    // ═══════════════════════════════════════════════════════
    void AbrirChat()
    {
        var chatUI = FindAnyObjectByType<OllamaUI.ChatUIController>(FindObjectsInactive.Include);
        if (chatUI == null)
        {
            Debug.LogWarning("No se encontró Ollama_Chat_Canvas (ChatUIController).");
            return;
        }

        // Sin teclado: si el input toma foco, el teclado de Android roba el
        // foco de audio y corta el reconocimiento de voz nativo.
        chatUI.OpenChatPanel(false);

        // Reactivar la sesión de comandos de voz en el siguiente frame:
        // hacerlo dentro del callback del propio SpeechRecognizer destruye
        // el recognizer mientras está entregando su resultado y la escucha
        // muere ("abrir chat" y luego no detecta nada).
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (AndroidVoiceRecognizer.Instance != null)
                AndroidVoiceRecognizer.Instance.IniciarEscuchaManual();
        });

        Debug.Log("Chat del asistente abierto por comando de voz.");
    }

    void CerrarChat()
    {
        var chatUI = FindAnyObjectByType<OllamaUI.ChatUIController>(FindObjectsInactive.Include);
        if (chatUI != null)
            chatUI.CloseChatPanel();
        else
            Debug.LogWarning("No se encontró Ollama_Chat_Canvas (ChatUIController).");
    }

    IEnumerator VolverAlMenu()
    {
        Debug.Log("Volviendo al menú principal...");

        if (AndroidVoiceRecognizer.Instance != null)
            AndroidVoiceRecognizer.Instance.DetenerEscuchaManual();

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene("MenuPrincipal");
    }

    void ColocarModeloPorNombre(string nombre)
    {
        ItemButtonManager[] botones = FindObjectsByType<ItemButtonManager>(FindObjectsSortMode.None);
        foreach (var boton in botones)
        {
            if (boton.name.ToLower().Contains(nombre))
            {
                boton.Create3DModel();
                GameManager.instance.ArPosition();
                return;
            }
        }
        Debug.LogWarning("No encontrado: " + nombre);
    }

    public void BotonMicrofono()
    {
        if (micIcon) micIcon.SetActive(true);
        if (AndroidVoiceRecognizer.Instance != null)
            AndroidVoiceRecognizer.Instance.IniciarEscuchaManual();
    }

    void OnDestroy()
    {
        AndroidVoiceRecognizer.Instance.OnCommandRecognized -= ProcesarComando;
    }
}