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
        else if (comando.Contains("volver") || comando.Contains("salir")
                 || comando.Contains("menu principal"))
        {
            StartCoroutine(VolverAlMenu());
        }
        else if (comando.Contains("cerrar") || comando.Contains("menu"))
        {
            GameManager.instance.MainMenu();
        }
        else if (comando.Contains("eliminar") || comando.Contains("borrar"))
        {
            FindAnyObjectByType<ARInteractionManager>().DeleteItem();
            GameManager.instance.MainMenu();
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

    IEnumerator VolverAlMenu()
    {
        Debug.Log("Volviendo al menú principal...");

        // Detener escucha continua antes de cambiar escena
        if (AndroidVoiceRecognizer.Instance != null)
            AndroidVoiceRecognizer.Instance.DetenerEscuchaContinua();

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene("MenuPrincipal");
    }

    void ColocarModeloPorNombre(string nombre)
    {
        ItemButtonManager[] botones = FindObjectsByType<ItemButtonManager>();
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
        AndroidVoiceRecognizer.Instance.StartListening();
    }

    void OnDestroy()
    {
        if (AndroidVoiceRecognizer.Instance != null)
            AndroidVoiceRecognizer.Instance.OnCommandRecognized -= ProcesarComando;
    }
}