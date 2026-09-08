#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Text;

public class AutoLocalizeTool : EditorWindow
{
    [MenuItem("Tools/Traducir/Escanear y Ver Claves en Consola")]
    public static void ShowWindow()
    {
        GetWindow<AutoLocalizeTool>("Autolocalizador UI");
    }

    void OnGUI()
    {
        GUILayout.Label("Herramienta de Localización Masiva", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Escanear y Generar Diccionario", GUILayout.Height(40)))
        {
            EscanearYMostrarClaves();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Escanea la escena, asigna las claves automáticamente y te imprime el bloque de código listo para copiar en la Consola.", MessageType.Info);
    }

    static void EscanearYMostrarClaves()
    {
        TMP_Text[] todosLosTextos = Resources.FindObjectsOfTypeAll<TMP_Text>();
        int contadorAgregados = 0;
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("--- COPIA ESTO EN TU DICTIONARY DE LocalizationManager ---");

        foreach (TMP_Text textoUI in todosLosTextos)
        {
            if (EditorUtility.IsPersistent(textoUI.gameObject.transform.root.gameObject))
                continue;

            LocalizedUIText localizador = textoUI.GetComponent<LocalizedUIText>();
            if (localizador == null)
            {
                localizador = textoUI.gameObject.AddComponent<LocalizedUIText>();
                string claveSugerida = textoUI.gameObject.name.ToLower().Replace(" ", "_").Replace("(", "").Replace(")", "");
                localizador.translationKey = claveSugerida;
                contadorAgregados++;
                EditorUtility.SetDirty(textoUI.gameObject);
            }

            // Genera la línea de código con el texto actual como base para el español
            sb.AppendLine($"{{ \"{localizador.translationKey}\", new string[] {{ \"{textoUI.text}\", \"[Traducir al inglés]\" }} }},");
        }

        Debug.Log(sb.ToString());
        Debug.Log($"¡Escaneo completado! Se procesaron los textos. Nuevos componentes configurados: {contadorAgregados}");
    }
}
#endif