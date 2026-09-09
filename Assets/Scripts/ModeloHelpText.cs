using UnityEngine;
using TMPro;
using DG.Tweening;

public class ModeloHelpText : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text textoAyuda;

    private const float DURACION = 10f;
    private const string MENSAJE = 
        "Gire o amplíe el modelo con sus dedos";

    void Start()
    {
        textoAyuda.gameObject.SetActive(false);
    }

    // Llamar este método cuando se coloque un modelo
    public void MostrarTexto()
    {
        StopAllCoroutines();

        textoAyuda.gameObject.SetActive(true);
        textoAyuda.alpha = 0f;

        // Fade in
        textoAyuda.DOFade(1f, 0.5f)
            .OnComplete(() =>
            {
                // Esperar y hacer fade out
                textoAyuda.DOFade(0f, 1f)
                    .SetDelay(DURACION - 1f)
                    .OnComplete(() =>
                        textoAyuda.gameObject.SetActive(false));
            });
    }
}