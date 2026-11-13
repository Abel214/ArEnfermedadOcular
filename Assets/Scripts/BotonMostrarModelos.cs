using UnityEngine;
using DG.Tweening;

/// <summary>
/// Botón de la pantalla de inicio que abre el menú de modelos sin usar la voz,
/// como alternativa al micrófono ("Mostrar modelos").
/// </summary>
public class BotonMostrarModelos : MonoBehaviour
{
    public void MostrarModelos()
    {
        if (GameManager.instance == null)
        {
            Debug.LogWarning("[BotonMostrarModelos] GameManager no encontrado.");
            return;
        }

        // Pequeña animación para dar feedback al toque.
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 6, 0.6f);

        GameManager.instance.ItemsMenu();
        Debug.Log("Menú de modelos abierto desde el botón (sin voz).");
    }
}
