using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PortalLevelLoader : MonoBehaviour, IOnEventCallback
{
    [Header("Scene To Load")]
    public string sceneToLoad = "Level1";
    // For your second portal, change this to "Level2" in the Inspector.

    [Header("Player Detection")]
    public string playerTag = "Player";

    private const byte PortalLoadEventCode = 44;
    private static bool isLoading = false;

    private void Start()
    {
        // Important: makes all players follow the Master Client's scene load
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLoading) return;

        if (!other.CompareTag(playerTag)) return;

        PhotonView playerPhotonView = other.GetComponentInParent<PhotonView>();

        if (playerPhotonView == null) return;

        // Only the player who actually owns this character should trigger the portal
        if (!playerPhotonView.IsMine) return;

        if (PhotonNetwork.InRoom)
        {
            RequestLevelLoad(sceneToLoad);
        }
        else
        {
            // For testing outside Photon
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void RequestLevelLoad(string levelName)
    {
        RaiseEventOptions eventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.MasterClient
        };

        SendOptions sendOptions = new SendOptions
        {
            Reliability = true
        };

        PhotonNetwork.RaiseEvent(
            PortalLoadEventCode,
            levelName,
            eventOptions,
            sendOptions
        );
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != PortalLoadEventCode) return;

        if (!PhotonNetwork.IsMasterClient) return;

        if (isLoading) return;

        string levelName = photonEvent.CustomData as string;

        if (string.IsNullOrEmpty(levelName)) return;

        isLoading = true;

        // Optional: prevent new players from joining while loading
        if (PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }

        PhotonNetwork.LoadLevel(levelName);
    }
}