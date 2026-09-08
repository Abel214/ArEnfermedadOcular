using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedUIText : MonoBehaviour
{
    public string translationKey; // Ejemplo: "config_titulo", "btn_empezar", etc.
    private TMP_Text label;

    void Start()
    {
        label = GetComponent<TMP_Text>();
        ActualizarTexto();
    }

    public void ActualizarTexto()
    {
        if (label == null) label = GetComponent<TMP_Text>();

        if (!string.IsNullOrEmpty(translationKey))
        {
            label.text = LocalizationManager.GetText(translationKey);
        }
    }
}