using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Level1ObjectiveManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static Level1ObjectiveManager Instance;

    [Header("Objective")]
    public int customersNeeded = 5;

    [Header("Reaction Timing")]
    public float defaultReactionDelay = 2.5f;

    [Header("UI")]
    public Text progressText;
    public GameObject levelCompletePanel;
    public Button backToLobbyButton;
    public Text hostOnlyText;

    private const string ServedCountKey = "Level1ServedCount";
    private const string Level1ObjectiveCompleteKey = "Level1ObjectiveComplete";

    private const byte CustomerServedEventCode = 60;

    private int servedCount = 0;
    private bool levelComplete = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.RemoveAllListeners();
            backToLobbyButton.onClick.AddListener(BackToLobbyButton);
        }

        InitializeLevel1Progress();
        ReadProgressFromRoom();
        RefreshUI();
    }

    private void InitializeLevel1Progress()
    {
        if (!PhotonNetwork.InRoom) return;
        if (!PhotonNetwork.IsMasterClient) return;

        servedCount = 0;
        levelComplete = false;

        Hashtable props = new Hashtable
        {
            { ServedCountKey, servedCount },
            { Level1ObjectiveCompleteKey, levelComplete }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private void ReadProgressFromRoom()
    {
        if (!PhotonNetwork.InRoom) return;
        if (PhotonNetwork.CurrentRoom == null) return;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ServedCountKey, out object countValue))
        {
            servedCount = (int)countValue;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(Level1ObjectiveCompleteKey, out object completeValue))
        {
            levelComplete = (bool)completeValue;
        }
    }

    public void CustomerServedAfterReaction(float reactionDelay = -1f)
    {
        if (levelComplete) return;

        float delay = reactionDelay >= 0f ? reactionDelay : defaultReactionDelay;

        if (!PhotonNetwork.InRoom)
        {
            StartCoroutine(AddServedCustomerAfterDelay(delay));
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(AddServedCustomerAfterDelay(delay));
        }
        else
        {
            RaiseEventOptions options = new RaiseEventOptions
            {
                Receivers = ReceiverGroup.MasterClient
            };

            SendOptions sendOptions = new SendOptions
            {
                Reliability = true
            };

            PhotonNetwork.RaiseEvent(
                CustomerServedEventCode,
                delay,
                options,
                sendOptions
            );
        }
    }

    private IEnumerator AddServedCustomerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (levelComplete) yield break;

        servedCount++;

        if (servedCount >= customersNeeded)
        {
            servedCount = customersNeeded;
            levelComplete = true;
        }

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable
            {
                { ServedCountKey, servedCount },
                { Level1ObjectiveCompleteKey, levelComplete }
            };

            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        RefreshUI();
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != CustomerServedEventCode) return;
        if (!PhotonNetwork.IsMasterClient) return;

        float delay = defaultReactionDelay;

        if (photonEvent.CustomData is float receivedDelay)
        {
            delay = receivedDelay;
        }

        StartCoroutine(AddServedCustomerAfterDelay(delay));
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(ServedCountKey))
        {
            servedCount = (int)propertiesThatChanged[ServedCountKey];
        }

        if (propertiesThatChanged.ContainsKey(Level1ObjectiveCompleteKey))
        {
            levelComplete = (bool)propertiesThatChanged[Level1ObjectiveCompleteKey];
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (progressText != null)
        {
            progressText.text = "Customers Served: " + servedCount + " / " + customersNeeded;
        }

        bool isHost = !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(levelComplete);
        }

        if (backToLobbyButton != null)
        {
            backToLobbyButton.gameObject.SetActive(levelComplete && isHost);
            backToLobbyButton.interactable = levelComplete && isHost;
        }

        if (hostOnlyText != null)
        {
            hostOnlyText.gameObject.SetActive(levelComplete && !isHost);
            hostOnlyText.text = "Waiting for host to return to lobby...";
        }
    }

    private void BackToLobbyButton()
    {
        bool isHost = !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;

        if (!isHost) return;
        if (!levelComplete) return;

        if (OurGameManager.Instance != null)
        {
            OurGameManager.Instance.CompleteLevelAndReturnToLobby(1);
        }
    }
}