using System;
using System.Collections.Generic;
using System.Linq;

public static class HandEvaluator
{
    // Evaluate best 5-card hand from up to 7 cards. Return a numeric score (higher = better).
    /// <summary>
    /// 从最多 7 张牌中评估出最好的 5 张牌组合并返回比较用的分值（越大越优）。
    /// 返回值是将牌型类别编码在高位、用于平局比较的关键牌位编码在低位的长整型数值，便于直接比较大小。
    /// </summary>
    /// <param name="cards">要评估的牌列表（5 到 7 张）。</param>
    /// <returns>表示最佳 5 张牌手牌强度的数值分数。</returns>
    public static long EvaluateBest(List<Card> cards)
    {
        if (cards == null || cards.Count < 5) return 0;
        long best = 0;
        int n = cards.Count;
        int[] idx = new int[5];
        for (int a = 0; a < n - 4; a++)
            for (int b = a + 1; b < n - 3; b++)
                for (int c = b + 1; c < n - 2; c++)
                    for (int d = c + 1; d < n - 1; d++)
                        for (int e = d + 1; e < n; e++)
                        {
                            var hand = new List<Card> { cards[a], cards[b], cards[c], cards[d], cards[e] };
                            long score = Evaluate5(hand);
                            if (score > best) best = score;
                        }
        return best;
    }

    /// <summary>
    /// 对固定的 5 张牌手牌进行评估并返回可比较的分值。
    /// 该方法识别手牌类别（同花顺、四条、葫芦等）并将类别与用于平局决胜的牌位一并编码进一个 long 值中。
    /// </summary>
    /// <param name="hand">恰好 5 张牌的列表。</param>
    /// <returns>该 5 张牌手牌的可比较数值分数。</returns>
    private static long Evaluate5(List<Card> hand)
    {
        var ranks = hand.Select(c => c.rank).OrderByDescending(x => x).ToList();
        bool isFlush = hand.All(c => c.suit == hand[0].suit);
        // 处理顺子（A 可作低牌）
        var distinctRanks = hand.Select(c => c.rank).Distinct().OrderByDescending(x => x).ToList();
        bool isStraight = false;
        int topStraight = 0;
        // 暴力检测顺子：将 A 当作 1 也考虑在内
        var rr = hand.Select(c => c.rank).Distinct().ToList();
        var candidate = rr.ToList();
        if (candidate.Contains(14)) candidate.Add(1);
        candidate = candidate.Distinct().OrderByDescending(x => x).ToList();
        for (int i = 0; i <= candidate.Count - 5; i++)
        {
            bool ok = true;
            for (int k = 0; k < 4; k++)
            {
                if (candidate[i + k] - 1 != candidate[i + k + 1])
                {
                    ok = false; break;
                }
            }
            if (ok)
            {
                isStraight = true;
                topStraight = candidate[i];
                break;
            }
        }

        var groups = hand.GroupBy(c => c.rank).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
        // 确定牌型类别并准备平局比较牌位
        int category = 0; // 8 同花顺,7 四条,6 葫芦,5 同花,4 顺子,3 三条,2 两对,1 一对,0 高牌
        List<int> tiebreak = new List<int>();

        if (isStraight && isFlush)
        { category = 8; tiebreak.Add(topStraight); }

        else if (groups[0].Count() == 4)
        {
            category = 7;
            tiebreak.Add(groups[0].Key);
            tiebreak.AddRange(hand.Select(c => c.rank).Where(r => r != groups[0].Key).OrderByDescending(x => x));
        }
        else if (groups[0].Count() == 3 && groups.Count > 1 && groups[1].Count() >= 2)
        {
            category = 6; tiebreak.Add(groups[0].Key); tiebreak.Add(groups[1].Key);
        }
        else if (isFlush)
        {
            category = 5; tiebreak.AddRange(ranks);
        }
        else if (isStraight)
        {
            category = 4; tiebreak.Add(topStraight);
        }
        else if (groups[0].Count() == 3)
        {
            category = 3; tiebreak.Add(groups[0].Key); tiebreak.AddRange(groups.Skip(1).Select(g => g.Key).OrderByDescending(x => x));
        }
        else if (groups[0].Count() == 2 && groups.Count > 1 && groups[1].Count() == 2)
        {
            category = 2; var pairs = groups.Where(g => g.Count() == 2).Select(g => g.Key).OrderByDescending(x => x); tiebreak.AddRange(pairs); tiebreak.AddRange(groups.Where(g => g.Count() == 1).Select(g => g.Key).OrderByDescending(x => x));
        }
        else if
        (groups[0].Count() == 2)
        {
            category = 1; tiebreak.Add(groups[0].Key); tiebreak.AddRange(groups.Where(g => g.Count() == 1).Select(g => g.Key).OrderByDescending(x => x));
        }
        else
        {
            category = 0; tiebreak.AddRange(ranks);
        }

        long score = ((long)category << 40);
        long shift = 32;
        foreach (var v in tiebreak)
        {
            score |= ((long)v << (int)shift);
            shift -= 6;
            if (shift < 0) break;
        }
        return score;
    }
}
