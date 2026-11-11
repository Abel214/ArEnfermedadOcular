using System;
using Firebase.AI;
using UnityEngine;

public class GeminiSmokeTest : MonoBehaviour
{
    async void Start()
    {
        while (!FirebaseBootstrap.Ready) await System.Threading.Tasks.Task.Yield();

        try
        {
            var model = FirebaseBootstrap.AI.GetGenerativeModel("gemini-3.5-flash-lite");
            var r = await model.GenerateContentAsync("Responde solo: OK");
            Debug.Log($"Gemini respondió: {r.Text}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Fallo Gemini: {e.Message}");
        }
    }
}