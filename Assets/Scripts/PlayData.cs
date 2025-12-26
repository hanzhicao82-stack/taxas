using System;
using System.Collections.Generic;

/// <summary>
/// Generic wrapper that holds a value and notifies listeners when it changes.
/// 使用泛型 Data<T> 封装单个值，调用 Set 或设置 Value 会触发 OnValueChanged(oldValue, newValue)。
/// </summary>
public class Data<T>
{
    public event Action<T, T> OnValueChanged;

    private T _value;

    public Data() { }
    public Data(T initial)
    {
        _value = initial;
    }

    public T Value
    {
        get => _value;
        set => Set(value);
    }

    public void Set(T newValue)
    {
        if (EqualityComparer<T>.Default.Equals(_value, newValue)) return;
        var old = _value;
        _value = newValue;
        OnValueChanged?.Invoke(old, newValue);
    }
}

/// <summary>
/// Encapsulates per-player mutable data and exposes change events.
/// 将玩家的可变数据（筹码栈、底牌、下注等）封装到此类，并提供监听器事件。
/// </summary>
public class PlayData
{
    // Backing Data<T> instances (retain for event subscriptions)
    public Data<int> StackData = new Data<int>(1000);
    public Data<List<Card>> HoleData = new Data<List<Card>>(new List<Card>());
    public Data<int> CurrentBetData = new Data<int>(0);
    public Data<bool> FoldedData = new Data<bool>(false);
    public Data<bool> AllInData = new Data<bool>(false);
    public Data<bool> ActiveData = new Data<bool>(true);
    public Data<float> AggressionData = new Data<float>(1.0f);

    // Direct properties for convenient get/set (no need to use .Value/Set)
    public int Stack { get => StackData.Value; set => StackData.Set(value); }
    public List<Card> Hole { get => HoleData.Value; set => HoleData.Set(value); }
    public int CurrentBet { get => CurrentBetData.Value; set => CurrentBetData.Set(value); }
    public bool Folded { get => FoldedData.Value; set => FoldedData.Set(value); }
    public bool AllIn { get => AllInData.Value; set => AllInData.Set(value); }
    public bool Active { get => ActiveData.Value; set => ActiveData.Set(value); }
    public float Aggression { get => AggressionData.Value; set => AggressionData.Set(value); }

    public PlayData() { }

    public void AddHole(Card c)
    {
        var old = HoleData.Value ?? new List<Card>();
        var copy = new List<Card>(old) { c };
        HoleData.Set(copy);
    }

    public void ClearHole()
    {
        var old = HoleData.Value;
        if (old == null || old.Count == 0) return;
        HoleData.Set(new List<Card>());
    }

    public void ResetForHand()
    {
        ClearHole();
        CurrentBet = 0;
        Folded = false;
        AllIn = false;
        Active = true;
    }
}
