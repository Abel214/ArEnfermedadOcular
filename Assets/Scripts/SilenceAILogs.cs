// SilenceAILogs.cs
using UnityEngine;

public class SilenceAILogs : MonoBehaviour
{
    void Awake()
    {
        Application.logMessageReceived += FilterLogs;
    }

    void FilterLogs(string condition, string stackTrace, LogType type)
    {
        // No hace nada — solo intercepta para que no aparezca en consola
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= FilterLogs;
    }
}