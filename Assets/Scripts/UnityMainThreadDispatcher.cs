using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher instance;
    private Queue<Action> queue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (instance == null)
        {
            var go = new GameObject("MainThreadDispatcher");
            instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return instance;
    }

    public void Enqueue(Action action)
    {
        lock (queue) { queue.Enqueue(action); }
    }

    void Update()
    {
        lock (queue)
        {
            while (queue.Count > 0)
                queue.Dequeue().Invoke();
        }
    }
}