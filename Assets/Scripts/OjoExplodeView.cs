using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// Vista explosionada del modelo de ojo.
/// Va en el RAIZ del prefab OjoSanoCompleto (no en un Empty de escena),
/// porque el modelo se instancia en runtime desde ItemButtonManager.
/// </summary>
public class OjoExplodedView : MonoBehaviour
{
    public enum Eje { X, Y, Z }

    [System.Serializable]
    public class Capa
    {
        public string nombre;

        [Tooltip("Los 2 objetos de la capa: derecha e izquierda")]
        public List<Transform> objetos = new List<Transform>();

        [Tooltip("Dejar vacio hasta confirmar la anatomia real (regla 10)")]
        [TextArea(2, 4)]
        public string descripcion;

        [HideInInspector] public List<Vector3> posicionesOriginales = new List<Vector3>();
        [HideInInspector] public List<Vector3> escalasOriginales = new List<Vector3>();
    }

    [Header("Capas — orden de AFUERA hacia ADENTRO (capa1 primero)")]
    public List<Capa> capas = new List<Capa>();

    [Header("Estructuras internas (pupila, retina, venas...)")]
    [Tooltip("No se mueven; opcionalmente se atenuan mientras las capas estan cerradas")]
    public List<Transform> estructurasInternas = new List<Transform>();

    [Header("Configuracion de explosion")]
    [Tooltip("Eje sobre el que se separan las mitades. Probar Z o Y si X no funciona.")]
    public Eje ejeExplosion = Eje.Z;

    [Tooltip("Separacion en metros que gana cada nivel de capa")]
    public float distanciaExplosion = 0.03f;

    [Tooltip("Si es true, la capa mas externa (capa1) se aleja mas")]
    public bool externasSeAlejanMas = true;

    public float duracionAnimacion = 0.8f;

    [Header("Resaltado")]
    [Range(0f, 1f)] public float alphaAtenuado = 0.25f;
    public float escalaResaltado = 1.05f;

    [Header("Salida de voz")]
    [Tooltip("Conectar aqui el metodo de TTS de ARMedicalAI. NO se crea un TTS propio.")]
    public UnityEvent<string> OnAnunciarCapa;

    private bool estaExplotado = false;
    private int capaActual = -1;
    private bool inicializado = false;

    public bool EstaExplotado => estaExplotado;
    public int CapaActual => capaActual;

    void Awake()
    {
        GuardarEstadoOriginal();
    }

    void GuardarEstadoOriginal()
    {
        if (inicializado) return;

        foreach (var capa in capas)
        {
            capa.posicionesOriginales.Clear();
            capa.escalasOriginales.Clear();

            foreach (var obj in capa.objetos)
            {
                if (obj == null)
                {
                    capa.posicionesOriginales.Add(Vector3.zero);
                    capa.escalasOriginales.Add(Vector3.one);
                    continue;
                }
                capa.posicionesOriginales.Add(obj.localPosition);
                capa.escalasOriginales.Add(obj.localScale);
            }
        }
        inicializado = true;
    }

    /// <summary>
    /// Deduce hacia que lado debe alejarse el objeto comparando el centro
    /// de sus bounds con el centro del modelo completo.
    /// </summary>
    Vector3 CalcularDireccion(Transform obj)
    {
        Vector3 ejeLocal = ejeExplosion switch
        {
            Eje.X => Vector3.right,
            Eje.Y => Vector3.up,
            _     => Vector3.forward
        };

        var rend = obj.GetComponentInChildren<Renderer>();
        if (rend == null) return ejeLocal;

        // Centro del objeto relativo al raiz del modelo
        Vector3 centroLocal = transform.InverseTransformPoint(rend.bounds.center);
        float componente = Vector3.Dot(centroLocal, ejeLocal);

        // Si esta practicamente centrado, fallback al nombre
        if (Mathf.Abs(componente) < 0.0005f)
        {
            string n = obj.name.ToLower();
            if (n.Contains("izquierda")) return -ejeLocal;
            return ejeLocal;
        }

        return componente > 0f ? ejeLocal : -ejeLocal;
    }

    public void ExplotarVista()
    {
        GuardarEstadoOriginal();
        if (estaExplotado) return;
        estaExplotado = true;

        for (int i = 0; i < capas.Count; i++)
        {
            var capa = capas[i];

            // capa1 (i=0) es la mas externa
            int nivel = externasSeAlejanMas ? (capas.Count - i) : (i + 1);
            float magnitud = distanciaExplosion * nivel;

            for (int j = 0; j < capa.objetos.Count; j++)
            {
                var obj = capa.objetos[j];
                if (obj == null) continue;

                Vector3 destino = capa.posicionesOriginales[j]
                                + CalcularDireccion(obj) * magnitud;

                obj.DOKill();
                obj.DOLocalMove(destino, duracionAnimacion)
                   .SetEase(Ease.OutBack)
                   .SetDelay(i * 0.05f);
            }
        }

        RestaurarAlphas();
    }

