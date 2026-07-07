using System.Collections;
using Photon.Pun;
using Photon.Voice.Unity;
using UnityEngine;

public class PlayerVoiceRoleSetup : MonoBehaviourPun
{
    [Header("Player Role")]
    public int playerIndex;
    public string playerIndexPropertyKey = "CharacterIndex";

    [Header("Voice Components")]
    public Recorder recorder;
    public Speaker speaker;

    [Header("Debug")]
    public bool showDebugLog = true;

    private int localPlayerIndex = -1;
    private bool micButtonOn = false;

    private void Start()
    {
        if (recorder == null)
            recorder = GetComponent<Recorder>();

        if (speaker == null)
            speaker = GetComponent<Speaker>();

        StartCoroutine(SetupVoiceRoutine());
    }

    private IEnumerator SetupVoiceRoutine()
    {
        // Wait for Photon custom properties and remote player objects.
        yield return null;
        yield return new WaitForSeconds(0.3f);

        ReadThisPlayerIndex();
        ReadLocalPlayerIndex();
        SetupVoice();
    }

    private void Update()
    {
        UpdateMic();
    }

    private void ReadThisPlayerIndex()
    {
        if (photonView != null &&
            photonView.Owner != null &&
            photonView.Owner.CustomProperties.TryGetValue(playerIndexPropertyKey, out object value))
        {
            playerIndex = (int)value;
        }
    }

    private void ReadLocalPlayerIndex()
    {
        if (PhotonNetwork.LocalPlayer != null &&
            PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(playerIndexPropertyKey, out object value))
        {
            localPlayerIndex = (int)value;
        }
    }

    private void SetupVoice()
    {
        bool thisPlayerCanSpeak =
            playerIndex == 0 ||
            playerIndex == 2;

        bool localPlayerIsDoodle =
            localPlayerIndex == 1;

        bool thisIsRemoteSpeaker =
            !photonView.IsMine &&
            thisPlayerCanSpeak;

        // Only Order Taker and Chef can transmit.
        if (recorder != null)
        {
            bool thisIsLocalSpeaker =
                photonView.IsMine &&
                thisPlayerCanSpeak;

            recorder.RecordingEnabled = false;
            recorder.TransmitEnabled = false;
            recorder.enabled = thisIsLocalSpeaker;
        }

        // Only Doodle can hear remote Order Taker and remote Chef.
        if (speaker != null)
        {
            speaker.enabled =
                localPlayerIsDoodle &&
                thisIsRemoteSpeaker;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "[VoiceRole] Object=" + gameObject.name +
                " | IsMine=" + photonView.IsMine +
                " | this playerIndex=" + playerIndex +
                " | localPlayerIndex=" + localPlayerIndex +
                " | recorder=" + (recorder != null && recorder.enabled) +
                " | speaker=" + (speaker != null && speaker.enabled)
            );
        }
    }

    private void UpdateMic()
    {
        if (recorder == null)
            return;

        bool thisIsLocalSpeaker =
            photonView.IsMine &&
            (playerIndex == 0 || playerIndex == 2);

        bool shouldTransmit =
            thisIsLocalSpeaker &&
            micButtonOn;

        recorder.RecordingEnabled = shouldTransmit;
        recorder.TransmitEnabled = shouldTransmit;
    }

    public void SetMicButton(bool isOn)
    {
        micButtonOn = isOn;
        UpdateMic();
    }

    public bool IsMicButtonOn()
    {
        return micButtonOn;
    }
}