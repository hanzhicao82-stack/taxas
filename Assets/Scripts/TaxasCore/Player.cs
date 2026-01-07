using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Threading.Tasks;

/// <summary>
/// Simple player model holding hole cards and basic metadata.
/// 玩家模型：保存手牌（hole cards）以及一些基础元数据和下注状态。
/// 该类用于示例用途，包含筹码栈、当前下注、是否弃牌/全下等字段。
/// </summary>

public enum EPlayerAction
{
    //summary> Player actions during betting rounds. </summary>
    //summary> 下注轮次中的玩家操作。 </summary>
    Fold,
    //summary> Check action (no additional bet). </summary>
    //summary> 过牌操作（不追加下注）。 </summary>
    Check,
    //summary> Call action (match current bet). </summary>
    //summary> 跟注操作（匹配当前下注）。 </summary>
    Call,
    //summary> Bet action (initial bet). </summary> 
    //summary> 下注操作（初始下注）。 </summary>
    Bet,
    //summary> Raise action (increase current bet). </summary>
    //summary> 加注操作（增加当前下注）。 </summary>
    Raise,
    //summary> All-in action (bet all remaining chips). </summary>
    //summary> 全下操作（下注所有剩余筹码）。 </summary>
    AllIn
    ,
    // Buy-in / add stack
    BuyIn
}

