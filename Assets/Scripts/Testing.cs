using UnityEngine;

/// <summary>
/// SOLO PARA PRUEBAS. Botones en pantalla para probar OjoExplodedView
/// sin depender del reconocimiento de voz. Borrar este script (y el
/// GameObject donde este) antes de la build final.
/// </summary>
public class TestExplodedView : MonoBehaviour
{
    OjoExplodedView vistaActual;

    void OnGUI()
    {
        // Estilo grande para que sea facil tocar en el celular
        GUI.skin.button.fontSize = 30;
        GUI.skin.label.fontSize = 24;
        GUI.skin.label.normal.textColor = Color.yellow;

        float ancho = 260;
        float alto = 90;
        float x = 20;
        float y = 20;

        // Buscar el modelo colocado en AR (puede no existir aun)
        if (vistaActual == null)
            vistaActual = FindAnyObjectByType<OjoExplodedView>();

        if (vistaActual == null)
        {
            GUI.Label(new Rect(x, y, 500, 60),
                "No hay OjoExplodedView\nen la escena todavia");
            return;
        }

        if (GUI.Button(new Rect(x, y, ancho, alto), "Explotar"))
            vistaActual.ExplotarVista();
        y += alto + 10;

        if (GUI.Button(new Rect(x, y, ancho, alto), "Unir"))
            vistaActual.UnirVista();
        y += alto + 10;

        if (GUI.Button(new Rect(x, y, ancho, alto), "Siguiente capa"))
            vistaActual.MostrarSiguienteCapa();
        y += alto + 10;

        if (GUI.Button(new Rect(x, y, ancho, alto), "Capa anterior"))
            vistaActual.MostrarCapaAnterior();
        y += alto + 10;

        if (GUI.Button(new Rect(x, y, ancho, alto), "Reiniciar modelo"))
            vistaActual.ReiniciarModeloCompleto();
        y += alto + 10;

        GUI.Label(new Rect(x, y, 500, 60),
            "Explotado: " + vistaActual.EstaExplotado
            + "\nCapa actual: " + vistaActual.CapaActual);
    }
}