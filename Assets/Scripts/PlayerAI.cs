using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 简易 AI 决策封装：将行为逻辑从 `PokerGame` 中抽离以便集中维护。
/// 方法会对 `Player` 与 `PokerGame.currentBet` 做出修改并返回是否改变了下注状态（下注/加注/跟注/全下）。
/// </summary>
public static class PlayerAI
{
    /// <summary>
    /// 根据需要跟注的金额 `need` 为玩家 `p` 选择动作并执行。
    /// 如果动作改变了下注状态（下注/加注/跟注/全下）则返回 true。
    /// </summary>
    public static bool Act(Player p, PokerGame game, int need)
    {
        if (p.data.Folded || p.data.AllIn) return false;
        var cfg = game.aiConfig;
        // fallback defaults if no config provided
        float raiseProb = cfg != null ? cfg.raiseProbability : 0.12f;
        float betProb = cfg != null ? cfg.betProbability : 0.06f;
        float raiseBase = cfg != null ? cfg.raiseSizeBase : 0.5f;
        float raiseScale = cfg != null ? cfg.raiseSizeAggressionScale : 1.0f;
        float minRaiseFrac = cfg != null ? cfg.minRaiseFraction : 0.5f;

        // Estimate win probability via Monte Carlo (using only known cards: hero hole + community)
        float winProb = EstimateWinProb(p, game, cfg != null ? cfg.simIterations : 30);

        // Need to call or fold/all-in
        if (need > 0)
        {
            if (p.data.Stack <= need)
            {
                // 筹码不足则全下
                p.data.CurrentBet = p.data.CurrentBet + p.data.Stack;
                p.data.Stack = 0;
                p.data.AllIn = true;
                Debug.Log($"P{p.id + 1} 全下 {p.data.CurrentBet}");
                return true;
            }
            else
            {
                // 使用胜率与底池赔率决定动作
                int potNow = game.players.Sum(x => x.data.CurrentBet);
                float potOdds = (float)need / (float)(potNow + need);

                // 若胜率远低于底池赔率，则弃牌
                // 调整弃牌阈值：在 Preflop（无公共牌）时放宽阈值；同时依据玩家 aggression 调整（更激进的玩家更少弃牌）
                float foldFactor = 0.85f;
                bool isPreflop = (game.community == null || game.community.Count == 0);
                if (isPreflop)
                    foldFactor *= 0.3f; // 在翻牌前更宽松
                foldFactor /= Mathf.Clamp(p.data.Aggression, 0.5f, 2.0f); // aggression 越高，foldFactor 越小（更少弃牌）
                if (winProb < potOdds * foldFactor)
                {
                    p.data.Folded = true;
                    Debug.Log($"P{p.id + 1} 弃牌 (胜率={winProb:F2} 底池赔率={potOdds:F2} 阈值={potOdds * foldFactor:F2})");
                    return true;
                }

                // 若胜率显著高于底池赔率且概率触发，则尝试加注
                if (winProb > potOdds * 1.6f && UnityEngine.Random.value < raiseProb * p.data.Aggression && p.data.Stack > need + game.bigBlindAmount)
                {
                    int raise = Mathf.Max(1, Mathf.FloorToInt(game.bigBlindAmount * (raiseBase + (p.data.Aggression - 1f) * raiseScale)));
                    raise = Mathf.Max(raise, Mathf.FloorToInt(game.bigBlindAmount * minRaiseFrac));
                    p.data.Stack = p.data.Stack - (need + raise);
                    p.data.CurrentBet = p.data.CurrentBet + (need + raise);
                    game.currentBet = p.data.CurrentBet;
                    Debug.Log($"P{p.id + 1} 加注 {raise}, 新当前注额={game.currentBet} (胜率={winProb:F2})");
                    return true;
                }

                // 否则跟注
                p.data.Stack = p.data.Stack - need;
                p.data.CurrentBet = p.data.CurrentBet + need;
                Debug.Log($"P{p.id + 1} 跟注 {need} (胜率={winProb:F2} 底池赔率={potOdds:F2})");
                return true;
            }
        }
        else
        {
            // 无需跟注：根据胜率决定是否下注
            if (p.data.Stack > 0 && UnityEngine.Random.value < betProb * p.data.Aggression * Mathf.Clamp01(winProb + 0.1f))
            {
                int bet = Mathf.Min(game.bigBlindAmount, p.data.Stack);
                int extra = UnityEngine.Random.Range(0, Mathf.Max(1, Mathf.FloorToInt(game.bigBlindAmount * (p.data.Aggression - 1f))));
                bet = Mathf.Min(p.data.Stack, bet + extra);
                p.data.Stack = p.data.Stack - bet;
                p.data.CurrentBet = p.data.CurrentBet + bet;
                game.currentBet = p.data.CurrentBet;
                Debug.Log($"P{p.id + 1} 下注 {bet} (胜率={winProb:F2})");
                return true;
            }
            else
            {
                Debug.Log($"P{p.id + 1} 过牌 (胜率={winProb:F2})");
                return false;
            }
        }
    }

