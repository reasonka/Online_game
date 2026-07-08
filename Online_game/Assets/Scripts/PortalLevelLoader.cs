using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PortalLevelLoader : MonoBehaviour, IOnEventCallback
{
    [Header("Scene To Load")]
    public string sceneToLoad = "Level1";

    [Header("Player Detection")]
    public string playerTag = "Player";
    public string playerOtherTag = "PlayerOther";

    [Header("Debug")]
    public bool showDebugLogs = true;

    private const byte PortalLoadEventCode = 44;
    private static bool isLoading = false;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void OnEnable()
    {
        // Reset when this scene loads again, otherwise static bool can stay true.
        isLoading = false;
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLoading)
            return;

        if (!IsPlayerTag(other))
            return;

        PhotonView playerPhotonView = other.GetComponentInParent<PhotonView>();

        if (playerPhotonView == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("Portal touched by player-tagged object, but no PhotonView was found.");

            return;
        }

        // Important:
        // Each client sees all players, including remote clones.
        // Only the owner of the player who entered should request the level load.
        if (PhotonNetwork.InRoom && !playerPhotonView.IsMine)
            return;

        if (showDebugLogs)
            Debug.Log("Portal triggered by local player. Requesting load: " + sceneToLoad);

        if (PhotonNetwork.InRoom)
        {
            RequestLevelLoad(sceneToLoad);
        }
        else
        {
            isLoading = true;
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private bool IsPlayerTag(Collider other)
    {
        if (other.CompareTag(playerTag) || other.CompareTag(playerOtherTag))
            return true;

        PhotonView view = other.GetComponentInParent<PhotonView>();

        if (view != null)
        {
            GameObject rootObject = view.gameObject;

            if (rootObject.CompareTag(playerTag) || rootObject.CompareTag(playerOtherTag))
                return true;
        }

        return false;
    }

    private void RequestLevelLoad(string levelName)
    {
        isLoading = true;

        // If the host is the one who entered, load immediately.
        if (PhotonNetwork.IsMasterClient)
        {
            LoadLevelAsMaster(levelName);
            return;
        }

        // If a non-host entered, ask the host to load the level.
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
        if (photonEvent.Code != PortalLoadEventCode)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        if (isLoading)
            return;

        string levelName = photonEvent.CustomData as string;

        if (string.IsNullOrEmpty(levelName))
            return;

        LoadLevelAsMaster(levelName);
    }

    private void LoadLevelAsMaster(string levelName)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        isLoading = true;

        if (showDebugLogs)
            Debug.Log("Master Client loading level for everyone: " + levelName);

        if (PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }

        PhotonNetwork.LoadLevel(levelName);
    }
}