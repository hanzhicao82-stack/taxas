using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Simple human-player UI controller for poker actions.
// Attach this to a GameObject and assign the Panel and Buttons in the Inspector.
public class HumanPlayerUI : MonoBehaviour
{
    [Tooltip("Seat index for the human player (1-based). Default is 1.")]
    public int humanSeat = 1;

    [Tooltip("Parent panel that contains the action buttons.")]
    public GameObject panel;

    [Tooltip("Optional Text used to show which seat is acting.")]
    public Text seatLabel;
    public int currentSeat = 1;

    [Tooltip("Optional panel shown when entering a raise amount.")]
    public GameObject raisePanel;
    public Slider raiseSlider;
    public Text raiseValueLabel;
    public Button raiseConfirmButton;
    public Button raiseCancelButton;

    [Tooltip("Optional Bet panel and controls for initial bets when no current bet.")]
    public Button betButton;
    public GameObject betPanel;
    public Slider betSlider;
    public Text betValueLabel;
    public Button betConfirmButton;
    public Button betCancelButton;

    public Button allinButton;
    public Button foldButton;
    public Button callButton;
    public Button checkButton;
    public Button raiseButton;

    // Action callback: action string and optional amount (0 if not used)
    public delegate void PlayerAction(EPlayerAction action, params object[] args);
    public event PlayerAction OnAction;
    public static HumanPlayerUI Instance;

    void Awake()
    {
        Instance = this;

    }

    void Start()
    {
        if (allinButton != null) allinButton.onClick.AddListener(AllIn);
        if (foldButton != null) foldButton.onClick.AddListener(Fold);
        if (callButton != null) callButton.onClick.AddListener(Call);
        if (checkButton != null) checkButton.onClick.AddListener(Check);
        if (raiseButton != null) raiseButton.onClick.AddListener(Raise);

        if (raiseConfirmButton != null) raiseConfirmButton.onClick.AddListener(ConfirmRaise);
        if (raiseCancelButton != null) raiseCancelButton.onClick.AddListener(CancelRaise);
        if (betButton != null) betButton.onClick.AddListener(Bet);
        if (betConfirmButton != null) betConfirmButton.onClick.AddListener(ConfirmBet);
        if (betCancelButton != null) betCancelButton.onClick.AddListener(CancelBet);

        if (raisePanel != null) raisePanel.SetActive(false);
        if (betPanel != null) betPanel.SetActive(false);

        Hide();

    }

