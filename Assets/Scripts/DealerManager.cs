using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Encapsulates dealer/button and blind posting logic so PokerGame can delegate responsibilities.
/// Provides helpers to post blinds, compute first-to-act seats and advance the dealer.
/// </summary>
public static class DealerManager
{
    public static void PostBlinds(PokerGame game)
    {
        if (game == null) return;
        int numPlayers = game.numPlayers;
        int sb = (game.dealerIndex + 1) % numPlayers;
        int bb = (game.dealerIndex + 2) % numPlayers;
        var sPlayer = game.players[sb];
        var bPlayer = game.players[bb];

        int postedSB = Mathf.Min(sPlayer.data.Stack, game.data.SmallBlindAmount);
        sPlayer.data.Stack = sPlayer.data.Stack - postedSB;
        sPlayer.data.CurrentBet = sPlayer.data.CurrentBet + postedSB;

        int postedBB = Mathf.Min(bPlayer.data.Stack, game.data.BigBlindAmount);
        bPlayer.data.Stack = bPlayer.data.Stack - postedBB;
        bPlayer.data.CurrentBet = bPlayer.data.CurrentBet + postedBB;

        game.data.Pot += postedSB + postedBB;
        game.data.CurrentBet = postedBB;
        game.currentBet = game.data.CurrentBet;
        game.pot = game.data.Pot;
        Debug.Log($"盲注（由 DealerManager）：P{sb + 1} 支付 小盲={postedSB}, P{bb + 1} 支付 大盲={postedBB}");
    }

    public static int GetFirstToActAfterBigBlind(int dealerIndex, int numPlayers) => (dealerIndex + 3) % numPlayers;

    public static int GetFirstToActAfterDealer(int dealerIndex, int numPlayers) => (dealerIndex + 1) % numPlayers;

    public static void AdvanceDealer(PokerGame game)
    {
        if (game == null) return;
        game.dealerIndex = (game.dealerIndex + 1) % game.numPlayers;
    }
}
