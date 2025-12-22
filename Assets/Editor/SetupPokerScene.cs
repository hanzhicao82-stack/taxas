using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor helper to quickly create a minimal poker UI and wire `PokerGame` and `UIManager`.
/// Run via menu: Tools / Setup Poker Scene
/// </summary>
public static class SetupPokerScene
{
    [MenuItem("Tools/Setup Poker Scene")]
    public static void CreateSceneObjects()
    {
        // Create PokerGame object
        var gameGO = new GameObject("PokerGame");
        var poker = gameGO.AddComponent<PokerGame>();

        // Create UIManager object
        var uiGO = new GameObject("UIManager");
        var ui = uiGO.AddComponent<UIManager>();

        // Create Canvas
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Create a simple panel for layout
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGO.transform, false);
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0, 0);
        panelRT.anchorMax = new Vector2(1, 0.3f);
        panelRT.anchoredPosition = Vector2.zero;

        // Create player text slots (4 by default)
        int slots = 4;
        ui.playerTexts = new Text[slots];
        for (int i = 0; i < slots; i++)
        {
            var tgo = new GameObject($"Player{i+1}Text", typeof(RectTransform), typeof(Text));
            tgo.transform.SetParent(panel.transform, false);
            var txt = tgo.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 24;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.rectTransform.anchorMin = new Vector2(0, i * 0.25f);
            txt.rectTransform.anchorMax = new Vector2(1, (i + 1) * 0.25f);
            txt.text = "Player" + (i + 1);
            ui.playerTexts[i] = txt;
        }

        // Community text
        var commGO = new GameObject("CommunityText", typeof(RectTransform), typeof(Text));
        commGO.transform.SetParent(canvasGO.transform, false);
        var commTxt = commGO.GetComponent<Text>();
        commTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        commTxt.fontSize = 28;
        commTxt.alignment = TextAnchor.MiddleCenter;
        var commRT = commGO.GetComponent<RectTransform>();
        commRT.anchorMin = new Vector2(0.1f, 0.5f);
        commRT.anchorMax = new Vector2(0.9f, 0.7f);
        commTxt.text = "Community";
        ui.communityText = commTxt;

        // Result text
        var resGO = new GameObject("ResultText", typeof(RectTransform), typeof(Text));
        resGO.transform.SetParent(canvasGO.transform, false);
        var resTxt = resGO.GetComponent<Text>();
        resTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        resTxt.fontSize = 28;
        resTxt.alignment = TextAnchor.UpperCenter;
        var resRT = resGO.GetComponent<RectTransform>();
        resRT.anchorMin = new Vector2(0.1f, 0.7f);
        resRT.anchorMax = new Vector2(0.9f, 0.85f);
        resTxt.text = "Winner:";
        ui.resultText = resTxt;

        // Deal Button
        var btnGO = new GameObject("DealButton", typeof(RectTransform), typeof(Button), typeof(Image));
        btnGO.transform.SetParent(canvasGO.transform, false);
        var btn = btnGO.GetComponent<Button>();
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.4f, 0.05f);
        btnRT.anchorMax = new Vector2(0.6f, 0.15f);
        var labelGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(btnGO.transform, false);
        var label = labelGO.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.alignment = TextAnchor.MiddleCenter;
        label.text = "Deal";
        ui.dealButton = btn;

        // Wire references
        ui.game = poker;
        poker.ui = ui;

        // Select the canvas for convenience
        Selection.activeGameObject = canvasGO;

        Debug.Log("Poker scene setup complete. Configure PokerGame.numPlayers as needed.");
    }
}
