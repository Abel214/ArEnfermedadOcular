using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BotonMicrofonoController : MonoBehaviour
{
    [Header("Referencias Visuales")]
    [SerializeField] private Image imagenIconoMicrofono; // El componente Image del hijo IconoMicrofono
    [SerializeField] private Sprite spriteMicOff;        // Icono apagado (tachado)
    [SerializeField] private Sprite spriteMicOn;         // Icono encendido (activo)
    [SerializeField] private TMP_Text textoGuia;         // El texto de indicaciones central

    [Header("Anillo de Resplandor")]
    [SerializeField] private GameObject anilloPulso;     // El halo circular exterior

    private bool estaEscuchando = false;
    private Tweener pulsoTween;

    void Start()
    {
        // Estado inicial obligatorio: Apagado, texto visible y sin escuchar
        if (imagenIconoMicrofono != null && spriteMicOff != null)
        {
            imagenIconoMicrofono.sprite = spriteMicOff;
            imagenIconoMicrofono.gameObject.SetActive(true);
        }
        if (anilloPulso != null)
        {
            anilloPulso.SetActive(false);
        }
        if (textoGuia != null)
        {
            textoGuia.gameObject.SetActive(true);
            textoGuia.alpha = 0.8f;
        }
    }

    public void AlternarEscucha()
    {
        estaEscuchando = !estaEscuchando;

        if (estaEscuchando)
        {
            // Cambiar a icono encendido
            if (imagenIconoMicrofono != null && spriteMicOn != null)
            {
                imagenIconoMicrofono.sprite = spriteMicOn;
                imagenIconoMicrofono.gameObject.SetActive(true);
            }

            // Ocultar texto guía suavemente
            //if (textoGuia != null)
            //{
              //  textoGuia.DOFade(0f, 0.3f);
            //}

            // Activar halo de pulso
            if (anilloPulso != null)
                anilloPulso.SetActive(true);

            // Animación de pulso continuo con DOTween
            pulsoTween = transform.DOScale(1.12f, 0.5f).SetLoops(-1, LoopType.Yoyo);

            // Activar el reconocimiento de voz manual en Android
            if (AndroidVoiceRecognizer.Instance != null)
            {
                AndroidVoiceRecognizer.Instance.IniciarEscuchaManual();
            }

            Debug.Log("Micrófono activado por usuario: Escuchando comando...");
        }
        else
        {
            // Cambiar a icono apagado
            if (imagenIconoMicrofono != null && spriteMicOff != null)
            {
                imagenIconoMicrofono.sprite = spriteMicOff;
                imagenIconoMicrofono.gameObject.SetActive(true);
            }

            // Mostrar nuevamente el texto guía
            if (textoGuia != null)
            {
                textoGuia.gameObject.SetActive(true);
                textoGuia.DOFade(0.8f, 0.3f);
            }

            // Desactivar halo
            if (anilloPulso != null)
                anilloPulso.SetActive(false);

            // Detener animación de pulso y regresar a escala normal
            if (pulsoTween != null)
                pulsoTween.Kill();
            
            transform.localScale = Vector3.one;

            // Detener el reconocimiento de voz
            if (AndroidVoiceRecognizer.Instance != null)
            {
                AndroidVoiceRecognizer.Instance.DetenerEscuchaManual();
            }

            Debug.Log("Micrófono desactivado por usuario.");
        }
    }
}