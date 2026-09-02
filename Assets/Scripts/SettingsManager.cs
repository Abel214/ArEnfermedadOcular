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

    [Header("Idioma")]
    public Button btnEspanol;
    public Button btnIngles;
    private string idiomaActual = "es";

    [Header("Info App")]
    public TMP_Text labelVersion;
    public TMP_Text labelAutor;
    public TMP_Text labelUniversidad;
    public TMP_Text labelFacultad;

    // Clave para guardar preferencias
    private const string KEY_VOLUMEN = "tts_volumen";
    private const string KEY_IDIOMA = "tts_idioma";

    void Start()
    {
        // Cargar preferencias guardadas
        float volumenGuardado = PlayerPrefs.GetFloat(KEY_VOLUMEN, 1f);
        idiomaActual = PlayerPrefs.GetString(KEY_IDIOMA, "es");

        // Configurar slider
        sliderVolumen.minValue = 0f;
        sliderVolumen.maxValue = 1f;
        sliderVolumen.value = volumenGuardado;
        sliderVolumen.onValueChanged.AddListener(OnVolumenChanged);
        ActualizarLabelVolumen(volumenGuardado);

        // Configurar info
        labelVersion.text = "Versión: 1.0.0";
        labelAutor.text = "Autor: [Tu nombre]";
        labelUniversidad.text = "Universidad Nacional de Loja";
        labelFacultad.text = "Facultad de la Salud Humana";

        // Actualizar botones de idioma
        ActualizarBotonesIdioma();

        // Ocultar panel al inicio
        panelSettings.SetActive(false);
    }

    // ─── ABRIR / CERRAR ──────────────────────────

    public void AbrirSettings()
    {
        panelSettings.SetActive(true);
        panelSettings.transform.localScale = Vector3.zero;
        panelSettings.transform
            .DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack);
    }

    public void CerrarSettings()
    {
        panelSettings.transform
            .DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() => panelSettings.SetActive(false));
    }

    // ─── VOLUMEN TTS ──────────────────────────────

    void OnVolumenChanged(float valor)
    {
        ActualizarLabelVolumen(valor);
        PlayerPrefs.SetFloat(KEY_VOLUMEN, valor);
        PlayerPrefs.Save();

        // Aplicar volumen al TTS Android
        SetVolumenTTS(valor);
    }

    void ActualizarLabelVolumen(float valor)
    {
        int porcentaje = Mathf.RoundToInt(valor * 100);
        labelVolumen.text = "Volumen: " + porcentaje + "%";
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
            Debug.LogWarning("Error TTS volumen: " + e.Message);
        }
#endif
    }

    // ─── IDIOMA ───────────────────────────────────

    public void SeleccionarEspanol()
    {
        idiomaActual = "es";
        PlayerPrefs.SetString(KEY_IDIOMA, "es");
        PlayerPrefs.Save();
        ActualizarBotonesIdioma();
        Debug.Log("Idioma: Español");
    }

    public void SeleccionarIngles()
    {
        idiomaActual = "en";
        PlayerPrefs.SetString(KEY_IDIOMA, "en");
        PlayerPrefs.Save();
        ActualizarBotonesIdioma();
        Debug.Log("Idioma: Inglés");
    }

    void ActualizarBotonesIdioma()
    {
        // Resaltar botón activo
        Color activo = new Color(0f, 0.8f, 0.8f, 1f);   // teal
        Color inactivo = new Color(1f, 1f, 1f, 0.3f);    // transparente

        btnEspanol.GetComponent<Image>().color =
            idiomaActual == "es" ? activo : inactivo;
        btnIngles.GetComponent<Image>().color =
            idiomaActual == "en" ? activo : inactivo;
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