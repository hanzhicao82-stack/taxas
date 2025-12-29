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
    public InputField raiseInputField;
    public Button raiseConfirmButton;
    public Button raiseCancelButton;

    [Tooltip("Optional Bet panel and controls for initial bets when no current bet.")]
    public Button betButton;
    public GameObject betPanel;
    public InputField betInputField;
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

        // Raise panel
        var raisePanel = new GameObject("RaisePanel"); raisePanel.transform.SetParent(root.transform, false);
        var rrt = raisePanel.AddComponent<RectTransform>(); rrt.sizeDelta = new Vector2(260, 80);
        rrt.anchorMin = new Vector2(0.5f, 0); rrt.anchorMax = new Vector2(0.5f, 0); rrt.pivot = new Vector2(0.5f, 0); rrt.anchoredPosition = new Vector2(0, 140);
        var rimg = raisePanel.AddComponent<Image>(); rimg.color = new Color(0f, 0f, 0f, 0.75f);
        raisePanel.SetActive(false);

        // InputField
        var inputGO = new GameObject("RaiseInput"); inputGO.transform.SetParent(raisePanel.transform, false);
        var irt = inputGO.AddComponent<RectTransform>(); irt.sizeDelta = new Vector2(160, 30); irt.anchoredPosition = new Vector2(-30, 10);
        var inputBg = inputGO.AddComponent<Image>(); inputBg.color = Color.white * 0.9f;
        var input = inputGO.AddComponent<InputField>();
        var textGO = new GameObject("Text"); textGO.transform.SetParent(inputGO.transform, false);
        var txtRt = textGO.AddComponent<RectTransform>(); txtRt.sizeDelta = irt.sizeDelta; txtRt.anchoredPosition = Vector2.zero;
        var textComp = textGO.AddComponent<Text>(); textComp.font = font; textComp.fontSize = 14; textComp.color = Color.black; textComp.alignment = TextAnchor.MiddleLeft;
        input.textComponent = textComp;
        // placeholder
        var placeholderGO = new GameObject("Placeholder"); placeholderGO.transform.SetParent(inputGO.transform, false);
        var phRt = placeholderGO.AddComponent<RectTransform>(); phRt.sizeDelta = irt.sizeDelta; phRt.anchoredPosition = Vector2.zero;
        var phText = placeholderGO.AddComponent<Text>(); phText.font = font; phText.fontSize = 14; phText.color = new Color(0.4f, 0.4f, 0.4f); phText.alignment = TextAnchor.MiddleLeft; phText.text = "输入加注金额";
        input.placeholder = phText;
        input.contentType = InputField.ContentType.IntegerNumber;

        // Confirm / Cancel (create smaller buttons inside raisePanel)
        Button CreateSmall(string label, Vector2 pos, Transform parent)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rtBtn = go.AddComponent<RectTransform>(); rtBtn.sizeDelta = new Vector2(96, 36); rtBtn.anchoredPosition = pos;
            var imgBtn = go.AddComponent<Image>(); imgBtn.color = new Color(0.2f, 0.2f, 0.2f);
            var btn = go.AddComponent<Button>();
            var lbl = new GameObject("Label"); lbl.transform.SetParent(go.transform, false);
            var lblRt = lbl.AddComponent<RectTransform>(); lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one; lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
            var txt = lbl.AddComponent<Text>(); txt.font = font; txt.fontSize = 14; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.text = label;
            lbl.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);
            return btn;
        }

        var confBtn = CreateSmall("确定", new Vector2(20, 10), raisePanel.transform);
        var cancBtn = CreateSmall("取消", new Vector2(140, 10), raisePanel.transform);
        // adjust button labels and colors for clarity
        confBtn.GetComponentInChildren<Text>().text = "确定";
        cancBtn.GetComponentInChildren<Text>().text = "取消";
        confBtn.GetComponent<Image>().color = new Color(0.1f, 0.6f, 0.1f);
        cancBtn.GetComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f);

        // Bet panel (similar to raise)
        var betPanel = new GameObject("BetPanel"); betPanel.transform.SetParent(root.transform, false);
        var bprt = betPanel.AddComponent<RectTransform>(); bprt.sizeDelta = new Vector2(260, 80);
        bprt.anchorMin = new Vector2(0.5f, 0); bprt.anchorMax = new Vector2(0.5f, 0); bprt.pivot = new Vector2(0.5f, 0); bprt.anchoredPosition = new Vector2(0, 230);
        var bimg = betPanel.AddComponent<Image>(); bimg.color = new Color(0f, 0f, 0f, 0.75f);
        betPanel.SetActive(false);
        var betInputGO = new GameObject("BetInput"); betInputGO.transform.SetParent(betPanel.transform, false);
        var birt = betInputGO.AddComponent<RectTransform>(); birt.sizeDelta = new Vector2(160, 30); birt.anchoredPosition = new Vector2(-30, 10);
        var betInputBg = betInputGO.AddComponent<Image>(); betInputBg.color = Color.white * 0.9f;
        var betInput = betInputGO.AddComponent<InputField>();
        var betTextGO = new GameObject("Text"); betTextGO.transform.SetParent(betInputGO.transform, false);
        var betTxtRt = betTextGO.AddComponent<RectTransform>(); betTxtRt.sizeDelta = birt.sizeDelta; betTxtRt.anchoredPosition = Vector2.zero;
        var betTextComp = betTextGO.AddComponent<Text>(); betTextComp.font = font; betTextComp.fontSize = 14; betTextComp.color = Color.black; betTextComp.alignment = TextAnchor.MiddleLeft;
        betInput.textComponent = betTextComp;
        var betPlaceGO = new GameObject("Placeholder"); betPlaceGO.transform.SetParent(betInputGO.transform, false);
        var bpRt = betPlaceGO.AddComponent<RectTransform>(); bpRt.sizeDelta = birt.sizeDelta; bpRt.anchoredPosition = Vector2.zero;
        var bpText = betPlaceGO.AddComponent<Text>(); bpText.font = font; bpText.fontSize = 14; bpText.color = new Color(0.4f, 0.4f, 0.4f); bpText.alignment = TextAnchor.MiddleLeft; bpText.text = "输入下注金额";
        betInput.placeholder = bpText;
        betInput.contentType = InputField.ContentType.IntegerNumber;
        var betConf = CreateSmall("确定", new Vector2(20, 10), betPanel.transform);
        var betCanc = CreateSmall("取消", new Vector2(140, 10), betPanel.transform);
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
        h.raiseInputField = input;
        h.raiseConfirmButton = confBtn;
        h.raiseCancelButton = cancBtn;
        h.betPanel = betPanel;
        h.betInputField = betInput;
        h.betConfirmButton = betConf;
        h.betCancelButton = betCanc;
        h.seatLabel = seatText;

        return h;
    }



    // Call this from your game-turn logic when it's a player's turn.
    // Call this from your game-turn logic when it's a player's turn.
    // Show the panel and update the seat label so player knows who is acting.
    public void ShowForSeat(int seat)
    {
        currentSeat = seat;
        if (seatLabel != null)
        {
            seatLabel.text = $"座位 {seat} 行动";
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
            if (raiseInputField != null) raiseInputField.text = "";
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
            if (betInputField != null) betInputField.text = "";
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
        if (raiseInputField != null)
        {
            int.TryParse(raiseInputField.text, out amount);
        }
        // send action with entered amount (0 means treat as minimum raise in game logic)
        OnAction?.Invoke(EPlayerAction.Raise, amount);
        if (raisePanel != null) raisePanel.SetActive(false);
        if (panel != null) panel.SetActive(false);
    }

    void ConfirmBet()
    {
        int amount = 0;
        if (betInputField != null)
        {
            int.TryParse(betInputField.text, out amount);
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
            if (betInputField != null)
                betInputField.text = "";
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
