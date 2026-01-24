using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class ARInteractionManager : MonoBehaviour
{
    [SerializeField] private Camera aRCamera;

    [Header("Configuración de Movimiento")]
    [SerializeField] private float verticalSensitivity = 0.002f; // Sensibilidad de movimiento vertical

    [Header("Configuración de Escala (Zoom)")]
    [SerializeField] private float minScale = 0.1f; // Escala mínima
    [SerializeField] private float maxScale = 5f; // Escala máxima

    [Header("Configuración de Rotación")]
    [SerializeField] private float rotationSensitivity = 1f; // Sensibilidad de rotación

    private ARRaycastManager aRRaycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject item3Dmodel;
    private bool isPlacingMode;
    private bool isDragging;
    private bool isOverUi;

    // Variables para gestos con dos dedos
    private float initialPinchDistance; // Para zoom
    private Vector3 initialScale; // Escala inicial
    private Vector2 initialTouchVector; // Para rotación

    private Vector2 lastTouchPosition; // Para movimiento vertical

    public GameObject Item3DModel
    {
        set
        {
            item3Dmodel = value;
            isPlacingMode = true;
        }
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        aRRaycastManager = FindFirstObjectByType<ARRaycastManager>();
        GameManager.instance.OnMainMenu += SetItemPosition;
    }

    void Update()
    {
        // ═══════════════════════════════════════════════════════
        // MODO COLOCACIÓN INICIAL
        // ═══════════════════════════════════════════════════════
        if (isPlacingMode && item3Dmodel != null)
        {
            Vector2 middlePointScreen = new Vector2(Screen.width / 2, Screen.height / 2);

            if (aRRaycastManager.Raycast(middlePointScreen, hits, TrackableType.Planes))
            {
                item3Dmodel.transform.position = hits[0].pose.position;
                item3Dmodel.transform.rotation = hits[0].pose.rotation;
            }

            if (Touch.activeFingers.Count > 0)
            {
                Touch touch = Touch.activeFingers[0].currentTouch;
                if (touch.phase == TouchPhase.Began && !IsTapOverUI(touch.screenPosition))
                {
                    isPlacingMode = false;
                    Debug.Log("Objeto colocado");
                }
            }
            return;
        }

        // ═══════════════════════════════════════════════════════
        // INTERACCIÓN CON OBJETOS
        // ═══════════════════════════════════════════════════════
        if (Touch.activeFingers.Count > 0)
        {
            Touch touch = Touch.activeFingers[0].currentTouch;

            // ─────────────────────────────────────────
            // DETECTAR INICIO DEL TOQUE
            // ─────────────────────────────────────────
            if (touch.phase == TouchPhase.Began)
            {
                Vector2 touchPosition = touch.screenPosition;
                isOverUi = IsTapOverUI(touchPosition);

                if (!isOverUi && IsTapOver3DModel(touchPosition))
                {
                    item3Dmodel = itemSelect;
                    itemSelect = null;
                    isDragging = true;
                    lastTouchPosition = touchPosition;
                    GameManager.instance.ArPosition();
                    Debug.Log("Objeto seleccionado: " + item3Dmodel.name);
                }
            }

            // ─────────────────────────────────────────
            // ZOOM + ROTACIÓN CON 2 DEDOS
            // ─────────────────────────────────────────
            if (Touch.activeFingers.Count == 2 && item3Dmodel != null)
            {
                Touch touchTwo = Touch.activeFingers[1].currentTouch;

                // Inicializar valores al comenzar el gesto
                if (touch.phase == TouchPhase.Began || touchTwo.phase == TouchPhase.Began)
                {
                    // Para ZOOM
                    initialPinchDistance = Vector2.Distance(touch.screenPosition, touchTwo.screenPosition);
                    initialScale = item3Dmodel.transform.localScale;

                    // Para ROTACIÓN
                    initialTouchVector = touchTwo.screenPosition - touch.screenPosition;
                }

                // Aplicar ZOOM y ROTACIÓN simultáneamente
                if (touch.phase == TouchPhase.Moved || touchTwo.phase == TouchPhase.Moved)
                {
                    // ═══ ZOOM (PINCH) ═══
                    float currentPinchDistance = Vector2.Distance(touch.screenPosition, touchTwo.screenPosition);
                    float scaleFactor = currentPinchDistance / initialPinchDistance;

                    Vector3 newScale = initialScale * scaleFactor;

                    // Limitar la escala
                    newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
                    newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
                    newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);

                    item3Dmodel.transform.localScale = newScale;

                    // ═══ ROTACIÓN (TWIST) ═══
                    Vector2 currentTouchVector = touchTwo.screenPosition - touch.screenPosition;
                    float angle = Vector2.SignedAngle(initialTouchVector, currentTouchVector);

                    // Rotar el objeto sobre su eje Y
                    item3Dmodel.transform.Rotate(0, -angle * rotationSensitivity, 0, Space.World);

                    // Actualizar el vector para la siguiente iteración
                    initialTouchVector = currentTouchVector;
                }
            }
            // ─────────────────────────────────────────
            // MOVER OBJETO CON 1 DEDO (XZ + altura Y)
            // ─────────────────────────────────────────
            else if (touch.phase == TouchPhase.Moved && !isOverUi && isDragging && item3Dmodel != null && Touch.activeFingers.Count == 1)
            {
                Vector2 touchPosition = touch.screenPosition;

                // Calcular delta vertical (arriba/abajo)
                float deltaY = (touchPosition.y - lastTouchPosition.y) * verticalSensitivity;

                // Mover en el plano XZ usando raycast
                if (aRRaycastManager.Raycast(touchPosition, hits, TrackableType.Planes))
                {
                    Vector3 newPosition = hits[0].pose.position;

                    // Mantener la altura actual y ajustarla con el delta
                    newPosition.y = item3Dmodel.transform.position.y + deltaY;

                    item3Dmodel.transform.position = newPosition;
                }
                else
                {
                    // Si no hay plano, solo mover verticalmente
                    item3Dmodel.transform.position += new Vector3(0, deltaY, 0);
                }

                lastTouchPosition = touchPosition;
            }

            // ─────────────────────────────────────────
            // AL SOLTAR EL DEDO
            // ─────────────────────────────────────────
            if (touch.phase == TouchPhase.Ended)
            {
                isDragging = false;
            }
        }
    }

    private GameObject itemSelect;

    public bool IsTapOverUI(Vector2 touchPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = touchPosition;
        List<RaycastResult> result = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, result);
        return result.Count > 0;
    }

    private bool IsTapOver3DModel(Vector2 touchPosition)
    {
        Ray ray = aRCamera.ScreenPointToRay(touchPosition);
        if (Physics.Raycast(ray, out RaycastHit hit3DModel))
        {
            if (hit3DModel.transform.CompareTag("Item"))
            {
                itemSelect = hit3DModel.transform.gameObject;
                return true;
            }
        }
        return false;
    }

    public void SetItemPosition()
    {
        if (item3Dmodel != null)
        {
            item3Dmodel = null;
            isDragging = false;
            isPlacingMode = false;
            Debug.Log("Posición confirmada");
        }
    }

    public void DeleteItem()
    {
        if (item3Dmodel != null)
        {
            Destroy(item3Dmodel);
            item3Dmodel = null;
        }
        isDragging = false;
        isPlacingMode = false;
        GameManager.instance.MainMenu();
    }
}