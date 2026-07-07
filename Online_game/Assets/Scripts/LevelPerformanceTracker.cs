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
    public float levelDurationSeconds = 300f;
    public TMP_Text timerText;
    public TMP_Text chefTimerText;
    public bool startTimerAutomatically = true;

    [Header("Timer Audio")]
    public AudioSource audioSource;
    public AudioClip timerStartSound;
    public AudioClip minuteChangeSound;
    public AudioClip finalCountdownTickSound;
    public AudioClip timerEndSound;

    [Header("Timer Sound Rules")]
    public bool playRegularSecondTick = false;
    public AudioClip regularSecondTickSound;
    public int finalCountdownStartSeconds = 5;

    [Header("Timer Animation")]
    public bool animateTimer = true;

    [Tooltip("Small pulse when the minute changes, for example 05:00 to 04:59.")]
    public float minutePulseScale = 1.2f;

    [Tooltip("Bigger pulse for 5, 4, 3, 2, 1.")]
    public float finalCountdownPulseScale = 1.45f;

    public float pulseDuration = 0.25f;

    [Header("Final Countdown Text Color")]
    public bool changeColorDuringFinalCountdown = true;
    public Color finalCountdownColor = Color.red;

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

    private int lastDisplayedSecond = -1;
    private int lastDisplayedMinute = -1;

    private Vector3 originalTimerTextScale = Vector3.one;
    private Vector3 originalChefTimerTextScale = Vector3.one;

    private Color originalTimerTextColor = Color.white;
    private Color originalChefTimerTextColor = Color.white;

    private Coroutine timerPulseCoroutine;
    private Coroutine chefTimerPulseCoroutine;

    private void Awake()
    {
        Instance = this;

        if (levelCompleteUI == null)
        {
            levelCompleteUI = FindObjectOfType<LevelCompleteUI>(true);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        CacheOriginalTimerVisuals();

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

        HandleTimerSecondChanged();
        UpdateTimerUI();

        if (timerRemaining <= 0f)
        {
            timerRunning = false;
            PlaySound(timerEndSound);
            TriggerLevelComplete();
        }
    }

    public void StartLevelTimer()
    {
        timerRemaining = levelDurationSeconds;
        timerRunning = true;
        levelCompleteTriggered = false;

        lastDisplayedSecond = Mathf.CeilToInt(timerRemaining);
        lastDisplayedMinute = Mathf.FloorToInt(timerRemaining / 60f);

        ResetTimerVisuals();
        UpdateTimerUI();

        PlaySound(timerStartSound);

        Debug.Log("Level " + levelNumber + " timer started: " + levelDurationSeconds + " seconds.");
    }

    public void StopLevelTimer()
    {
        timerRunning = false;
    }

    private void HandleTimerSecondChanged()
    {
        int currentSecond = Mathf.CeilToInt(timerRemaining);

        if (currentSecond == lastDisplayedSecond)
        {
            return;
        }

        lastDisplayedSecond = currentSecond;

        int currentMinute = Mathf.FloorToInt(timerRemaining / 60f);

        bool minuteChanged = currentMinute < lastDisplayedMinute;
        bool isFinalCountdown = currentSecond > 0 && currentSecond <= finalCountdownStartSeconds;

        if (minuteChanged)
        {
            lastDisplayedMinute = currentMinute;

            PlaySound(minuteChangeSound);

            if (animateTimer)
            {
                PulseBothTimerTexts(minutePulseScale);
            }
        }

        if (isFinalCountdown)
        {
            PlaySound(finalCountdownTickSound);

            if (animateTimer)
            {
                PulseBothTimerTexts(finalCountdownPulseScale);
            }

            if (changeColorDuringFinalCountdown)
            {
                SetTimerTextColor(finalCountdownColor);
            }
        }
        else
        {
            if (playRegularSecondTick)
            {
                PlaySound(regularSecondTickSound);
            }
        }
    }

    private void PulseBothTimerTexts(float targetScale)
    {
        if (timerText != null)
        {
            if (timerPulseCoroutine != null)
            {
                StopCoroutine(timerPulseCoroutine);
            }

            timerPulseCoroutine = StartCoroutine(PulseTextRoutine(timerText.transform, originalTimerTextScale, targetScale));
        }

        if (chefTimerText != null)
        {
            if (chefTimerPulseCoroutine != null)
            {
                StopCoroutine(chefTimerPulseCoroutine);
            }

            chefTimerPulseCoroutine = StartCoroutine(PulseTextRoutine(chefTimerText.transform, originalChefTimerTextScale, targetScale));
        }
    }

    private IEnumerator PulseTextRoutine(Transform target, Vector3 originalScale, float targetScaleMultiplier)
    {
        if (target == null)
        {
            yield break;
        }

        float halfDuration = pulseDuration * 0.5f;
        Vector3 bigScale = originalScale * targetScaleMultiplier;

        float timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            t = SmoothStep(t);

            target.localScale = Vector3.Lerp(originalScale, bigScale, t);
            yield return null;
        }

        timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            t = SmoothStep(t);

            target.localScale = Vector3.Lerp(bigScale, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
    }

    private float SmoothStep(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private void CacheOriginalTimerVisuals()
    {
        if (timerText != null)
        {
            originalTimerTextScale = timerText.transform.localScale;
            originalTimerTextColor = timerText.color;
        }

        if (chefTimerText != null)
        {
            originalChefTimerTextScale = chefTimerText.transform.localScale;
            originalChefTimerTextColor = chefTimerText.color;
        }
    }

    private void ResetTimerVisuals()
    {
        if (timerText != null)
        {
            timerText.transform.localScale = originalTimerTextScale;
            timerText.color = originalTimerTextColor;
        }

        if (chefTimerText != null)
        {
            chefTimerText.transform.localScale = originalChefTimerTextScale;
            chefTimerText.color = originalChefTimerTextColor;
        }
    }

    private void SetTimerTextColor(Color color)
    {
        if (timerText != null)
        {
            timerText.color = color;
        }

        if (chefTimerText != null)
        {
            chefTimerText.color = color;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
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
        int minutes = Mathf.FloorToInt(timerRemaining / 60f);
        int seconds = Mathf.FloorToInt(timerRemaining % 60f);

        string timeText = minutes.ToString("00") + ":" + seconds.ToString("00");

        if (timerText != null)
        {
            timerText.text = timeText;
        }

        if (chefTimerText != null)
        {
            chefTimerText.text = timeText;
        }
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