using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class InstruccionesManager : MonoBehaviour
{
    [Header("UI Referencias")]
    public TMP_Text tituloText;
    public TMP_Text descripcionText;
    public Image iconoImage;
    public GameObject[] puntos;
    public Button btnAnterior;
    public Button btnSiguiente;
    public Button btnCerrar;
    public CanvasGroup slideCanvasGroup;

    [Header("Iconos por slide")]
    public Sprite[] iconosSlides;

    private int slideActual = 0;

    private List<SlideData> slides = new List<SlideData>();

    void Awake()
    {
        // Definir contenido de cada slide
        slides = new List<SlideData>
        {
            new SlideData(
                "👋 Bienvenido",
                "Esta aplicación usa Realidad Aumentada para visualizar enfermedades oculares en 3D. Toca el micrófono y da comandos de voz.",
                0
            ),
            new SlideData(
                "🎤 Comando de Voz",
                "Di 'colocar objeto' para abrir el menú de modelos. El sistema reconoce tu voz automáticamente en español.",
                1
            ),
            new SlideData(
                "👁️ Seleccionar Modelo",
                "Elige entre Catarata, Conjuntivitis, Glaucoma u Ojo Sano. También puedes decir el nombre de la enfermedad directamente.",
                2
            ),
            new SlideData(
                "✋ Interactuar",
                "Usa un dedo para mover el modelo. Usa dos dedos para hacer zoom o rotar. Di 'eliminar' para borrar el modelo.",
                3
            ),
            new SlideData(
                "🤖 Consultar IA",
                "Di 'explica', 'síntomas' o 'tratamiento' para que la IA médica te dé información por voz sobre la enfermedad.",
                4
            )
        };
    }

    void OnEnable()
    {
        slideActual = 0;
        MostrarSlide(0, false);
    }

    public void MostrarSlide(int index, bool animar = true)
    {
        if (index < 0 || index >= slides.Count) return;

        slideActual = index;
        SlideData slide = slides[index];

        if (animar)
        {
            // Animación fade out → cambio → fade in
            slideCanvasGroup.DOFade(0, 0.2f).OnComplete(() =>
            {
                ActualizarContenido(slide);
                slideCanvasGroup.DOFade(1, 0.3f);
            });
        }
        else
        {
            ActualizarContenido(slide);
        }

        // Actualizar botones
        btnAnterior.interactable = index > 0;
        btnSiguiente.gameObject.GetComponentInChildren<TMP_Text>().text =
            index == slides.Count - 1 ? "Cerrar" : "Siguiente ▶";

        // Actualizar puntos indicadores
        for (int i = 0; i < puntos.Length; i++)
        {
            puntos[i].GetComponent<Image>().color =
                i == index ? Color.white : new Color(1, 1, 1, 0.4f);
        }

        // Leer en voz alta
        HablarSlide(slide.descripcion);
    }

    void ActualizarContenido(SlideData slide)
    {
        tituloText.text = slide.titulo;
        descripcionText.text = slide.descripcion;

        if (iconosSlides != null && slide.iconoIndex < iconosSlides.Length
            && iconosSlides[slide.iconoIndex] != null)
        {
            iconoImage.sprite = iconosSlides[slide.iconoIndex];
            iconoImage.transform.DOScale(Vector3.one * 1.1f, 0.3f)
                .OnComplete(() => iconoImage.transform.DOScale(Vector3.one, 0.2f));
        }
    }

    public void Siguiente()
    {
        if (slideActual < slides.Count - 1)
            MostrarSlide(slideActual + 1);
        else
            Cerrar();
    }

    public void Anterior()
    {
        if (slideActual > 0)
            MostrarSlide(slideActual - 1);
    }

    public void Cerrar()
    {
        StopTTS();
        transform.parent.gameObject.SetActive(false);
    }

    void HablarSlide(string texto)
    {
#if UNITY_ANDROID
        try
        {
            StopTTS();
            AndroidJavaClass unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject tts = new AndroidJavaObject(
                "android.speech.tts.TextToSpeech", activity, null);
            AndroidJavaObject locale =
                new AndroidJavaObject("java.util.Locale", "es", "ES");
            tts.Call<int>("setLanguage", locale);
            tts.Call<int>("speak", texto, 0, null, null);
            currentTTS = tts;
        }
        catch (System.Exception e)
        {
            Debug.LogError("TTS Error: " + e.Message);
        }
#else
        Debug.Log("🔊 " + texto);
#endif
    }

    private AndroidJavaObject currentTTS;

    void StopTTS()
    {
#if UNITY_ANDROID
        try { currentTTS?.Call<int>("stop"); } catch { }
#endif
    }
}

[System.Serializable]
public class SlideData
{
    public string titulo;
    public string descripcion;
    public int iconoIndex;

    public SlideData(string titulo, string descripcion, int iconoIndex)
    {
        this.titulo = titulo;
        this.descripcion = descripcion;
        this.iconoIndex = iconoIndex;
    }
}