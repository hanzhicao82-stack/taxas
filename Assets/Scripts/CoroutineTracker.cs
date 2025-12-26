using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Simple per-GameObject coroutine tracker. Use CoroutineTracker.Start(owner, routine)
/// to start a coroutine that will be recorded and automatically stopped when the
/// owner is destroyed. Also provides Stop and StopAll helpers.
/// </summary>
public class CoroutineTracker : MonoBehaviour
{
    private List<Coroutine> tracked = new List<Coroutine>();

    private Coroutine StartTracked(IEnumerator routine)
    {
        Coroutine handle = null;
        IEnumerator Wrapper()
        {
            yield return routine;
            tracked.Remove(handle);
        }
        handle = StartCoroutine(Wrapper());
        tracked.Add(handle);
        return handle;
    }

    private void StopTracked(Coroutine c)
    {
        if (c == null) return;
        try { StopCoroutine(c); } catch { }
        tracked.Remove(c);
    }

    private void StopAllTracked()
    {
        foreach (var c in tracked.ToList())
        {
            if (c != null)
            {
                try { StopCoroutine(c); } catch { }
            }
        }
        tracked.Clear();
    }

    private void OnDestroy()
    {
        StopAllTracked();
    }

    // Static convenience wrappers
    public static Coroutine Start(MonoBehaviour owner, IEnumerator routine)
    {
        if (owner == null) return null;
        var t = owner.gameObject.GetComponent<CoroutineTracker>();
        if (t == null) t = owner.gameObject.AddComponent<CoroutineTracker>();
        return t.StartTracked(routine);
    }

    public static void Stop(MonoBehaviour owner, Coroutine c)
    {
        if (owner == null || c == null) return;
        var t = owner.gameObject.GetComponent<CoroutineTracker>();
        if (t != null) t.StopTracked(c);
    }

    public static void StopAll(MonoBehaviour owner)
    {
        if (owner == null) return;
        var t = owner.gameObject.GetComponent<CoroutineTracker>();
        if (t != null) t.StopAllTracked();
    }
}
