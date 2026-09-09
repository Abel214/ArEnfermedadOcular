using UnityEngine;
using UnityEngine.Video;
using TMPro;
using DG.Tweening;
using System.Collections;
using UnityEngine.UI;

public class PresentacionAvatar : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panelPresentacion;
    public CanvasGroup fondoBlanco;

    [Header("Avatar")]
    public RawImage avatarVideo;
    public VideoPlayer videoPlayer;
    public VideoClip videoSaludo;
    public VideoClip videoExplicacion;
    public VideoClip videoDespedida;

    [Header("Globo")]
    public GameObject globoTexto;
    public TMP_Text textoGlobo;

    [Header("Botones menu a presentar")]
    public GameObject btnTutorial;
    public GameObject btnEmpezar;
    public GameObject btnSalida;
    public GameObject iconoSettings;
    public GameObject iconoInfo;
    public GameObject ojoAnatomico;
    public GameObject titulo;

    void Start()
    {
        // Ocultar todos los elementos del menú
        btnTutorial.SetActive(false);
        btnEmpezar.SetActive(false);
        btnSalida.SetActive(false);
        iconoSettings.SetActive(false);
        iconoInfo.SetActive(false);
        ojoAnatomico.SetActive(false);
        titulo.SetActive(false);

        // Estado inicial
        fondoBlanco.alpha = 1f;
        fondoBlanco.blocksRaycasts = false;
        avatarVideo.gameObject.SetActive(false);
        globoTexto.SetActive(false);

        StartCoroutine(FlujoPresentacion());
    }

    IEnumerator FlujoPresentacion()
    {
        yield return new WaitForSeconds(0.5f);

        // Avatar aparece con animación
        avatarVideo.gameObject.SetActive(true);
        avatarVideo.transform.localScale = Vector3.zero;
        avatarVideo.transform
            .DOScale(Vector3.one, 0.5f)
            .SetEase(Ease.OutBack);

        yield return new WaitForSeconds(0.5f);

        // Video saludo — esperar que termine completo
        yield return StartCoroutine(
            ReproducirVideoYEsperar(videoSaludo));

        // Globo de bienvenida
        yield return StartCoroutine(
            MostrarGlobo("Hola!\nBienvenido a\nAR Enfermedad Ocular", 3f));

        // Video explicación en loop mientras presenta botones
        if (videoExplicacion != null)
        {
            videoPlayer.clip = videoExplicacion;
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }

        yield return StartCoroutine(
            MostrarGlobo("Te presento\nlos botones\nde la app", 2f));

        // Presentar cada botón
        yield return StartCoroutine(
            PresentarBoton(titulo,
            "AR Enfermedad\nOcular"));

        yield return StartCoroutine(
            PresentarBoton(ojoAnatomico,
            "Modelo anatomico\ndel ojo humano"));

        yield return StartCoroutine(
            PresentarBoton(btnTutorial,
            "TUTORIAL\nAprende a usar\nla aplicacion"));

        yield return StartCoroutine(
            PresentarBoton(btnEmpezar,
            "EMPEZAR\nInicia la experiencia\nde Realidad Aumentada"));

        yield return StartCoroutine(
            PresentarBoton(btnSalida,
            "SALIDA\nCierra la\naplicacion"));

        yield return StartCoroutine(
            PresentarBoton(iconoSettings,
            "AJUSTES\nConfigura volumen\ne idioma"));

        yield return StartCoroutine(
            PresentarBoton(iconoInfo,
            "INFO\nConoce mas sobre\nesta app"));

        // Video despedida — esperar que termine completo
        videoPlayer.isLooping = false;
        yield return StartCoroutine(
            ReproducirVideoYEsperar(videoDespedida));

        yield return StartCoroutine(
            MostrarGlobo("Todo listo!\nComencemos!", 2.5f));

        // Avatar desaparece
        globoTexto.SetActive(false);
        avatarVideo.transform
            .DOScale(Vector3.zero, 0.4f)
            .SetEase(Ease.InBack);
        yield return new WaitForSeconds(0.5f);

        // Fondo blanco desaparece → menú visible
        fondoBlanco.DOFade(0f, 0.8f)
            .OnComplete(() =>
            {
                panelPresentacion.SetActive(false);
            });
    }

    IEnumerator ReproducirVideoYEsperar(VideoClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("Video clip es null");
            yield break;
        }

        videoPlayer.clip = clip;
        videoPlayer.isLooping = false;
        videoPlayer.Play();

        Debug.Log("Reproduciendo: " + clip.name
            + " | Duracion: " + clip.length + "s");

        // Esperar a que el video esté preparado
        yield return new WaitUntil(() => videoPlayer.isPrepared);

        // Esperar a que termine
        yield return new WaitUntil(() =>
            !videoPlayer.isPlaying ||
            videoPlayer.time >= clip.length - 0.1f);

        Debug.Log("Video terminado: " + clip.name);
    }

    IEnumerator PresentarBoton(GameObject boton, string texto)
    {
        // Activar y animar el botón
        boton.SetActive(true);
        boton.transform.localScale = Vector3.zero;
        boton.transform
            .DOScale(Vector3.one * 1.15f, 0.3f)
            .SetEase(Ease.OutBack);

        // Mostrar globo explicativo
        yield return StartCoroutine(MostrarGlobo(texto, 2.5f));

        // Volver a escala normal
        boton.transform.DOScale(Vector3.one, 0.2f);
        yield return new WaitForSeconds(0.2f);
    }

    IEnumerator MostrarGlobo(string texto, float duracion)
    {
        textoGlobo.text = texto;
        globoTexto.SetActive(true);
        globoTexto.transform.localScale = Vector3.zero;
        globoTexto.transform
            .DOScale(Vector3.one, 0.25f)
            .SetEase(Ease.OutBack);

        yield return new WaitForSeconds(duracion);

        globoTexto.transform
            .DOScale(Vector3.zero, 0.2f)
            .OnComplete(() => globoTexto.SetActive(false));

        yield return new WaitForSeconds(0.25f);
    }

    public void SaltarPresentacion()
    {
        StopAllCoroutines();
        videoPlayer.Stop();
        globoTexto.SetActive(false);
        avatarVideo.gameObject.SetActive(false);

        // Activar todos los elementos del menú
        titulo.SetActive(true);
        ojoAnatomico.SetActive(true);
        btnTutorial.SetActive(true);
        btnEmpezar.SetActive(true);
        btnSalida.SetActive(true);
        iconoSettings.SetActive(true);
        iconoInfo.SetActive(true);

        fondoBlanco.DOFade(0f, 0.5f)
            .OnComplete(() => panelPresentacion.SetActive(false));
    }
}