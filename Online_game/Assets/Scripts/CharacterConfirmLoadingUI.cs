using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterConfirmLoadingUI : MonoBehaviourPunCallbacks
{
    [Header("Character Buttons")]
    public Button orderTakerButton;
    public Button doodleBuddyButton;
    public Button chefButton;
    public Button confirmButton;

    [Header("Loading Panels")]
    public GameObject orderTakerLoadingPanel;
    public GameObject doodleBuddyLoadingPanel;
    public GameObject chefLoadingPanel;

    [Header("Cutscene Text")]
    public TMP_Text orderTakerCutsceneText;
    public TMP_Text doodleBuddyCutsceneText;
    public TMP_Text chefCutsceneText;

    [Header("Prompt")]
    public TMP_Text promptText;

    [Header("Scene")]
    public string gameSceneName = "GameScene";

    [Header("Loading Text")]
    public string loadingMessage = "Loading...";

    private int selectedCharacterIndex = -1;

    private const string CharacterPropertyKey = "CharacterIndex";
    private const string ReadyPropertyKey = "Ready";

    private void Start()
    {
        orderTakerButton.onClick.AddListener(() => SelectCharacter(0));
        doodleBuddyButton.onClick.AddListener(() => SelectCharacter(1));
        chefButton.onClick.AddListener(() => SelectCharacter(2));
        confirmButton.onClick.AddListener(ConfirmCharacter);

        CloseAllLoadingPanels();
    }

    private void SelectCharacter(int characterIndex)
    {
        if (IsCharacterTaken(characterIndex))
        {
            SFXManager.Instance?.PlayError();

            if (promptText != null)
                promptText.text = "This character is already chosen.";

            return;
        }

        selectedCharacterIndex = characterIndex;

        Hashtable properties = new Hashtable
        {
            { CharacterPropertyKey, selectedCharacterIndex }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);

        if (promptText != null)
            promptText.text = "Character selected. Press Confirm.";

        SFXManager.Instance?.PlayCharacterSelected();
    }

    private bool IsCharacterTaken(int characterIndex)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player == PhotonNetwork.LocalPlayer)
                continue;

            if (player.CustomProperties.TryGetValue(CharacterPropertyKey, out object value))
            {
                if ((int)value == characterIndex)
                    return true;
            }
        }

        return false;
    }

    private void ConfirmCharacter()
    {
        if (selectedCharacterIndex < 0)
        {
            SFXManager.Instance?.PlayError();

            if (promptText != null)
                promptText.text = "Please choose a character first.";

            return;
        }

        ShowLoadingPanel(selectedCharacterIndex);

        SFXManager.Instance?.PlayLoadingStart();

        if (promptText != null)
            promptText.text = loadingMessage;

        Hashtable properties = new Hashtable
        {
            { ReadyPropertyKey, true }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);

        TryStartGameIfEveryoneReady();
    }

    private void ShowLoadingPanel(int characterIndex)
    {
        CloseAllLoadingPanels();

        if (characterIndex == 0)
        {
            orderTakerLoadingPanel.SetActive(true);
            orderTakerCutsceneText.text = GetOrderTakerCutscene();
        }
        else if (characterIndex == 1)
        {
            doodleBuddyLoadingPanel.SetActive(true);
            doodleBuddyCutsceneText.text = GetDoodleBuddyCutscene();
        }
        else if (characterIndex == 2)
        {
            chefLoadingPanel.SetActive(true);
            chefCutsceneText.text = GetChefCutscene();
        }
    }

    private void CloseAllLoadingPanels()
    {
        if (orderTakerLoadingPanel != null)
            orderTakerLoadingPanel.SetActive(false);

        if (doodleBuddyLoadingPanel != null)
            doodleBuddyLoadingPanel.SetActive(false);

        if (chefLoadingPanel != null)
            chefLoadingPanel.SetActive(false);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        TryStartGameIfEveryoneReady();
    }

    private void TryStartGameIfEveryoneReady()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (PhotonNetwork.CurrentRoom.PlayerCount < 3)
            return;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey(CharacterPropertyKey))
                return;

            if (!player.CustomProperties.ContainsKey(ReadyPropertyKey))
                return;
        }

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel(gameSceneName);
    }

    private string GetOrderTakerCutscene()
    {
        return "You cannot hear the chaos around you, but your eyes are sharp. Guide your team before history changes.";
    }

    private string GetDoodleBuddyCutscene()
    {
        return "Your voice is gone, but your drawings speak for you. Listen carefully what's been described by the Order Taker.";
    }

    private string GetChefCutscene()
    {
        return "The kitchen is full of colors you cannot fully trust. Follow the Doodle Buddy's hints. Cook fast, cook carefully, and save the order.";
    }
}