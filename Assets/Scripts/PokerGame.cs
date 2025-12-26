using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Poker game main flow (dealer, blinds, Preflop/Flop/Turn/River/Showdown).
/// Coroutine-driven so UI can update between AI actions. Publishes events
/// via GameEventBus: Events.HandStarted, Events.Flop, Events.Turn, Events.River.
/// </summary>
public class PokerGame : MonoBehaviour
{
    public int numPlayers = 4;
    public List<Player> players = new List<Player>();
    public List<Card> community = new List<Card>();

    private Deck deck;
    public UIManager ui;
    public AIConfig aiConfig;

    // Tracked coroutines so we can stop them when this object is destroyed
    private List<Coroutine> trackedCoroutines = new List<Coroutine>();

    public int dealerIndex = 0;
    public int smallBlindAmount = 5;
    public int bigBlindAmount = 10;

    public int pot = 0;
    public int currentBet = 0;

    private enum Phase { Preflop, Flop, Turn, River, Showdown }
    private Phase phase;

    void Start() { }

    private Coroutine StartTrackedCoroutine(System.Collections.IEnumerator routine)
    {
        Coroutine handle = null;
        System.Collections.IEnumerator Wrapper()
        {
            yield return routine;
            // remove when finished
            trackedCoroutines.Remove(handle);
        }
        handle = StartCoroutine(Wrapper());
        trackedCoroutines.Add(handle);
        return handle;
    }

    public void StopTrackedCoroutine(Coroutine c)
    {
        if (c == null) return;
        try { StopCoroutine(c); } catch { }
        trackedCoroutines.Remove(c);
    }

    public void StopAllTrackedCoroutines()
    {
        foreach (var c in trackedCoroutines.ToList())
        {
            if (c != null)
            {
                try { StopCoroutine(c); } catch { }
            }
        }
        trackedCoroutines.Clear();
    }

    private void OnDestroy()
    {
        StopAllTrackedCoroutines();
    }


    public System.Collections.IEnumerator StartHandRoutine()
    {
        if (players == null || players.Count != numPlayers)
        {
            players = new List<Player>();
            for (int i = 0; i < numPlayers; i++) players.Add(new Player(i, "P" + (i + 1)));
        }

        foreach (var p in players) p.ResetForHand();
        foreach (var p in players) p.data.Aggression = UnityEngine.Random.Range(0.2f, 1.5f);

        deck = new Deck();
        deck.Shuffle();
        community.Clear();
        pot = 0;
        currentBet = 0;

        for (int i = 0; i < numPlayers; i++)
        {
            players[i].data.AddHole(deck.Draw());
            players[i].data.AddHole(deck.Draw());
        }

        PostBlinds();
        GameEventBus.Submit(Events.HandStarted, players.Select(p => p).ToList());

        // Preflop
        phase = Phase.Preflop;
        Debug.Log("--- Preflop: 开始下注轮 ---");
        yield return StartTrackedCoroutine(RunBettingRound(GetFirstToActAfterBigBlind()));

        if (ActivePlayersCountExcludingAllIn() > 0)
        {
            // Flop
            phase = Phase.Flop;
            deck.Draw(); // burn
            var flopAdded = new List<Card> { deck.Draw(), deck.Draw(), deck.Draw() };
            community.AddRange(flopAdded);
            currentBet = 0;
            Debug.Log("--- Flop: " + string.Join(" ", community.Select(c => c.ToString())) + " ---");
            GameEventBus.Submit(Events.Flop, Tuple.Create(community.ToList(), flopAdded));
            yield return StartTrackedCoroutine(RunBettingRound(GetFirstToActAfterDealer()));
        }

        if (ActivePlayersCountExcludingAllIn() > 0)
        {
            // Turn
            phase = Phase.Turn;
            deck.Draw(); // burn
            var turnAdded = new List<Card> { deck.Draw() };
            community.AddRange(turnAdded);
            currentBet = 0;
            Debug.Log("--- Turn: " + string.Join(" ", community.Select(c => c.ToString())) + " ---");
            GameEventBus.Submit(Events.Turn, Tuple.Create(community.ToList(), turnAdded));
            yield return StartTrackedCoroutine(RunBettingRound(GetFirstToActAfterDealer()));
        }

        if (ActivePlayersCountExcludingAllIn() > 0)
        {
            // River
            phase = Phase.River;
            deck.Draw(); // burn
            var riverAdded = new List<Card> { deck.Draw() };
            community.AddRange(riverAdded);
            currentBet = 0;
            Debug.Log("--- River: " + string.Join(" ", community.Select(c => c.ToString())) + " ---");
            GameEventBus.Submit(Events.River, Tuple.Create(community.ToList(), riverAdded));
            yield return StartTrackedCoroutine(RunBettingRound(GetFirstToActAfterDealer()));
        }

        // Showdown & payout
        phase = Phase.Showdown;
        Debug.Log("--- Showdown & Payout ---");
        var pots = CollectPots();
        foreach (var potInfo in pots)
        {
            int amount = potInfo.amount;
            var elig = potInfo.eligible;
            long best = -1;
            List<int> winners = new List<int>();
            foreach (int pid in elig)
            {
                var p = players[pid];
                if (p.data.Folded) continue;
                var all = new List<Card>(); all.AddRange(p.data.Hole ?? new List<Card>()); all.AddRange(community);
                long sc = HandEvaluator.EvaluateBest(all);
                if (sc > best) { best = sc; winners.Clear(); winners.Add(pid); }
                else if (sc == best) winners.Add(pid);
            }
            if (winners.Count == 0) continue;
            int share = amount / winners.Count;
            foreach (var w in winners)
            {
                var pd = players[w].data;
                pd.Stack = pd.Stack + share;
            }
        }

        foreach (var p in players) Debug.LogWarning($"P{p.id + 1} stack={p.data.Stack}");

        ui?.UpdatePot(pot);

        dealerIndex = (dealerIndex + 1) % numPlayers;
        yield break;
    }

