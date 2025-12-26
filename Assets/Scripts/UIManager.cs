using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    // Runtime-resolved references (do not configure in inspector)
    private PokerGame game => UnityEngine.Object.FindObjectOfType<PokerGame>();
    private Text[] playerTexts;
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
        int initialCount = (game != null && game.players != null) ? game.players.Count : playerCount;
        RebuildPlayerTextFields(initialCount, font, uiScale);

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

        raiseProbSlider = CreateLabeledSlider(paramsContainerGO.transform, Vector2.zero, new Vector2(300, 18) * uiScale, 0f, 1f, (game != null && game.aiConfig != null) ? game.aiConfig.raiseProbability : 0.12f, out raiseProbLabel, "加注概率", font, uiScale);
        betProbSlider = CreateLabeledSlider(paramsContainerGO.transform, Vector2.zero, new Vector2(300, 18) * uiScale, 0f, 1f, (game != null && game.aiConfig != null) ? game.aiConfig.betProbability : 0.06f, out betProbLabel, "下注概率", font, uiScale);
        raiseBaseSlider = CreateLabeledSlider(paramsContainerGO.transform, Vector2.zero, new Vector2(300, 18) * uiScale, 0f, 5f, (game != null && game.aiConfig != null) ? game.aiConfig.raiseSizeBase : 0.5f, out raiseBaseLabel, "基础加注", font, uiScale);
        raiseScaleSlider = CreateLabeledSlider(paramsContainerGO.transform, Vector2.zero, new Vector2(300, 18) * uiScale, 0f, 3f, (game != null && game.aiConfig != null) ? game.aiConfig.raiseSizeAggressionScale : 1f, out raiseScaleLabel, "激进系数", font, uiScale);
        minRaiseSlider = CreateLabeledSlider(paramsContainerGO.transform, Vector2.zero, new Vector2(300, 18) * uiScale, 0f, 1f, (game != null && game.aiConfig != null) ? game.aiConfig.minRaiseFraction : 0.5f, out minRaiseLabel, "最小加注比例", font, uiScale);
        simIterSlider = CreateLabeledSlider(paramsContainerGO.transform, Vector2.zero, new Vector2(300, 18) * uiScale, 10f, 1000f, (game != null && game.aiConfig != null) ? game.aiConfig.simIterations : 20, out simIterLabel, "模拟次数", font, uiScale);
        simIterSlider.wholeNumbers = true;

        numPlayersSlider = CreateLabeledSlider(paramsContainerGO.transform, Vector2.zero, new Vector2(300, 18) * uiScale, 2f, 8f, game != null ? game.numPlayers : 4f, out numPlayersLabel, "玩家数", font, uiScale);
        numPlayersSlider.wholeNumbers = true;
        smallBlindSlider = CreateLabeledSlider(paramsContainerGO.transform, Vector2.zero, new Vector2(300, 18) * uiScale, 1f, 100f, (game != null) ? game.smallBlindAmount : 5f, out smallBlindLabel, "小盲注", font, uiScale);
        smallBlindSlider.wholeNumbers = true;
        bigBlindSlider = CreateLabeledSlider(paramsContainerGO.transform, Vector2.zero, new Vector2(300, 18) * uiScale, 1f, 500f, (game != null) ? game.bigBlindAmount : 10f, out bigBlindLabel, "大盲注", font, uiScale);
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
        onHandStartedWrapper = (obj) => { try { var t = (List<Player>)obj; OnHandStarted(t); } catch { } };
        GameEventBus.Subscribe(Events.Flop, onFlopWrapper);
        GameEventBus.Subscribe(Events.Turn, onTurnWrapper);
        GameEventBus.Subscribe(Events.River, onRiverWrapper);
        GameEventBus.Subscribe(Events.HandStarted, onHandStartedWrapper);
        // Ensure we display player chip info if the game creates players slightly later
        CoroutineTracker.Start(this, EnsureInitialPlayerInfo());
    }

    private IEnumerator EnsureInitialPlayerInfo()
    {
        float start = Time.realtimeSinceStartup;
        float timeout = 2f;
        while (Time.realtimeSinceStartup - start < timeout)
        {
            if (game != null && game.players != null && game.players.Count > 0)
            {
                // rebuild to match actual player count and subscribe to data-driven updates
                RebuildPlayerTextFields(game.players.Count, uiFont, uiScale);
                yield break;
            }
            yield return null;
        }
        // final attempt: subscribe and update with what we have
        SubscribeAllPlayerData();
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

    // Recreate player text UI elements to match count and animate transitions
    private void RebuildPlayerTextFields(int count, Font font, float uiScale)
    {
        if (playerTextGOs == null)
        {
            playerTextGOs = new List<GameObject>();
        }

        // create missing elements
        for (int idx = playerTextGOs.Count; idx < count; idx++)
        {
            var tgo = new GameObject($"PlayerText_{idx + 1}");
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
            txt.font = font; txt.fontSize = Mathf.RoundToInt(14 * uiScale); txt.color = Color.white; txt.alignment = TextAnchor.MiddleCenter;
            txt.text = $"玩家{idx + 1}：";

            // start at center
            var r = tgo.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f); r.pivot = new Vector2(0.5f, 0.5f); r.anchoredPosition = Vector2.zero;

            // initial invisible
            var cg = tgo.AddComponent<CanvasGroup>(); cg.alpha = 0f;

            playerTextGOs.Add(tgo);
        }

        // destroy extras
        while (playerTextGOs.Count > count)
        {
            var last = playerTextGOs[playerTextGOs.Count - 1];
            playerTextGOs.RemoveAt(playerTextGOs.Count - 1);
            if (last != null)
            {
                Destroy(last);
            }
        }

        // update label list
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

        // Arrange and animate to circular positions
        CoroutineTracker.Start(this, ArrangePlayersSmooth(playerTextGOs, uiScale, 0.35f));

        // Ensure community/result/pot/params placed relative to center
        if (communityText != null)
        {
            var crt2 = communityText.GetComponent<RectTransform>();
            if (crt2 != null)
            {
                crt2.anchoredPosition = Vector2.zero;
            }
        }
        if (resultText != null)
        {
            var rrt2 = resultText.GetComponent<RectTransform>();
            if (rrt2 != null)
            {
                rrt2.anchoredPosition = new Vector2(0, -30 * uiScale);
            }
        }
        if (potText != null)
        {
            var prt2 = potText.GetComponent<RectTransform>();
            if (prt2 != null)
            {
                prt2.anchoredPosition = new Vector2(0, -60 * uiScale);
            }
        }
        if (paramsContainerGO != null)
        {
            var prt3 = paramsContainerGO.GetComponent<RectTransform>();
            if (prt3 != null)
            {
                prt3.anchoredPosition = new Vector2(0, -90 * uiScale);
            }
        }
    }

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
        if (cnt != playerTexts.Length)
        {
            RebuildPlayerTextFields(cnt, uiFont, uiScale);
        }
        // subscribe to per-player data changes and update UI
        SubscribeAllPlayerData();
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
            var s = dataSubs[i];
            if (s == null || p == null) continue;
            if (s.stack != null) p.data.StackData.OnValueChanged -= s.stack;
            if (s.hole != null) p.data.HoleData.OnValueChanged -= s.hole;
            if (s.bet != null) p.data.CurrentBetData.OnValueChanged -= s.bet;
            if (s.folded != null) p.data.FoldedData.OnValueChanged -= s.folded;
            if (s.allin != null) p.data.AllInData.OnValueChanged -= s.allin;
        }
        dataSubs.Clear();
    }

    private void SubscribeAllPlayerData()
    {
        UnsubscribeAllPlayerData();
        if (game == null || game.players == null) return;
        for (int i = 0; i < game.players.Count; i++)
        {
            var p = game.players[i];
            var s = new DataSubs();
            int idx = i;
            s.stack = (oldv, newv) => UpdatePlayerLabel(idx);
            s.hole = (oldv, newv) => UpdatePlayerLabel(idx);
            s.bet = (oldv, newv) => { UpdatePlayerLabel(idx); UpdatePotText(game.pot); };
            s.folded = (oldv, newv) => UpdatePlayerLabel(idx);
            s.allin = (oldv, newv) => UpdatePlayerLabel(idx);
            p.data.StackData.OnValueChanged += s.stack;
            p.data.HoleData.OnValueChanged += s.hole;
            p.data.CurrentBetData.OnValueChanged += s.bet;
            p.data.FoldedData.OnValueChanged += s.folded;
            p.data.AllInData.OnValueChanged += s.allin;
            dataSubs.Add(s);
        }
        // initial update
        for (int i = 0; i < game.players.Count && i < playerTextGOs.Count; i++) UpdatePlayerLabel(i);
        UpdatePotText(game.pot);
    }

    private void UpdatePlayerLabel(int i)
    {
        if (game == null || game.players == null) return;
        if (i < 0 || i >= game.players.Count) return;
        if (playerTextGOs == null || i >= playerTextGOs.Count) return;
        var p = game.players[i];
        var label = playerTextGOs[i].transform.Find("Label")?.GetComponent<Text>() ?? playerTextGOs[i].GetComponentInChildren<Text>();
        if (label == null) return;
        string holeStr = "";
        var holeList = p.data.Hole;
        if (holeList != null && holeList.Count >= 2)
        {
            holeStr = $"{holeList[0]} {holeList[1]} ";
        }
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.text = $"{p.name}：{holeStr}筹码：{p.data.Stack}";
    }

    private void UpdatePotText(int pot)
    {
        if (potText != null) potText.text = "底池：" + pot.ToString();
    }

    public void UpdatePot(int pot)
    {
        UpdatePotText(pot);
    }

    public void OnDealClicked()
    {
        // Start a new hand when the user clicks the deal button.
        // if (game != null) game.StartHand();
    }

    private void OnStartClicked()
    {

        // Subscribe to player data and update UI
        SubscribeAllPlayerData();

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
        int players = (numPlayersSlider != null) ? Mathf.RoundToInt(numPlayersSlider.value) : ((game != null) ? game.numPlayers : 4);
        int small = (smallBlindSlider != null) ? Mathf.RoundToInt(smallBlindSlider.value) : ((game != null) ? game.smallBlindAmount : 5);
        int big = (bigBlindSlider != null) ? Mathf.RoundToInt(bigBlindSlider.value) : ((game != null) ? game.bigBlindAmount : 10);
        if (game != null)
        {
            game.numPlayers = players;
            game.smallBlindAmount = small;
            game.bigBlindAmount = big;
            game.aiConfig = cfg;
        }

        // Create and run the test runner while hiding settings
        var tester = new GameObject("Test");
        var flowTest = tester.AddComponent<PokerGameFlowTest>();
        CoroutineTracker.Start(this, RunGameWithHiddenSettings(flowTest.Run(cfg, 10, players)));
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
    private Slider CreateLabeledSlider(Transform parent, Vector2 anchoredPos, Vector2 size, float min, float max, float initial, out Text labelOut, string name, Font font, float uiScale)
    {
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
        var sliderLE = sliderGO.AddComponent<LayoutElement>(); sliderLE.flexibleWidth = 1; sliderLE.preferredHeight = size.y;

        var slider = sliderGO.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = false;

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
        slider.onValueChanged.AddListener((v) => { lbl.text = name + "： " + v.ToString("0.##"); });

        labelOut = lbl;
        return slider;
    }

    // Refresh removed: UI now updates from PlayData events and GameEventBus for community.
}

