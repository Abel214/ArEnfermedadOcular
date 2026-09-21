#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using OllamaIntegration;

namespace OllamaEditor
{
    [CustomEditor(typeof(OllamaChatManager))]
    public class OllamaChatManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var manager = (OllamaChatManager)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Pruebas Rápidas (Editor y Play Mode)", EditorStyles.boldLabel);

            if (GUILayout.Button("1. Probar Conexión (Ping / Tags)"))
            {
                manager.TestConnection();
            }

            if (GUILayout.Button("2. Enviar Mensaje de Prueba"))
            {
                manager.SendTestPrompt();
            }

            if (GUILayout.Button("3. Limpiar Historial"))
            {
                manager.ClearHistory();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Conexión con Celular Android (USB)", EditorStyles.boldLabel);

            if (GUILayout.Button("📱 Conectar Celular por USB (ADB Reverse 11434)", GUILayout.Height(30)))
            {
                SetupAdbReverse();
            }
        }

        [MenuItem("Ollama/Conectar Celular USB (ADB Reverse 11434)", false, 20)]
        public static void SetupAdbReverse()
        {
            string adbPath = FindAdbPath();

            if (string.IsNullOrEmpty(adbPath) || !File.Exists(adbPath))
            {
                EditorUtility.DisplayDialog(
                    "ADB no encontrado",
                    "No se encontró 'adb.exe' automáticamente. Asegúrate de tener instalado el módulo de Android Build Support en Unity Hub.",
                    "Entendido"
                );
                return;
            }

            try
            {
                // 1. Listar dispositivos conectados
                var procDevices = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = adbPath,
                        Arguments = "devices",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                procDevices.Start();
                string outputDevices = procDevices.StandardOutput.ReadToEnd();
                procDevices.WaitForExit();

                // 2. Ejecutar reverse port forwarding
                var procReverse = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = adbPath,
                        Arguments = "reverse tcp:11434 tcp:11434",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                procReverse.Start();
                string outputReverse = procReverse.StandardOutput.ReadToEnd();
                string errorReverse = procReverse.StandardError.ReadToEnd();
                procReverse.WaitForExit();

                if (procReverse.ExitCode == 0)
                {
                    UnityEngine.Debug.Log($"[Ollama] ¡Puerto 11434 redirigido exitosamente al celular por USB!\nDispositivos:\n{outputDevices}");
                    EditorUtility.DisplayDialog(
                        "¡Celular Conectado Exitosamente!",
                        "Se ha establecido la conexión directa por cable USB (ADB Reverse 11434).\n\n" +
                        "Ahora tu celular puede comunicarse con Ollama en tu PC usando 'http://127.0.0.1:11434' como Base URL.\n\n" +
                        "Dispositivos detectados:\n" + outputDevices.Trim(),
                        "Aceptar"
                    );
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[Ollama] ADB Reverse falló: {errorReverse}");
                    EditorUtility.DisplayDialog(
                        "Aviso de Conexión USB",
                        "No se pudo completar la redirección ADB.\n\n" +
                        "Por favor verifica:\n" +
                        "1. Tu celular está conectado por cable USB a la computadora.\n" +
                        "2. Tienes activada la 'Depuración USB' en las Opciones de Desarrollador de tu celular.\n" +
                        "3. Aceptaste el permiso de 'Permitir depuración USB' en la pantalla del celular.\n\n" +
                        "Detalle: " + (string.IsNullOrEmpty(errorReverse) ? outputDevices : errorReverse),
                        "Entendido"
                    );
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[Ollama] Error al ejecutar ADB: {ex.Message}");
                EditorUtility.DisplayDialog("Error ADB", ex.Message, "Cerrar");
            }
        }

        private static string FindAdbPath()
        {
            // 1. Buscar en el SDK de Android integrado con Unity
            string unityEditorDir = Path.GetDirectoryName(EditorApplication.applicationPath);
            if (!string.IsNullOrEmpty(unityEditorDir))
            {
                string unityAdb = Path.Combine(unityEditorDir, "Data", "PlaybackEngines", "AndroidPlayer", "SDK", "platform-tools", "adb.exe");
                if (File.Exists(unityAdb)) return unityAdb;
            }

            // 2. Buscar en AppData local del usuario
            string localSdk = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk", "platform-tools", "adb.exe");
            if (File.Exists(localSdk)) return localSdk;

            // 3. Fallback al comando del sistema
            return "adb.exe";
        }
    }
}
#endif

