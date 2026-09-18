using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
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
    // 0 = Capa 1
    // 1 = Capa 2
    // 2 = Capa 3
    // 3 = Capa 4
    // 4 = Capa 5
    // 5 = Vista interna
    private int capaActual = 0;
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

        Debug.Log($"[OjoExplodedView] Inicializando con {capas.Count} capas");

        foreach (var capa in capas)
        {
            if (capa.objetos.Count == 0)
                Debug.LogWarning($"[OjoExplodedView] '{capa.nombre}' no tiene objetos asignados — se saltara visualmente");

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
            _ => Vector3.forward
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
            int nivel = externasSeAlejanMas ? (capas.Count - i) : (i + 1);
            float magnitud = distanciaExplosion * nivel;

            for (int j = 0; j < capa.objetos.Count; j++)
            {
                var obj = capa.objetos[j];
                if (obj == null) continue;

                Vector3 destino = capa.posicionesOriginales[j] + CalcularDireccion(obj) * magnitud;

                obj.DOKill();
                obj.DOLocalMove(destino, duracionAnimacion).SetEase(Ease.OutBack).SetDelay(i * 0.05f);
            }
        }

        RestaurarAlphas(duracionAnimacion);   // <-- duración sincronizada
    }

    public void UnirVista()
    {
        if (!estaExplotado) return;

        estaExplotado = false;
        // capaActual NO se toca (esto sigue igual)

        for (int i = 0; i < capas.Count; i++)
        {
            var capa = capas[i];

            // Capas ya peladas (i < capaActual) quedan ocultas.
            // Capa actual y mas internas (i >= capaActual) quedan visibles.
            bool debeQuedarVisible = i >= capaActual;

            for (int j = 0; j < capa.objetos.Count; j++)
            {
                var obj = capa.objetos[j];
                if (obj == null) continue;

                obj.DOKill();
                obj.DOLocalMove(capa.posicionesOriginales[j], duracionAnimacion)
                   .SetEase(Ease.InOutCubic);
                obj.DOScale(capa.escalasOriginales[j], duracionAnimacion);

                // Forzamos el estado siempre, no solo cuando hay que ocultar.
                // Esto es lo que faltaba: si el objeto venia oculto de antes
                // y ahora "debeQuedarVisible" es true, hay que ENCENDERLO,
                // no solo "no tocarlo".
                MostrarObjeto(obj, debeQuedarVisible);
            }
        }

        foreach (var obj in estructurasInternas)
            MostrarObjeto(obj, true);
    }
    /// <summary>
    /// Reinicia el modelo a su vista completa, mostrando todas las capas
    /// y regresando el indice de navegacion a Capa 1.
    /// Util como accion de voz "reiniciar"/"vista completa" y como
    /// fallback si el usuario se pierde navegando entre capas.
    /// </summary>
    public void ReiniciarModeloCompleto()
    {
        capaActual = 0;
        UnirVista(); // con capaActual en 0, i >= 0 siempre es true -> revela todo
    }

    bool EstaOculto(Transform obj)
    {
        var rend = obj.GetComponentInChildren<Renderer>();
        return rend != null && !rend.enabled;
    }
    public void Alternar()
    {
        if (estaExplotado) UnirVista();
        else ExplotarVista();
    }

    public void MostrarSiguienteCapa()
    {
        if (capas.Count == 0) return;

        if (!estaExplotado)
            ExplotarVista();

        // Si actualmente estamos en Vista Interna,
        // el siguiente estado debe ser Capa 1.
        if (capaActual == capas.Count)
        {
            capaActual = 0;

            Debug.Log("[OjoExplodedView] Siguiente -> Capa 1");

            ResaltarCapa(capaActual);
            return;
        }

        // Avanzar a la siguiente capa
        capaActual++;

        // Si llegamos después de Capa 5,
        // mostrar Vista Interna.
        if (capaActual >= capas.Count)
        {
            capaActual = capas.Count;

            MostrarVistaInterna();
            return;
        }

        Debug.Log(
            $"[OjoExplodedView] Siguiente -> index {capaActual} " +
            $"de {capas.Count} ({capas[capaActual].nombre}, " +
            $"{capas[capaActual].objetos.Count} objetos)"
        );

        ResaltarCapa(capaActual);
    }

    public void MostrarCapaAnterior()
    {
        if (capas.Count == 0) return;

        if (!estaExplotado)
            ExplotarVista();

        // Si estamos en Vista Interna
        if (capaActual == capas.Count)
        {
            capaActual = capas.Count - 1;

            Debug.Log($"[OjoExplodedView] Anterior -> Capa 5");

            ResaltarCapa(capaActual);
            return;
        }

        capaActual--;

        // Desde Capa 1, Anterior lleva a Vista Interna
        if (capaActual < 0)
        {
            capaActual = capas.Count;

            MostrarVistaInterna();
            return;
        }

        Debug.Log($"[OjoExplodedView] Anterior -> index {capaActual} de {capas.Count} ({capas[capaActual].nombre}, {capas[capaActual].objetos.Count} objetos)");

        ResaltarCapa(capaActual);
    }



    void MostrarVistaInterna()
    {
        for (int i = 0; i < 4 && i < capas.Count; i++)
            MostrarCapa(capas[i], false); // ocultas instantáneo

        if (capas.Count >= 5)
        {
            var capa5 = capas[4];
            foreach (var obj in capa5.objetos)
            {
                if (obj == null) continue;
                bool esDerecha = obj.name.ToLower().Contains("derecha");
                MostrarObjeto(obj, !esDerecha); // derecha oculta, izquierda visible
            }
        }

        foreach (var obj in estructurasInternas)
        {
            if (obj == null) continue;
            if (obj.name.ToLower() == "retina")
                AplicarAlpha(obj, 0.25f); // única excepción — sigue usando fade porque no se mueve
            else
                MostrarObjeto(obj, true);
        }

        OnAnunciarCapa?.Invoke("Vista interna");
    }

    void MostrarObjeto(Transform obj, bool visible)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
            rend.enabled = visible;
    }

    void MostrarCapa(Capa capa, bool visible)
    {
        foreach (var obj in capa.objetos)
            if (obj != null) MostrarObjeto(obj, visible);
    }

    void ResaltarCapa(int index)
    {
        for (int i = 0; i < capas.Count; i++)
        {
            bool visible = i >= index; // capas ya "peladas" (i < index) quedan ocultas
            MostrarCapa(capas[i], visible);

            float escalaMultiplicador = (i == index) ? escalaResaltado : 1f;

            for (int j = 0; j < capas[i].objetos.Count; j++)
            {
                var obj = capas[i].objetos[j];
                if (obj == null) continue;

                Vector3 escalaDestino = capas[i].escalasOriginales[j] * escalaMultiplicador;
                obj.DOScale(escalaDestino, 0.3f);
            }
        }

        Anunciar(capas[index]);
    }

    void AplicarAlpha(Transform obj, float alphaObjetivo, float duracion = 0.3f)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        bool ocultando = alphaObjetivo < 0.999f;

        foreach (var rend in renderers)
        {
            rend.enabled = true;

            foreach (var mat in rend.materials)
            {
                string prop = mat.HasProperty("_BaseColor") ? "_BaseColor"
                            : mat.HasProperty("_Color") ? "_Color"
                            : null;
                if (prop == null) continue;

                if (ocultando) SetMaterialTransparent(mat);

                DOTween.Kill(mat);
                DOTween.To(() => mat.GetColor(prop).a,
                           a => { var c = mat.GetColor(prop); c.a = a; mat.SetColor(prop, c); },
                           alphaObjetivo, duracion)   // <-- usa el parámetro, no 0.3f fijo
                       .SetId(mat)
                       .OnComplete(() =>
                       {
                           if (alphaObjetivo <= 0.001f)
                               rend.enabled = false;
                           else if (alphaObjetivo >= 0.999f)
                               SetMaterialOpaque(mat);
                       });
            }
        }
    }

    void RestaurarAlphas(float duracion = 0.3f)
    {
        foreach (var capa in capas)
            foreach (var obj in capa.objetos)
                if (obj != null) AplicarAlpha(obj, 1f, duracion);
    }
    static readonly int PropSurface = Shader.PropertyToID("_Surface");
    static readonly int PropZWrite = Shader.PropertyToID("_ZWrite");
    static readonly int PropSrcBlend = Shader.PropertyToID("_SrcBlend");
    static readonly int PropDstBlend = Shader.PropertyToID("_DstBlend");

    static void SetMaterialOpaque(Material mat)
    {
        if (!mat.HasProperty(PropSurface)) return; // no es URP Lit/Simple Lit

        mat.SetFloat(PropSurface, 0f); // 0 = Opaque
        mat.SetOverrideTag("RenderType", "Opaque");
        mat.SetInt(PropZWrite, 1);
        mat.SetInt(PropSrcBlend, (int)BlendMode.One);
        mat.SetInt(PropDstBlend, (int)BlendMode.Zero);
        mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)RenderQueue.Geometry;
    }

    static void SetMaterialTransparent(Material mat)
    {
        if (!mat.HasProperty(PropSurface)) return; // no es URP Lit/Simple Lit

        mat.SetFloat(PropSurface, 1f); // 1 = Transparent
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt(PropZWrite, 0);
        mat.SetInt(PropSrcBlend, (int)BlendMode.SrcAlpha);
        mat.SetInt(PropDstBlend, (int)BlendMode.OneMinusSrcAlpha);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
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