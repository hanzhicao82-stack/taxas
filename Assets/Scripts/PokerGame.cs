using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 更完整的回合控制示例：包含庄家、盲注、阶段（Preflop/Flop/Turn/River/Showdown）、多次加注与边池结算。
/// 该实现旨在教学，下注决策仍为自动化示例，但此处实现了多轮加注、all-in 与边池处理。
/// </summary>
public class PokerGame : MonoBehaviour
{
    /// <summary>玩家数量（默认 4）。</summary>
    /// <summary>玩家数量（默认 4）。</summary>
    public int numPlayers = 4;

    /// <summary>玩家列表（`Player` 对象）。</summary>
    /// <summary>玩家列表（`Player` 对象）。</summary>
    public List<Player> players = new List<Player>();

    /// <summary>公共牌（翻牌/转牌/河牌）。</summary>
    /// <summary>公共牌（翻牌/转牌/河牌）。</summary>
    public List<Card> community = new List<Card>();

    /// <summary>牌堆实例（`Deck`）。</summary>
    private Deck deck;

    /// <summary>界面管理器（可为 null）。</summary>
    /// <summary>界面管理器（可为 null）。</summary>
    public UIManager ui;

    /// <summary>AI 参数配置（可通过 ScriptableObject 在 Inspector 中调整）。</summary>
    public AIConfig aiConfig;

    // 游戏参数
    /// <summary>庄家索引（按钮位置）。每手结束后移动。</summary>
    public int dealerIndex = 0;

    /// <summary>小盲注金额。</summary>
    public int smallBlindAmount = 5;

    /// <summary>大盲注金额。</summary>
    public int bigBlindAmount = 10;

    // 当前手牌状态
    /// <summary>显示用的总彩池（组合自各轮投注），实际分配由 `CollectPots` 计算。</summary>
    public int pot = 0; // 总彩池（仅用于显示，实际分配由 CollectPots 处理）

    /// <summary>当前下注轮的最高注额，玩家需跟到该数额或更高（fold/all-in 除外）。</summary>
    public int currentBet = 0; // 当前需要跟注的最高金额

    private enum Phase
    {
        Preflop,
        Flop,
        Turn,
        River,
        Showdown
    }
    private Phase phase;

    void Start()
    {
        // 可手动调用 StartHand() 或通过 UI 触发
    }

    /// <summary>
    /// 开始一手牌：初始化玩家、发牌、盲注，依次执行下注轮并在摊牌时结算边池。
    /// </summary>
    public void StartHand()
    {
        // 初始化玩家列表（保留已有 players 的 stack）
        if (players == null || players.Count != numPlayers)
        {
            players = new List<Player>();
            for (int i = 0; i < numPlayers; i++)
                players.Add(new Player(i, "P" + (i + 1)));
        }

        foreach (var p in players)
            p.ResetForHand();

        // 为每位玩家分配一个小幅随机的攻击性系数，影响其下注/加注概率与规模，增加行为差异性。
        foreach (var p in players)
            p.aggression = UnityEngine.Random.Range(0.2f, 1.5f);

        deck = new Deck();
        deck.Shuffle();
        community.Clear();
        pot = 0;
        currentBet = 0;

        // 发底牌
        for (int i = 0; i < numPlayers; i++)
        {
            players[i].hole.Add(deck.Draw());
            players[i].hole.Add(deck.Draw());
        }

        // 盲注
        PostBlinds();

        // Preflop
        phase = Phase.Preflop;
        Debug.Log("--- Preflop: 开始下注轮 ---");
        RunBettingRound(GetFirstToActAfterBigBlind());

        if (ActivePlayersCountExcludingAllIn() > 0)
        {
            // Flop
            phase = Phase.Flop;
            deck.Draw(); // burn
            community.Add(deck.Draw());
            community.Add(deck.Draw());
            community.Add(deck.Draw());
            currentBet = 0; Debug.Log("--- Flop: " + string.Join(" ", community.Select(c => c.ToString())) + " ---");
            RunBettingRound(GetFirstToActAfterDealer());
        }

        if (ActivePlayersCountExcludingAllIn() > 0)
        {
            // Turn
            phase = Phase.Turn;
            deck.Draw(); // burn
            community.Add(deck.Draw());
            currentBet = 0; Debug.Log("--- Turn: " + string.Join(" ", community.Select(c => c.ToString())) + " ---");
            RunBettingRound(GetFirstToActAfterDealer());
        }

        if (ActivePlayersCountExcludingAllIn() > 0)
        {
            // River
            phase = Phase.River;
            deck.Draw(); // burn
            community.Add(deck.Draw());
            currentBet = 0; Debug.Log("--- River: " + string.Join(" ", community.Select(c => c.ToString())) + " ---");
            RunBettingRound(GetFirstToActAfterDealer());
        }

        // 摊牌并分配边池
        phase = Phase.Showdown;
        Debug.Log("--- Showdown & Payout ---");
        var pots = CollectPots();
        foreach (var potInfo in pots)
        {
            int amount = potInfo.amount;
            var elig = potInfo.eligible;
            // 在 eligible 玩家中找出最高得分
            long best = -1; List<int> winners = new List<int>();
            foreach (int pid in elig)
            {
                var p = players[pid];
                if (p.folded) continue;
                var all = new List<Card>(); all.AddRange(p.hole); all.AddRange(community);
                long sc = HandEvaluator.EvaluateBest(all);
                if (sc > best) { best = sc; winners.Clear(); winners.Add(pid); }
                else if (sc == best) winners.Add(pid);
            }
            if (winners.Count == 0) continue;
            int share = amount / winners.Count;
            foreach (var w in winners) { players[w].stack += share; Debug.Log($"彩池({amount}) 胜者 P{w + 1} 获得 {share}"); }
        }

        // 打印玩家筹码
        foreach (var p in players) Debug.Log($"P{p.id + 1} stack={p.stack}");

        ui?.Refresh(this);

        // 移动庄家
        dealerIndex = (dealerIndex + 1) % numPlayers;
    }

