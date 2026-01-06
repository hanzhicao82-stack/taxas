using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    // Runtime-resolved references (do not configure in inspector)
    // Cached reference to avoid repeated FindObjectOfType calls
    private PokerGame game => UnityEngine.Object.FindObjectOfType<PokerGame>();
    private Text[] playerTexts;
    private int[] playerBaseFontSizes;
    private Color[] playerBaseColors;
    private List<GameObject> playerTextGOs;
    private Text communityText;
    private Text potText;
    private Button dealButton;
    private Text resultText;
    private Slider aiDelaySlider;
    private Text aiDelayLabel;
    private Slider raiseProbSlider; private Text raiseProbLabel;
    private Slider betProbSlider; private Text betProbLabel;
    private Slider raiseBaseSlider; private Text raiseBaseLabel;
    private Slider raiseScaleSlider; private Text raiseScaleLabel;
    private Slider minRaiseSlider; private Text minRaiseLabel;
    private Slider simIterSlider; private Text simIterLabel;
    private Slider numPlayersSlider; private Text numPlayersLabel;
    private Slider smallBlindSlider; private Text smallBlindLabel;
    private Slider bigBlindSlider; private Text bigBlindLabel;
    private Button startButton;
    private GameObject paramsContainerGO;
    private GameObject panelGO;
    private Font uiFont;
    private float uiScale = 1.5f;
    private RectTransform panelRect;
    private Coroutine currentRunCoroutine;
    private bool currentRunFinished;
    private Button restartButton;
    private GameObject restartButtonGO;
    private Coroutine winnerCoroutine;

    void Start()
    {

        // Create or find a Canvas
        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("UI Canvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        var root = canvas.transform;


        // Ensure an EventSystem exists so UI (Slider, Buttons) can receive input
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
        // Ensure canvas rect stretches full screen
        var canvasRt = canvas.GetComponent<RectTransform>();
        canvasRt.anchorMin = Vector2.zero; canvasRt.anchorMax = Vector2.one; canvasRt.sizeDelta = Vector2.zero; canvasRt.offsetMin = Vector2.zero; canvasRt.offsetMax = Vector2.zero;

        // Determine player count
        int playerCount = (game != null) ? game.numPlayers : 4;

        // Create a simple vertical layout container for texts
        panelGO = new GameObject("UI Panel");
        panelGO.transform.SetParent(root, false);
        var panelRt = panelGO.AddComponent<RectTransform>();
        panelRect = panelRt;
        // Center the panel and scale UI
        panelRt.anchorMin = new Vector2(0.5f, 0.5f); panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(800, 600) * uiScale;
        panelRt.anchoredPosition = Vector2.zero;

        // Prepare font
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var font = uiFont;

        // Create player text fields based on existing game.players if available
        playerTextGOs = new List<GameObject>();


        // Community text
        var cgo = new GameObject("CommunityText");
        cgo.transform.SetParent(panelGO.transform, false);
        var crt = cgo.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(600, 24) * uiScale;
        crt.anchoredPosition = Vector2.zero;
        communityText = cgo.AddComponent<Text>(); communityText.font = font; communityText.fontSize = Mathf.RoundToInt(18 * uiScale); communityText.color = Color.yellow; communityText.alignment = TextAnchor.MiddleCenter;
        communityText.text = "";

        // Result text
        var rgo = new GameObject("ResultText");
        rgo.transform.SetParent(panelGO.transform, false);
        var rrt = rgo.AddComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0.5f, 0.5f); rrt.anchorMax = new Vector2(0.5f, 0.5f); rrt.pivot = new Vector2(0.5f, 0.5f);
        rrt.sizeDelta = new Vector2(400, 24) * uiScale;
        rrt.anchoredPosition = new Vector2(0, -30 * uiScale);
        resultText = rgo.AddComponent<Text>(); resultText.font = font; resultText.fontSize = Mathf.RoundToInt(18 * uiScale); resultText.color = Color.cyan; resultText.alignment = TextAnchor.MiddleCenter;

        // Pot text (display current pot)
        var pgo = new GameObject("PotText");
        pgo.transform.SetParent(panelGO.transform, false);
        var prt = pgo.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f); prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(400, 24) * uiScale;
        prt.anchoredPosition = new Vector2(0, -60 * uiScale);
        potText = pgo.AddComponent<Text>(); potText.font = font; potText.fontSize = Mathf.RoundToInt(16 * uiScale); potText.color = Color.magenta; potText.alignment = TextAnchor.MiddleCenter;

        // AI delay slider + label
        var sgo = new GameObject("AIDelayLabel");
        sgo.transform.SetParent(panelGO.transform, false);
        var slrt = sgo.AddComponent<RectTransform>();
        slrt.anchorMin = new Vector2(0, 1); slrt.anchorMax = new Vector2(0, 1); slrt.pivot = new Vector2(0, 1);
        slrt.sizeDelta = new Vector2(200, 20) * uiScale;
        slrt.anchoredPosition = new Vector2(0, -24 * playerCount * uiScale - 92 * uiScale);
        aiDelayLabel = sgo.AddComponent<Text>(); aiDelayLabel.font = font; aiDelayLabel.fontSize = Mathf.RoundToInt(14 * uiScale); aiDelayLabel.color = Color.white;

        var sliderGO = new GameObject("AIDelaySlider");
        sliderGO.transform.SetParent(panelGO.transform, false);
        var sliderRt = sliderGO.AddComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0, 1); sliderRt.anchorMax = new Vector2(0, 1); sliderRt.pivot = new Vector2(0, 1);
        sliderRt.sizeDelta = new Vector2(180, 20) * uiScale;
        sliderRt.anchoredPosition = new Vector2(210 * uiScale, -24 * playerCount * uiScale - 92 * uiScale);
        aiDelaySlider = sliderGO.AddComponent<Slider>();
        aiDelaySlider.direction = Slider.Direction.LeftToRight;
        aiDelaySlider.minValue = 0.1f;
        aiDelaySlider.maxValue = 3f;
        aiDelaySlider.wholeNumbers = false;

        // Background image for the slider
        var bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        var bgImg = bg.AddComponent<UnityEngine.UI.Image>(); bgImg.color = new Color(0.15f, 0.15f, 0.15f);
        var bgRt = bg.GetComponent<RectTransform>(); bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        // Fill area and fill image
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0.25f); faRt.anchorMax = new Vector2(1, 0.75f); faRt.offsetMin = new Vector2(6, 0); faRt.offsetMax = new Vector2(-6, 0);
        var fill = new GameObject("Fill"); fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<UnityEngine.UI.Image>(); fillImg.color = new Color(0.2f, 0.6f, 0.2f);
        var fillRt = fill.GetComponent<RectTransform>(); fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;

        // Handle area and handle image
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        var haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one; haRt.offsetMin = Vector2.zero; haRt.offsetMax = Vector2.zero;
        var handle = new GameObject("Handle"); handle.transform.SetParent(handleArea.transform, false);
        var handleImg = handle.AddComponent<UnityEngine.UI.Image>(); handleImg.color = Color.white;
        var handleRt = handle.GetComponent<RectTransform>(); handleRt.sizeDelta = new Vector2(12, 20) * uiScale; handleRt.anchorMin = new Vector2(0.5f, 0.5f); handleRt.anchorMax = new Vector2(0.5f, 0.5f);

        // Hook up slider rects/graphics
        aiDelaySlider.fillRect = fillImg.rectTransform;
        aiDelaySlider.handleRect = handleImg.rectTransform;
        aiDelaySlider.targetGraphic = handleImg;
        // initial value
        float initDelay = 0.1f;
        if (game != null && game.aiConfig != null)
        {
            initDelay = game.aiConfig.actionDelay;
        }
        aiDelaySlider.value = initDelay;
        aiDelayLabel.text = $"AI 延迟：{aiDelaySlider.value:0.00}秒";
        aiDelaySlider.onValueChanged.AddListener((v) =>
        {
            aiDelayLabel.text = $"AI 延迟：{v:0.00}秒";
            if (game != null)
            {
                if (game.aiConfig == null)
                {
                    // lazily create a default config if none assigned
                    game.aiConfig = ScriptableObject.CreateInstance<AIConfig>();
                }
                game.aiConfig.actionDelay = v;
            }
        });

        // Create configurable parameter sliders in a vertical container
        float paramBaseY = -24 * playerCount * uiScale - 122 * uiScale;
        paramsContainerGO = new GameObject("ParamsContainer");
        paramsContainerGO.transform.SetParent(panelGO.transform, false);
        var pRt = paramsContainerGO.AddComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0, 1); pRt.anchorMax = new Vector2(0, 1); pRt.pivot = new Vector2(0, 1);
        pRt.anchoredPosition = new Vector2(0, paramBaseY);
        pRt.sizeDelta = new Vector2(panelRt.sizeDelta.x * 0.9f, 300 * uiScale);
        var vlg = paramsContainerGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = Mathf.RoundToInt(6 * uiScale);
        vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true; vlg.childAlignment = TextAnchor.UpperLeft;
        var csf = paramsContainerGO.AddComponent<ContentSizeFitter>(); csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        raiseProbSlider = CreateLabeledSlider(paramsContainerGO.transform, new Vector2(300, 18) * uiScale, 0f, 1f, (game != null && game.aiConfig != null) ? game.aiConfig.raiseProbability : 0.12f, "加注概率", font);
        betProbSlider = CreateLabeledSlider(paramsContainerGO.transform, new Vector2(300, 18) * uiScale, 0f, 1f, (game != null && game.aiConfig != null) ? game.aiConfig.betProbability : 0.06f, "下注概率", font);
        raiseBaseSlider = CreateLabeledSlider(paramsContainerGO.transform, new Vector2(300, 18) * uiScale, 0f, 5f, (game != null && game.aiConfig != null) ? game.aiConfig.raiseSizeBase : 0.5f, "基础加注", font);
        raiseScaleSlider = CreateLabeledSlider(paramsContainerGO.transform, new Vector2(300, 18) * uiScale, 0f, 3f, (game != null && game.aiConfig != null) ? game.aiConfig.raiseSizeAggressionScale : 1f, "激进系数", font);
        minRaiseSlider = CreateLabeledSlider(paramsContainerGO.transform, new Vector2(300, 18) * uiScale, 0f, 1f, (game != null && game.aiConfig != null) ? game.aiConfig.minRaiseFraction : 0.5f, "最小加注比例", font);
        simIterSlider = CreateLabeledSlider(paramsContainerGO.transform, new Vector2(300, 18) * uiScale, 10f, 1000f, (game != null && game.aiConfig != null) ? game.aiConfig.simIterations : 20, "模拟次数", font);
        simIterSlider.wholeNumbers = true;

        numPlayersSlider = CreateLabeledSlider(paramsContainerGO.transform, new Vector2(300, 18) * uiScale, 2f, 8f, game != null ? game.numPlayers : 4f, "玩家数", font);
        numPlayersSlider.wholeNumbers = true;
        smallBlindSlider = CreateLabeledSlider(paramsContainerGO.transform, new Vector2(300, 18) * uiScale, 1f, 100f, (game != null) ? game.data.SmallBlindAmount : 5f, "小盲注", font);
        smallBlindSlider.wholeNumbers = true;
        bigBlindSlider = CreateLabeledSlider(paramsContainerGO.transform, new Vector2(300, 18) * uiScale, 1f, 500f, (game != null) ? game.data.BigBlindAmount : 10f, "大盲注", font);
        bigBlindSlider.wholeNumbers = true;

        // Start button (use helper)
        startButton = CreateButton("StartButton", panelGO.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12 * uiScale), new Vector2(160, 28) * uiScale, new Color(0.1f, 0.5f, 0.9f), "开始游戏", font, Mathf.RoundToInt(16 * uiScale));
        startButton.onClick.AddListener(OnStartClicked);

        // Restart button (initially hidden). Shown during a run to allow cancelling and returning to settings
        restartButton = CreateButton("RestartButton", panelGO.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -200f), new Vector2(160, 28) * uiScale, new Color(0.9f, 0.2f, 0.2f), "Restart", font, Mathf.RoundToInt(16 * uiScale));
        restartButtonGO = restartButton.gameObject;
        restartButton.onClick.AddListener(OnRestartClicked);
        restartButtonGO.SetActive(false);





        // Subscribe to game events for incremental updates using stored wrappers so we can unsubscribe reliably
        onFlopWrapper = (obj) => { try { var t = (Tuple<List<Card>, List<Card>>)obj; OnCommunityUpdated(t); } catch { } };
        onTurnWrapper = (obj) => { try { var t = (Tuple<List<Card>, List<Card>>)obj; OnCommunityUpdated(t); } catch { } };
        onRiverWrapper = (obj) => { try { var t = (Tuple<List<Card>, List<Card>>)obj; OnCommunityUpdated(t); } catch { } };
        onHandStartedWrapper = (obj) =>
        {
            try
            {
                var t = (List<Player>)obj;
                OnHandStarted(t);
            }
            catch
            {

            }
        };
        GameEventBus.Subscribe(Events.Flop, onFlopWrapper);
        GameEventBus.Subscribe(Events.Turn, onTurnWrapper);
        GameEventBus.Subscribe(Events.River, onRiverWrapper);
        GameEventBus.Subscribe(Events.HandStarted, onHandStartedWrapper);
        // Ensure we display player chip info if the game creates players slightly later


    }

    // Helper to run an IEnumerator as a Task (keeps execution on Unity main thread via coroutine)
    private Task RunCoroutineAsTask(System.Collections.IEnumerator routine)
    {
        var tcs = new TaskCompletionSource<bool>();
        StartCoroutine(RunCoroutineRoutine(routine, tcs));
        return tcs.Task;
    }

    private System.Collections.IEnumerator RunCoroutineRoutine(System.Collections.IEnumerator routine, TaskCompletionSource<bool> tcs)
    {
        yield return StartCoroutine(routine);
        tcs.TrySetResult(true);
    }



    private IEnumerator FadeInCanvasGroup(CanvasGroup cg, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / dur);
            yield return null;
        }
        cg.alpha = 1f;
    }

    private IEnumerator MoveRectTo(RectTransform rt, Vector2 target, float dur)
    {
        if (rt == null)
        {
            yield break;
        }
        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            rt.anchoredPosition = Vector2.Lerp(start, target, k);
            yield return null;
        }
        rt.anchoredPosition = target;
    }

    private IEnumerator RotateRectTo(RectTransform rt, float targetZ, float dur)
    {
        if (rt == null)
        {
            yield break;
        }
        float startZ = rt.localEulerAngles.z;
        // normalize shortest path
        float delta = Mathf.DeltaAngle(startZ, targetZ);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            float z = startZ + delta * k;
            var e = rt.localEulerAngles; e.z = z; rt.localEulerAngles = e;
            yield return null;
        }
        var end = rt.localEulerAngles; end.z = targetZ; rt.localEulerAngles = end;
    }

    private IEnumerator ArrangePlayersSmooth(List<GameObject> gos, float uiScale, float dur)
    {
        if (panelRect == null)
        {
            yield break;
        }
        int n = gos.Count;
        float radius = Mathf.Min(panelRect.sizeDelta.x, panelRect.sizeDelta.y) * 0.45f;
        if (n <= 0)
        {
            yield break;
        }

        float angleStep = 360f / n;
        List<Coroutine> running = new List<Coroutine>();
        for (int i = 0; i < n; i++)
        {
            float angleDeg = 90f - i * angleStep;
            float rad = angleDeg * Mathf.Deg2Rad;
            var rt = gos[i].GetComponent<RectTransform>();
            if (rt == null)
            {
                continue;
            }
            Vector2 target = new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);

            // start movement coroutine
            CoroutineTracker.Start(this, MoveRectTo(rt, target, dur));

            // rotate whole object so card faces center
            float bgRot = angleDeg - 90f;
            CoroutineTracker.Start(this, RotateRectTo(rt, bgRot, dur));

            // ensure label upright: set child Label rotation to 0 over same duration
            var labelTf = rt.transform.Find("Label");
            if (labelTf != null)
            {
                CoroutineTracker.Start(this, RotateRectTo(labelTf.GetComponent<RectTransform>(), 0f, dur));
            }

            // fade in if needed
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                CoroutineTracker.Start(this, FadeInCanvasGroup(cg, dur * 0.8f));
            }
        }
        yield return new WaitForSeconds(dur);
    }

    // Async wrappers for coroutine-based utilities (preserve main-thread safety)
    public Task FadeInCanvasGroupAsync(CanvasGroup cg, float dur) => RunCoroutineAsTask(FadeInCanvasGroup(cg, dur));
    public Task MoveRectToAsync(RectTransform rt, Vector2 target, float dur) => RunCoroutineAsTask(MoveRectTo(rt, target, dur));
    public Task RotateRectToAsync(RectTransform rt, float targetZ, float dur) => RunCoroutineAsTask(RotateRectTo(rt, targetZ, dur));
    public Task ArrangePlayersSmoothAsync(List<GameObject> gos, float uiScale, float dur) => RunCoroutineAsTask(ArrangePlayersSmooth(gos, uiScale, dur));

    public Task ShowResultAsync(int pot) => RunCoroutineAsTask(ShowResult(pot));
    public Task ShowWinnersAsync() => RunCoroutineAsTask(ShowWinners());

    public async Task RunGameWithHiddenSettingsAsync(System.Collections.IEnumerator inner)
    {
        SetActiveSafe(paramsContainerGO, false);
        SetActiveSafe(aiDelayLabel?.gameObject, false);
        SetActiveSafe(aiDelaySlider?.gameObject, false);
        SetActiveSafe(startButton?.gameObject, false);
        SetActiveSafe(dealButton?.gameObject, false);

        // show restart button so user can cancel and return to settings
        SetActiveSafe(restartButtonGO, true);

        // run inner using wrapper so we can cancel from Restart
        currentRunFinished = false;
        currentRunCoroutine = null;
        if (inner != null)
        {
            currentRunCoroutine = CoroutineTracker.Start(this, RunInnerAndMark(inner));
            while (!currentRunFinished)
            {
                await Task.Yield();
            }
        }

        // cleanup
        if (currentRunCoroutine != null)
        {
            CoroutineTracker.Stop(this, currentRunCoroutine);
            currentRunCoroutine = null;
        }

        SetActiveSafe(restartButtonGO, false);
        SetActiveSafe(paramsContainerGO, true);
        SetActiveSafe(aiDelayLabel?.gameObject, true);
        SetActiveSafe(aiDelaySlider?.gameObject, true);
        SetActiveSafe(startButton?.gameObject, true);
        SetActiveSafe(dealButton?.gameObject, true);
    }

    public Task RunInnerAndMarkAsync(System.Collections.IEnumerator inner) => RunCoroutineAsTask(RunInnerAndMark(inner));


    void BindPlayer(int idx, Player player)
    {

        if (playerTextGOs == null) playerTextGOs = new List<GameObject>();

        // create a single missing entry if needed (one-at-a-time)
        if (playerTextGOs.Count <= idx)
        {
            int newIdx = playerTextGOs.Count;
            var tgo = new GameObject($"PlayerText_{newIdx + 1}");
            tgo.transform.SetParent(panelGO.transform, false);
            var rt = tgo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(170, 34) * uiScale;

            // Background card
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(tgo.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 0.5f); bgRt.anchorMax = new Vector2(0.5f, 0.5f); bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(170, 34) * uiScale;
            var img = bgGo.AddComponent<UnityEngine.UI.Image>(); img.color = new Color(0f, 0f, 0f, 0.5f);

            // Text
            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(tgo.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0.5f, 0.5f); txtRt.anchorMax = new Vector2(0.5f, 0.5f); txtRt.pivot = new Vector2(0.5f, 0.5f);
            txtRt.sizeDelta = new Vector2(140, 28) * uiScale;
            var txt = txtGo.AddComponent<Text>();
            txt.font = uiFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); txt.fontSize = Mathf.RoundToInt(14 * uiScale); txt.color = Color.white; txt.alignment = TextAnchor.MiddleCenter;
            txt.text = $"玩家{newIdx + 1}：";

            // start at center
            var r = tgo.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f); r.pivot = new Vector2(0.5f, 0.5f); r.anchoredPosition = Vector2.zero;

            // initial invisible
            var cg = tgo.AddComponent<CanvasGroup>(); cg.alpha = 0f;

            playerTextGOs.Add(tgo);
        }


        // update the label for this player
        UpdatePlayerLabel(player);

        // refresh playerTexts array and base sizes/colors
        var labelsList = new List<Text>();
        for (int j = 0; j < playerTextGOs.Count; j++)
        {
            var label = playerTextGOs[j].transform.Find("Label")?.GetComponent<Text>();
            if (label == null)
            {
                label = playerTextGOs[j].GetComponentInChildren<Text>();
            }
            labelsList.Add(label);
        }
        playerTexts = labelsList.ToArray();

        playerBaseFontSizes = new int[playerTexts.Length];
        playerBaseColors = new Color[playerTexts.Length];
        for (int i = 0; i < playerTexts.Length; i++)
        {
            playerBaseFontSizes[i] = (playerTexts[i] != null) ? playerTexts[i].fontSize : Mathf.RoundToInt(14 * uiScale);
            playerBaseColors[i] = (playerTexts[i] != null) ? playerTexts[i].color : Color.white;
        }

        // animate arrangement to account for new player element
        CoroutineTracker.Start(this, ArrangePlayersSmooth(playerTextGOs, uiScale, 0.25f));
    }


    // RebuildPlayerTextFields removed — use BindPlayer for per-player setup.

    void OnDestroy()
    {
        UnsubscribeAllPlayerData();
        // Clean up subscriptions
        if (onFlopWrapper != null)
        {
            GameEventBus.Unsubscribe(Events.Flop, onFlopWrapper);
        }
        if (onTurnWrapper != null)
        {
            GameEventBus.Unsubscribe(Events.Turn, onTurnWrapper);
        }
        if (onRiverWrapper != null)
        {
            GameEventBus.Unsubscribe(Events.River, onRiverWrapper);
        }
        if (onHandStartedWrapper != null)
        {
            GameEventBus.Unsubscribe(Events.HandStarted, onHandStartedWrapper);
        }

        if (dealButton != null)
        {
            dealButton.onClick.RemoveListener(OnDealClicked);
        }
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }
    }

    // Wrappers are used for Unsubscribe because Subscribe<T> wraps handlers internally
    private void OnCommunityUpdated(Tuple<List<Card>, List<Card>> tpl)
    {
        if (tpl == null)
        {
            return;
        }
        // Incrementally append added cards to the communityText, then do a full refresh

        StringBuilder sb = new StringBuilder();
        foreach (var c in tpl.Item1)
        {
            sb.Append(c).Append(' ');
        }
        communityText.text = sb.ToString();

        // UI updates driven by data subscriptions; only community text needed here
    }

    private void OnHandStarted(List<Player> players)
    {
        // Rebuild player text fields if player count changed, then refresh
        int cnt = (players != null) ? players.Count : ((game != null && game.players != null) ? game.players.Count : 0);
        // Ensure UI elements exist for each player and bind them
        for (int i = 0; i < cnt; i++)
        {
            BindPlayer(i, players[i]);
        }
        // subscribe to per-player data changes and update UI
    }

    // These object-typed wrappers are stored as delegates to allow reliable Unsubscribe
    private Action<object> onFlopWrapper;
    private Action<object> onTurnWrapper;
    private Action<object> onRiverWrapper;
    private Action<object> onHandStartedWrapper;

    // Per-player data subscriptions so we can unsubscribe later
    private class DataSubs { public Action<int, int> stack; public Action<List<Card>, List<Card>> hole; public Action<int, int> bet; public Action<bool, bool> folded; public Action<bool, bool> allin; }
    private List<DataSubs> dataSubs = new List<DataSubs>();




    private void UnsubscribeAllPlayerData()
    {
        if (game == null || game.players == null) return;
        for (int i = 0; i < dataSubs.Count && i < game.players.Count; i++)
        {
            var p = game.players[i];
            var subs = dataSubs[i];
            if (subs.stack != null)
                p.data.StackData.OnValueChanged -= subs.stack;
            if (subs.folded != null)
                p.data.FoldedData.OnValueChanged -= subs.folded;
        }
        dataSubs.Clear();
    }

    private void SubscribeAllPlayerData()
    {
        UnsubscribeAllPlayerData();
        if (game == null || game.players == null) return;
        dataSubs = new List<DataSubs>();
        for (int i = 0; i < game.players.Count; i++)
        {
            var p = game.players[i];
            var subs = new DataSubs();
            subs.stack = (oldv, newv) =>
            {
                UpdatePlayerLabel(p);
            };
            p.data.StackData.OnValueChanged += subs.stack;
            subs.folded = (oldv, newv) =>
            {
                UpdatePlayerLabel(p);
            };
            p.data.FoldedData.OnValueChanged += subs.folded;
            dataSubs.Add(subs);
        }
        // initial update
        for (int i = 0; i < game.players.Count && i < playerTextGOs.Count; i++)
            UpdatePlayerLabel(game.players[i]);
    }

    private void UpdatePlayerLabel(Player player)
    {
        int i = player.id;
        var label = playerTextGOs[i].transform.Find("Label")?.GetComponent<Text>() ?? playerTextGOs[i].GetComponentInChildren<Text>();
        if (label == null) return;
        string holeStr = "";
        var holeList = player.data.Hole;
        if (holeList != null && holeList.Count >= 2)
        {
            holeStr = $"{holeList[0]} {holeList[1]} ";
        }
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.text = $"{player.name}：{holeStr}筹码：{player.data.Stack}";
    }

    private void UpdatePotText(int pot)
    {
        if (potText != null)
            potText.text = "底池：" + pot.ToString();
    }

    public IEnumerator ShowResult(int pot)
    {
        yield return ShowWinners();
        yield return new WaitForSeconds(2f);
    }

    // Display a ranked leaderboard for the current players sorted by profit (收益).
    // This coroutine builds a simple text leaderboard from highest to lowest profit
    // (profit = current stack - initialStack), displays it in `resultText`, waits,
    // then clears the text.
    IEnumerator ShowWinners()
    {
        if (resultText == null)
        {
            yield break;
        }

        var players = game?.players;
        if (players == null || players.Count == 0)
        {
            resultText.text = "排行榜：无玩家";
            yield return new WaitForSeconds(2f);
            resultText.text = "";
            yield break;
        }

        // Create a list of (name, profit) and sort by profit descending
        var entries = new List<(string name, int profit)>();
        foreach (var p in players)
        {
            if (p == null) continue;
            int initial = 0;
            int stack = 0;
            if (p.data != null)
            {
                initial = p.data.initialStack;
                stack = p.data.Stack;
            }
            entries.Add((p.name ?? "Player", stack - initial));
        }

        entries.Sort((a, b) => b.profit.CompareTo(a.profit));

        var sb = new StringBuilder();
        sb.AppendLine("排行榜：");
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            string profitStr = (e.profit >= 0) ? $"+{e.profit}" : e.profit.ToString();
            sb.AppendLine($"{i + 1}. {e.name} ({profitStr})");
        }

        // Show the leaderboard for a few seconds
        resultText.text = sb.ToString().TrimEnd('\n', '\r');
        resultText.horizontalOverflow = HorizontalWrapMode.Overflow;
        resultText.verticalOverflow = VerticalWrapMode.Overflow;
        yield return new WaitForSeconds(4f);
        resultText.text = "";
    }



    // Highlight the current acting player's label by enlarging font size by 50%.
    public void HighlightPlayer(int seat)
    {
        if (playerTexts == null || playerTexts.Length == 0) return;
        int idx = seat - 1;
        for (int i = 0; i < playerTexts.Length; i++)
        {
            var t = playerTexts[i];
            if (t == null) continue;
            int baseSize = (playerBaseFontSizes != null && i < playerBaseFontSizes.Length) ? playerBaseFontSizes[i] : t.fontSize;
            if (i == idx)
            {
                t.fontSize = Mathf.RoundToInt(baseSize * 1.5f);
                t.color = Color.green;
            }
            else
            {
                t.fontSize = baseSize;
                // restore base color if available
                if (playerBaseColors != null && i < playerBaseColors.Length)
                    t.color = Color.white;
            }
        }
    }

    public void OnDealClicked()
    {
        // Start a new hand when the user clicks the deal button.
        // if (game != null) game.StartHand();
    }

    private void OnStartClicked()
    {
        // Build or reuse an AIConfig to pass to the test/run
        AIConfig cfg = null;
        if (game != null && game.aiConfig != null)
        {
            cfg = game.aiConfig;
        }
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<AIConfig>();
        }

        // Pull values from sliders into cfg
        if (betProbSlider != null)
        {
            cfg.betProbability = betProbSlider.value;
        }
        if (raiseBaseSlider != null)
        {
            cfg.raiseSizeBase = raiseBaseSlider.value;
        }
        if (raiseScaleSlider != null)
        {
            cfg.raiseSizeAggressionScale = raiseScaleSlider.value;
        }
        if (minRaiseSlider != null)
        {
            cfg.minRaiseFraction = minRaiseSlider.value;
        }
        if (simIterSlider != null)
        {
            cfg.simIterations = Mathf.Max(1, Mathf.RoundToInt(simIterSlider.value));
        }
        if (aiDelaySlider != null)
        {
            cfg.actionDelay = aiDelaySlider.value;
        }

        // Apply number of players and blinds to the active game if present
        int players = Mathf.RoundToInt(numPlayersSlider.value);
        int small = Mathf.RoundToInt(smallBlindSlider.value);
        int big = Mathf.RoundToInt(bigBlindSlider.value);

        // Create and run the test runner while hiding settings
        var tester = new GameObject("Test");


        var flowTest = tester.AddComponent<PokerGameFlowTest>();
        CoroutineTracker.Start(this, RunGameWithHiddenSettings(flowTest.Run(cfg, 10, players)));

        if (game != null)
        {
            game.numPlayers = players;
            game.data.SmallBlindAmount = small;
            game.data.BigBlindAmount = big;
            game.aiConfig = cfg;
            game.data.PotData.OnValueChanged += (oldv, newv) => UpdatePotText(newv);
        }


        int initialCount = (game != null && game.players != null) ? game.players.Count : 0;
        for (int i = 0; i < initialCount; i++)
        {
            BindPlayer(i, game.players[i]);
        }
        SubscribeAllPlayerData();
    }

    // Hide settings while the provided inner routine runs, then restore UI
    private IEnumerator RunGameWithHiddenSettings(IEnumerator inner)
    {
        SetActiveSafe(paramsContainerGO, false);
        SetActiveSafe(aiDelayLabel?.gameObject, false);
        SetActiveSafe(aiDelaySlider?.gameObject, false);
        SetActiveSafe(startButton?.gameObject, false);
        SetActiveSafe(dealButton?.gameObject, false);

        // show restart button so user can cancel and return to settings
        SetActiveSafe(restartButtonGO, true);

        // run inner using wrapper so we can cancel from Restart
        currentRunFinished = false;
        currentRunCoroutine = null;
        if (inner != null)
        {
            currentRunCoroutine = CoroutineTracker.Start(this, RunInnerAndMark(inner));
            while (!currentRunFinished)
            {
                yield return null;
            }
        }

        // cleanup
        if (currentRunCoroutine != null)
        {
            CoroutineTracker.Stop(this, currentRunCoroutine);
            currentRunCoroutine = null;
        }

        SetActiveSafe(restartButtonGO, false);
        SetActiveSafe(paramsContainerGO, true);
        SetActiveSafe(aiDelayLabel?.gameObject, true);
        SetActiveSafe(aiDelaySlider?.gameObject, true);
        SetActiveSafe(startButton?.gameObject, true);
        SetActiveSafe(dealButton?.gameObject, true);
    }

    private IEnumerator RunInnerAndMark(IEnumerator inner)
    {
        if (inner != null)
        {
            yield return inner;
        }
        currentRunFinished = true;
    }

    private void OnRestartClicked()
    {
        // Cancel running test and return to settings
        if (currentRunCoroutine != null)
        {
            CoroutineTracker.Stop(this, currentRunCoroutine);
            currentRunCoroutine = null;
        }
        currentRunFinished = true;
        // also destroy the transient Test GameObject if present
        var t = GameObject.Find("Test");
        if (t != null)
        {
            Destroy(t);
        }
        // hide restart button and restore interactable start (RunGameWithHiddenSettings will restore full UI)
        if (restartButtonGO != null)
        {
            restartButtonGO.SetActive(false);
        }
    }

    // Small helpers to reduce repetition
    private void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null)
        {
            go.SetActive(active);
        }
    }

    private Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Color color, string labelText, Font font, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot; rt.sizeDelta = size; rt.anchoredPosition = anchoredPos;
        var img = go.AddComponent<UnityEngine.UI.Image>(); img.color = color;
        var btn = go.AddComponent<Button>();
        var lblGO = new GameObject("Label"); lblGO.transform.SetParent(go.transform, false);
        var lblRt = lblGO.AddComponent<RectTransform>(); lblRt.sizeDelta = size; lblRt.anchoredPosition = Vector2.zero;
        var txt = lblGO.AddComponent<Text>(); txt.font = font; txt.fontSize = fontSize; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.text = labelText;
        return btn;
    }



    // Helper to create a labeled slider. Returns the Slider and outputs the label Text.
    private Slider CreateLabeledSlider(Transform parent, Vector2 size, float min, float max, float initial, string name, Font font, Action<float, Text> onValueChanged = null)
    {
        float uiScale = 1f;
        // Row container with HorizontalLayoutGroup
        var row = new GameObject(name + "_Row");
        row.transform.SetParent(parent, false);
        var rowRt = row.AddComponent<RectTransform>();
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = Mathf.RoundToInt(8 * uiScale);
        hlg.childForceExpandHeight = false; hlg.childForceExpandWidth = false; hlg.childAlignment = TextAnchor.MiddleLeft;
        var rowLE = row.AddComponent<LayoutElement>(); rowLE.preferredHeight = size.y;

        // Label
        var lblGO = new GameObject(name + "_Label");
        lblGO.transform.SetParent(row.transform, false);
        var lblRt = lblGO.AddComponent<RectTransform>();
        var lbl = lblGO.AddComponent<Text>(); lbl.font = font; lbl.fontSize = Mathf.RoundToInt(12 * uiScale); lbl.color = Color.white; lbl.alignment = TextAnchor.MiddleLeft;
        // set initial label text
        lbl.text = name + "： " + initial.ToString("0.##");
        var lblLE = lblGO.AddComponent<LayoutElement>();
        // measure preferred width
        var genSettings = lbl.GetGenerationSettings(Vector2.zero);
        float pref = lbl.cachedTextGenerator.GetPreferredWidth(lbl.text, genSettings) / lbl.pixelsPerUnit;
        lblLE.preferredWidth = Mathf.Ceil(pref + 8f * uiScale);

        // Slider container
        var sliderGO = new GameObject(name + "_Slider");
        sliderGO.transform.SetParent(row.transform, false);
        var sliderRt = sliderGO.AddComponent<RectTransform>();
        // ensure the slider rect matches the requested size so background/fill stretch correctly
        sliderRt.sizeDelta = size;
        var sliderLE = sliderGO.AddComponent<LayoutElement>(); sliderLE.flexibleWidth = 1; sliderLE.preferredHeight = size.y;

        var slider = sliderGO.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;

        // Background
        var bg = new GameObject("Background"); bg.transform.SetParent(sliderGO.transform, false);
        var bgImg = bg.AddComponent<UnityEngine.UI.Image>(); bgImg.color = new Color(0.15f, 0.15f, 0.15f);
        var bgRt = bg.GetComponent<RectTransform>(); bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        // Fill area (use full rect with padding)
        var fillArea = new GameObject("Fill Area"); fillArea.transform.SetParent(sliderGO.transform, false);
        var faRt = fillArea.AddComponent<RectTransform>(); faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one; faRt.offsetMin = new Vector2(6, 6) * uiScale; faRt.offsetMax = new Vector2(-6, -6) * uiScale;
        var fill = new GameObject("Fill"); fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<UnityEngine.UI.Image>(); fillImg.color = new Color(0.2f, 0.6f, 0.2f);
        var fillRt = fill.GetComponent<RectTransform>(); fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;

        // Handle
        var handleArea = new GameObject("Handle Slide Area"); handleArea.transform.SetParent(sliderGO.transform, false);
        var haRt = handleArea.AddComponent<RectTransform>(); haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one; haRt.offsetMin = Vector2.zero; haRt.offsetMax = Vector2.zero;
        var handle = new GameObject("Handle"); handle.transform.SetParent(handleArea.transform, false);
        var handleImg = handle.AddComponent<UnityEngine.UI.Image>(); handleImg.color = Color.white;
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(12, 20) * uiScale;
        handleRt.anchorMin = new Vector2(0.5f, 0.5f);
        handleRt.anchorMax = new Vector2(0.5f, 0.5f);
        handleRt.anchoredPosition = Vector2.zero;

        slider.fillRect = fillImg.rectTransform;
        slider.handleRect = handleImg.rectTransform;
        slider.targetGraphic = handleImg;

        slider.value = initial;
        lbl.text = name + "： " + slider.value.ToString("0.##");
        slider.onValueChanged.AddListener((v) =>
        {
            if (onValueChanged != null)
            {
                onValueChanged(v, lbl);
            }
            else
                lbl.text = name + "： " + v.ToString("0.##");
        });
        return slider;
    }

    // Refresh removed: UI now updates from PlayData events and GameEventBus for community.
}

