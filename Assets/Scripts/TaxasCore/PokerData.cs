using System;
using System.Collections.Generic;

// Game-level data container using Data<T> wrappers.
public class PokerData
{
    public Data<List<Card>> CommunityData = new Data<List<Card>>(new List<Card>());
    public Data<int> SmallBlindAmountData = new Data<int>(5);
    public Data<int> BigBlindAmountData = new Data<int>(10);
    public Data<int> CurrentBetData = new Data<int>(0);
    public Data<int> PotData = new Data<int>(0);

    public List<Card> Community { get => CommunityData.Value; set => CommunityData.Set(value); }
    public int SmallBlindAmount { get => SmallBlindAmountData.Value; set => SmallBlindAmountData.Set(value); }
    public int BigBlindAmount { get => BigBlindAmountData.Value; set => BigBlindAmountData.Set(value); }
    public int CurrentBet { get => CurrentBetData.Value; set => CurrentBetData.Set(value); }
    public int Pot { get => PotData.Value; set => PotData.Set(value); }

    public void ClearCommunity() => CommunityData.Set(new List<Card>());
}
