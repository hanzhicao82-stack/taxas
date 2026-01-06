using System;
using UnityEngine;
using System.Threading.Tasks;
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
    // transient hint label shown when actions are disabled or require explanation
    public Text hintLabel;
    public float hintDuration = 2f;

    [Tooltip("Optional panel shown when entering a raise amount.")]
    public GameObject raisePanel;
    public Slider raiseSlider;
    public Text raiseValueLabel;
    public Button raiseConfirmButton;
    public Button raiseCancelButton;


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
    // Optional confirm UI for AllIn when Call would be insufficient
    public GameObject allinConfirmPanel;
    public Button allinConfirmButton;
    public Button allinCancelButton;

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

        if (allinConfirmButton != null) allinConfirmButton.onClick.AddListener(ConfirmAllIn);
        if (allinCancelButton != null) allinCancelButton.onClick.AddListener(CancelAllIn);

        if (raiseConfirmButton != null) raiseConfirmButton.onClick.AddListener(ConfirmRaise);
        if (raiseCancelButton != null) raiseCancelButton.onClick.AddListener(CancelRaise);

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
        var raiseB = makeBtn("Raise");
        // Tweak labels and colors
        allinB.GetComponentInChildren<Text>().text = "全下"; allinB.GetComponent<Image>().color = new Color(0.95f, 0.6f, 0.1f);
        foldB.GetComponentInChildren<Text>().text = "弃牌"; foldB.GetComponent<Image>().color = new Color(0.8f, 0.1f, 0.1f);
        callB.GetComponentInChildren<Text>().text = "跟注"; callB.GetComponent<Image>().color = new Color(0.1f, 0.6f, 0.1f);
        checkB.GetComponentInChildren<Text>().text = "过牌"; checkB.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f);
        raiseB.GetComponentInChildren<Text>().text = "加注"; raiseB.GetComponent<Image>().color = new Color(0.1f, 0.4f, 0.9f);
        seatText.fontSize = 20; seatText.fontStyle = FontStyle.Bold;
        var seatShadow = seatLabelGO.AddComponent<Shadow>(); seatShadow.effectColor = new Color(0f, 0f, 0f, 0.6f); seatShadow.effectDistance = new Vector2(2, -2);

        // Raise panel (vertical layout: slider on top, buttons below)
        var raisePanel = new GameObject("RaisePanel"); raisePanel.transform.SetParent(root.transform, false);
        var rrt = raisePanel.AddComponent<RectTransform>(); rrt.sizeDelta = new Vector2(300, 96);
        rrt.anchorMin = new Vector2(0.5f, 0); rrt.anchorMax = new Vector2(0.5f, 0); rrt.pivot = new Vector2(0.5f, 0); rrt.anchoredPosition = new Vector2(0, 140);
        var rimg = raisePanel.AddComponent<Image>(); rimg.color = new Color(0f, 0f, 0f, 0.75f);
        var rVlg = raisePanel.AddComponent<VerticalLayoutGroup>(); 
        rVlg.spacing = 20;
        rVlg.childAlignment = TextAnchor.UpperCenter;
        rVlg.childForceExpandWidth = true;
        rVlg.childControlHeight = false;
        rVlg.childControlWidth = true;
        var rCsf = raisePanel.AddComponent<ContentSizeFitter>(); rCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        raisePanel.SetActive(false);

        // Raise slider (top)

        var sliderGO = GameObject.Instantiate(Resources.Load<GameObject>("Slider"));
        sliderGO.transform.SetParent(raisePanel.transform, false);
        Slider raiseSlider = sliderGO.GetComponent<Slider>();
        Text valText = sliderGO.GetComponentInChildren<Text>();
        // initial percent-only listener (will be overridden when panel opens to include chip amount)
        raiseSlider.onValueChanged.AddListener((v) => { valText.text = Mathf.RoundToInt(v * 100f) + "%"; });

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
        var bVlg = betPanel.AddComponent<VerticalLayoutGroup>();
        bVlg.spacing = 20; bVlg.childAlignment = TextAnchor.UpperCenter;
        bVlg.childForceExpandWidth = true;
        bVlg.childControlHeight = false;
        bVlg.childControlWidth = true;
        var bCsf = betPanel.AddComponent<ContentSizeFitter>(); bCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        betPanel.SetActive(false);




        var bSliderGO = GameObject.Instantiate(Resources.Load<GameObject>("Slider"));
        bSliderGO.transform.SetParent(betPanel.transform, false);
        var bSlider = bSliderGO.GetComponent<Slider>();
        var bValText = bSliderGO.GetComponentInChildren<Text>();
        bValText.text = "50%";
        bSlider.onValueChanged.AddListener((v) =>
        {
            string stack = "";
            if (Player.current != null)
                stack = Mathf.RoundToInt(v * Player.current.data.Stack).ToString();
            bValText.text = Mathf.RoundToInt(v * 100f) + "%" + " (" + stack + " 筹码)";
        });


        var betBtnRow = new GameObject("BetButtons"); betBtnRow.transform.SetParent(betPanel.transform, false);
        var bbrt = betBtnRow.AddComponent<RectTransform>();

        var bHlg = betBtnRow.AddComponent<HorizontalLayoutGroup>(); bHlg.spacing = 8; bHlg.childAlignment = TextAnchor.MiddleCenter; bHlg.childForceExpandWidth = false;
        var betConf = CreateSmall("确定", Vector2.zero, betBtnRow.transform);
        var betCanc = CreateSmall("取消", Vector2.zero, betBtnRow.transform);
        betConf.GetComponentInChildren<Text>().text = "确定";
        betCanc.GetComponentInChildren<Text>().text = "取消";
        betConf.GetComponent<Image>().color = new Color(0.1f, 0.6f, 0.1f);
        betCanc.GetComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f);

        // Hint label (small helper text shown above panel)
        var hintGO = new GameObject("HintLabel"); hintGO.transform.SetParent(root.transform, false);
        var hintRt = hintGO.AddComponent<RectTransform>(); hintRt.sizeDelta = new Vector2(400, 20);
        hintRt.anchorMin = new Vector2(0.5f, 0); hintRt.anchorMax = new Vector2(0.5f, 0); hintRt.pivot = new Vector2(0.5f, 0); hintRt.anchoredPosition = new Vector2(0, 80);
        var hintText = hintGO.AddComponent<Text>(); hintText.font = font; hintText.fontSize = 14; hintText.alignment = TextAnchor.MiddleCenter; hintText.color = Color.yellow; hintText.text = "";
        hintGO.SetActive(false);

        // Wire references into HumanPlayerUI
        h.panel = panel;
        h.allinButton = allinB;
        h.foldButton = foldB;
        h.callButton = callB;
        h.checkButton = checkB;

        h.raiseButton = raiseB;
        h.raisePanel = raisePanel;
        h.raiseSlider = raiseSlider;
        h.raiseValueLabel = valText;
        h.raiseConfirmButton = confBtn;
        h.raiseCancelButton = cancBtn;
        h.betPanel = betPanel;
        h.betSlider = bSlider;
        h.betValueLabel = bValText;
        h.betConfirmButton = betConf;
        h.betCancelButton = betCanc;
        h.seatLabel = seatText;
        h.hintLabel = hintText;

        // All-in confirmation panel (hidden by default)
        var confirmPanel = new GameObject("AllInConfirm"); confirmPanel.transform.SetParent(root.transform, false);
        var cRt = confirmPanel.AddComponent<RectTransform>(); cRt.sizeDelta = new Vector2(320, 96);
        cRt.anchorMin = new Vector2(0.5f, 0); cRt.anchorMax = new Vector2(0.5f, 0); cRt.pivot = new Vector2(0.5f, 0); cRt.anchoredPosition = new Vector2(0, 140);
        var cImg = confirmPanel.AddComponent<Image>(); cImg.color = new Color(0f, 0f, 0f, 0.9f);
        var cTxtGo = new GameObject("Text"); cTxtGo.transform.SetParent(confirmPanel.transform, false);
        var cTxtRt = cTxtGo.AddComponent<RectTransform>(); cTxtRt.anchorMin = new Vector2(0.5f, 0.5f); cTxtRt.anchorMax = new Vector2(0.5f, 0.5f); cTxtRt.sizeDelta = new Vector2(300, 32); cTxtRt.anchoredPosition = new Vector2(0, 16);
        var cTxt = cTxtGo.AddComponent<Text>(); cTxt.font = font; cTxt.fontSize = 16; cTxt.alignment = TextAnchor.MiddleCenter; cTxt.color = Color.white; cTxt.text = "筹码不足以跟注，是否全下？";
        var btnRow2 = new GameObject("AllInButtons"); btnRow2.transform.SetParent(confirmPanel.transform, false);
        var brt2 = btnRow2.AddComponent<RectTransform>(); brt2.sizeDelta = new Vector2(300, 36);
        var hrow2 = btnRow2.AddComponent<HorizontalLayoutGroup>(); hrow2.spacing = 8; hrow2.childAlignment = TextAnchor.MiddleCenter; hrow2.childForceExpandWidth = false;
        var allinConfBtn = CreateSmall("全下", Vector2.zero, btnRow2.transform);
        var allinCancelBtn = CreateSmall("取消", Vector2.zero, btnRow2.transform);
        allinConfBtn.GetComponentInChildren<Text>().text = "全下";
        allinCancelBtn.GetComponentInChildren<Text>().text = "取消";
        allinConfBtn.GetComponent<Image>().color = new Color(0.95f, 0.6f, 0.1f);
        allinCancelBtn.GetComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f);
        confirmPanel.SetActive(false);

        h.allinConfirmPanel = confirmPanel;
        h.allinConfirmButton = allinConfBtn;
        h.allinCancelButton = allinCancelBtn;

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
    public void ConfigureForNeed(int need, bool canOpenBet)
    {
        if (checkButton != null)
        {
            checkButton.interactable = (need == 0);
            var txt = checkButton.GetComponentInChildren<Text>();
            if (txt != null) txt.text = (need == 0) ? "过牌" : "过牌(需跟注)";
        }
        if (callButton != null)
        {
            callButton.interactable = (need > 0);
            var txt = callButton.GetComponentInChildren<Text>();
            if (txt != null) txt.text = "跟注";
        }
        if (raiseButton != null) raiseButton.interactable = (need > 0) || canOpenBet;
        if (allinButton != null) allinButton.interactable = true;
        if (foldButton != null) foldButton.interactable = true;

        // Show a short hint when Check is disabled to explain why
        if (hintLabel != null)
        {
            if (need > 0)
                ShowHint("过牌不可用：需先跟注或全下");
            else
                HideHintImmediate();
        }
    }

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
        if (raisePanel != null) raisePanel.SetActive(false);
        if (allinConfirmPanel != null) allinConfirmPanel.SetActive(false);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    void AllIn()
    {
        OnAction?.Invoke(EPlayerAction.AllIn);
        Hide();
    }

    void Fold()
    {
        OnAction?.Invoke(EPlayerAction.Fold);
        Hide();
    }

    void Call()
    {
        var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
        if (game != null && currentSeat >= 0 && currentSeat < game.players.Count)
        {
            var player = game.players[currentSeat];
            int need = Mathf.Max(0, game.currentBet - player.data.CurrentBet);
            if (need > 0 && player.data.Stack < need)
            {
                // show confirm panel if available, otherwise fallback to a hint
                if (allinConfirmPanel != null)
                {
                    if (panel != null) panel.SetActive(false);
                    allinConfirmPanel.SetActive(true);
                    return;
                }
                else
                {
                    ShowHint("筹码不足以跟注：点击 全下 以全下或取消");
                    return;
                }
            }
        }
        OnAction?.Invoke(EPlayerAction.Call);
        Hide();
    }

    void ConfirmAllIn()
    {
        OnAction?.Invoke(EPlayerAction.AllIn);
        if (allinConfirmPanel != null) allinConfirmPanel.SetActive(false);
        Hide();
    }

    void CancelAllIn()
    {
        if (allinConfirmPanel != null) allinConfirmPanel.SetActive(false);
        if (panel != null) panel.SetActive(true);
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
                raiseSlider.onValueChanged.AddListener((v) =>
                {
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

    void ShowHint(string text)
    {
        if (hintLabel == null) return;
        hintLabel.text = text;
        hintLabel.gameObject.SetActive(true);
        StopAllCoroutines();
        // fire-and-forget async hide (keeps main-thread safety via coroutine under the hood)
        _ = HideHintAfterAsync(hintDuration);
    }

    System.Collections.IEnumerator HideHintAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (hintLabel != null) hintLabel.gameObject.SetActive(false);
    }

    // Async wrapper around HideHintAfter coroutine for Task-based callers
    private Task RunCoroutineAsTask(System.Collections.IEnumerator routine)
    {
        var tcs = new TaskCompletionSource<bool>();
        StartCoroutine(RunCoroutineRoutine(routine, tcs));
        return tcs.Task;
    }

    private System.Collections.IEnumerator RunCoroutineRoutine(System.Collections.IEnumerator routine, TaskCompletionSource<bool> tcs)
    {
        yield return routine;
        try { tcs.TrySetResult(true); } catch { }
    }

    public Task HideHintAfterAsync(float t)
    {
        return RunCoroutineAsTask(HideHintAfter(t));
    }

    void HideHintImmediate()
    {
        if (hintLabel != null) hintLabel.gameObject.SetActive(false);
    }



    void ConfirmRaise()
    {
        int amount = 0;
        var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
        if (raiseSlider != null && game != null && currentSeat >= 0 && currentSeat < game.players.Count)
        {
            var player = game.players[currentSeat];
            int stack = player.data.Stack;
            amount = Mathf.RoundToInt(raiseSlider.value * stack);
            // compute required minimum based on need + minRaise
            int need = Mathf.Max(0, game.currentBet - player.data.CurrentBet);
            float minRaiseFrac = (game.aiConfig != null) ? game.aiConfig.minRaiseFraction : 1f;
            int minByConfig = Mathf.Max(1, Mathf.FloorToInt(game.data.BigBlindAmount * minRaiseFrac));
            int minByLastRaise = Mathf.Max(1, game.lastRaiseAmount);
            int minRaise = Mathf.Max(minByConfig, minByLastRaise);
            int minAllowed = Mathf.Max(1, need + minRaise);
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
            int bigBlind = Mathf.Max(1, game.data.BigBlindAmount);
            int minAllowed = Mathf.Max(bigBlind, 1);
            // Quantize amount to multiples of big blind (floor to avoid exceeding stack)
            int multiples = Mathf.Max(1, Mathf.FloorToInt((float)amount / bigBlind));
            amount = multiples * bigBlind;
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
                var game = UnityEngine.Object.FindObjectOfType<PokerGame>();
                int stack = 0;
                int bigBlind = 1;
                if (game != null && currentSeat >= 0 && currentSeat < game.players.Count)
                {
                    stack = game.players[currentSeat].data.Stack;
                    bigBlind = Mathf.Max(1, game.data.BigBlindAmount);
                }
                float minFrac = (stack > 0) ? Mathf.Clamp01((float)bigBlind / Mathf.Max(1, stack)) : 0f;
                betSlider.minValue = minFrac;
                betSlider.maxValue = 1f;
                betSlider.value = Mathf.Clamp(betSlider.value, minFrac, 1f);
                betSlider.onValueChanged.RemoveAllListeners();
                betSlider.onValueChanged.AddListener((v) =>
                {
                    int s = stack;
                    if (game != null && currentSeat >= 0 && currentSeat < game.players.Count) s = game.players[currentSeat].data.Stack;
                    int chips = Mathf.RoundToInt(v * s);
                    if (betValueLabel != null) betValueLabel.text = Mathf.RoundToInt(v * 100f) + "%" + " 投注(" + chips + ")";
                });
                // initialize label
                betSlider.onValueChanged.Invoke(betSlider.value);
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
