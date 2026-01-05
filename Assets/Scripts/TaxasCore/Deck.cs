using System;
using System.Collections.Generic;


/// <summary>
/// Simple 52-card deck with shuffle and draw operations.
/// This class is not Unity-specific and uses System.Random.
///
/// 简单的 52 张牌牌堆，包含洗牌和抽牌操作。
/// 该类不依赖 Unity 引擎特性，使用 System.Random 来生成洗牌序列。
/// </summary>
public class Deck
{
    private List<Card> cards = new List<Card>();
    private Random rng = new Random();

    /// <summary>Create and populate a new deck.</summary>
    /// <summary>
    /// 构造函数：创建并初始化为一副完整牌堆（调用 Reset）。
    /// </summary>
    public Deck()
    {
        Reset();
    }

    /// <summary>Reset the deck to a full 52-card ordered deck (2..A per suit).</summary>
    /// <remarks>
    /// 重置牌堆为有序状态，按点数和花色生成 52 张牌（2..A）。
    /// 该方法不会打乱牌堆，若需随机顺序请调用 <see cref="Shuffle"/> 。
    /// </remarks>
    public void Reset()
    {
        cards.Clear();
        for (int r = 2; r <= 14; r++)
        {
            foreach (Suit s in Enum.GetValues(typeof(Suit)))
            {
                cards.Add(new Card(r, s));
            }
        }
    }

    /// <summary>Shuffle the deck in-place using Fisher–Yates.</summary>
    /// <remarks>
    /// 原地使用 Fisher–Yates 算法随机打乱牌堆。
    /// 使用 <see cref="rng"/> 提供的伪随机数生成器。
    /// </remarks>
    public void Shuffle()
    {
        int n = cards.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var tmp = cards[k];
            cards[k] = cards[n];
            cards[n] = tmp;
        }
    }

    /// <summary>Draw the top card; returns null if deck is empty.</summary>
    /// <remarks>
    /// 从牌堆顶部抽一张牌并从牌堆中移除；当牌堆为空时返回 null。
    /// 注意：对于真实游戏应在抽牌前处理“烧牌”（burn），调用者负责实现该逻辑。
    /// </remarks>
    public Card Draw()
    {
        if (cards.Count == 0) return null;
        var c = cards[0];
        cards.RemoveAt(0);
        return c;
    }
}
