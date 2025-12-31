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
    public static Player current = null;
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
        if (CanAct())
        {
            Player.current = this;
            BindUI(phase);

            acting = true;
            while (acting)
            {
                yield return null;
            }
            HumanPlayerUI.Instance.OnAction -= HandlePlayerAction;
            Player.current = null;
        }
        else
        {
            yield return null;
        }

    }



    bool CanAct()
    {
        return !data.Folded && !data.AllIn && data.Stack > 0;
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
                    if (game != null) game.data.Pot += pay2;
                    if (data.Stack <= 0)
                        data.AllIn = true;
                }
                break;
            case EPlayerAction.Call:
                {
                    // Follow standard call semantics:
                    // - `need` is how much the player must add to match `game.currentBet`.
                    // - If `need <= 0` there's nothing to call (should be a check); do nothing here.
                    // - The player pays at most their remaining stack (all-in allowed).
                    // - If the player cannot fully cover `need` (all-in), their `CurrentBet` will be
                    //   less than `game.currentBet` and side-pot logic elsewhere should handle it.
                    if (need <= 0)
                        break;

                    int pay = Mathf.Min(need, data.Stack);
                    // Deduct chips from the player's stack and add to their current bet
                    data.Stack -= pay;
                    data.CurrentBet += pay;
                    // Immediately reflect the paid chips in the main pot
                    if (game != null) game.data.Pot += pay;
                    // If the player used all chips mark as all-in
                    if (data.Stack <= 0)
                        data.AllIn = true;
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
                    if (game != null) game.data.Pot += pay;
                    if (data.CurrentBet > game.currentBet) game.currentBet = data.CurrentBet;
                    if (data.Stack <= 0)
                        data.AllIn = true;
                }
                break;
            case EPlayerAction.Raise:
                {
                    // 用户选择加注：
                    // - 从参数中读取玩家想要额外加注的 `amount`（以筹码为单位）。
                    // - 计算最小加注额 `minRaise`（至少为大盲注或 1），并取两者较大值作为实际额外需求 `desiredExtra`。
                    // - 总共支付的金额为当前跟注需求 `need` 加上 `desiredExtra`，但不能超过玩家剩余筹码 `data.Stack`。
                    // - 从玩家 `data.Stack` 扣除该金额，增加 `data.CurrentBet`，并立即将该金额加入游戏的 `data.Pot`，
                    //   以便 UI 和其他逻辑能实时看到底池变化。
                    // - 若玩家耗尽筹码则标记为 All-In，并在必要时更新游戏的 `currentBet`。
                    int amount = 0;
                    if (args != null && args.Length > 0 && args[0] is int)
                        amount = (int)args[0];
                    // 最小加注不低于大盲注（或 1）
                    int minRaise = Mathf.Max(1, game.data.BigBlindAmount);
                    // 实际需要额外加注 = 玩家指定的 amount 与 minRaise 中的较大者
                    int desiredExtra = Mathf.Max(amount, minRaise);
                    // 总共要支付 = 需要跟注的金额 + 额外加注；不能超过玩家当前筹码
                    int totalPay = Mathf.Min(need + desiredExtra, data.Stack);
                    data.Stack -= totalPay;
                    data.CurrentBet += totalPay;
                    // 立即更新游戏层的底池，保证 UI/其他系统可见
                    if (game != null)
                        game.data.Pot += totalPay;
                    // 如果耗尽筹码，标记为全下
                    if (data.Stack <= 0)
                        data.AllIn = true;
                    // 如果当前下注超过 game.currentBet，则更新游戏的 currentBet
                    if (data.CurrentBet > game.currentBet)
                        game.currentBet = data.CurrentBet;
                }
                break;
            case EPlayerAction.AllIn:
                {
                    int payAll = data.Stack;
                    data.CurrentBet += payAll;
                    if (game != null) game.data.Pot += payAll;
                    data.Stack = 0;
                    data.AllIn = true;
                    if (data.CurrentBet > game.currentBet) game.currentBet = data.CurrentBet;
                }
                break;
        }
        acting = false;
    }
}
