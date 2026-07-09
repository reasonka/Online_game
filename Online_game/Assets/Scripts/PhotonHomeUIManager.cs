using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhotonHomeUIManager : MonoBehaviourPunCallbacks
{
    [Header("Scene")]
    public string gameSceneName = "GameScene";
    public byte maxPlayersPerRoom = 3;

    [Header("Panels")]
    public GameObject homePanel;
    public GameObject namePanel;
    public GameObject mainMenuPanel;
    public GameObject createRoomPanel;
    public GameObject roomPanel;
    public GameObject roomListPanel;
    public GameObject characterSelectPanel;

    [Header("Home UI")]
    public Button startButton;
    public Button exitButton;

    [Header("Name UI")]
    public TMP_InputField playerNameInput;
    public TMP_Text namePromptText;

    [Header("Main Menu UI")]
    public Button createRoomMenuButton;
    public Button joinRandomRoomButton;
    public Button showRoomListButton;
    public TMP_Text mainMenuPromptText;

    [Header("Create Room UI")]
    public TMP_InputField roomNameInput;
    public Button createRoomButton;
    public Button createRoomBackButton;

    [Header("Room UI")]
    public TMP_Text roomNameText;
    public TMP_Text[] playerNameTexts;
    public GameObject[] playerYouLabels;
    public Button startGameButton;
    public Button leaveRoomButton;
    public TMP_Text roomPromptText;

    [Header("Room List UI")]
    public RoomListItemUI[] roomListItems;
    public Button roomListBackButton;
    public TMP_Text roomListPromptText;

    [Header("Character Select UI")]
    public Button characterButton1;
    public Button characterButton2;
    public Button characterButton3;
    public Button characterNextButton;
    public TMP_Text characterPromptText;

    private readonly Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private int selectedCharacterIndex = -1;

    private const string CharacterPropertyKey = "CharacterIndex";

    private const byte OpenCharacterSelectEventCode = 1;

    private void Start()
    {
        PhotonNetwork.GameVersion = "1.0";
        PhotonNetwork.AutomaticallySyncScene = true;

        startButton.onClick.AddListener(OpenNamePanel);
        exitButton.onClick.AddListener(QuitGame);

        playerNameInput.onSubmit.AddListener(SubmitPlayerName);

        createRoomMenuButton.onClick.AddListener(OpenCreateRoomPanel);
        joinRandomRoomButton.onClick.AddListener(JoinRandomRoom);
        showRoomListButton.onClick.AddListener(OpenRoomListPanel);

        createRoomButton.onClick.AddListener(CreateRoom);
        createRoomBackButton.onClick.AddListener(OpenMainMenuPanel);

        startGameButton.onClick.AddListener(OpenCharacterSelectPanel);
        leaveRoomButton.onClick.AddListener(LeaveRoom);

        roomListBackButton.onClick.AddListener(OpenMainMenuPanel);

        OpenHomePanel();
    }

    private void OpenHomePanel()
    {
        CloseAllPanels();
        homePanel.SetActive(true);
    }

    private void OpenNamePanel()
    {
        CloseAllPanels();
        namePanel.SetActive(true);

        if (namePromptText != null)
            namePromptText.text = "Enter your player name and press Enter";
    }

    private void SubmitPlayerName(string playerName)
    {
        playerName = playerName.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            namePromptText.text = "Please enter a player name.";

            SFXManager.Instance?.PlayError();
            return;
        }

        PhotonNetwork.NickName = playerName;

        if (!PhotonNetwork.IsConnected)
        {
            namePromptText.text = "Connecting...";
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            OpenMainMenuPanel();
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        OpenMainMenuPanel();
    }

    private void OpenMainMenuPanel()
    {
        CloseAllPanels();
        mainMenuPanel.SetActive(true);

        if (mainMenuPromptText != null)
            mainMenuPromptText.text = "";
    }

    private void OpenCreateRoomPanel()
    {
        CloseAllPanels();
        createRoomPanel.SetActive(true);
    }

    private void CreateRoom()
    {
        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
            roomName = PhotonNetwork.NickName + "'s Room";

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, options);
    }

    private void JoinRandomRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            if (mainMenuPromptText != null)
                mainMenuPromptText.text = "Connecting...";

            SFXManager.Instance?.PlayError();
            return;
        }

        if (mainMenuPromptText != null)
            mainMenuPromptText.text = "Searching for room...";

        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        OpenMainMenuPanel();

        if (mainMenuPromptText != null)
            mainMenuPromptText.text = "No room available.";

        Debug.LogWarning("Join random failed: " + message);

        SFXManager.Instance?.PlayError();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Create room failed: " + message);

        SFXManager.Instance?.PlayError();
    }

    public override void OnJoinedRoom()
    {
        selectedCharacterIndex = -1;
        ClearCharacterProperty();

        CloseAllPanels();
        roomPanel.SetActive(true);
        UpdateRoomUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateRoomUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateRoomUI();
    }

    public override void OnLeftRoom()
    {
        OpenMainMenuPanel();
    }

    private void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    private void UpdateRoomUI()
    {
        if (!PhotonNetwork.InRoom)
            return;

        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        for (int i = 0; i < playerNameTexts.Length; i++)
        {
            if (i < PhotonNetwork.PlayerList.Length)
            {
                Player player = PhotonNetwork.PlayerList[i];

                playerNameTexts[i].text = player.NickName;

                if (i < playerYouLabels.Length && playerYouLabels[i] != null)
                    playerYouLabels[i].SetActive(player == PhotonNetwork.LocalPlayer);
            }
            else
            {
                playerNameTexts[i].text = "Waiting...";

                if (i < playerYouLabels.Length && playerYouLabels[i] != null)
                    playerYouLabels[i].SetActive(false);
            }
        }

        if (roomPromptText != null)
            roomPromptText.text = PhotonNetwork.CurrentRoom.PlayerCount + "/" + maxPlayersPerRoom + " players";

        startGameButton.interactable =
            PhotonNetwork.IsMasterClient &&
            PhotonNetwork.CurrentRoom.PlayerCount == maxPlayersPerRoom;
    }

    private void OpenRoomListPanel()
    {
        CloseAllPanels();
        roomListPanel.SetActive(true);

        if (!PhotonNetwork.InLobby && PhotonNetwork.IsConnectedAndReady)
            PhotonNetwork.JoinLobby();

        RefreshRoomListUI();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
                cachedRoomList.Remove(room.Name);
            else
                cachedRoomList[room.Name] = room;
        }

        RefreshRoomListUI();
    }

    private void RefreshRoomListUI()
    {
        int index = 0;

        foreach (RoomInfo room in cachedRoomList.Values)
        {
            if (!room.IsOpen || !room.IsVisible || room.PlayerCount >= room.MaxPlayers)
                continue;

            if (index >= roomListItems.Length)
                break;

            roomListItems[index].gameObject.SetActive(true);
            roomListItems[index].SetRoom(room.Name, room.PlayerCount, room.MaxPlayers, JoinRoomByName);

            index++;
        }

        for (int i = index; i < roomListItems.Length; i++)
        {
            roomListItems[i].gameObject.SetActive(false);
        }

        if (roomListPromptText != null)
        {
            if (index == 0)
                roomListPromptText.text = "NO ROOMS FOUND!";
            else if (index >= roomListItems.Length)
                roomListPromptText.text = "ONLY SHOWING FIRST 3 ROOMS!";
            else
                roomListPromptText.text = "";
        }
    }

    private void JoinRoomByName(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    private void OpenCharacterSelectPanel()
    {
        if (!PhotonNetwork.InRoom)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        if (PhotonNetwork.CurrentRoom.PlayerCount < maxPlayersPerRoom)
        {
            roomPromptText.text = "Need 3 players before starting.";

            SFXManager.Instance?.PlayError();
            return;
        }

        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All
        };

        PhotonNetwork.RaiseEvent(
            OpenCharacterSelectEventCode,
            null,
            options,
            SendOptions.SendReliable
        );
    }

    private void SelectCharacter(int characterIndex)
    {
        if (IsCharacterTaken(characterIndex))
        {
            UpdateCharacterPrompt("This character is already chosen.");
            return;
        }

        selectedCharacterIndex = characterIndex;

        Hashtable properties = new Hashtable
        {
            { CharacterPropertyKey, selectedCharacterIndex }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
        UpdateCharacterPrompt("Character selected. Press Next.");
    }

    private bool IsCharacterTaken(int characterIndex)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player == PhotonNetwork.LocalPlayer)
                continue;

            if (player.CustomProperties.TryGetValue(CharacterPropertyKey, out object chosenCharacter))
            {
                if ((int)chosenCharacter == characterIndex)
                    return true;
            }
        }

        return false;
    }

    private void ConfirmCharacterAndStart()
    {
        if (selectedCharacterIndex < 0)
        {
            UpdateCharacterPrompt("Please choose a character first.");
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            UpdateCharacterPrompt("Waiting for host to start.");
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < maxPlayersPerRoom)
        {
            UpdateCharacterPrompt("Need 3 players before starting.");
            return;
        }

        if (!AllPlayersSelectedCharacter())
        {
            UpdateCharacterPrompt("Waiting for all players to choose.");
            return;
        }

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel(gameSceneName);
    }

    private bool AllPlayersSelectedCharacter()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey(CharacterPropertyKey))
                return false;
        }

        return true;
    }

    private void ClearCharacterProperty()
    {
        Hashtable properties = new Hashtable
        {
            { "CharacterIndex", null },
            { "Ready", null }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
    }

    private void UpdateCharacterPrompt(string message)
    {
        if (characterPromptText != null)
            characterPromptText.text = message;
    }

    private void CloseAllPanels()
    {
        homePanel.SetActive(false);
        namePanel.SetActive(false);
        mainMenuPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        roomPanel.SetActive(false);
        roomListPanel.SetActive(false);
        characterSelectPanel.SetActive(false);
    }

    private void QuitGame()
    {
        Application.Quit();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
    }

    private void OnPhotonEvent(EventData photonEvent)
    {
        if (photonEvent.Code == OpenCharacterSelectEventCode)
        {
            ShowCharacterSelectPanelForEveryone();
        }
    }

    private void ShowCharacterSelectPanelForEveryone()
    {
        CloseAllPanels();
        characterSelectPanel.SetActive(true);

        selectedCharacterIndex = -1;
        UpdateCharacterPrompt("Choose your character.");
    }

    public override void OnJoinedLobby()
    {
        if (!PhotonNetwork.InRoom)
            OpenMainMenuPanel();
    }
}