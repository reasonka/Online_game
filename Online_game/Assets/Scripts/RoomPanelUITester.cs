using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomPanelUITester : MonoBehaviour
{
    [Header("Room Test Data")]
    public string roomName = "Test Room";
    [Range(0, 3)] public int playerCount = 1;
    [Range(0, 2)] public int localPlayerIndex = 0;

    public string player1Name = "Order Taker";
    public string player2Name = "Doodle Buddy";
    public string player3Name = "Chef";

    [Header("Room UI")]
    public TMP_Text roomNameText;
    public TMP_Text[] playerNameTexts = new TMP_Text[3];
    public GameObject[] playerYouLabels = new GameObject[3];
    public TMP_Text roomPromptText;

    [Header("Buttons")]
    public Button startGameButton;

    [Header("Messages")]
    public string waitingText = "Waiting...";
    public string needPlayersText = "Need 3 players before starting.";
    public string readyText = "3/3 players ready.";

    [Header("Testing")]
    public bool liveUpdate = true;

    private void OnEnable()
    {
        UpdateTestUI();
    }

    private void Update()
    {
        if (liveUpdate)
            UpdateTestUI();
    }

    public void UpdateTestUI()
    {
        if (roomNameText != null)
            roomNameText.text = roomName;

        string[] names =
        {
            player1Name,
            player2Name,
            player3Name
        };

        for (int i = 0; i < playerNameTexts.Length; i++)
        {
            bool hasPlayer = i < playerCount;

            if (playerNameTexts[i] != null)
                playerNameTexts[i].text = hasPlayer ? names[i] : waitingText;

            if (i < playerYouLabels.Length && playerYouLabels[i] != null)
                playerYouLabels[i].SetActive(hasPlayer && i == localPlayerIndex);
        }

        if (roomPromptText != null)
            roomPromptText.text = playerCount < 3 ? needPlayersText : readyText;

        if (startGameButton != null)
            startGameButton.interactable = playerCount == 3;
    }
}