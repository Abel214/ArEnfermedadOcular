using Firebase;
using Firebase.AI;
using Firebase.AppCheck;
using UnityEngine;

public class FirebaseBootstrap : MonoBehaviour
{
    public static FirebaseAI AI { get; private set; }
    public static bool Ready { get; private set; }

    async void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // App Check ANTES de cualquier otro servicio Firebase.
        // Solo se configura si estamos en el Editor o en un Development Build.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 1. Fijamos el token de depuración antes de registrar la fábrica.
        // NOTA: Asegúrate de tener una clase "DebugSecrets" con la constante "AppCheckToken".
        DebugAppCheckProviderFactory.Instance.SetDebugToken(DebugSecrets.AppCheckToken);
        
        // 2. Registramos la fábrica de depuración.
        FirebaseAppCheck.SetAppCheckProviderFactory(DebugAppCheckProviderFactory.Instance);
#endif

        // El resto del bootstrap queda igual
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
        {
            Debug.LogError($"Firebase no disponible: {status}");
            return;
        }

        AI = FirebaseAI.GetInstance(FirebaseAI.Backend.GoogleAI());
        Ready = true;
        Debug.Log("Firebase + App Check + AI Logic listos");
    }
}