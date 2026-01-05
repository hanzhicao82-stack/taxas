using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;


// 回合阶段枚举：用于标识当前是哪个下注阶段（预翻牌/翻牌/转牌/河牌/摊牌）
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
/// <summary>
/// 主游戏流程控制类：负责发牌、盲注、每轮下注流程（协程）、摊牌与派彩。
/// 注：本类通过记录每位玩家的 `data.CurrentBet` 来累积本局下注，最终由 `CollectPots()` 汇总主/边池。
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
    public int smallBlindAmount = 0;
    public int bigBlindAmount = 0;

    // 游戏层面的总显示底池（可能用于 UI）；实际派彩时以 CollectPots() 结果为准
    public int pot = 0;
    // 本轮的当前最高下注（其他玩家需要跟注到此数）；注意这与每位玩家的 data.CurrentBet 不同
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


    public void Reset()
    {
        pot = 0;
        currentBet = 0;
    }

    public System.Collections.IEnumerator StartHandRoutine()
    {
        // StartHandRoutine：整手牌的主流程（发牌 -> 逐街下注 -> 摊牌 -> 派彩）
        // 关键点：每轮下注时玩家将筹码记入 players[i].data.CurrentBet，
        // 在进入下一街前会清零该值以开始新一轮（除摊牌前我们保留以便 CollectPots 读取）。
        // 协程设计方便 UI/玩家交互和 AI 延迟。
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

        foreach (var p in players) p.ResetForHand();
        foreach (var p in players) p.data.Aggression = UnityEngine.Random.Range(0.2f, 1.5f);

        deck = new Deck();
        deck.Shuffle();
        data.ClearCommunity();
        // ensure PokerData reflects the configured blind sizes
        data.SmallBlindAmount = smallBlindAmount;
        data.BigBlindAmount = bigBlindAmount;
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
        int preflopStart = (data.BigBlindAmount > 0)
            ? DealerManager.GetFirstToActAfterBigBlind(dealerIndex, numPlayers)
            : DealerManager.GetFirstToActAfterDealer(dealerIndex, numPlayers);
        yield return StartCoroutine(RunBettingRound(ERoundPhase.Preflop, preflopStart));

        // Normalize per-round bet state before progressing to next street:
        // - players' CurrentBet represent this betting round and should be cleared
        // - game-level currentBet/data.CurrentBet should be reset to 0 so next street allows Check
        for (int i = 0; i < players.Count; i++) players[i].data.CurrentBet = 0;
        data.CurrentBet = 0;
        currentBet = 0;

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
            int postflopStart = DealerManager.GetFirstToActAfterDealer(dealerIndex, numPlayers);
            yield return StartCoroutine(RunBettingRound(ERoundPhase.Flop, postflopStart));

            // clear per-round bets again before next street
            for (int i = 0; i < players.Count; i++) players[i].data.CurrentBet = 0;
            data.CurrentBet = 0;
            currentBet = 0;
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
            int turnStart = DealerManager.GetFirstToActAfterDealer(dealerIndex, numPlayers);
            yield return StartCoroutine(RunBettingRound(ERoundPhase.Turn, turnStart));

            // clear per-round bets again before next street
            for (int i = 0; i < players.Count; i++) players[i].data.CurrentBet = 0;
            data.CurrentBet = 0;
            currentBet = 0;
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
            int riverStart = DealerManager.GetFirstToActAfterDealer(dealerIndex, numPlayers);
            yield return StartCoroutine(RunBettingRound(ERoundPhase.River, riverStart));

            // (Do not clear per-round bets here) — CollectPots() needs the
            // players' committed `CurrentBet` values to compute side pots.
        }

        // Showdown & payout
        phase = Phase.Showdown;
        Debug.Log("--- 摊牌与派彩 ---");
        var pots = CollectPots();

        // Update the game-level pot value so UI and other systems can read it.
        try
        {
            int totalPot = pots.Sum(p => p.amount);
            data.Pot = totalPot;
        }
        catch
        {
            data.Pot = 0;
        }


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
        DealerManager.AdvanceDealer(this);
        yield break;
    }

    private void PostBlinds()
    {
        // Delegate to DealerManager implementation for consistency and testability
        DealerManager.PostBlinds(this);
    }

    private int GetFirstToActAfterBigBlind() => (dealerIndex + 3) % numPlayers;
    private int GetFirstToActAfterDealer() => (dealerIndex + 1) % numPlayers;

    private int ActivePlayersCountExcludingAllIn() => players.Count(p => !p.data.Folded && !p.data.AllIn && p.data.Stack > 0);

    private System.Collections.IEnumerator RunBettingRound(ERoundPhase phase, int startIndex)
    {
        // RunBettingRound：按座位顺序提示每位玩家行动（UI/highlight），
        // 期望玩家在其 Player.Act() 中更新其 data.CurrentBet 与 data.Stack。
        // 该方法不在此处处理具体下注策略（由 humanUI 或 AI 实现）。
        int n = players.Count;
        Debug.Log($"运行下注轮: 阶段={phase}, 玩家数={n}, 起始座位={startIndex + 1}");
        for (int offset = 0; offset < n; offset++)
        {
            int i = (startIndex + offset) % n;

            Player p = players[i];
            // Notify UI which player is acting
            ui?.HighlightPlayer(i + 1);
            Debug.Log($"当前行动玩家: 座位={i + 1}, 名称={p.name}, 弃牌={p.data.Folded}, 全下={p.data.AllIn}, 筹码={p.data.Stack}, 当前下注={p.data.CurrentBet}");
            if (humanUI != null)
            {
                int need = Math.Max(0, currentBet - p.data.CurrentBet);
                humanUI.ConfigureForNeed(need);
                humanUI.ShowForSeat(i, phase);
                yield return p.Act(phase);
                Debug.Log($"行动后 P{i + 1}: 弃牌={p.data.Folded}, 全下={p.data.AllIn}, 当前下注={p.data.CurrentBet}, 筹码={p.data.Stack}");
                yield return null;
            }
            else
            {
                // AI fallback currently not implemented here
            }
        }
        yield return null;

    }

    private List<(int amount, List<int> eligible)> CollectPots()
    {
        // CollectPots：将每位玩家的 data.CurrentBet 汇总为主/边池。
        // 算法：反复取当前所有正值中的最小下注 min，将该 min * count 累为一池，
        // 池的 eligible 为所有 bet >= min 的玩家，然后从每个正 bet 中减去 min，直到都为 0。
        // 返回值为一系列 (amount, eligible)；函数结束后会清零 players[i].data.CurrentBet。
        var pots = new List<(int amount, List<int> eligible)>();
        var bets = players.Select(p => p.data.CurrentBet).ToArray();
        while (bets.Any(b => b > 0))
        {
            int min = bets.Where(b => b > 0).Min();
            int count = bets.Count(b => b >= min);
            int amount = min * count;
            var eligible = new List<int>();
            for (int i = 0; i < bets.Length; i++)
                if (bets[i] >= min)
                    eligible.Add(i);
            pots.Add((amount, eligible));
            for (int i = 0; i < bets.Length; i++)
                if (bets[i] > 0)
                    bets[i] = Math.Max(0, bets[i] - min);
        }
        // 清理每位玩家的 CurrentBet，为下一手或下一轮复用做准备
        foreach (var p in players)
            p.data.CurrentBet = 0;
        return pots;
    }

    public int DetermineWinner()
    {
        // DetermineWinner：遍历未弃牌玩家，使用 HandEvaluator.EvaluateBest 比较最佳 5 张手牌分数，
        // 返回分数最高的玩家索引（若相等则当前实现返回首个达到最高分的索引）。
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

