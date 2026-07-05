using System.Collections;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public enum HistoryPerformanceOutcome
{
    None,
    Happy,
    Angry,
    Dead
}

public enum LevelCompletionMode
{
    ServedCustomerCount,
    Timer
}

public class LevelPerformanceSnapshot
{
    public int totalServed;
    public int correctServed;
    public int deathServed;
}

public class LevelPerformanceTracker : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static LevelPerformanceTracker Instance;

    private const byte CustomerReactionEventCode = 81;
    private const byte LevelCompleteEventCode = 82;

    [Header("Level Info")]
    public int levelNumber = 1;

    [Header("Completion Mode")]
    public LevelCompletionMode completionMode = LevelCompletionMode.ServedCustomerCount;

    [Header("Served Count Completion")]
    public int requiredServedToComplete = 5;

    [Header("Timer Completion")]
    public float levelDurationSeconds = 300f; // 5 minutes
    public TMP_Text timerText;
    public bool startTimerAutomatically = true;

    [Header("Completion UI")]
    public float delayBeforeShowingCompleteUI = 0.5f;
    public LevelCompleteUI levelCompleteUI;

    [Header("Stats")]
    [SerializeField] private int totalServed = 0;
    [SerializeField] private int correctServed = 0;
    [SerializeField] private int deathServed = 0;

    public int TotalServed => totalServed;
    public int CorrectServed => correctServed;
    public int DeathServed => deathServed;

    private bool levelCompleteTriggered = false;
    private bool timerRunning = false;
    private float timerRemaining;

    private void Awake()
    {
        Instance = this;

        if (levelCompleteUI == null)
        {
            levelCompleteUI = FindObjectOfType<LevelCompleteUI>(true);
        }

        timerRemaining = levelDurationSeconds;
        UpdateTimerUI();
    }

    private void Start()
    {
        if (completionMode == LevelCompletionMode.Timer && startTimerAutomatically)
        {
            StartLevelTimer();
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (completionMode != LevelCompletionMode.Timer)
        {
            return;
        }

        if (!timerRunning)
        {
            return;
        }

        if (levelCompleteTriggered)
        {
            return;
        }

        timerRemaining -= Time.deltaTime;

        if (timerRemaining < 0f)
        {
            timerRemaining = 0f;
        }

        UpdateTimerUI();

        if (timerRemaining <= 0f)
        {
            timerRunning = false;
            TriggerLevelComplete();
        }
    }

    public void StartLevelTimer()
    {
        timerRemaining = levelDurationSeconds;
        timerRunning = true;
        UpdateTimerUI();

        Debug.Log("Level " + levelNumber + " timer started: " + levelDurationSeconds + " seconds.");
    }

    public void StopLevelTimer()
    {
        timerRunning = false;
    }

    public void ReportCustomerReaction(CustomerReactionType reaction, float delay)
    {
        StartCoroutine(ReportCustomerReactionRoutine(reaction, delay));
    }

    private IEnumerator ReportCustomerReactionRoutine(CustomerReactionType reaction, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
        {
            RaiseEventOptions options = new RaiseEventOptions
            {
                Receivers = ReceiverGroup.MasterClient
            };

            SendOptions sendOptions = new SendOptions
            {
                Reliability = true
            };

            PhotonNetwork.RaiseEvent(CustomerReactionEventCode, (int)reaction, options, sendOptions);
        }
        else
        {
            CountReaction(reaction);
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == CustomerReactionEventCode)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            CustomerReactionType reaction = (CustomerReactionType)(int)photonEvent.CustomData;
            CountReaction(reaction);
            return;
        }

        if (photonEvent.Code == LevelCompleteEventCode)
        {
            int completedLevelNumber = (int)photonEvent.CustomData;
            ShowLevelCompleteUI(completedLevelNumber);
            return;
        }
    }

    private void CountReaction(CustomerReactionType reaction)
    {
        if (reaction == CustomerReactionType.None)
        {
            return;
        }

        totalServed++;

        if (reaction == CustomerReactionType.Reaction1)
        {
            correctServed++;
        }
        else if (reaction == CustomerReactionType.Reaction2)
        {
            deathServed++;
        }

        Debug.Log($"Level {levelNumber} stats: Total={totalServed}, Correct={correctServed}, Deaths={deathServed}");

        if (completionMode == LevelCompletionMode.ServedCustomerCount)
        {
            CheckServedCountCompletion();
        }
    }

    private void CheckServedCountCompletion()
    {
        if (levelCompleteTriggered)
        {
            return;
        }

        if (requiredServedToComplete <= 0)
        {
            Debug.LogWarning("LevelPerformanceTracker: requiredServedToComplete is 0 or less.");
            return;
        }

        if (totalServed >= requiredServedToComplete)
        {
            TriggerLevelComplete();
        }
    }

    private void TriggerLevelComplete()
    {
        if (levelCompleteTriggered)
        {
            return;
        }

        levelCompleteTriggered = true;
        timerRunning = false;

        StartCoroutine(TriggerLevelCompleteRoutine());
    }

    private IEnumerator TriggerLevelCompleteRoutine()
    {
        yield return new WaitForSeconds(delayBeforeShowingCompleteUI);

        if (PhotonNetwork.InRoom)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                yield break;
            }

            RaiseEventOptions options = new RaiseEventOptions
            {
                Receivers = ReceiverGroup.All
            };

            SendOptions sendOptions = new SendOptions
            {
                Reliability = true
            };

            PhotonNetwork.RaiseEvent(LevelCompleteEventCode, levelNumber, options, sendOptions);
        }
        else
        {
            ShowLevelCompleteUI(levelNumber);
        }
    }

    private void ShowLevelCompleteUI(int completedLevelNumber)
    {
        if (levelCompleteUI == null)
        {
            levelCompleteUI = FindObjectOfType<LevelCompleteUI>(true);
        }

        if (levelCompleteUI != null)
        {
            levelCompleteUI.ShowLevelComplete(completedLevelNumber);
        }
        else
        {
            Debug.LogWarning("No LevelCompleteUI found in this scene.");
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null)
        {
            return;
        }

        int minutes = Mathf.FloorToInt(timerRemaining / 60f);
        int seconds = Mathf.FloorToInt(timerRemaining % 60f);

        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    public LevelPerformanceSnapshot GetSnapshot()
    {
        return new LevelPerformanceSnapshot
        {
            totalServed = totalServed,
            correctServed = correctServed,
            deathServed = deathServed
        };
    }

    public HistoryPerformanceOutcome GetOutcome()
    {
        return CalculateOutcome(totalServed, correctServed, deathServed);
    }

    public static HistoryPerformanceOutcome CalculateOutcome(int total, int correct, int deaths)
    {
        if (total <= 0)
        {
            return HistoryPerformanceOutcome.None;
        }

        if (deaths > 0)
        {
            return HistoryPerformanceOutcome.Dead;
        }

        float correctRatio = (float)correct / total;

        if (correctRatio >= 0.5f)
        {
            return HistoryPerformanceOutcome.Happy;
        }

        return HistoryPerformanceOutcome.Angry;
    }
}