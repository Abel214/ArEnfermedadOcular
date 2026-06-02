using UnityEngine;

public class VoiceComanderManage : MonoBehaviour
{
    [Header("Menús")]
    public GameObject itemsMenuCanvas;
    public GameObject arPositionCanvas;

    [Header("Indicador visual")]
    public GameObject micIcon;

    void Start()
    {
        Debug.Log("=== VoiceCommandManager iniciado ===");

        if (AndroidVoiceRecognizer.Instance == null)
            Debug.LogError("AndroidVoiceRecognizer no encontrado!");
        else
        {
            AndroidVoiceRecognizer.Instance.OnCommandRecognized += ProcesarComando;
            Debug.Log("=== Evento suscrito correctamente ===");
        }

        itemsMenuCanvas.SetActive(false);
        arPositionCanvas.SetActive(false);
    }

    void ProcesarComando(string comando)
    {
        Debug.Log("=== COMANDO RECIBIDO: " + comando + " ===");
        if (micIcon) micIcon.SetActive(false);

        if (comando.Contains("colocar") || comando.Contains("modelo")
            || comando.Contains("objeto") || comando.Contains("mostrar"))
        {
            Debug.Log("=== ABRIENDO MENU ===");
            itemsMenuCanvas.SetActive(true);
        }
        else if (comando.Contains("catarata"))
            ColocarModelo("catarata");
        else if (comando.Contains("conjuntivitis"))
            ColocarModelo("conjuntivitis");
        else if (comando.Contains("glaucoma"))
            ColocarModelo("glaucoma");
        else if (comando.Contains("sano") || comando.Contains("normal"))
            ColocarModelo("sano");
        else if (comando.Contains("anatomia") || comando.Contains("anatomía"))
            ColocarModelo("anatomia");
        else if (comando.Contains("cerrar") || comando.Contains("ocultar"))
            itemsMenuCanvas.SetActive(false);
        else if (comando.Contains("eliminar") || comando.Contains("borrar"))
            EliminarModelo();
        else if (comando.Contains("explica") || comando.Contains("qué es")
         || comando.Contains("que es") || comando.Contains("información"))
        {
            FindAnyObjectByType<ARMedicalAI>().AskAboutCurrentItem();
        }
        else if (comando.Contains("síntomas") || comando.Contains("sintomas"))
        {
            FindAnyObjectByType<ARMedicalAI>()
                .AskAboutCurrentItem("¿Cuáles son los síntomas principales?");
        }
        else if (comando.Contains("tratamiento") || comando.Contains("cura"))
        {
            FindAnyObjectByType<ARMedicalAI>()
                .AskAboutCurrentItem("¿Cuál es el tratamiento?");
        }
        else if (comando.Contains("causa") || comando.Contains("por qué"))
        {
            FindAnyObjectByType<ARMedicalAI>()
                .AskAboutCurrentItem("¿Cuáles son las causas?");
        }
    }
    void ColocarModelo(string nombre)
    {
        itemsMenuCanvas.SetActive(false);
        FindAnyObjectByType<ARInteractionManager>().ColocarPorNombre(nombre);
    }

    void EliminarModelo()
    {
        FindAnyObjectByType<ARInteractionManager>().EliminarModelo();
    }

    public void BotonMicrofono()
    {
        if (micIcon) micIcon.SetActive(true);
        AndroidVoiceRecognizer.Instance.StartListening();
    }

    void OnDestroy()
    {
        if (AndroidVoiceRecognizer.Instance != null)
            AndroidVoiceRecognizer.Instance.OnCommandRecognized -=
                ProcesarComando;
    }
}