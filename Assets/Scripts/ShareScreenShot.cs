using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ShareScreenShot : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuCanvas;
    private ARPointCloudManager aRPointCloudManager;

    void Start()
    {
        aRPointCloudManager = FindFirstObjectByType<ARPointCloudManager>();
    }

    public void TakeScreenShot()
    {
        StartCoroutine(TakeScreenshotAndShare());
    }

    private IEnumerator TakeScreenshotAndShare()
    {
        // 1. Ocultar UI y puntos de AR
        TurnOnOffARContent();

        // 2. Esperar al final del frame para que se renderice todo
        yield return new WaitForEndOfFrame();

        // 3. Capturar la pantalla
        Texture2D ss = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        ss.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        ss.Apply();

        // 4. Guardar la imagen
        string filePath = Path.Combine(UnityEngine.Application.temporaryCachePath, "shared img.png");
        File.WriteAllBytes(filePath, ss.EncodeToPNG());

        // 5. Liberar memoria
        Destroy(ss);

        // 6. Mostrar de nuevo UI y puntos de AR
        TurnOnOffARContent();

        // 7. Compartir la imagen
        new NativeShare().AddFile(filePath)
            .SetSubject("Subject goes here").SetText("Mira esta imagen!!")
            .SetCallback((result, shareTarget) => Debug.Log("Share result: " + result + ", selected app: " + shareTarget))
            .Share();
    }

    private void TurnOnOffARContent()
    {
        var points = aRPointCloudManager.trackables;
        foreach (var point in points)
        {
            point.gameObject.SetActive(!point.gameObject.activeSelf);
        }
        mainMenuCanvas.SetActive(!mainMenuCanvas.activeSelf);
    }
}