/// <summary>
/// Player model used by the game loop and UI.
/// - Mutable per-hand fields (stack/current bet/fold/all-in) are stored in `data` (`PlayData`).
/// - `Act()` drives per-player coroutine-based action flow for UI-driven play.
/// </summary>
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

    // Helper to apply a payment from this player to the pot and update per-hand tracking
    // - deducts from `data.Stack`
    // - increases `data.CurrentBet` and `data.TotalCommitted`
    // - updates `game.data.Pot` if `game` is provided
    // - marks `data.AllIn` if stack reaches zero
    private void ApplyPayment(int amount, PokerGame game = null)
    {
        data.Stack -= amount;
        data.CurrentBet += amount;
        data.TotalCommitted += amount;
        if (game != null)
            game.data.Pot += amount;
        if (data.Stack <= 0)
            data.AllIn = true;
    }

    // Encapsulated handlers for player actions to keep switch body concise
    private bool HandleFold()
    {
        data.Folded = true;
        return true;
    }

    private bool HandleCheck(int need)
    {
        if (need > 0)
        {
            Debug.LogWarning($"P{this.id + 1} attempted to Check but needs to call {need}.");
            return false;
        }
        return true;
    }

    private bool HandleCall(PokerGame game, int need)
    {
        if (need <= 0)
            return false;

        // If player does not have enough chips to fully call, do NOT auto-all-in here.
        // Require the player to explicitly choose AllIn instead of silently committing a partial call.
        if (data.Stack < need)
        {
            Debug.LogWarning($"P{this.id + 1} cannot Call: insufficient chips ({data.Stack} < {need}). Choose AllIn instead.");
            return false;
        }

        int pay = Mathf.Min(need, data.Stack);
        int beforeStack = data.Stack;
        int beforeBet = data.CurrentBet;
        ApplyPayment(pay, game);
        Debug.Log($"P{this.id + 1} CALL pay={pay}, stackBefore={beforeStack}, stackAfter={data.Stack}, currentBetBefore={beforeBet}, currentBetAfter={data.CurrentBet}, totalCommitted={data.TotalCommitted}, gamePot={game?.data.Pot}");
        return true;
    }

    private bool HandleBet(PokerGame game, object[] args)
    {
        int amount = 0;
        if (args != null && args.Length > 0 && args[0] is int) amount = (int)args[0];
        int desired = Mathf.Max(amount, game.data.BigBlindAmount);
        int pay = Mathf.Min(desired, data.Stack);
        int beforeStackBet = data.Stack;
        int beforeCurrentBet = data.CurrentBet;
        ApplyPayment(pay, game);
        int prevGameCurrent = game.currentBet;
        if (data.CurrentBet > prevGameCurrent)
        {
            int delta = data.CurrentBet - prevGameCurrent;
            game.currentBet = data.CurrentBet;
            game.lastRaiseAmount = Math.Max(1, delta);
        }
        Debug.Log($"P{this.id + 1} BET pay={pay}, stackBefore={beforeStackBet}, stackAfter={data.Stack}, currentBetBefore={beforeCurrentBet}, currentBetAfter={data.CurrentBet}, totalCommitted={data.TotalCommitted}, gamePot={game?.data.Pot}");
        return true;
    }

    private bool HandleRaise(PokerGame game, object[] args, int need)
    {
        int amount = 0;
        if (args != null && args.Length > 0 && args[0] is int)
            amount = (int)args[0];
        // Minimum extra raise should honor both configured minimum (big blind fraction)
        // and the previous raise amount within the same betting round.
        int minByConfig = Mathf.Max(1, game.data.BigBlindAmount);
        int minByLastRaise = Mathf.Max(1, game.lastRaiseAmount);
        int minRaise = Mathf.Max(minByConfig, minByLastRaise);
        int desiredExtra = Mathf.Max(amount, minRaise);
        int totalPay = Mathf.Min(need + desiredExtra, data.Stack);
        int beforeStackRaise = data.Stack;
        int beforeCB = data.CurrentBet;
        int prevGameCurrent = game.currentBet;
        ApplyPayment(totalPay, game);
        if (data.CurrentBet > prevGameCurrent)
        {
            int delta = data.CurrentBet - prevGameCurrent;
            game.currentBet = data.CurrentBet;
            // update lastRaiseAmount to the actual raise delta for subsequent raises
            game.lastRaiseAmount = Math.Max(1, delta);
        }
        Debug.Log($"P{this.id + 1} RAISE pay={totalPay}, stackBefore={beforeStackRaise}, stackAfter={data.Stack}, currentBetBefore={beforeCB}, currentBetAfter={data.CurrentBet}, totalCommitted={data.TotalCommitted}, gamePot={game?.data.Pot}");
        return true;
    }

    private bool HandleAllIn(PokerGame game)
    {
        int payAll = data.Stack;
        int beforeStackAllIn = data.Stack;
        int beforeCBA = data.CurrentBet;
        int prevGameCurrent = game.currentBet;
        ApplyPayment(payAll, game);
        if (data.CurrentBet > prevGameCurrent)
        {
            int delta = data.CurrentBet - prevGameCurrent;
            game.currentBet = data.CurrentBet;
            game.lastRaiseAmount = Math.Max(1, delta);
        }
        Debug.Log($"P{this.id + 1} ALLIN pay={payAll}, stackBefore={beforeStackAllIn}, stackAfter={data.Stack}, currentBetBefore={beforeCBA}, currentBetAfter={data.CurrentBet}, totalCommitted={data.TotalCommitted}, gamePot={game?.data.Pot}");
        return true;
    }

    private bool HandleBuyIn(object[] args)
    {
        if (args == null || args.Length == 0) return false;
        try
        {
            int amt = Convert.ToInt32(args[0]);
            if (amt <= 0) return false;
            data.Stack += amt;
            Debug.Log($"P{this.id + 1} BuyIn amount={amt}, new stack={data.Stack}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"BuyIn failed: {ex.Message}");
            return false;
        }
    }

    // Async version of Act() which awaits UI action via TaskCompletionSource
    private TaskCompletionSource<bool> actionTcs;
    public async Task ActAsync(ERoundPhase phase)
    {
        if (CanAct())
        {
            acting = true;
            current = this;
            BindUI(phase);
            actionTcs = new TaskCompletionSource<bool>();
            if (HumanPlayerUI.Instance != null)
                HumanPlayerUI.Instance.OnAction += HandlePlayerAction;
            try
            {
                await actionTcs.Task;
            }
            finally
            {
                if (HumanPlayerUI.Instance != null)
                    HumanPlayerUI.Instance.OnAction -= HandlePlayerAction;
                current = null;
                acting = false;
            }
        }
        else
        {
            await Task.Yield();
        }
    }



    bool CanAct()
    {
        // Player may act if not folded, not already all-in and has chips.
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
        bool handled = false;
        switch (action)
        {
            case EPlayerAction.Fold:
                handled = HandleFold();
                break;
            case EPlayerAction.Check:
                handled = HandleCheck(need);
                break;
            case EPlayerAction.Call:
                handled = HandleCall(game, need);
                break;
            case EPlayerAction.Bet:
                handled = HandleBet(game, args);
                break;
            case EPlayerAction.Raise:
                handled = HandleRaise(game, args, need);
                break;
            case EPlayerAction.AllIn:
                handled = HandleAllIn(game);
                break;
            case EPlayerAction.BuyIn:
                handled = HandleBuyIn(args);
                break;
        }
        // Only signal completion if the handler actually applied an action
        if (handled)
        {
            try { actionTcs?.TrySetResult(true); } catch { }
            acting = false;
        }
    }
}
