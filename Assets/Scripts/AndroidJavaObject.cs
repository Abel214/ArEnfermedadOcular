using UnityEngine;
using UnityEngine.Android;

public class VoiceCommandManager : MonoBehaviour
{
    void Start()
    {
        // Pedir permiso de micrófono
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
    }
}