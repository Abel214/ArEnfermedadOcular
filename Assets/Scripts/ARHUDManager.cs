using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class ARHUDManager : MonoBehaviour
{
    [Header("Siempre visible")]
    public GameObject btnMicrofono;
    public TMP_Text textoGuia;

    [Header("Aparecen temporalmente")]
    public GameObject btnEliminar;
    public GameObject btnCaptura;
    public GameObject btnVolver;

    [Header("Estados del micrófono")]
    public Image imgMicrofono;
    public Color colorIdle;       // Color inicial / inactivo
    public Color colorEscuchando; // Color activo / escuchando

    private bool modeloColocado = false;
    private Coroutine ocultarCoroutine;

    void Start()
    {
        // Solo micrófono visible al inicio
        OcultarBotonesSecundarios(false);
        if (textoGuia != null)
        {
            textoGuia.DOFade(0.4f, 0.5f); // texto sutil
        }
    }

    // ─── MICRÓFONO TOCADO ────────────────────────
    public void OnMicrofonoTocado()
    {
        // Cambiar color a escuchando si la imagen está asignada
        if (imgMicrofono != null)
        {
            imgMicrofono.DOColor(colorEscuchando, 0.2f);
        }

        if (textoGuia != null)
        {
            textoGuia.DOFade(0f, 0.3f); // ocultar texto guía temporalmente
        }

        // CORREGIDO: Activar reconocimiento de voz manual sincronizado
        if (AndroidVoiceRecognizer.Instance != null)
        {
            AndroidVoiceRecognizer.Instance.IniciarEscuchaManual();
        }
    }

    // ─── CUANDO TERMINA DE ESCUCHAR ──────────────
    public void OnTerminoEscucha()
    {
        if (imgMicrofono != null)
        {
            imgMicrofono.DOColor(colorIdle, 0.3f);
        }
    }

    // ─── CUANDO SE COLOCA UN MODELO ──────────────
    public void OnModeloColocado()
    {
        modeloColocado = true;
        if (textoGuia != null)
        {
            textoGuia.DOFade(0f, 0.3f);
        }

        // Mostrar botones secundarios brevemente
        MostrarBotonesSecundarios();
    }

    // ─── MOSTRAR BOTONES SECUNDARIOS ─────────────
    public void MostrarBotonesSecundarios()
    {
        if (ocultarCoroutine != null)
            StopCoroutine(ocultarCoroutine);

        if (btnEliminar != null)
        {
            btnEliminar.SetActive(true);
            btnEliminar.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }

        if (btnCaptura != null)
        {
            btnCaptura.SetActive(true);
            btnCaptura.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }

        if (btnVolver != null && !modeloColocado)
        {
            btnVolver.SetActive(true);
            btnVolver.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }

        // Ocultar después de 3 segundos sin interacción
        ocultarCoroutine = StartCoroutine(OcultarDespuesDeTiempo(3f));
    }

    IEnumerator OcultarDespuesDeTiempo(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        OcultarBotonesSecundarios(true);
    }

    void OcultarBotonesSecundarios(bool animar)
    {
        if (animar)
        {
            if (btnEliminar != null && btnEliminar.activeSelf)
            {
                btnEliminar.transform.DOScale(Vector3.zero, 0.2f)
                    .OnComplete(() => btnEliminar.SetActive(false));
            }
            if (btnCaptura != null && btnCaptura.activeSelf)
            {
                btnCaptura.transform.DOScale(Vector3.zero, 0.2f)
                    .OnComplete(() => btnCaptura.SetActive(false));
            }
            if (btnVolver != null && btnVolver.activeSelf)
            {
                btnVolver.transform.DOScale(Vector3.zero, 0.2f)
                    .OnComplete(() => btnVolver.SetActive(false));
            }
        }
        else
        {
            if (btnEliminar != null) btnEliminar.SetActive(false);
            if (btnCaptura != null) btnCaptura.SetActive(false);
            if (btnVolver != null) btnVolver.SetActive(false);
        }
    }

    // ─── TOQUE EN PANTALLA → mostrar botones ─────
    void Update()
    {
        if (Input.touchCount > 0 && modeloColocado)
        {
            if (Input.GetTouch(0).phase == TouchPhase.Began)
                MostrarBotonesSecundarios();
        }
    }
}