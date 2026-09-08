using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SettingsManager : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panelSettings;

    [Header("Volumen")]
    public Slider sliderVolumen;
    public TMP_Text labelVolumen;

    [Header("Iconos Volumen")]
    public UnityEngine.UI.Image iconoVozUI;
    public Sprite spriteConVolumen;
    public Sprite spriteSinVolumen;

    [Header("Idioma")]
    public Button btnEspanol;
    public Button btnIngles;
    private string idiomaActual = "es";

    [Header("Info App")]
    public TMP_Text labelVersion;
    public TMP_Text labelAutor;
    public TMP_Text labelUniversidad;
    public TMP_Text labelFacultad;

    // Colores visuales profesionales (Fondo semitransparente oscuro y bordes Cian / Gris)
    private Color32 colorFondoActivo = new Color32(13, 34, 43, 200);    // Oscuro semitransparente (#0D222B)
    private Color32 colorBordeActivo = new Color32(0, 229, 255, 255);     // Cian brillante (#00E5FF)

    private Color32 colorFondoInactivo = new Color32(74, 77, 82, 255);    // Gris sólido (#4A4D52)
    private Color32 colorBordeInactivo = new Color32(122, 126, 133, 255); // Gris claro

    // Clave para guardar preferencias
    private const string KEY_VOLUMEN = "tts_volumen";
    private const string KEY_IDIOMA = "tts_idioma";

    void Awake()
    {
        // 1. Cargar preferencias guardadas antes del primer fotograma para evitar parpadeos visuales
        float volumenGuardado = PlayerPrefs.GetFloat(KEY_VOLUMEN, 1f);
        idiomaActual = PlayerPrefs.GetString(KEY_IDIOMA, "es");

        if (sliderVolumen != null)
        {
            sliderVolumen.minValue = 0f;
            sliderVolumen.maxValue = 1f;
            sliderVolumen.value = volumenGuardado;
            ActualizarLabelVolumen(volumenGuardado);
            ActualizarIconoVolumenVisual(volumenGuardado);
        }

        // 2. Aplicar estilos visuales iniciales de los botones instantáneamente
        ActualizarBotonesIdiomaVisual();
    }

    void Start()
    {
        if (sliderVolumen != null)
        {
            sliderVolumen.onValueChanged.AddListener(OnVolumenChanged);
        }

        // Vincular los clics de los botones de idioma de manera segura
        if (btnEspanol != null) btnEspanol.onClick.AddListener(SeleccionarEspanol);
        if (btnIngles != null) btnIngles.onClick.AddListener(SeleccionarIngles);

        // Ocultar panel al inicio de forma segura
        if (panelSettings != null) panelSettings.SetActive(false);

        // Forzar actualización inicial de textos traducidos al arrancar
        LocalizationManager.ActualizarTodosLosTextos();
    }

    // ─── ABRIR / CERRAR (Con animaciones DOTween) ──────────────────────────

    public void AbrirSettings()
    {
        if (panelSettings == null) return;
        panelSettings.SetActive(true);
        panelSettings.transform.localScale = Vector3.zero;
        panelSettings.transform
            .DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack);
    }

    public void CerrarSettings()
    {
        if (panelSettings == null) return;
        panelSettings.transform
            .DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() => panelSettings.SetActive(false));
    }

    // ─── VOLUMEN TTS ──────────────────────────────

    void OnVolumenChanged(float valor)
    {
        ActualizarLabelVolumen(valor);
        ActualizarIconoVolumenVisual(valor);
        PlayerPrefs.SetFloat(KEY_VOLUMEN, valor);
        PlayerPrefs.Save();

        SetVolumenTTS(valor);
    }

    void ActualizarLabelVolumen(float valor)
    {
        if (labelVolumen != null)
        {
            int porcentaje = Mathf.RoundToInt(valor * 100);
            labelVolumen.text = porcentaje + "%"; // Formato limpio y directo
        }
    }

    void ActualizarIconoVolumenVisual(float valor)
    {
        if (iconoVozUI == null) return;

        if (valor <= 0.01f)
        {
            if (spriteSinVolumen != null) iconoVozUI.sprite = spriteSinVolumen;
        }
        else
        {
            if (spriteConVolumen != null) iconoVozUI.sprite = spriteConVolumen;
        }
    }

    void SetVolumenTTS(float valor)
    {
#if UNITY_ANDROID
        try
        {
            AndroidJavaClass audioManager =
                new AndroidJavaClass("android.media.AudioManager");
            AndroidJavaClass unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject audio = activity.Call<AndroidJavaObject>(
                "getSystemService", "audio");

            int maxVol = audio.Call<int>("getStreamMaxVolume", 3);
            int newVol = Mathf.RoundToInt(valor * maxVol);
            audio.Call("setStreamVolume", 3, newVol, 0);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning("Error TTS volumen: " + e.Message);
        }
#endif
    }

    // ─── IDIOMA ───────────────────────────────────

    public void SeleccionarEspanol()
    {
        idiomaActual = "es";
        PlayerPrefs.SetString(KEY_IDIOMA, "es");
        PlayerPrefs.Save();
        ActualizarBotonesIdiomaVisual();

        // Refrescar automáticamente todos los textos de la interfaz al cambiar a español
        LocalizationManager.ActualizarTodosLosTextos();

        UnityEngine.Debug.Log("Idioma: Español");
    }

    public void SeleccionarIngles()
    {
        idiomaActual = "en";
        PlayerPrefs.SetString(KEY_IDIOMA, "en");
        PlayerPrefs.Save();
        ActualizarBotonesIdiomaVisual();

        // Refrescar automáticamente todos los textos de la interfaz al cambiar a inglés
        LocalizationManager.ActualizarTodosLosTextos();

        UnityEngine.Debug.Log("Idioma: Inglés");
    }

    void ActualizarBotonesIdiomaVisual()
    {
        if (btnEspanol == null || btnIngles == null) return;

        UnityEngine.UI.Image imgEspanol = btnEspanol.GetComponent<UnityEngine.UI.Image>();
        Outline outlineEspanol = btnEspanol.GetComponent<Outline>();

        UnityEngine.UI.Image imgIngles = btnIngles.GetComponent<UnityEngine.UI.Image>();
        Outline outlineIngles = btnIngles.GetComponent<Outline>();

        if (idiomaActual == "es")
        {
            // Español ACTIVO (Fondo semitransparente + Borde Cian brillante)
            if (imgEspanol != null) imgEspanol.color = colorFondoActivo;
            if (outlineEspanol != null) outlineEspanol.effectColor = colorBordeActivo;

            // Inglés INACTIVO (Fondo Gris sólido + Borde Gris claro)
            if (imgIngles != null) imgIngles.color = colorFondoInactivo;
            if (outlineIngles != null) outlineIngles.effectColor = colorBordeInactivo;
        }
        else
        {
            // Inglés ACTIVO (Fondo semitransparente + Borde Cian brillante)
            if (imgIngles != null) imgIngles.color = colorFondoActivo;
            if (outlineIngles != null) outlineIngles.effectColor = colorBordeActivo;

            // Español INACTIVO (Fondo Gris sólido + Borde Gris claro)
            if (imgEspanol != null) imgEspanol.color = colorFondoInactivo;
            if (outlineEspanol != null) outlineEspanol.effectColor = colorBordeInactivo;
        }
    }

    // ─── GETTER para otros scripts ─────────────────
    public static string GetIdioma()
    {
        return PlayerPrefs.GetString("tts_idioma", "es");
    }

    public static float GetVolumen()
    {
        return PlayerPrefs.GetFloat("tts_volumen", 1f);
    }
}