using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class LevelHistoryData
{
    public bool completed;
    public int totalServed;
    public int correctServed;
    public int deathServed;
    public string playerNames;
    public HistoryPerformanceOutcome outcome;
}

public class OurGameManager : MonoBehaviourPunCallbacks
{
    public static OurGameManager Instance;

    [Header("Scene Names")]
    public string lobbySceneName = "Lobby";
    public string level1SceneName = "Level1";
    public string level2SceneName = "Level2";

    private const string Level1CompleteKey = "Level1Complete";
    private const string Level2CompleteKey = "Level2Complete";

    private bool isLoading = false;

    private bool localLevel1Complete = false;
    private bool localLevel2Complete = false;

    private readonly Dictionary<string, object> localProgress = new Dictionary<string, object>();

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

        Hashtable props = new Hashtable();

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(Level1CompleteKey))
        {
            props.Add(Level1CompleteKey, false);
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(Level2CompleteKey))
        {
            props.Add(Level2CompleteKey, false);
        }

        if (props.Count > 0)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    public bool IsLevel1Complete()
    {
        if (TryGetProgressValue(Level1CompleteKey, out object value))
        {
            return value is bool completed && completed;
        }

        return localLevel1Complete;
    }

    public bool IsLevel2Complete()
    {
        if (TryGetProgressValue(Level2CompleteKey, out object value))
        {
            return value is bool completed && completed;
        }

        return localLevel2Complete;
    }

    public bool IsLevelUnlocked(int requiredCompletedLevel)
    {
        if (requiredCompletedLevel <= 0)
        {
            return true;
        }

        if (requiredCompletedLevel == 1)
        {
            return IsLevel1Complete();
        }

        if (requiredCompletedLevel == 2)
        {
            return IsLevel2Complete();
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
            SaveLevelHistoryResultLocal(completedLevelNumber);

            if (completedLevelNumber == 1)
            {
                localLevel1Complete = true;
            }
            else if (completedLevelNumber == 2)
            {
                localLevel2Complete = true;
            }

            isLoading = true;
            SceneManager.LoadScene(lobbySceneName);
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only the host can return everyone to the lobby.");
            return;
        }

        SaveLevelHistoryResultToRoom(completedLevelNumber);

        isLoading = true;
        PhotonNetwork.LoadLevel(lobbySceneName);
    }

    private void SaveLevelHistoryResultToRoom(int levelNumber)
    {
        LevelPerformanceSnapshot snapshot = GetCurrentLevelSnapshot();

        HistoryPerformanceOutcome outcome = LevelPerformanceTracker.CalculateOutcome(
            snapshot.totalServed,
            snapshot.correctServed,
            snapshot.deathServed
        );

        Hashtable props = new Hashtable
        {
            { GetLevelCompleteKey(levelNumber), true },
            { GetLevelKey(levelNumber, "TotalServed"), snapshot.totalServed },
            { GetLevelKey(levelNumber, "CorrectServed"), snapshot.correctServed },
            { GetLevelKey(levelNumber, "DeathServed"), snapshot.deathServed },
            { GetLevelKey(levelNumber, "Outcome"), outcome.ToString() },
            { GetLevelKey(levelNumber, "PlayerNames"), GetPhotonPlayerNames() }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        Debug.Log($"Saved Level {levelNumber} history result: {outcome}");
    }

    private void SaveLevelHistoryResultLocal(int levelNumber)
    {
        LevelPerformanceSnapshot snapshot = GetCurrentLevelSnapshot();

        HistoryPerformanceOutcome outcome = LevelPerformanceTracker.CalculateOutcome(
            snapshot.totalServed,
            snapshot.correctServed,
            snapshot.deathServed
        );

        SetLocalProgressValue(GetLevelCompleteKey(levelNumber), true);
        SetLocalProgressValue(GetLevelKey(levelNumber, "TotalServed"), snapshot.totalServed);
        SetLocalProgressValue(GetLevelKey(levelNumber, "CorrectServed"), snapshot.correctServed);
        SetLocalProgressValue(GetLevelKey(levelNumber, "DeathServed"), snapshot.deathServed);
        SetLocalProgressValue(GetLevelKey(levelNumber, "Outcome"), outcome.ToString());
        SetLocalProgressValue(GetLevelKey(levelNumber, "PlayerNames"), "Local Player");

        Debug.Log($"Saved local Level {levelNumber} history result: {outcome}");
    }

    private LevelPerformanceSnapshot GetCurrentLevelSnapshot()
    {
        if (LevelPerformanceTracker.Instance != null)
        {
            return LevelPerformanceTracker.Instance.GetSnapshot();
        }

        return new LevelPerformanceSnapshot
        {
            totalServed = 0,
            correctServed = 0,
            deathServed = 0
        };
    }

    public LevelHistoryData GetLevelHistoryData(int levelNumber)
    {
        LevelHistoryData data = new LevelHistoryData();

        if (TryGetProgressValue(GetLevelCompleteKey(levelNumber), out object completedValue))
        {
            data.completed = completedValue is bool completed && completed;
        }

        if (TryGetProgressValue(GetLevelKey(levelNumber, "TotalServed"), out object totalValue))
        {
            data.totalServed = totalValue is int total ? total : 0;
        }

        if (TryGetProgressValue(GetLevelKey(levelNumber, "CorrectServed"), out object correctValue))
        {
            data.correctServed = correctValue is int correct ? correct : 0;
        }

        if (TryGetProgressValue(GetLevelKey(levelNumber, "DeathServed"), out object deathValue))
        {
            data.deathServed = deathValue is int deaths ? deaths : 0;
        }

        if (TryGetProgressValue(GetLevelKey(levelNumber, "PlayerNames"), out object namesValue))
        {
            data.playerNames = namesValue as string;
        }

        if (string.IsNullOrWhiteSpace(data.playerNames))
        {
            data.playerNames = "Unknown Chefs";
        }

        data.outcome = HistoryPerformanceOutcome.None;

        if (TryGetProgressValue(GetLevelKey(levelNumber, "Outcome"), out object outcomeValue))
        {
            string outcomeString = outcomeValue as string;

            if (!string.IsNullOrWhiteSpace(outcomeString))
            {
                System.Enum.TryParse(outcomeString, out data.outcome);
            }
        }

        return data;
    }

    private string GetPhotonPlayerNames()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.PlayerList == null || PhotonNetwork.PlayerList.Length == 0)
        {
            return "Local Player";
        }

        List<string> names = new List<string>();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!string.IsNullOrWhiteSpace(player.NickName))
            {
                names.Add(player.NickName);
            }
            else
            {
                names.Add("Player " + player.ActorNumber);
            }
        }

        return string.Join(" & ", names);
    }

    private string GetLevelCompleteKey(int levelNumber)
    {
        if (levelNumber == 1) return Level1CompleteKey;
        if (levelNumber == 2) return Level2CompleteKey;

        return "Level" + levelNumber + "Complete";
    }

    private string GetLevelKey(int levelNumber, string suffix)
    {
        return "Level" + levelNumber + suffix;
    }

    private bool TryGetProgressValue(string key, out object value)
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out value))
            {
                return true;
            }
        }

        return localProgress.TryGetValue(key, out value);
    }

    private void SetLocalProgressValue(string key, object value)
    {
        if (localProgress.ContainsKey(key))
        {
            localProgress[key] = value;
        }
        else
        {
            localProgress.Add(key, value);
        }
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