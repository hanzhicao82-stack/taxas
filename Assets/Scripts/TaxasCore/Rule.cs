using System;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Rule: utility helpers and documented defaults for Texas Hold'em rules used by the project.
/// - Provides canonical behavior for handling insufficient-stack situations (call vs all-in vs fold)
/// - Small helper methods for callers to reuse when applying payments.
///
/// This file intentionally keeps logic simple and conservative: by default the engine treats
/// "insufficient to fully call" as requiring the player to explicitly choose AllIn (or Fold).
/// That matches common tournament/cash-game house rules where a short stack going "all-in"
/// participates up to their stack and side-pots are created for any excess.
/// </summary>
public static class Rule
{
    /// <summary>
    /// How the engine should treat a player who does not have enough chips to fully match the required call.
    /// - RequireAllInOrFold: player must explicitly choose AllIn or Fold (default in this project).
    /// - AllowPartialCall: allow a partial call to be treated as a valid call (less common; can be used for
    ///   simplified/house-specific rules).
    /// </summary>
    public enum InsufficientStackBehavior
    {
        RequireAllInOrFold,
        AllowPartialCall
    }

    /// <summary>
    /// Project default: require explicit AllIn or Fold when stack &lt; needed call amount.
    /// </summary>
    public static InsufficientStackBehavior DefaultInsufficientStackBehavior => InsufficientStackBehavior.RequireAllInOrFold;

    /// <summary>
    /// Return whether partial calls (paying less than the full need) should be accepted as a Call.
    /// Default: false (i.e. require AllIn or Fold).
    /// </summary>
    public static bool IsPartialCallAccepted(InsufficientStackBehavior behavior) => behavior == InsufficientStackBehavior.AllowPartialCall;

    /// <summary>
    /// Clamp the payment the player can make toward a call. This does NOT decide whether the
    /// action is permitted — caller should consult the rule behavior first. This helper simply
    /// returns the amount that will actually be transferred if the player pays everything they have.
    /// </summary>
    public static int ClampCallPayment(int need, int stack)
    {
        if (need <= 0) return 0;
        return Math.Min(need, Math.Max(0, stack));
    }

    /// <summary>
    /// Short explanation of side-pot semantics for callers or documentation consumers.
    /// </summary>
    public static string SidePotExplanation()
    {
        return "If a player goes all-in for less than the full bet, they compete for the main pot up to their contribution; " +
               "any additional chips form side pots that exclude that all-in player. Side pots are settled independently at showdown.";
    }

    public static EPlayerAction[] GetValiedAtion(Player player, PokerGame game)
    {
        var actions = new List<EPlayerAction>();

        if (player == null)
            return actions.ToArray();

        // If already folded or all-in, there are no actions to take.
        if (player.data.Folded)
            return actions.ToArray();

        actions.Add(EPlayerAction.BuyIn);
        if (player.data.AllIn || player.data.Stack <= 0)
        {
            return actions.ToArray();
        }

        // Fold is always available when the player may act.
        actions.Add(EPlayerAction.Fold);

        // Always allow AllIn as an explicit choice when stack > 0.
        if (player.data.Stack > 0) actions.Add(EPlayerAction.AllIn);

        // Determine amount required to call from game context.
        int toCall = game.currentBet - player.data.CurrentBet;

        if (toCall <= 0)
        {
            // No bet to call: player may Check or open a Bet.
            actions.Add(EPlayerAction.Check);
            actions.Add(EPlayerAction.Bet);
        }
        else
        {
            // There is an amount to call. If the player has enough chips, Call is allowed.
            if (player.data.Stack >= toCall)
            {
                actions.Add(EPlayerAction.Call);
            }
            else
            {
                // By default project rules require explicit AllIn rather than implicit partial call.
                // If partial calls are accepted by rules, include Call as a possible action.
                if (IsPartialCallAccepted(DefaultInsufficientStackBehavior))
                    actions.Add(EPlayerAction.Call);
            }
            // Determine whether a Raise is possible. Compute a conservative minRaise using
            // game.lastRaiseAmount and a big-blind-based minimum; aiConfig may specify a fraction.
            int minByLastRaise = Math.Max(1, game.lastRaiseAmount);
            float minRaiseFrac = 1f;
            try { if (game.aiConfig != null) minRaiseFrac = Math.Max(0.0f, game.aiConfig.minRaiseFraction); } catch { }
            int minByConfig = Math.Max(1, (int)Math.Floor(game.data.BigBlindAmount * (double)minRaiseFrac));
            int minRaise = Math.Max(minByConfig, minByLastRaise);

            // Player can raise if after calling they still have at least minRaise to increase the bet.
            int extraAfterCall = player.data.Stack - toCall;
            if (extraAfterCall >= minRaise && player.data.Stack > toCall)
            {
                actions.Add(EPlayerAction.Raise);
            }
            else
            {
                // Special case: if the player's all-in contribution would itself satisfy the minRaise
                // (i.e. all-in amount > toCall and (all-in - toCall) >= minRaise), then consider that
                // all-in as effectively performing a raise and expose Raise as an option as well.
                if (player.data.Stack > toCall)
                {
                    int allInExtra = player.data.Stack - toCall;
                    if (allInExtra >= minRaise)
                    {
                        actions.Add(EPlayerAction.Raise);
                    }
                }
            }
        }

        return actions.ToArray();
    }
}