    /// <summary>
    /// 支付盲注并设置 currentBet 为大盲
    /// </summary>
    private void PostBlinds()
    {
        int sb = (dealerIndex + 1) % numPlayers;
        int bb = (dealerIndex + 2) % numPlayers;
        var sPlayer = players[sb];
        var bPlayer = players[bb];

        int postedSB = Mathf.Min(sPlayer.stack, smallBlindAmount);
        sPlayer.stack -= postedSB; sPlayer.currentBet += postedSB;

        int postedBB = Mathf.Min(bPlayer.stack, bigBlindAmount);
        bPlayer.stack -= postedBB; bPlayer.currentBet += postedBB;

        currentBet = postedBB;
        Debug.Log($"Blinds: P{sb + 1} posts SB={postedSB}, P{bb + 1} posts BB={postedBB}");
    }

    private int GetFirstToActAfterBigBlind() => (dealerIndex + 3) % numPlayers;
    private int GetFirstToActAfterDealer() => (dealerIndex + 1) % numPlayers;

    /// <summary>返回大盲注之后第一个行动玩家的索引（用于 Preflop）。</summary>
    /// <summary>返回大盲注之后第一个行动玩家的索引（用于 Preflop）。</summary>

    private int ActivePlayersCount()
    {
        return players.Count(p => !p.folded);
    }

    private int ActivePlayersCountExcludingAllIn()
    {
        return players.Count(p => !p.folded && !p.allIn && p.stack > 0);
    }

    /// <summary>返回仍在局中且非 all-in 的玩家数量（用于判定是否继续发下一阶段公共牌）。</summary>
    /// <summary>返回仍在局中且非 all-in 的玩家数量（用于判定是否继续发下一阶段公共牌）。</summary>

    /// <summary>
    /// 运行一轮下注：实现多次加注直到所有活跃（非 all-in）的玩家将注补齐或弃牌。
    /// 玩家决策为自动化示例：可根据需要替换为 UI 输入。
    /// </summary>
    private void RunBettingRound(int startIndex)
    {
        int n = players.Count;
        int safety = 0;

        // 循环直到所有仍可行动的玩家满足 currentBet 或成为 all-in/弃牌
        bool changed = true;
        while (changed && safety < 2000)
        {
            changed = false;
            safety++;
            for (int i = 0; i < n; i++)
            {
                int idx = (startIndex + i) % n;
                var p = players[idx];
                if (p.folded || p.allIn) continue;

                int need = currentBet - p.currentBet;
                // Delegate AI decision to PlayerAI; it returns true when the betting state changed.
                if (PlayerAI.Act(p, this, need)) changed = true;
            }
        }

        // 完成一轮下注后：不要在每轮都调用 CollectPots()（那会清空 players.currentBet）。
        // 保留 players.currentBet 以跨轮累积，最终在摊牌时一次性调用 CollectPots() 结算。
        pot = players.Sum(p => p.currentBet);
        Debug.Log($"下注轮结束。已投入彩池={pot}");
    }

    /// <summary>
    /// 从 players.currentBet 中根据不同投入生成一个或多个 pot（主池 + 边池），
    /// 每个 pot 包含 amount 与有资格赢取该 pot 的玩家索引列表。
    /// 此方法会消耗 players.currentBet（会将其设为 0）。
    /// </summary>
    /// <returns>列表，每项为 (amount, eligible player ids)</returns>
    private List<(int amount, List<int> eligible)> CollectPots()
    {
        var pots = new List<(int amount, List<int> eligible)>();
        // 复制投注数组
        var bets = players.Select(p => p.currentBet).ToArray();
        int playersLeft = bets.Count(b => b > 0);
        while (bets.Any(b => b > 0))
        {
            int min = bets.Where(b => b > 0).Min();
            int count = bets.Count(b => b >= min);
            int amount = min * count;
            var eligible = new List<int>();
            for (int i = 0; i < bets.Length; i++) if (bets[i] >= min) eligible.Add(i);
            pots.Add((amount, eligible));
            // subtract
            for (int i = 0; i < bets.Length; i++) if (bets[i] > 0) bets[i] = Math.Max(0, bets[i] - min);
        }
        // 清除 players.currentBet（实际筹码已从 stack 扣除）
        foreach (var p in players) p.currentBet = 0;
        return pots;
    }

    /// <summary>
    /// 比较仍未弃牌玩家的牌力并返回赢家索引（若多赢家会在 CollectPots 逻辑中分配）。
    /// 本方法单纯返回最高分玩家（用于简单场景）。
    /// </summary>
    /// <returns>赢家玩家 id 或 -1。</returns>
    public int DetermineWinner()
    {
        long bestScore = -1; int bestIdx = -1;
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p.folded) continue;
            var all = new List<Card>(); all.AddRange(p.hole); all.AddRange(community);
            long sc = HandEvaluator.EvaluateBest(all);
            Debug.Log($"P{i + 1} hand: {p.hole[0]} {p.hole[1]} score={sc}");
            if (sc > bestScore) { bestScore = sc; bestIdx = i; }
        }
        return bestIdx;
    }
}
