using System;
using UnityEngine;

// Controller for UI confirm/cancel actions following a lightweight MVC pattern.
// This class centralizes the logic that was previously implemented in HumanPlayerUI
// (ConfirmRaise/CancelRaise, ConfirmBet/CancelBet, ConfirmBuyStack/CancelBuyStack,
// ConfirmAllIn/CancelAllIn). HumanPlayerUI will forward calls to this controller.
public class UICtrl : MonoBehaviour
{
    public static UICtrl Instance;
    public HumanPlayerUI ui;

    void Awake()
    {
        Instance = this;
    }

    public void Init(HumanPlayerUI ui)
    {
        this.ui = ui;
    }

    public void ConfirmRaise()
    {
        if (ui == null) return;
        int amount = 0;
        var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
        if (ui.raiseSlider != null && game != null && ui.currentSeat >= 0 && ui.currentSeat < game.players.Count)
        {
            var player = game.players[ui.currentSeat];
            int stack = player.data.Stack;
            amount = Mathf.CeilToInt(ui.raiseSlider.value * stack);
            int need = Mathf.Max(0, game.currentBet - player.data.CurrentBet);
            float minRaiseFrac = (game.aiConfig != null) ? game.aiConfig.minRaiseFraction : 1f;
            int minByConfig = Mathf.Max(1, Mathf.FloorToInt(game.data.BigBlindAmount * minRaiseFrac));
            int minByLastRaise = Mathf.Max(1, game.lastRaiseAmount);
            int minRaise = Math.Max(minByConfig, minByLastRaise);
            int minAllowed = Math.Max(1, need + minRaise);
            amount = Mathf.Clamp(amount, minAllowed, stack);
        }
        ui.OnAction?.Invoke(EPlayerAction.Raise, amount);
        var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
        if (game != null && ui != null) ui.UpdateButtons(game.players[ui.currentSeat], game);
        if (ui.raisePanel != null) ui.raisePanel.SetActive(false);
        if (ui.panel != null) ui.panel.SetActive(false);
    }

    public void CancelRaise()
    {
        if (ui.raisePanel != null) ui.raisePanel.SetActive(false);
        if (ui.panel != null) ui.panel.SetActive(true);
    }

    public void ConfirmBet()
    {
        if (ui == null) return;
        int amount = 0;
        var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
        if (ui.betSlider != null && game != null && ui.currentSeat >= 0 && ui.currentSeat < game.players.Count)
        {
            int stack = game.players[ui.currentSeat].data.Stack;
            amount = Mathf.CeilToInt(ui.betSlider.value * stack);
            int bigBlind = Mathf.Max(1, game.data.BigBlindAmount);
            float minRaiseFrac = (game.aiConfig != null) ? game.aiConfig.minRaiseFraction : 1f;
            int minByConfig = Mathf.Max(1, Mathf.FloorToInt(game.data.BigBlindAmount * minRaiseFrac));
            int minByLastRaise = Mathf.Max(1, game.lastRaiseAmount);
            int minRaise = Math.Max(minByConfig, minByLastRaise);
            int minAllowed = Math.Max(bigBlind, minRaise);
            int required = Mathf.Max(amount, minAllowed);
            int multiples = Mathf.Max(1, Mathf.CeilToInt((float)required / bigBlind));
            amount = multiples * bigBlind;
            amount = Mathf.Clamp(amount, minAllowed, stack);
        }
        ui.OnAction?.Invoke(EPlayerAction.Bet, amount == 0 ? 0 : amount);
        var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
        if (game != null && ui != null) ui.UpdateButtons(game.players[ui.currentSeat], game);
        if (ui.betPanel != null) ui.betPanel.SetActive(false);
        if (ui.panel != null) ui.panel.SetActive(false);
    }

    public void CancelBet()
    {
        // signal cancellation as a zero bet (mirrors previous behavior)
        ui.OnAction?.Invoke(EPlayerAction.Bet, 0);
        if (ui.betPanel != null) ui.betPanel.SetActive(false);
        if (ui.panel != null) ui.panel.SetActive(true);
    }

    public void ConfirmBuyStack()
    {
        if (ui == null) return;
        if (ui.buySlider == null)
        {
            if (ui.buyPanel != null) ui.buyPanel.SetActive(false);
            if (ui.panel != null) ui.panel.SetActive(true);
            return;
        }
        int maxVal = Mathf.RoundToInt(ui.buySlider != null ? ui.buySlider.maxValue : 9999);
        int amount = Mathf.Clamp(Mathf.RoundToInt(ui.buySlider.value), 1, maxVal);
        ui.OnAction?.Invoke(EPlayerAction.BuyIn, amount);
        var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
        if (game != null && ui != null) ui.UpdateButtons(game.players[ui.currentSeat], game);
        if (ui.buyPanel != null) ui.buyPanel.SetActive(false);
        if (ui.panel != null) ui.panel.SetActive(true);
    }

    public void CancelBuyStack()
    {
        if (ui.buyPanel != null) ui.buyPanel.SetActive(false);
        if (ui.panel != null) ui.panel.SetActive(true);
    }

    public void ConfirmAllIn()
    {
        if (ui == null) return;
        ui.OnAction?.Invoke(EPlayerAction.AllIn);
        if (ui.allinConfirmPanel != null) ui.allinConfirmPanel.SetActive(false);
        if (ui.panel != null) ui.panel.SetActive(false);
    }

    public void CancelAllIn()
    {
        if (ui.allinConfirmPanel != null) ui.allinConfirmPanel.SetActive(false);
        if (ui.panel != null) ui.panel.SetActive(true);
    }
}
