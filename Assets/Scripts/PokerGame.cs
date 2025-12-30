using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;


public enum ERoundPhase
{
    Preflop,
    Flop,
    Turn,
    River,
    Showdown
}
/// <summary>
/// Poker game main flow (dealer, blinds, Preflop/Flop/Turn/River/Showdown).
/// Coroutine-driven so UI can update between AI actions. Publishes events
/// via GameEventBus: Events.HandStarted, Events.Flop, Events.Turn, Events.River.
/// </summary>
public class PokerGame : MonoBehaviour
{
    public int numPlayers = 4;
    public List<Player> players = new List<Player>();
    public PokerData data = new PokerData();

    private Deck deck;
    public UIManager ui;
    public HumanPlayerUI humanUI;
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

    void Start()
    {

    }

    // CreateHumanPlayerUI moved to HumanPlayerUI.CreateHumanPlayerUI()



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
        Debug.Log($"开始发牌流程：庄家={dealerIndex + 1}, 玩家数={numPlayers}");
        if (humanUI == null)
        {
            humanUI = UnityEngine.Object.FindObjectOfType<HumanPlayerUI>();
            if (humanUI == null)
            {
                humanUI = HumanPlayerUI.CreateHumanPlayerUI();
            }
        }

        // ensure UIManager reference is available so we can call ShowWinners etc.
        if (ui == null)
        {
            ui = UnityEngine.Object.FindObjectOfType<UIManager>();
        }

        yield return null;
        yield return null;

        if (players == null || players.Count != numPlayers)
        {
            players = new List<Player>();
            for (int i = 0; i < numPlayers; i++) players.Add(new Player(i, "P" + (i + 1)));
        }

        foreach (var p in players) p.ResetForHand();
        foreach (var p in players) p.data.Aggression = UnityEngine.Random.Range(0.2f, 1.5f);

        deck = new Deck();
        deck.Shuffle();
        data.ClearCommunity();
        data.Pot = 0;
        data.CurrentBet = 0;

        for (int i = 0; i < numPlayers; i++)
        {
            players[i].data.AddHole(deck.Draw());
            players[i].data.AddHole(deck.Draw());
            // debug: show hole cards dealt (useful during development)
            var h = players[i].data.Hole;
            if (h != null && h.Count >= 2)
            {
                Debug.Log($"发牌给 P{i + 1}: {h[0]} {h[1]}");
            }
        }

        PostBlinds();
        GameEventBus.Submit(Events.HandStarted, players.Select(p => p).ToList());

        // Preflop
        phase = Phase.Preflop;
        Debug.Log("--- 预翻牌阶段: 开始下注轮 ---");
        yield return StartCoroutine(RunBettingRound(ERoundPhase.Preflop));

        if (ActivePlayersCountExcludingAllIn() > 0)
        {
            // Flop
            phase = Phase.Flop;
            deck.Draw(); // burn
            var flopAdded = new List<Card> { deck.Draw(), deck.Draw(), deck.Draw() };
            data.Community.AddRange(flopAdded);
            data.CurrentBet = 0;
            Debug.Log("--- 翻牌: " + string.Join(" ", data.Community.Select(c => c.ToString())) + " ---");
            GameEventBus.Submit(Events.Flop, Tuple.Create(data.Community.ToList(), flopAdded));
            yield return StartCoroutine(RunBettingRound(ERoundPhase.Flop));
        }

        if (ActivePlayersCountExcludingAllIn() > 0)
        {
            // Turn
            phase = Phase.Turn;
            deck.Draw(); // burn
            var turnAdded = new List<Card> { deck.Draw() };
            data.Community.AddRange(turnAdded);
            data.CurrentBet = 0;
            Debug.Log("--- 转牌: " + string.Join(" ", data.Community.Select(c => c.ToString())) + " ---");
            GameEventBus.Submit(Events.Turn, Tuple.Create(data.Community.ToList(), turnAdded));
            yield return StartCoroutine(RunBettingRound(ERoundPhase.Turn));
        }

        if (ActivePlayersCountExcludingAllIn() > 0)
        {
            // River
            phase = Phase.River;
            deck.Draw(); // burn
            var riverAdded = new List<Card> { deck.Draw() };
            data.Community.AddRange(riverAdded);
            data.CurrentBet = 0;
            Debug.Log("--- 河牌: " + string.Join(" ", data.Community.Select(c => c.ToString())) + " ---");
            GameEventBus.Submit(Events.River, Tuple.Create(data.Community.ToList(), riverAdded));
            yield return StartCoroutine(RunBettingRound(ERoundPhase.River));
        }

