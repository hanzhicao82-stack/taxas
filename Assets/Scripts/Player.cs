using System.Collections.Generic;

/// <summary>
/// Simple player model holding hole cards and basic metadata.
/// 玩家模型：保存手牌（hole cards）以及一些基础元数据和下注状态。
/// 该类用于示例用途，包含筹码栈、当前下注、是否弃牌/全下等字段。
/// </summary>
public class Player
{
    /// <summary>Player index (0-based).</summary>
    /// <summary>玩家索引（从 0 开始）。</summary>
    public int id;

    /// <summary>Display name.</summary>
    /// <summary>显示名称。</summary>
    public string name;

    /// <summary>Hole cards (2 cards in Texas Hold'em).</summary>
    /// <summary>底牌（Hole cards），德州扑克每人两张。</summary>
    public List<Card> hole = new List<Card>();

    /// <summary>Player chip stack (用于简化示例，单位任意)。</summary>
    /// <summary>玩家筹码（栈），表示玩家当前拥有的筹码数量。</summary>
    public int stack = 1000;

    /// <summary>本轮已投入的筹码（用于下注轮比较）。</summary>
    /// <summary>本手/本轮已投入的筹码（用于比较当前投注量和构建边池）。</summary>
    public int currentBet = 0;

    /// <summary>是否已弃牌（fold）。</summary>
    /// <summary>是否已弃牌（folded），弃牌的玩家不再参与本手牌的胜负比较。</summary>
    public bool folded = false;

    /// <summary>是否已全下（all-in）。</summary>
    /// <summary>是否已全下（all-in），全下后玩家不能再增加筹码但仍可获分配边池。</summary>
    public bool allIn = false;

    /// <summary>是否仍在本手牌比赛（未被淘汰）。</summary>
    /// <summary>是否仍然处于活动状态（例如用于标记被移除或站起等情况）。</summary>
    public bool active = true;

    /// <summary>玩家攻击性系数（影响加注/下注概率与规模），每手可随机分配以产生行为差异。</summary>
    /// <summary>攻击性（aggression），默认 1.0；>1 更激进，<1 更保守。</summary>
    public float aggression = 1.0f;

    public Player(int id, string name)
    {
        this.id = id;
        this.name = name;
    }

    /// <summary>Reset transient per-hand fields before a new hand.</summary>
    /// <summary>重置每手牌的临时状态：清空底牌、重置下注和状态标志供下一手使用。</summary>
    public void ResetForHand()
    {
        hole.Clear();
        currentBet = 0;
        folded = false;
        allIn = false;
        active = true;
    }
}
