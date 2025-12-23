using System;
using System.Collections.Generic;

public enum Events
{
    Flop,
    Turn,
    River,
    HandStarted,
}

/// <summary>
/// 以枚举 `Events` 为键的简单事件总线，负载为 object。
/// Subscribe: GameEventBus.Subscribe(Events.CommunityUpdated, obj => { var payload = (Tuple<List<Card>,List<Card>>)obj; ... });
/// Submit:   GameEventBus.Submit(Events.CommunityUpdated, Tuple.Create(communityList, addedList));
/// </summary>
public static class GameEventBus
{
    private static readonly Dictionary<Events, Delegate> handlers = new Dictionary<Events, Delegate>();
    private static readonly object sync = new object();

    public static void Subscribe(Events e, Action<object> handler)
    {
        if (handler == null) return;
        lock (sync)
        {
            if (handlers.TryGetValue(e, out var d)) handlers[e] = Delegate.Combine(d, handler);
            else handlers[e] = handler;
        }
    }


    public static void Unsubscribe(Events e, Action<object> handler)
    {
        if (handler == null) return;
        lock (sync)
        {
            if (!handlers.TryGetValue(e, out var d)) return;
            var nd = Delegate.Remove(d, handler);
            if (nd == null) handlers.Remove(e);
            else handlers[e] = nd;
        }
    }

    public static void Submit(Events e, object payload = null)
    {
        Delegate d = null;
        lock (sync)
        {
            handlers.TryGetValue(e, out d);
        }
        if (d == null) return;
        var invocationList = d.GetInvocationList();
        foreach (var del in invocationList)
        {
            try { ((Action<object>)del)(payload); }
            catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
        }
    }
}