        // Showdown & payout
        phase = Phase.Showdown;
        Debug.Log("--- 摊牌与派彩 ---");
        var pots = CollectPots();

     
        foreach (var potInfo in pots)
        {
            int amount = potInfo.amount;
            var elig = potInfo.eligible;
            Debug.Log($"派彩：底池金额={amount}, 有资格的玩家=[{string.Join(",", elig)}]");
            long best = -1;
            List<int> winners = new List<int>();
            foreach (int pid in elig)
            {
                var p = players[pid];
                if (p.data.Folded) continue;
                var all = new List<Card>();
                all.AddRange(p.data.Hole ?? new List<Card>());
                all.AddRange(data.Community ?? new List<Card>());
                long sc = HandEvaluator.EvaluateBest(all);
                Debug.Log($"计算 P{pid + 1}：手牌=[{string.Join(" ", p.data.Hole ?? new List<Card>())}] 公共牌=[{string.Join(" ", data.Community ?? new List<Card>())}] 得分={sc}");
                if (sc > best)
                {
                    best = sc; 
                    winners.Clear(); winners.Add(pid);
                }
                else if (sc == best)
                {
                    winners.Add(pid);
                }
            }
            if (winners.Count == 0) 
            continue;
            int share = amount / winners.Count;
            Debug.Log($"底池胜者=[{string.Join(",", winners)}], 每人分得={share}");
            foreach (var w in winners)
            {
                var pd = players[w].data;
                pd.Stack = pd.Stack + share;
                Debug.Log($"发放 P{w + 1} +{share} => 新筹码={pd.Stack}");
            }
        }

        foreach (var p in players) Debug.LogWarning($"P{p.id + 1} 筹码={p.data.Stack}");

        yield return ui?.ShowResult(data.Pot);

        dealerIndex = (dealerIndex + 1) % numPlayers;
        yield break;
    }

    private void PostBlinds()
    {
        int sb = (dealerIndex + 1) % numPlayers;
        int bb = (dealerIndex + 2) % numPlayers;
        var sPlayer = players[sb];
        var bPlayer = players[bb];

        int postedSB = Mathf.Min(sPlayer.data.Stack, data.SmallBlindAmount);
        sPlayer.data.Stack = sPlayer.data.Stack - postedSB;
        sPlayer.data.CurrentBet = sPlayer.data.CurrentBet + postedSB;

        int postedBB = Mathf.Min(bPlayer.data.Stack, data.BigBlindAmount);
        bPlayer.data.Stack = bPlayer.data.Stack - postedBB;
        bPlayer.data.CurrentBet = bPlayer.data.CurrentBet + postedBB;

        data.CurrentBet = postedBB;
        Debug.Log($"盲注：P{sb + 1} 支付 小盲={postedSB}, P{bb + 1} 支付 大盲={postedBB}");
    }

    private int GetFirstToActAfterBigBlind() => (dealerIndex + 3) % numPlayers;
    private int GetFirstToActAfterDealer() => (dealerIndex + 1) % numPlayers;

    private int ActivePlayersCountExcludingAllIn() => players.Count(p => !p.data.Folded && !p.data.AllIn && p.data.Stack > 0);

    private System.Collections.IEnumerator RunBettingRound(ERoundPhase phase)
    {
        int n = players.Count;
        Debug.Log($"运行下注轮: 阶段={phase}, 玩家数={n}");
        for (int i = 0; i < n; i++)
        {

            Player p = players[i];
            // Notify UI which player is acting
            ui?.HighlightPlayer(i + 1);
            Debug.Log($"当前行动玩家: 座位={i + 1}, 名称={p.name}, 弃牌={p.data.Folded}, 全下={p.data.AllIn}, 筹码={p.data.Stack}, 当前下注={p.data.CurrentBet}");
            // If a HumanPlayerUI is available, use it for every seat (no AI)
            if (humanUI != null)
            {
                // show the UI (indicate which seat by context; UI can display seat if needed)
                humanUI.ShowForSeat(i, phase);
                // wait for player action
                yield return p.Act(phase);
                // after action, log resulting state
                Debug.Log($"行动后 P{i + 1}: 弃牌={p.data.Folded}, 全下={p.data.AllIn}, 当前下注={p.data.CurrentBet}, 筹码={p.data.Stack}");
                yield return null;
            }
            else
            {
                // // fallback to AI if no human UI provided
                // PlayerAI.Act(p, this, need);


                // float d = (aiConfig != null) ? aiConfig.actionDelay : 0.3f;
                // if (d > 0f)
                //     yield return new WaitForSeconds(d);
                // else
                //     yield return null;
            }
        }
        yield return null;

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
            var all = new List<Card>(); all.AddRange(p.data.Hole ?? new List<Card>()); all.AddRange(data.Community ?? new List<Card>());
            long sc = HandEvaluator.EvaluateBest(all);
            if (sc > bestScore) { bestScore = sc; bestIdx = i; }
        }
        return bestIdx;
    }
}

