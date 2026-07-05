using Photon.Pun;
using Photon.Voice.Unity;
using UnityEngine;

public class PlayerVoiceRoleSetup : MonoBehaviourPun
{
    public int playerIndex;
    public string playerIndexPropertyKey = "CharacterIndex";

    public Recorder recorder;
    public Speaker speaker;

    private int localPlayerIndex = -1;
    private bool micButtonHeld = false;

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
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(playerIndexPropertyKey, out object value))
            localPlayerIndex = (int)value;
    }

    private void SetupVoice()
    {
        bool thisIsOrderTaker = playerIndex == 0;
        bool localPlayerIsDoodle = localPlayerIndex == 1;

        if (recorder != null)
        {
            recorder.RecordingEnabled = false;
            recorder.TransmitEnabled = false;
        }

        if (speaker != null)
        {
            speaker.enabled = thisIsOrderTaker && localPlayerIsDoodle && !photonView.IsMine;
        }
    }

    private void UpdateMic()
    {
        if (recorder == null)
            return;

        bool thisIsLocalOrderTaker =
            photonView.IsMine &&
            playerIndex == 0;

        bool micOn =
            thisIsLocalOrderTaker &&
            micButtonHeld;

        recorder.RecordingEnabled = micOn;
        recorder.TransmitEnabled = micOn;
    }

    public void PressMicButton()
    {
        micButtonHeld = true;
    }

    public void ReleaseMicButton()
    {
        micButtonHeld = false;
    }
}