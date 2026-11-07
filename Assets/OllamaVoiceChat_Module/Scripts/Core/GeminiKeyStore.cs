using UnityEngine;

namespace OllamaIntegration
{
    /// <summary>
    /// Guarda la API key de Gemini escrita por el usuario en el propio
    /// dispositivo (PlayerPrefs). No viaja en el APK, no se serializa en la
    /// escena y no se sube al repositorio: se escribe una vez en el teléfono.
    /// </summary>
    public static class GeminiKeyStore
    {
        private const string PrefsKey = "arvison.gemini.api_key";

        public static bool HasKey => !string.IsNullOrWhiteSpace(Load());

        public static string Load()
        {
            try
            {
                string value = PlayerPrefs.GetString(PrefsKey, string.Empty);
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
            catch
            {
                return null;
            }
        }

        public static void Save(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            PlayerPrefs.SetString(PrefsKey, key.Trim());
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        /// <summary>Pista para mostrar en pantalla sin revelar la clave completa.</summary>
        public static string Hint()
        {
            string key = Load();
            if (string.IsNullOrEmpty(key)) return null;
            if (key.Length <= 4) return "••••";
            return "••••" + key.Substring(key.Length - 4);
        }
    }
}
