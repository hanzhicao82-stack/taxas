using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // Runtime-resolved references (do not configure in inspector)
    private PokerGame game;
    private Text[] playerTexts;
    private Text communityText;
    private Button dealButton;
    private Text resultText;

    void Start()
    {
        // Resolve game reference
        if (game == null) game = UnityEngine.Object.FindObjectOfType<PokerGame>();

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
        // Ensure canvas rect stretches full screen
        var canvasRt = canvas.GetComponent<RectTransform>();
        canvasRt.anchorMin = Vector2.zero; canvasRt.anchorMax = Vector2.one; canvasRt.sizeDelta = Vector2.zero; canvasRt.offsetMin = Vector2.zero; canvasRt.offsetMax = Vector2.zero;

        // Determine player count
        int playerCount = (game != null) ? game.numPlayers : 4;

        // Create a simple vertical layout container for texts
        var panelGO = new GameObject("UI Panel");
        panelGO.transform.SetParent(root, false);
        var panelRt = panelGO.AddComponent<RectTransform>();
        // Center the panel and scale UI by 1.5x
        float uiScale = 1.5f;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f); panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(800, 600) * uiScale;
        panelRt.anchoredPosition = Vector2.zero;

        // Prepare font
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Create player text fields
        var list = new List<Text>();
        for (int i = 0; i < playerCount; i++)
        {
            var tgo = new GameObject($"PlayerText_{i + 1}");
            tgo.transform.SetParent(panelGO.transform, false);
            var rt = tgo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(300, 24) * uiScale;
            rt.anchoredPosition = new Vector2(0, -24 * i * uiScale);
            var txt = tgo.AddComponent<Text>();
            txt.font = font; txt.fontSize = Mathf.RoundToInt(18 * uiScale); txt.color = Color.white; txt.alignment = TextAnchor.UpperLeft;
            txt.text = $"P{i + 1}:";
            list.Add(txt);
        }
        playerTexts = list.ToArray();

        // Community text
        var cgo = new GameObject("CommunityText");
        cgo.transform.SetParent(panelGO.transform, false);
        var crt = cgo.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(0, 1); crt.pivot = new Vector2(0, 1);
        crt.sizeDelta = new Vector2(600, 24) * uiScale;
        crt.anchoredPosition = new Vector2(0, -24 * playerCount * uiScale - 10 * uiScale);
        communityText = cgo.AddComponent<Text>(); communityText.font = font; communityText.fontSize = Mathf.RoundToInt(18 * uiScale); communityText.color = Color.yellow;
        communityText.text = "";

        // Result text
        var rgo = new GameObject("ResultText");
        rgo.transform.SetParent(panelGO.transform, false);
        var rrt = rgo.AddComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(0, 1); rrt.pivot = new Vector2(0, 1);
        rrt.sizeDelta = new Vector2(400, 24) * uiScale;
        rrt.anchoredPosition = new Vector2(0, -24 * playerCount * uiScale - 40 * uiScale);
        resultText = rgo.AddComponent<Text>(); resultText.font = font; resultText.fontSize = Mathf.RoundToInt(18 * uiScale); resultText.color = Color.cyan;

        // Deal button
        var bgo = new GameObject("DealButton");
        bgo.transform.SetParent(panelGO.transform, false);
        var brt = bgo.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(0, 1); brt.pivot = new Vector2(0, 1);
        brt.sizeDelta = new Vector2(100, 32) * uiScale;
        brt.anchoredPosition = new Vector2(0, -24 * playerCount * uiScale - 80 * uiScale);
        var img = bgo.AddComponent<UnityEngine.UI.Image>(); img.color = new Color(0.2f, 0.6f, 0.2f);
        dealButton = bgo.AddComponent<Button>();
        var labelGO = new GameObject("Label"); labelGO.transform.SetParent(bgo.transform, false);
        var lrt = labelGO.AddComponent<RectTransform>(); lrt.sizeDelta = brt.sizeDelta; lrt.anchoredPosition = Vector2.zero;
        var ltxt = labelGO.AddComponent<Text>(); ltxt.font = font; ltxt.fontSize = Mathf.RoundToInt(18 * uiScale); ltxt.alignment = TextAnchor.MiddleCenter; ltxt.color = Color.white; ltxt.text = "Deal";

        // Hook up button
        dealButton.onClick.AddListener(OnDealClicked);

        // Initial refresh
        Refresh(game);

        // Subscribe to game events for incremental updates using stored wrappers so we can unsubscribe reliably
        onFlopWrapper = (obj) => { try { var t = (Tuple<List<Card>, List<Card>>)obj; OnCommunityUpdated(t); } catch { } };
        onTurnWrapper = (obj) => { try { var t = (Tuple<List<Card>, List<Card>>)obj; OnCommunityUpdated(t); } catch { } };
        onRiverWrapper = (obj) => { try { var t = (Tuple<List<Card>, List<Card>>)obj; OnCommunityUpdated(t); } catch { } };
        onHandStartedWrapper = (obj) => { try { var t = (List<Player>)obj; OnHandStarted(t); } catch { } };
        GameEventBus.Subscribe(Events.Flop, onFlopWrapper);
        GameEventBus.Subscribe(Events.Turn, onTurnWrapper);
        GameEventBus.Subscribe(Events.River, onRiverWrapper);
        GameEventBus.Subscribe(Events.HandStarted, onHandStartedWrapper);
    }

    void OnDestroy()
    {
        // Clean up subscriptions
        if (onFlopWrapper != null) GameEventBus.Unsubscribe(Events.Flop, onFlopWrapper);
        if (onTurnWrapper != null) GameEventBus.Unsubscribe(Events.Turn, onTurnWrapper);
        if (onRiverWrapper != null) GameEventBus.Unsubscribe(Events.River, onRiverWrapper);
        if (onHandStartedWrapper != null) GameEventBus.Unsubscribe(Events.HandStarted, onHandStartedWrapper);

        if (dealButton != null) dealButton.onClick.RemoveListener(OnDealClicked);
    }

    // Wrappers are used for Unsubscribe because Subscribe<T> wraps handlers internally
    private void OnCommunityUpdated(Tuple<List<Card>, List<Card>> tpl)
    {
        if (tpl == null) return;
        // Incrementally append added cards to the communityText, then do a full refresh
        if (communityText != null && tpl.Item2 != null && tpl.Item2.Count > 0)
        {
            var sb = new StringBuilder(communityText.text ?? "");
            foreach (var c in tpl.Item2) sb.Append(c).Append(' ');
            communityText.text = sb.ToString();
        }
        Refresh(game);
    }

    private void OnHandStarted(List<Player> players)
    {
        // When a new hand starts, perform a full refresh
        Refresh(game);
    }

    // These object-typed wrappers are stored as delegates to allow reliable Unsubscribe
    private Action<object> onFlopWrapper;
    private Action<object> onTurnWrapper;
    private Action<object> onRiverWrapper;
    private Action<object> onHandStartedWrapper;

    public void OnDealClicked()
    {
        // Start a new hand when the user clicks the deal button.
        // if (game != null) game.StartHand();
    }

    public void Refresh(PokerGame g)
    {
        // Update UI to reflect current game state (players, community cards, winner).
        if (g == null) return;
        for (int i = 0; i < playerTexts.Length; i++)
        {
            if (i < g.players.Count)
            {
                var p = g.players[i];
                if (p.hole != null && p.hole.Count >= 2)
                    playerTexts[i].text = $"{p.name}: {p.hole[0]} {p.hole[1]}";
                else
                    playerTexts[i].text = $"{p.name}:";
            }
            else playerTexts[i].text = "";
        }
        if (communityText != null)
        {
            var sb = new StringBuilder();
            foreach (var c in g.community) sb.Append(c).Append(' ');
            communityText.text = sb.ToString();
        }
        if (resultText != null)
        {
            int w = g.DetermineWinner();
            if (w >= 0) resultText.text = "Winner: P" + (w + 1);
            else resultText.text = "";
        }
    }
}

