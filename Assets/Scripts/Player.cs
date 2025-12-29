using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Simple player model holding hole cards and basic metadata.
/// 玩家模型：保存手牌（hole cards）以及一些基础元数据和下注状态。
/// 该类用于示例用途，包含筹码栈、当前下注、是否弃牌/全下等字段。
/// </summary>

public enum EPlayerAction
{
    Fold,
    Check,
    Call,
    Bet,
    Raise,
    AllIn
}

public class Player
{

    protected bool acting = false;

    /// <summary>Player index (0-based).</summary>
    /// <summary>玩家索引（从 0 开始）。</summary>
    public int id;

    /// <summary>Display name.</summary>
    /// <summary>显示名称。</summary>
    public string name;

    /// <summary>Hole cards (2 cards in Texas Hold'em).</summary>
    /// <summary>底牌（Hole cards），德州扑克每人两张。</summary>
    /// Move mutable state into PlayData and expose as `data`.
    public PlayData data = new PlayData();

    public Player(int id, string name)
    {
        this.id = id;
        this.name = name;
    }

    /// <summary>Reset transient per-hand fields before a new hand.</summary>
    /// <summary>重置每手牌的临时状态：清空底牌、重置下注和状态标志供下一手使用。</summary>
    public void ResetForHand()
    {
        data.ResetForHand();
    }

    public IEnumerator Act(ERoundPhase phase)
    {
        BindUI(phase);
        acting = true;
        while (acting)
        {
            yield return null;

        }
        HumanPlayerUI.Instance.OnAction -= HandlePlayerAction;
    }



    protected void BindUI(ERoundPhase phase)
    {
        switch (phase)
        {
            case ERoundPhase.Preflop:
                HumanPlayerUI.Instance.ShowBet();
                break;
            case ERoundPhase.Flop:
            case ERoundPhase.Turn:
            case ERoundPhase.River:
                HumanPlayerUI.Instance.ShowActionButtons();
                break;
        }
        HumanPlayerUI.Instance.OnAction += HandlePlayerAction;
    }

    protected void HandlePlayerAction(EPlayerAction action, params object[] args)
    {
        var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
        if (game == null)
        {
            acting = false;
            return;
        }
        int need = game.currentBet - data.CurrentBet;
        switch (action)
        {
            case EPlayerAction.Fold:
                data.Folded = true;
                break;
            case EPlayerAction.Check:
                if (need > 0)
                {
                    int pay2 = Mathf.Min(need, data.Stack);
                    data.Stack -= pay2;
                    data.CurrentBet += pay2;
                    if (data.Stack <= 0) data.AllIn = true;
                }
                break;
            case EPlayerAction.Call:
                {
                    int pay = Mathf.Min(need, data.Stack);
                    data.Stack -= pay;
                    data.CurrentBet += pay;
                    if (data.Stack <= 0) data.AllIn = true;
                }
                break;
            case EPlayerAction.Bet:
                {
                    int amount = 0;
                    if (args != null && args.Length > 0 && args[0] is int) amount = (int)args[0];
                    int desired = Mathf.Max(amount, game.data.BigBlindAmount);
                    int pay = Mathf.Min(desired, data.Stack);
                    data.Stack -= pay;
                    data.CurrentBet += pay;
                    if (data.CurrentBet > game.currentBet) game.currentBet = data.CurrentBet;
                    if (data.Stack <= 0) data.AllIn = true;
                }
                break;
            case EPlayerAction.Raise:
                {
                    int amount = 0;
                    if (args != null && args.Length > 0 && args[0] is int) amount = (int)args[0];
                    int minRaise = Mathf.Max(1, game.data.BigBlindAmount);
                    int desiredExtra = Mathf.Max(amount, minRaise);
                    int totalPay = Mathf.Min(need + desiredExtra, data.Stack);
                    data.Stack -= totalPay;
                    data.CurrentBet += totalPay;
                    if (data.Stack <= 0) data.AllIn = true;
                    if (data.CurrentBet > game.currentBet) game.currentBet = data.CurrentBet;
                }
                break;
            case EPlayerAction.AllIn:
                {
                    int payAll = data.Stack;
                    data.CurrentBet += payAll;
                    data.Stack = 0;
                    data.AllIn = true;
                    if (data.CurrentBet > game.currentBet) game.currentBet = data.CurrentBet;
                }
                break;
        }
        acting = false;
    }
}