    public void UnirVista()
    {
        if (!estaExplotado) return;
        estaExplotado = false;
        capaActual = -1;

        foreach (var capa in capas)
        {
            for (int j = 0; j < capa.objetos.Count; j++)
            {
                var obj = capa.objetos[j];
                if (obj == null) continue;

                obj.DOKill();
                obj.DOLocalMove(capa.posicionesOriginales[j], duracionAnimacion)
                   .SetEase(Ease.InOutCubic);
                obj.DOScale(capa.escalasOriginales[j], duracionAnimacion);
            }
        }

        RestaurarAlphas();
    }

    public void Alternar()
    {
        if (estaExplotado) UnirVista();
        else ExplotarVista();
    }

    public void MostrarSiguienteCapa()
    {
        if (capas.Count == 0) return;
        if (!estaExplotado) ExplotarVista();

        capaActual = (capaActual + 1) % capas.Count;
        ResaltarCapa(capaActual);
    }

    public void MostrarCapaAnterior()
    {
        if (capas.Count == 0) return;
        if (!estaExplotado) ExplotarVista();

        capaActual--;
        if (capaActual < 0) capaActual = capas.Count - 1;
        ResaltarCapa(capaActual);
    }

    void ResaltarCapa(int index)
    {
        for (int i = 0; i < capas.Count; i++)
        {
            float alpha;
            float escalaMultiplicador;

            if (i == index)
            {
                // Capa actual: resaltada y opaca
                alpha = 1f;
                escalaMultiplicador = escalaResaltado;
            }
            else if (i < index)
            {
                // Capas mas externas ya "peladas": ocultas por completo
                alpha = 0f;
                escalaMultiplicador = 1f;
            }
            else
            {
                // Capas mas internas, todavia no alcanzadas: visibles normal
                alpha = 1f;
                escalaMultiplicador = 1f;
            }

            for (int j = 0; j < capas[i].objetos.Count; j++)
            {
                var obj = capas[i].objetos[j];
                if (obj == null) continue;

                AplicarAlpha(obj, alpha);

                Vector3 escalaDestino = capas[i].escalasOriginales[j] * escalaMultiplicador;
                obj.DOScale(escalaDestino, 0.3f);
            }
        }

        Anunciar(capas[index]);
    }

    void RestaurarAlphas()
    {
        foreach (var capa in capas)
            foreach (var obj in capa.objetos)
                if (obj != null) AplicarAlpha(obj, 1f);
    }

    /// <summary>
    /// Requiere materiales en Surface Type = Transparent (URP)
    /// o Rendering Mode = Transparent/Fade (Built-in).
    /// </summary>
    void AplicarAlpha(Transform obj, float alpha)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (var rend in renderers)
        {
            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    DOTween.To(() => mat.GetColor("_BaseColor").a,
                               a => { c.a = a; mat.SetColor("_BaseColor", c); },
                               alpha, 0.3f);
                }
                else if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    DOTween.To(() => mat.color.a,
                               a => { c.a = a; mat.color = c; },
                               alpha, 0.3f);
                }
            }
        }
    }

    void Anunciar(Capa capa)
    {
        string texto = string.IsNullOrEmpty(capa.descripcion)
            ? capa.nombre
            : capa.nombre + ". " + capa.descripcion;

        OnAnunciarCapa?.Invoke(texto);
        Debug.Log("[OjoExplodedView] " + texto);
    }

#if UNITY_EDITOR
    [ContextMenu("Autollenar capas desde OJO_V1")]
    void AutollenarCapas()
    {
        // Registrar para Undo Y para que Unity marque el objeto como modificado
        UnityEditor.Undo.RecordObject(this, "Autollenar capas OjoExplodedView");

        Transform raiz = transform;
        var ojo = raiz.GetComponentsInChildren<Transform>(true);

        capas.Clear();
        estructurasInternas.Clear();

        for (int n = 1; n <= 5; n++)
        {
            var capa = new Capa { nombre = "Capa " + n };
            foreach (var t in ojo)
            {
                if (t.name == $"capa{n}_derecha" || t.name == $"capa{n}_izquierda")
                    capa.objetos.Add(t);
            }
            if (capa.objetos.Count > 0) capas.Add(capa);
        }

        string[] internos = { "pupila", "retina", "vena_azul",
                              "vena_derecha", "vena_izquierda",
                              "lagrimal", "palitos" };
        foreach (var t in ojo)
            foreach (var nombre in internos)
                if (t.name == nombre) estructurasInternas.Add(t);

        // CRITICO: sin esto, el cambio no se marca "dirty" y se pierde
        // al salir del modo prefab, aunque Auto Save este activado.
        UnityEditor.EditorUtility.SetDirty(this);

        // Si estamos editando el asset del prefab directamente,
        // marcar esa escena temporal como sucia (el Auto Save del modo
        // prefab se encarga de escribirla a disco al salir).
        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(prefabStage.scene);
        }
        else
        {
            // Solo forzar guardado inmediato si NO estamos en modo prefab
            // (llamar SaveAssets dentro del modo prefab puede hacer que el
            // Inspector pierda temporalmente la referencia al objeto).
            UnityEditor.AssetDatabase.SaveAssets();
        }

        Debug.Log($"Autollenado: {capas.Count} capas, {estructurasInternas.Count} estructuras internas (guardado)");
    }
#endif
}