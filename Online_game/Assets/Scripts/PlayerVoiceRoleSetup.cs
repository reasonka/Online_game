using Photon.Pun;
using Photon.Voice.Unity;
using UnityEngine;

public class PlayerVoiceRoleSetup : MonoBehaviourPun
{
    [Header("Role")]
    public int playerIndex;
    public string playerIndexPropertyKey = "CharacterIndex";

    [Header("Push To Talk")]
    public KeyCode pushToTalkKey = KeyCode.V;

    [Header("Photon Voice")]
    public Recorder recorder;
    public Speaker speaker;

    private bool isLocalPlayer;
    private bool isOrderTaker;
    private bool isDoodleBuddy;

    private void Start()
    {
        ReadPlayerIndex();

        isLocalPlayer =
            !PhotonNetwork.IsConnected ||
            photonView.IsMine;

        isOrderTaker = playerIndex == 0;
        isDoodleBuddy = playerIndex == 1;

        SetupVoice();
    }

    private void Update()
    {
        UpdatePushToTalk();
    }

    private void ReadPlayerIndex()
    {
        if (!PhotonNetwork.IsConnected || photonView == null || photonView.Owner == null)
            return;

        if (photonView.Owner.CustomProperties.TryGetValue(playerIndexPropertyKey, out object value))
            playerIndex = (int)value;
    }

    private void SetupVoice()
    {
        bool canSpeak = isLocalPlayer && isOrderTaker;
        bool canHear = isLocalPlayer && isDoodleBuddy;

        if (recorder != null)
        {
            recorder.RecordingEnabled = false;
            recorder.TransmitEnabled = false;
        }

        if (speaker != null)
        {
            speaker.enabled = canHear;
            speaker.gameObject.SetActive(canHear);
        }
    }

    private void UpdatePushToTalk()
    {
        if (recorder == null)
            return;

        bool canSpeak = isLocalPlayer && isOrderTaker;
        bool isPressingMicKey = Input.GetKey(pushToTalkKey);

        bool micOn = canSpeak && isPressingMicKey;

        recorder.RecordingEnabled = micOn;
        recorder.TransmitEnabled = micOn;
    }
}