using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// Botón de la pantalla de inicio que vuelve al menú principal
/// (la pantalla con el logo y las opciones Tutorial / Empezar / Salir).
/// </summary>
public class BotonVolverInicio : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena del menú principal.")]
    public string escenaInicio = "MenuPrincipal";

    public void VolverAlInicio()
    {
        // Feedback visual del toque.
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 6, 0.6f);

        // Cortar la voz del asistente para que no siga hablando al cambiar de escena.
        var tts = FindAnyObjectByType<OllamaVoice.TextToSpeechService>(FindObjectsInactive.Include);
        if (tts != null && tts.IsSpeaking)
            tts.Stop();

        // Detener el reconocimiento de voz antes de salir.
        if (AndroidVoiceRecognizer.Instance != null)
            AndroidVoiceRecognizer.Instance.DetenerEscuchaManual();

        Debug.Log("Volviendo al menú principal desde el botón.");
        SceneManager.LoadScene(escenaInicio);
    }
}
