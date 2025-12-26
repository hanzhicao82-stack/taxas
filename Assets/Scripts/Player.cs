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
    /// Move mutable state into PlayData and expose as `data`.
    public PlayData data = new PlayData();

    public Player(int id, string name)
    {
        this.id = id;
        this.name = name;
    }

    /// <summary>Reset transient per-hand fields before a new hand.</summary>
    /// <summary>重置每手牌的临时状态：清空底牌、重置下注和状态标志供下一手使用。</summary>
    public void ResetForHand()
    {
        data.ResetForHand();
    }
}