    private static float EstimateWinProb(Player hero, PokerGame game, int iterations)
    {
        // Build remaining deck excluding hero hole and current community
        var all = new System.Collections.Generic.List<Card>();
        for (int r = 2; r <= 14; r++)
            foreach (Suit s in Enum.GetValues(typeof(Suit)))
                all.Add(new Card(r, s));

        // Remove hero hole and community from available pool
        var known = new System.Collections.Generic.List<Card>();
        known.AddRange(hero.data.Hole ?? new List<Card>());
        known.AddRange(game.community);
        // Remove by matching rank+suit
        foreach (var k in known)
        {
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].rank == k.rank && all[i].suit == k.suit)
                {
                    all.RemoveAt(i);
                    break;
                }
            }
        }

        int opponents = game.players.Count(p => !p.data.Folded && p != hero);
        if (opponents <= 0) return 1.0f;

        int needCommunity = Math.Max(0, 5 - game.community.Count);
        int winsNumeratorScaled = 0;
        var rnd = new System.Random();

        for (int it = 0; it < iterations; it++)
        {
            // copy and shuffle
            var pool = new System.Collections.Generic.List<Card>(all);
            // Fisher–Yates shuffle
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
            }

            int pos = 0;
            // deal hole cards to opponents
            var oppHands = new System.Collections.Generic.List<System.Collections.Generic.List<Card>>();
            for (int o = 0; o < opponents; o++)
            {
                var h = new System.Collections.Generic.List<Card> { pool[pos++], pool[pos++] };
                oppHands.Add(h);
            }

            // complete community
            var communityComplete = new System.Collections.Generic.List<Card>(game.community);
            for (int k = 0; k < needCommunity; k++) communityComplete.Add(pool[pos++]);

            // evaluate hero
            var heroAll = new System.Collections.Generic.List<Card>();
            heroAll.AddRange(hero.data.Hole ?? new List<Card>());
            heroAll.AddRange(communityComplete);
            long heroScore = HandEvaluator.EvaluateBest(heroAll);

            long bestOpp = -1;

            var oppScores = new System.Collections.Generic.List<long>();
            for (int o = 0; o < opponents; o++)
            {
                var oppAll = new System.Collections.Generic.List<Card>();
                oppAll.AddRange(oppHands[o]);
                oppAll.AddRange(communityComplete);
                long sc = HandEvaluator.EvaluateBest(oppAll);
                oppScores.Add(sc);
                if (sc > bestOpp) bestOpp = sc;
            }

            // determine winner share
            long maxScore = Math.Max(bestOpp, heroScore);
            int tied = 0;
            if (heroScore == maxScore) tied++;
            for (int o = 0; o < opponents; o++)
                if (oppScores[o] == maxScore)
                    tied++;
            if (heroScore == maxScore)
                winsNumeratorScaled += 1000 / Math.Max(1, tied); // scale by 1000 to keep integer
        }

        return (float)winsNumeratorScaled / (iterations * 1000f);
    }
}
