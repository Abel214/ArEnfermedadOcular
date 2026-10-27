using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de Interfaz")]
    [SerializeField] private GameObject panelPresentacionEstatica; // El fondo oscuro con las instrucciones iniciales
    [SerializeField] private GameObject itemsMenuCanvas;           // El scrollbar con los modelos de ojos
    [SerializeField] private GameObject arPositionCanvas;          // Botones de confirmar/eliminar en AR
    [SerializeField] private GameObject aiCanvas;

    [Header("Componentes de Realidad Aumentada")]
    [SerializeField] private GameObject arSessionObj;              // El objeto AR Session
    [SerializeField] private GameObject xrOriginObj;               // El objeto XR Origin (Mobile AR)

    void Start()
    {
        // Estado inicial limpio al entrar a la escena
        ActivarPresentacionEstatica();

        // Nos suscribimos a los eventos del GameManager
        GameManager.instance.OnMainMenu += ActivarPresentacionEstatica;
        GameManager.instance.OnItemsMenu += ActivarMenuDeItems;
        GameManager.instance.OnArPosition += ActivarArPosition;
    }

    private void ActivarPresentacionEstatica()
    {
        // Mostrar panel estático y apagar el AR al volver al inicio
        if (panelPresentacionEstatica != null)
            panelPresentacionEstatica.SetActive(true);

        if (arSessionObj != null) arSessionObj.SetActive(false);
        if (xrOriginObj != null) xrOriginObj.SetActive(false);

        // Ocultar menús de ítems y AR
        if (itemsMenuCanvas != null)
        {
            itemsMenuCanvas.transform.GetChild(0).transform.DOScale(Vector3.zero, 0.3f);
            itemsMenuCanvas.transform.GetChild(1).transform.DOScale(Vector3.zero, 0.3f);
        }

        if (arPositionCanvas != null)
        {
            arPositionCanvas.transform.GetChild(0).transform.DOScale(Vector3.zero, 0.3f);
            arPositionCanvas.transform.GetChild(1).transform.DOScale(Vector3.zero, 0.3f);
        }

        if (aiCanvas != null)
            aiCanvas.SetActive(false);
    }

    private void ActivarMenuDeItems()
    {
        // 1. Ocultar la presentación estática de bienvenida
        if (panelPresentacionEstatica != null)
            panelPresentacionEstatica.SetActive(false);

        // 2. ¡ENCENDER EL ENTORNO DE AR! (Cámara y sesión listas)
        if (arSessionObj != null) arSessionObj.SetActive(true);
        if (xrOriginObj != null) xrOriginObj.SetActive(true);

        // 3. Mostrar el scrollbar con los modelos de ojos de forma fluida
        if (itemsMenuCanvas != null)
        {
            itemsMenuCanvas.transform.GetChild(0).transform.DOScale(Vector3.one, 0.5f);
            itemsMenuCanvas.transform.GetChild(1).transform.DOScale(Vector3.one, 0.3f);
            itemsMenuCanvas.transform.GetChild(1).transform.DOMoveY(300, 0.3f);
        }
    }

    private void ActivarArPosition()
    {
        // Ocultar menú de ítems cuando ya se seleccionó y colocó el modelo en AR
        if (itemsMenuCanvas != null)
        {
            itemsMenuCanvas.transform.GetChild(0).transform.DOScale(Vector3.zero, 0.5f);
            itemsMenuCanvas.transform.GetChild(1).transform.DOScale(Vector3.zero, 0.3f);
        }

        // Mostrar controles de posición en AR
        if (arPositionCanvas != null)
        {
            arPositionCanvas.transform.GetChild(0).transform.DOScale(Vector3.one, 0.3f);
            arPositionCanvas.transform.GetChild(1).transform.DOScale(Vector3.one, 0.3f);
        }
    }
}