    private void PostBlinds()
    {
        int sb = (dealerIndex + 1) % numPlayers;
        int bb = (dealerIndex + 2) % numPlayers;
        var sPlayer = players[sb];
        var bPlayer = players[bb];

        int postedSB = Mathf.Min(sPlayer.data.Stack, smallBlindAmount);
        sPlayer.data.Stack = sPlayer.data.Stack - postedSB;
        sPlayer.data.CurrentBet = sPlayer.data.CurrentBet + postedSB;

        int postedBB = Mathf.Min(bPlayer.data.Stack, bigBlindAmount);
        bPlayer.data.Stack = bPlayer.data.Stack - postedBB;
        bPlayer.data.CurrentBet = bPlayer.data.CurrentBet + postedBB;

        currentBet = postedBB;
        Debug.Log($"Blinds: P{sb + 1} posts SB={postedSB}, P{bb + 1} posts BB={postedBB}");
    }

    private int GetFirstToActAfterBigBlind() => (dealerIndex + 3) % numPlayers;
    private int GetFirstToActAfterDealer() => (dealerIndex + 1) % numPlayers;

    private int ActivePlayersCount() => players.Count(p => !p.data.Folded);
    private int ActivePlayersCountExcludingAllIn() => players.Count(p => !p.data.Folded && !p.data.AllIn && p.data.Stack > 0);

    private System.Collections.IEnumerator RunBettingRound(int startIndex)
    {
        int n = players.Count;
        int safety = 0;
        bool changed = true;
        while (changed && safety < 100)
        {
            changed = false;
            safety++;
            for (int i = 0; i < n; i++)
            {
                int idx = (startIndex + i) % n;
                var p = players[idx];
                if (p.data.Folded || p.data.AllIn)
                    continue;
                int need = currentBet - p.data.CurrentBet;
                if (PlayerAI.Act(p, this, need))
                    changed = true;

                float d = (aiConfig != null) ? aiConfig.actionDelay : 0.3f;
                if (d > 0f)
                    yield return new WaitForSeconds(d);
                else
                    yield return null;
            }
            yield return null;
        }
        pot = players.Sum(p => p.data.CurrentBet);
        Debug.Log($"下注轮结束。已投入彩池={pot}");
        yield break;
    }

    private List<(int amount, List<int> eligible)> CollectPots()
    {
        var pots = new List<(int amount, List<int> eligible)>();
        var bets = players.Select(p => p.data.CurrentBet).ToArray();
        while (bets.Any(b => b > 0))
        {
            int min = bets.Where(b => b > 0).Min();
            int count = bets.Count(b => b >= min);
            int amount = min * count;
            var eligible = new List<int>();
            for (int i = 0; i < bets.Length; i++) if (bets[i] >= min) eligible.Add(i);
            pots.Add((amount, eligible));
            for (int i = 0; i < bets.Length; i++) if (bets[i] > 0) bets[i] = Math.Max(0, bets[i] - min);
        }
        foreach (var p in players) p.data.CurrentBet = 0;
        return pots;
    }

    public int DetermineWinner()
    {
        long bestScore = -1; int bestIdx = -1;
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i]; if (p.data.Folded) continue;
            var all = new List<Card>(); all.AddRange(p.data.Hole ?? new List<Card>()); all.AddRange(community);
            long sc = HandEvaluator.EvaluateBest(all);
            if (sc > bestScore) { bestScore = sc; bestIdx = i; }
        }
        return bestIdx;
    }
}

