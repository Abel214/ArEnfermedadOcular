using UnityEngine;

namespace OllamaIntegration
{
    /// <summary>
    /// API key de Gemini. Dos niveles:
    /// 1) CLAVE DE FÁBRICA (BuiltInApiKey): viaja dentro del APK, la app
    ///    funciona sin que el usuario escriba nada. OJO: en el APK se puede
    ///    extraer; limita la cuota y restringe el acceso en Google Cloud.
    /// 2) Clave escrita por el usuario (PlayerPrefs): si existe, tiene
    ///    prioridad y reemplaza a la de fábrica.
    /// </summary>
    public static class GeminiKeyStore
    {
        // ═══════════════════════════════════════════════════════════
        // ⬇️ PEGA AQUÍ TU API KEY REAL DE GOOGLE AI STUDIO ⬇️
        // Mientras quede el texto de marcador, la clave de fábrica
        // está desactivada (HasKey devolverá false si no hay clave
        // guardada por el usuario).
        // ═══════════════════════════════════════════════════════════
        private const string BuiltInApiKey = "PEGA_TU_API_KEY_DE_GEMINI_AQUI";

        private const string PrefsKey = "arvison.gemini.api_key";

        private static bool builtinParsed;
        private static string builtinKey;

        /// <summary>Clave de fábrica, null si está en marcador o vacía.</summary>
        private static string GetBuiltInKey()
        {
            if (!builtinParsed)
            {
                builtinParsed = true;
                string k = BuiltInApiKey?.Trim();
                if (!string.IsNullOrEmpty(k) &&
                    !k.EndsWith("PEGA_TU_API_KEY_DE_GEMINI_AQUI") &&
                    k.Length >= 20)
                {
                    builtinKey = k;
                }
                else
                {
                    builtinKey = null;
                }
            }
            return builtinKey;
        }

        /// <summary>True si existe clave de fábrica (además de la del usuario).</summary>
        public static bool HasBuiltInKey => GetBuiltInKey() != null;

        /// <summary>True si hay alguna clave utilizable (usuario o fábrica).</summary>
        public static bool HasKey => !string.IsNullOrWhiteSpace(Load());

        public static string Load()
        {
            // La clave que el usuario escribió en el teléfono tiene prioridad
            string user = null;
            try
            {
                string value = PlayerPrefs.GetString(PrefsKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(value)) user = value.Trim();
            }
            catch { /* PlayerPrefs indisponible */ }

            if (user != null) return user;
            return GetBuiltInKey();
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
            // Tras limpiar la clave del usuario se vuelve a usar la de fábrica (si existe).
        }

        /// <summary>Pista para mostrar en pantalla sin revelar la clave completa.</summary>
        public static string Hint()
        {
            string key = Load();
            if (string.IsNullOrEmpty(key)) return null;

            string origen = HasBuiltInKey && key == GetBuiltInKey() ? "fábrica" : "este teléfono";
            if (key.Length <= 4) return "•••• (" + origen + ")";
            return "••••" + key.Substring(key.Length - 4) + " (" + origen + ")";
        }
    }
}
