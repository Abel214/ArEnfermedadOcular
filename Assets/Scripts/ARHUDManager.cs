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
    public Color colorIdle;      // blanco transparente
    public Color colorEscuchando; // rojo/verde

    private bool modeloColocado = false;
    private Coroutine ocultarCoroutine;

    void Start()
    {
        // Solo micrófono visible al inicio
        OcultarBotonesSecundarios(false);
        textoGuia.DOFade(0.4f, 0.5f); // texto sutil
    }

    // ─── MICRÓFONO TOCADO ────────────────────────
    public void OnMicrofonoTocado()
    {
        // Cambiar color a escuchando
        imgMicrofono.DOColor(colorEscuchando, 0.2f);
        textoGuia.DOFade(0f, 0.3f); // ocultar texto guía

        // Activar reconocimiento de voz
        AndroidVoiceRecognizer.Instance.StartListening();
    }

    // ─── CUANDO TERMINA DE ESCUCHAR ──────────────
    public void OnTerminoEscucha()
    {
        imgMicrofono.DOColor(colorIdle, 0.3f);
    }

    // ─── CUANDO SE COLOCA UN MODELO ──────────────
    public void OnModeloColocado()
    {
        modeloColocado = true;
        textoGuia.DOFade(0f, 0.3f);

        // Mostrar botones secundarios brevemente
        MostrarBotonesSecundarios();
    }

    // ─── MOSTRAR BOTONES SECUNDARIOS ─────────────
    public void MostrarBotonesSecundarios()
    {
        if (ocultarCoroutine != null)
            StopCoroutine(ocultarCoroutine);

        btnEliminar.SetActive(true);
        btnCaptura.SetActive(true);
        if (!modeloColocado) btnVolver.SetActive(true);

        btnEliminar.transform.DOScale(Vector3.one, 0.2f)
            .SetEase(Ease.OutBack);
        btnCaptura.transform.DOScale(Vector3.one, 0.2f)
            .SetEase(Ease.OutBack);

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
            btnEliminar.transform.DOScale(Vector3.zero, 0.2f)
                .OnComplete(() => btnEliminar.SetActive(false));
            btnCaptura.transform.DOScale(Vector3.zero, 0.2f)
                .OnComplete(() => btnCaptura.SetActive(false));
            btnVolver.transform.DOScale(Vector3.zero, 0.2f)
                .OnComplete(() => btnVolver.SetActive(false));
        }
        else
        {
            btnEliminar.SetActive(false);
            btnCaptura.SetActive(false);
            btnVolver.SetActive(false);
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