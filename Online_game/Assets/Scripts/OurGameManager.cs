using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class OurGameManager : MonoBehaviourPunCallbacks
{
    public static OurGameManager Instance;

    [Header("Scene Names")]
    public string lobbySceneName = "Lobby";
    public string level1SceneName = "Level1";
    public string level2SceneName = "Level2";

    private const string Level1CompleteKey = "Level1Complete";

    private bool isLoading = false;

    // For testing without Photon
    private bool localLevel1Complete = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        InitializeRoomProgress();
        RefreshAllPortals();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isLoading = false;

        InitializeRoomProgress();
        RefreshAllPortals();
    }

    private void InitializeRoomProgress()
    {
        if (!PhotonNetwork.InRoom) return;
        if (!PhotonNetwork.IsMasterClient) return;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(Level1CompleteKey))
        {
            Hashtable props = new Hashtable
            {
                { Level1CompleteKey, false }
            };

            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    public bool IsLevel1Complete()
    {
        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(Level1CompleteKey, out object value))
            {
                return value is bool completed && completed;
            }

            return false;
        }

        return localLevel1Complete;
    }

    public bool IsLevelUnlocked(int requiredCompletedLevel)
    {
        // Level 1 portal is always unlocked
        if (requiredCompletedLevel <= 0)
        {
            return true;
        }

        // Level 2 portal unlocks after Level 1
        if (requiredCompletedLevel == 1)
        {
            return IsLevel1Complete();
        }

        return false;
    }

    public void LoadLevelForEveryone(string sceneName)
    {
        if (isLoading) return;

        if (!PhotonNetwork.InRoom)
        {
            isLoading = true;
            SceneManager.LoadScene(sceneName);
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only the Master Client should load scenes.");
            return;
        }

        isLoading = true;
        PhotonNetwork.LoadLevel(sceneName);
    }

    public void CompleteLevelAndReturnToLobby(int completedLevelNumber)
    {
        if (isLoading) return;

        if (!PhotonNetwork.InRoom)
        {
            if (completedLevelNumber == 1)
            {
                localLevel1Complete = true;
            }

            isLoading = true;
            SceneManager.LoadScene(lobbySceneName);
            return;
        }

        // Important: only host can press this button
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only the host can return everyone to the lobby.");
            return;
        }

        if (completedLevelNumber == 1)
        {
            Hashtable props = new Hashtable
            {
                { Level1CompleteKey, true }
            };

            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        isLoading = true;
        PhotonNetwork.LoadLevel(lobbySceneName);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        RefreshAllPortals();
    }

    public void RefreshAllPortals()
    {
        LevelPortal[] portals = FindObjectsOfType<LevelPortal>(true);

        foreach (LevelPortal portal in portals)
        {
            portal.RefreshPortal();
        }
    }
}