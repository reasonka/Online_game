using System.Collections;
using UnityEngine;
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

    [Header("Level Info")]
    public int levelNumber = 1;

    [Header("Stats")]
    [SerializeField] private int totalServed = 0;
    [SerializeField] private int correctServed = 0;
    [SerializeField] private int deathServed = 0;

    public int TotalServed => totalServed;
    public int CorrectServed => correctServed;
    public int DeathServed => deathServed;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);

        if (Instance == this)
        {
            Instance = null;
        }
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
        if (photonEvent.Code != CustomerReactionEventCode)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        CustomerReactionType reaction = (CustomerReactionType)(int)photonEvent.CustomData;
        CountReaction(reaction);
    }

    private void CountReaction(CustomerReactionType reaction)
    {
        if (reaction == CustomerReactionType.None)
            return;

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
            return HistoryPerformanceOutcome.None;

        // Death image has priority.
        if (deaths > 0)
            return HistoryPerformanceOutcome.Dead;

        float correctRatio = (float)correct / total;

        if (correctRatio >= 0.5f)
            return HistoryPerformanceOutcome.Happy;

        return HistoryPerformanceOutcome.Angry;
    }
}