    // Create the runtime UI if the scene doesn't supply one.
    public static HumanPlayerUI CreateHumanPlayerUI()
    {
        // Ensure Canvas
        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        GameObject canvasGO;
        if (canvas == null)
        {
            canvasGO = new GameObject("AutoUI Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasGO = canvas.gameObject;
        }

        // Ensure EventSystem
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var root = new GameObject("HumanPlayerUI_Auto");
        root.transform.SetParent(canvasGO.transform, false);
        var h = root.AddComponent<HumanPlayerUI>();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Panel (with layout)
        var panel = new GameObject("HP_Panel");
        panel.transform.SetParent(root.transform, false);
        var prt = panel.AddComponent<RectTransform>();
        prt.sizeDelta = new Vector2(560, 100);
        prt.anchorMin = new Vector2(0.5f, 0); prt.anchorMax = new Vector2(0.5f, 0); prt.pivot = new Vector2(0.5f, 0);
        prt.anchoredPosition = new Vector2(0, 40);
        var pImg = panel.AddComponent<Image>(); pImg.color = new Color(0f, 0f, 0f, 0.6f);
        var layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        // Seat label
        var seatLabelGO = new GameObject("SeatLabel"); seatLabelGO.transform.SetParent(root.transform, false);
        var slt = seatLabelGO.AddComponent<RectTransform>(); slt.sizeDelta = new Vector2(200, 24);
        slt.anchorMin = new Vector2(0.5f, 0); slt.anchorMax = new Vector2(0.5f, 0); slt.pivot = new Vector2(0.5f, 0); slt.anchoredPosition = new Vector2(0, 120);
        var seatText = seatLabelGO.AddComponent<Text>(); seatText.font = font; seatText.fontSize = 18; seatText.alignment = TextAnchor.MiddleCenter; seatText.color = Color.yellow; seatText.text = "";

        // Buttons container (so layout spacing and resizing is stable)
        var btnContainer = new GameObject("ButtonsContainer");
        btnContainer.transform.SetParent(panel.transform, false);
        var bcrt = btnContainer.AddComponent<RectTransform>(); bcrt.sizeDelta = new Vector2(520, 64);
        var hlg = btnContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8; hlg.childAlignment = TextAnchor.MiddleCenter; hlg.padding = new RectOffset(6, 6, 6, 6);

        // Helper to create button inside container
        System.Func<string, Button> makeBtn = (name) =>
        {
            var go = new GameObject(name);
            go.transform.SetParent(btnContainer.transform, false);
            var rt = go.AddComponent<RectTransform>(); rt.sizeDelta = new Vector2(96, 48);
            var img = go.AddComponent<Image>(); img.color = new Color(0.18f, 0.18f, 0.18f);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors; colors.highlightedColor = new Color(0.28f, 0.28f, 0.28f); colors.pressedColor = new Color(0.15f, 0.15f, 0.15f);
            btn.colors = colors;
            var le = go.AddComponent<LayoutElement>(); le.preferredWidth = 96; le.preferredHeight = 48;
            var lblGO = new GameObject("Label"); lblGO.transform.SetParent(go.transform, false);
            var lblRt = lblGO.AddComponent<RectTransform>(); lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one; lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
            var txt = lblGO.AddComponent<Text>(); txt.font = font; txt.fontSize = 16; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.text = name;
            var ol = lblGO.AddComponent<Outline>(); ol.effectColor = new Color(0f, 0f, 0f, 0.6f); ol.effectDistance = new Vector2(1, -1);
            return btn;
        };

        // Create five buttons spaced horizontally
        var allinB = makeBtn("AllIn");
        var foldB = makeBtn("Fold");
        var callB = makeBtn("Call");
        var checkB = makeBtn("Check");
        var betB = makeBtn("Bet");
        var raiseB = makeBtn("Raise");
        // Tweak labels and colors
        allinB.GetComponentInChildren<Text>().text = "All-In"; allinB.GetComponent<Image>().color = new Color(0.95f, 0.6f, 0.1f);
        foldB.GetComponentInChildren<Text>().text = "Fold"; foldB.GetComponent<Image>().color = new Color(0.8f, 0.1f, 0.1f);
        callB.GetComponentInChildren<Text>().text = "Call"; callB.GetComponent<Image>().color = new Color(0.1f, 0.6f, 0.1f);
        checkB.GetComponentInChildren<Text>().text = "Check"; checkB.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f);
        betB.GetComponentInChildren<Text>().text = "Bet"; betB.GetComponent<Image>().color = new Color(0.05f, 0.5f, 0.9f);
        raiseB.GetComponentInChildren<Text>().text = "Raise"; raiseB.GetComponent<Image>().color = new Color(0.1f, 0.4f, 0.9f);
        seatText.fontSize = 20; seatText.fontStyle = FontStyle.Bold;
        var seatShadow = seatLabelGO.AddComponent<Shadow>(); seatShadow.effectColor = new Color(0f, 0f, 0f, 0.6f); seatShadow.effectDistance = new Vector2(2, -2);

        // Raise panel (vertical layout: slider on top, buttons below)
        var raisePanel = new GameObject("RaisePanel"); raisePanel.transform.SetParent(root.transform, false);
        var rrt = raisePanel.AddComponent<RectTransform>(); rrt.sizeDelta = new Vector2(300, 96);
        rrt.anchorMin = new Vector2(0.5f, 0); rrt.anchorMax = new Vector2(0.5f, 0); rrt.pivot = new Vector2(0.5f, 0); rrt.anchoredPosition = new Vector2(0, 140);
        var rimg = raisePanel.AddComponent<Image>(); rimg.color = new Color(0f, 0f, 0f, 0.75f);
        var rVlg = raisePanel.AddComponent<VerticalLayoutGroup>(); rVlg.spacing = 6; rVlg.childAlignment = TextAnchor.UpperCenter; rVlg.childForceExpandWidth = true; rVlg.childControlHeight = true; rVlg.childControlWidth = true;
        var rCsf = raisePanel.AddComponent<ContentSizeFitter>(); rCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        raisePanel.SetActive(false);

        // Raise slider (top)
        var sliderGO = new GameObject("RaiseSlider"); sliderGO.transform.SetParent(raisePanel.transform, false);
        var srt = sliderGO.AddComponent<RectTransform>(); srt.sizeDelta = new Vector2(260, 40);
        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.wholeNumbers = false; slider.value = 0.5f;
        var sBg = new GameObject("Background"); sBg.transform.SetParent(sliderGO.transform, false);
        var sBgImg = sBg.AddComponent<Image>(); sBgImg.color = new Color(0.15f, 0.15f, 0.15f);
        var handle = new GameObject("Handle"); handle.transform.SetParent(sliderGO.transform, false);
        var hImg = handle.AddComponent<Image>(); hImg.color = Color.white;
        slider.fillRect = sBgImg.rectTransform; slider.handleRect = hImg.rectTransform; slider.targetGraphic = hImg;
        var valGO = new GameObject("RaiseValue"); valGO.transform.SetParent(raisePanel.transform, false);
        var valRt = valGO.AddComponent<RectTransform>(); valRt.sizeDelta = new Vector2(260, 24);
        var valText = valGO.AddComponent<Text>(); valText.font = font; valText.fontSize = 14; valText.alignment = TextAnchor.MiddleCenter; valText.color = Color.white; valText.text = "50%";
        // initial percent-only listener (will be overridden when panel opens to include chip amount)
        slider.onValueChanged.AddListener((v) => { valText.text = Mathf.RoundToInt(v * 100f) + "%"; });

        // Confirm / Cancel container (buttons below)
        Button CreateSmall(string label, Vector2 pos, Transform parent)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rtBtn = go.AddComponent<RectTransform>(); rtBtn.sizeDelta = new Vector2(120, 36);
            var imgBtn = go.AddComponent<Image>(); imgBtn.color = new Color(0.2f, 0.2f, 0.2f);
            var btn = go.AddComponent<Button>();
            var le = go.AddComponent<LayoutElement>(); le.preferredWidth = 120; le.preferredHeight = 36;
            var lbl = new GameObject("Label"); lbl.transform.SetParent(go.transform, false);
            var lblRt = lbl.AddComponent<RectTransform>(); lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one; lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
            var txt = lbl.AddComponent<Text>(); txt.font = font; txt.fontSize = 14; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.text = label;
            lbl.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);
            return btn;
        }

        var btnRow = new GameObject("RaiseButtons"); btnRow.transform.SetParent(raisePanel.transform, false);
        var brt = btnRow.AddComponent<RectTransform>(); brt.sizeDelta = new Vector2(260, 40);
        var hrow = btnRow.AddComponent<HorizontalLayoutGroup>(); hrow.spacing = 8; hrow.childAlignment = TextAnchor.MiddleCenter; hrow.childForceExpandWidth = false;
        var confBtn = CreateSmall("确定", Vector2.zero, btnRow.transform);
        var cancBtn = CreateSmall("取消", Vector2.zero, btnRow.transform);
        confBtn.GetComponentInChildren<Text>().text = "确定";
        cancBtn.GetComponentInChildren<Text>().text = "取消";
        confBtn.GetComponent<Image>().color = new Color(0.1f, 0.6f, 0.1f);
        cancBtn.GetComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f);

        // Bet panel (similar to raise)
        var betPanel = new GameObject("BetPanel"); betPanel.transform.SetParent(root.transform, false);
        var bprt = betPanel.AddComponent<RectTransform>(); bprt.sizeDelta = new Vector2(300, 96);
        bprt.anchorMin = new Vector2(0.5f, 0); bprt.anchorMax = new Vector2(0.5f, 0); bprt.pivot = new Vector2(0.5f, 0); bprt.anchoredPosition = new Vector2(0, 230);
        var bimg = betPanel.AddComponent<Image>(); bimg.color = new Color(0f, 0f, 0f, 0.75f);
        var bVlg = betPanel.AddComponent<VerticalLayoutGroup>(); bVlg.spacing = 6; bVlg.childAlignment = TextAnchor.UpperCenter; bVlg.childForceExpandWidth = true; bVlg.childControlHeight = true; bVlg.childControlWidth = true;
        var bCsf = betPanel.AddComponent<ContentSizeFitter>(); bCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        betPanel.SetActive(false);
        var bSliderGO = new GameObject("BetSlider"); bSliderGO.transform.SetParent(betPanel.transform, false);
        var bsRt = bSliderGO.AddComponent<RectTransform>(); bsRt.sizeDelta = new Vector2(260, 40);
        var bSlider = bSliderGO.AddComponent<Slider>(); bSlider.minValue = 0f; bSlider.maxValue = 1f; bSlider.wholeNumbers = false; bSlider.value = 0.5f;
        var bBg = new GameObject("Background"); bBg.transform.SetParent(bSliderGO.transform, false);
        var bBgImg = bBg.AddComponent<Image>(); bBgImg.color = new Color(0.15f, 0.15f, 0.15f);
        var bHandle = new GameObject("Handle"); bHandle.transform.SetParent(bSliderGO.transform, false);
        var bHImg = bHandle.AddComponent<Image>(); bHImg.color = Color.white;
        bSlider.fillRect = bBgImg.rectTransform; bSlider.handleRect = bHImg.rectTransform; bSlider.targetGraphic = bHImg;
        var bValGO = new GameObject("BetValue"); bValGO.transform.SetParent(betPanel.transform, false);
        var bValRt = bValGO.AddComponent<RectTransform>(); bValRt.sizeDelta = new Vector2(260, 24);
        var bValText = bValGO.AddComponent<Text>(); bValText.font = font; bValText.fontSize = 14; bValText.alignment = TextAnchor.MiddleCenter; bValText.color = Color.white; bValText.text = "50%";
        bSlider.onValueChanged.AddListener((v) => { bValText.text = Mathf.RoundToInt(v * 100f) + "%"; });
        var betBtnRow = new GameObject("BetButtons"); betBtnRow.transform.SetParent(betPanel.transform, false);
        var bbrt = betBtnRow.AddComponent<RectTransform>(); bbrt.sizeDelta = new Vector2(260, 40);
        var bHlg = betBtnRow.AddComponent<HorizontalLayoutGroup>(); bHlg.spacing = 8; bHlg.childAlignment = TextAnchor.MiddleCenter; bHlg.childForceExpandWidth = false;
        var betConf = CreateSmall("确定", Vector2.zero, betBtnRow.transform);
        var betCanc = CreateSmall("取消", Vector2.zero, betBtnRow.transform);
        betConf.GetComponentInChildren<Text>().text = "确定";
        betCanc.GetComponentInChildren<Text>().text = "取消";
        betConf.GetComponent<Image>().color = new Color(0.1f, 0.6f, 0.1f);
        betCanc.GetComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f);

        // Wire references into HumanPlayerUI
        h.panel = panel;
        h.allinButton = allinB;
        h.foldButton = foldB;
        h.callButton = callB;
        h.checkButton = checkB;
        h.betButton = betB;
        h.raiseButton = raiseB;
        h.raisePanel = raisePanel;
        h.raiseSlider = slider;
        h.raiseValueLabel = valText;
        h.raiseConfirmButton = confBtn;
        h.raiseCancelButton = cancBtn;
        h.betPanel = betPanel;
        h.betSlider = bSlider;
        h.betValueLabel = bValText;
        h.betConfirmButton = betConf;
        h.betCancelButton = betCanc;
        h.seatLabel = seatText;

        return h;
    }



    // Call this from your game-turn logic when it's a player's turn.
    // Call this from your game-turn logic when it's a player's turn.
    // Show the panel and update the seat label so player knows who is acting.
    public void ShowForSeat(int seat, ERoundPhase phase)
    {
        currentSeat = seat;
        if (seatLabel != null)
        {
            switch (phase)
            {
                case ERoundPhase.Preflop:
                    seatLabel.text = $"座位 {currentSeat + 1} 下注）";
                    break;
                case ERoundPhase.Flop:
                    seatLabel.text = $"座位 {currentSeat + 1} 行动（翻牌）";
                    break;
                case ERoundPhase.Turn:
                    seatLabel.text = $"座位 {currentSeat + 1} 行动（转牌）";
                    break;
                case ERoundPhase.River:
                    seatLabel.text = $"座位 {currentSeat + 1} 行动（河牌）";
                    break;
            }
        }
        Show();
    }

    // Configure which actions are available given the current need to call.
    // If need == 0, allow Check and Bet; otherwise allow Call and Raise.
    public void ConfigureForNeed(int need)
    {
        if (checkButton != null) checkButton.interactable = (need == 0);
        if (betButton != null) betButton.interactable = (need == 0);
        if (callButton != null) callButton.interactable = (need > 0);
        if (raiseButton != null) raiseButton.interactable = (need > 0);
        if (allinButton != null) allinButton.interactable = true;
        if (foldButton != null) foldButton.interactable = true;
    }

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
        if (raisePanel != null) raisePanel.SetActive(false);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    void AllIn()
    {
            if (betSlider != null)
            {
                var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
                int stack = 0; int minAllowed = 1;
                if (game != null && currentSeat >= 0 && currentSeat < game.players.Count)
                {
                    stack = game.players[currentSeat].data.Stack;
                    minAllowed = game.data.BigBlindAmount;
                }
                float minFrac = (stack > 0) ? Mathf.Clamp01((float)minAllowed / Mathf.Max(1, stack)) : 0f;
                betSlider.minValue = minFrac;
                betSlider.maxValue = 1f;
                betSlider.value = Mathf.Clamp(betSlider.value, minFrac, 1f);
                betSlider.onValueChanged.RemoveAllListeners();
                betSlider.onValueChanged.AddListener((v) => {
                    var g = UnityEngine.Object.FindObjectOfType<PokerGame>();
                    int s = 0;
                    if (g != null && currentSeat >= 0 && currentSeat < g.players.Count) s = g.players[currentSeat].data.Stack;
                    int chips = Mathf.RoundToInt(v * s);
                    if (betValueLabel != null) betValueLabel.text = Mathf.RoundToInt(v * 100f) + "% (" + chips + ")";
                });
                betSlider.onValueChanged.Invoke(betSlider.value);
            }

    void Call()
    {
        OnAction?.Invoke(EPlayerAction.Call);
        Hide();
    }

    void Check()
    {
        OnAction?.Invoke(EPlayerAction.Check);
        Hide();
    }

    void Raise()
    {
        // Show raise input panel if configured, otherwise fall back to simple raise
        if (raisePanel != null)
        {
            if (panel != null) panel.SetActive(false);
            raisePanel.SetActive(true);
            if (raiseSlider != null)
            {
                var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
                int stack = 0; int minAllowed = 1;
                if (game != null && currentSeat >= 0 && currentSeat < game.players.Count)
                {
                    stack = game.players[currentSeat].data.Stack;
                    minAllowed = game.data.BigBlindAmount;
                }
                float minFrac = (stack > 0) ? Mathf.Clamp01((float)minAllowed / Mathf.Max(1, stack)) : 0f;
                raiseSlider.minValue = minFrac;
                raiseSlider.maxValue = 1f;
                raiseSlider.value = Mathf.Clamp(raiseSlider.value, minFrac, 1f);
                raiseSlider.onValueChanged.RemoveAllListeners();
                raiseSlider.onValueChanged.AddListener((v) => {
                    var g = UnityEngine.Object.FindObjectOfType<PokerGame>();
                    int s = 0;
                    if (g != null && currentSeat >= 0 && currentSeat < g.players.Count) s = g.players[currentSeat].data.Stack;
                    int chips = Mathf.RoundToInt(v * s);
                    if (raiseValueLabel != null) raiseValueLabel.text = Mathf.RoundToInt(v * 100f) + "% (" + chips + ")";
                });
                // initialize label
                raiseSlider.onValueChanged.Invoke(raiseSlider.value);
            }
        }
        else
        {
            OnAction?.Invoke(EPlayerAction.Raise);
            Hide();
        }
    }

    void Bet()
    {
        if (betPanel != null)
        {
            if (panel != null) panel.SetActive(false);
            betPanel.SetActive(true);
            if (betSlider != null)
            {
                betSlider.value = 0.5f;
                if (betValueLabel != null) betValueLabel.text = Mathf.RoundToInt(betSlider.value * 100f) + "%";
            }
        }
        else
        {
            OnAction?.Invoke(EPlayerAction.Bet);
            Hide();
        }
    }

    void ConfirmRaise()
    {
        int amount = 0;
        var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
        if (raiseSlider != null && game != null && currentSeat >= 0 && currentSeat < game.players.Count)
        {
            int stack = game.players[currentSeat].data.Stack;
            amount = Mathf.RoundToInt(raiseSlider.value * stack);
            // enforce minimum raise (at least big blind) and clamp to stack
            int minAllowed = Mathf.Max(1, game.data.BigBlindAmount);
            amount = Mathf.Clamp(amount, minAllowed, stack);
        }
        OnAction?.Invoke(EPlayerAction.Raise, amount);
        if (raisePanel != null) raisePanel.SetActive(false);
        if (panel != null) panel.SetActive(false);
    }

    void ConfirmBet()
    {
        int amount = 0;
        var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
        if (betSlider != null && game != null && currentSeat >= 0 && currentSeat < game.players.Count)
        {
            int stack = game.players[currentSeat].data.Stack;
            amount = Mathf.RoundToInt(betSlider.value * stack);
            int minAllowed = Mathf.Max(1, game.data.BigBlindAmount);
            amount = Mathf.Clamp(amount, minAllowed, stack);
        }
        OnAction?.Invoke(EPlayerAction.Bet, amount == 0 ? 0 : amount);
        if (betPanel != null) betPanel.SetActive(false);
        if (panel != null) panel.SetActive(false);
    }

    void CancelBet()
    {
        if (betPanel != null) betPanel.SetActive(false);
        if (panel != null) panel.SetActive(true);
    }

    void CancelRaise()
    {
        if (raisePanel != null) raisePanel.SetActive(false);
        if (panel != null) panel.SetActive(true);
    }

    // Show the Bet input panel (keeps the main action panel hidden).
    public void ShowBet()
    {
        if (betPanel != null)
        {
            if (panel != null)
                panel.SetActive(false);
            betPanel.SetActive(true);
            if (betSlider != null)
            {
                betSlider.value = 0.5f;
                // update label immediately
                var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
                if (betValueLabel != null && game != null && currentSeat >= 0 && currentSeat < game.players.Count)
                {
                    int stack = game.players[currentSeat].data.Stack;
                    betValueLabel.text = Mathf.RoundToInt(betSlider.value * 100f) + "%";
                }
            }
        }
    }

    // Show/Hide a specific action button so callers can enable or disable
    // actions individually. Uses EPlayerAction enum to identify the button.
    public void ShowActionButtons()
    {
        betPanel.SetActive(false);
        panel.SetActive(true);
    }
}
