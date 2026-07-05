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

    private int localPlayerIndex = -1;
    private bool micButtonOn = false;

    private void Start()
    {
        ReadThisPlayerIndex();
        ReadLocalPlayerIndex();

        if (recorder == null)
            recorder = GetComponent<Recorder>();

        if (speaker == null)
            speaker = GetComponent<Speaker>();

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

        bool localPlayerCanHear =
            localPlayerIndex == 1;

        bool thisIsRemoteSpeaker =
            !photonView.IsMine &&
            thisPlayerCanSpeak;

        if (recorder != null)
        {
            recorder.RecordingEnabled = false;
            recorder.TransmitEnabled = false;
        }

        if (speaker != null)
        {
            speaker.enabled =
                localPlayerCanHear &&
                thisIsRemoteSpeaker;
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
    }

    public bool IsMicButtonOn()
    {
        return micButtonOn;
    }
}