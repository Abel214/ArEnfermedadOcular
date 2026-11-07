using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SplashVideo : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string escenaSiguiente = "MenuPrincipal"; // nombre exacto de tu escena de inicio
    bool cargando;

    void Start()
    {
        videoPlayer.prepareCompleted += _ => videoPlayer.Play();
        videoPlayer.loopPointReached += _ => Cargar();
        videoPlayer.errorReceived += (v, msg) => Cargar();
        videoPlayer.Prepare();
    }

    void Update()
    {
        // Tocar la pantalla (celular) o clic (PC) para saltar el video
        bool toque = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        bool clic = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (toque || clic) Cargar();
    }

    void Cargar()
    {
        if (cargando) return;
        cargando = true;
        SceneManager.LoadScene(escenaSiguiente);
    }
}