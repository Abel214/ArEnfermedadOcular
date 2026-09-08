using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LocalizationManager : MonoBehaviour
{
    private static LocalizationManager instance;
    public static LocalizationManager Instance => instance;

    // Diccionario estático para acceso inmediato sin depender del orden de Awake
    private static Dictionary<string, string[]> database = new Dictionary<string, string[]>()
    {
        { "titulo_acerca", new string[] { "ACERCA DE", "ABOUT" } },
        { "labelautor", new string[] { "Autores", "Authors" } },
        { "labelidioma", new string[] { "Idioma", "Language" } },
        { "labelversion", new string[] { "Versión:", "Version:" } },
        { "labelAutor", new string[] { "Desarrollado por:", "Developed by:" } },
        { "labeldirector", new string[] { "Director:", "Director:" } },
        { "labeldescripcion", new string[] { "Aplicación de Realidad Aumentada para la visualización educativa de enfermedades oculares.", "Augmented Reality application for educational visualization of ocular diseases." } },
        { "btn_cerrar", new string[] { "X", "X" } },
        { "btn_tutorial", new string[] { "Tutorial", "Tutorial" } },
        { "labelfacultad", new string[] { "Facultad de la Salud Humana", "Faculty of Human Health" } },
        { "btn_empezar", new string[] { "Empezar", "Start" } },
        { "titulo_config", new string[] { "CONFIGURACIÓN", "SETTINGS" } },
        { "btn_espanol", new string[] { "Español", "Spanish" } },
        { "btn_ingles", new string[] { "Ingles", "English" } },
        { "labelanio", new string[] { "Loja - Ecuador, 2026", "Loja - Ecuador, 2026" } },
        { "labelvoz", new string[] { "Volumen de Voz", "Voice Volume" } },
        { "labeluniversidad", new string[] { "Universidad Nacional de Loja", "National University of Loja" } },
        { "titulo_app", new string[] { "AR ENFERMEDAD OCULAR", "AR OCULAR DISEASE" } },
        { "labelvolumen", new string[] { "100%", "100%" } },
        { "btn_salida", new string[] { "Salida", "Exit" } }
    };

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static string GetText(string key)
    {
        string idioma = PlayerPrefs.GetString("tts_idioma", "es");
        int index = (idioma == "en") ? 1 : 0;

        if (database.ContainsKey(key))
        {
            return database[key][index];
        }

        return key;
    }

    public static void ActualizarTodosLosTextos()
    {
        LocalizedUIText[] textos = FindObjectsOfType<LocalizedUIText>();
        foreach (LocalizedUIText t in textos)
        {
            t.ActualizarTexto();
        }
    }
}