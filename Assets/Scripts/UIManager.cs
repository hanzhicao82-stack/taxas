using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public PokerGame game;
    public Text[] playerTexts;
    public Text communityText;
    public Button dealButton;
    public Text resultText;

    void Start()
    {
        // Hook up the deal button at runtime (UIManager can be wired via the editor-created scene)
        if (dealButton != null) dealButton.onClick.AddListener(OnDealClicked);
        Refresh(game);
    }

    public void OnDealClicked()
    {
        // Start a new hand when the user clicks the deal button.
        if (game != null) game.StartHand();
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
                playerTexts[i].text = $"{p.name}: {p.hole[0]} {p.hole[1]}";
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

