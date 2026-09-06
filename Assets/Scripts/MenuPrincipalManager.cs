using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class MenuPrincipalManager : MonoBehaviour
{
    [Header("Elementos principales")]
    public Image ojoAnatomico;
    public TMP_Text titulo;
    public GameObject btnTutorial;
    public GameObject btnEmpezar;
    public GameObject btnSalida;
    public GameObject iconoSettings;
    public GameObject iconoInfo;

    [Header("Panel instrucciones")]
    public GameObject panelInstrucciones;

    void Start()
    {
        // Ocultar todo al inicio
        ojoAnatomico.transform.localScale = Vector3.zero;
        titulo.transform.localScale = Vector3.zero;
        btnTutorial.transform.localScale = Vector3.zero;
        btnEmpezar.transform.localScale = Vector3.zero;
        btnSalida.transform.localScale = Vector3.zero;

        if (iconoSettings != null)
            iconoSettings.transform.localScale = Vector3.zero;
        if (iconoInfo != null)
            iconoInfo.transform.localScale = Vector3.zero;

        if (panelInstrucciones != null)
            panelInstrucciones.SetActive(false);

        // Animación de entrada
        Sequence seq = DOTween.Sequence();

        seq.AppendInterval(0.3f);

        // Título aparece primero
        seq.Append(titulo.transform
            .DOScale(Vector3.one, 0.5f)
            .SetEase(Ease.OutBack));

        // Ojo aparece
        seq.Append(ojoAnatomico.transform
            .DOScale(Vector3.one, 0.6f)
            .SetEase(Ease.OutElastic));

        // Íconos de esquinas
        if (iconoSettings != null)
            seq.Append(iconoSettings.transform
                .DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack));
        if (iconoInfo != null)
            seq.Join(iconoInfo.transform
                .DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack));

        seq.AppendInterval(0.2f);

        // Botones aparecen uno por uno
        seq.Append(btnTutorial.transform
            .DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack));
        seq.Append(btnEmpezar.transform
            .DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack));
        seq.Append(btnSalida.transform
            .DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack));
    }

    // ─── BOTÓN TUTORIAL ───────────────────────────
    public void AbrirInstrucciones()
    {
        if (panelInstrucciones == null) return;

        panelInstrucciones.SetActive(true);
        panelInstrucciones.transform.localScale = Vector3.zero;
        panelInstrucciones.transform
            .DOScale(Vector3.one, 0.4f)
            .SetEase(Ease.OutBack);
    }

    public void CerrarInstrucciones()
    {
        if (panelInstrucciones == null) return;

        panelInstrucciones.transform
            .DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() => panelInstrucciones.SetActive(false));
    }

    // ─── BOTÓN EMPEZAR ────────────────────────────
    public void IniciarAR()
    {
        DOTween.KillAll();

        Sequence salida = DOTween.Sequence();
        salida.Append(btnTutorial.transform
            .DOScale(Vector3.zero, 0.15f));
        salida.Join(btnEmpezar.transform
            .DOScale(Vector3.zero, 0.15f));
        salida.Join(btnSalida.transform
            .DOScale(Vector3.zero, 0.15f));
        salida.Append(ojoAnatomico.transform
            .DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack));
        salida.Append(titulo.transform
            .DOScale(Vector3.zero, 0.2f));
        salida.OnComplete(() =>
        {
            
            SceneManager.LoadScene("Interfaz");
        });
    }

    // ─── BOTÓN SALIDA ─────────────────────────────
    public void SalirApp()
    {
        DOTween.KillAll();

        // Animación antes de salir
        Sequence salida = DOTween.Sequence();
        salida.Append(titulo.transform
            .DOScale(Vector3.zero, 0.2f));
        salida.Join(ojoAnatomico.transform
            .DOScale(Vector3.zero, 0.3f));
        salida.Join(btnTutorial.transform
            .DOScale(Vector3.zero, 0.2f));
        salida.Join(btnEmpezar.transform
            .DOScale(Vector3.zero, 0.2f));
        salida.Join(btnSalida.transform
            .DOScale(Vector3.zero, 0.2f));
        salida.OnComplete(() =>
        {
            Application.Quit();
            Debug.Log("App cerrada");
        });
    }

    // ─── BOTÓN SETTINGS (opcional) ────────────────
    public void AbrirSettings()
    {
        FindAnyObjectByType<SettingsManager>().AbrirSettings();
        Debug.Log("Settings abierto");
        // Implementar si es necesario
    }

    // ─── BOTÓN INFO (opcional) ────────────────────
    public void AbrirInfo()
    {
        FindAnyObjectByType<InfoManager>().AbrirInfo();
        Debug.Log("Info abierto");
        // Implementar si es necesario
    }
}