using UnityEngine;

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
        else if (comando.Contains("cerrar") || comando.Contains("volver")
                 || comando.Contains("menu"))
            GameManager.instance.MainMenu();
        else if (comando.Contains("eliminar") || comando.Contains("borrar"))
        {
            FindAnyObjectByType<ARInteractionManager>().DeleteItem();
            GameManager.instance.MainMenu(); // vuelve al menú tras eliminar
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