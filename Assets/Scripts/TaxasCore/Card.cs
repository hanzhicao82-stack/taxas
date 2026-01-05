using System;

/// <summary>
/// Suits for playing cards.
/// 花色：表示扑克牌的四种花色（梅花、方块、红心、黑桃）。
/// </summary>
public enum Suit { Clubs, Diamonds, Hearts, Spades }

/// <summary>
/// Simple representation of a playing card.
/// 牌的简单表示。点数使用 2..14 的整数表示，其中 11=J，12=Q，13=K，14=A。
/// </summary>
public class Card
{
    /// <summary>Card rank: 2..14 (A=14)</summary>
    /// <summary>牌点：2..14（A 为 14）</summary>
    public int rank; // 2-14 (11=J,12=Q,13=K,14=A)

    /// <summary>Card suit.</summary>
    /// <summary>牌花色。</summary>
    public Suit suit;

    /// <summary>Create a new card with given rank and suit.</summary>
    /// <summary>构造函数：使用指定的点数和花色创建一张牌。</summary>
    public Card(int rank, Suit suit)
    {
        this.rank = rank;
        this.suit = suit;
    }

    /// <summary>Return a short human-friendly string like "A♠" or "10♥".</summary>
    /// <summary>返回便于阅读的短字符串表示，例如 "A♠" 或 "10♥"。</summary>
    public override string ToString()
    {
        string r;
        if (rank <= 10) r = rank.ToString();
        else if (rank == 11) r = "J";
        else if (rank == 12) r = "Q";
        else if (rank == 13) r = "K";
        else r = "A";
        char s = suit switch
        {
            Suit.Clubs => '♣',
            Suit.Diamonds => '♦',
            Suit.Hearts => '♥',
            Suit.Spades => '♠',
            _ => '?'
        };
        return r + s;
    }
}
