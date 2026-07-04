using Photon.Pun;
using Photon.Voice.Unity;
using UnityEngine;

public class PlayerVoiceRoleSetup : MonoBehaviourPun
{
    [Header("Role")]
    public int playerIndex;
    public string playerIndexPropertyKey = "CharacterIndex";

    [Header("Photon Voice")]
    public Recorder recorder;
    public Speaker speaker;

    private void Start()
    {
        ReadPlayerIndex();
        SetupVoice();
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
        bool isLocalPlayer = !PhotonNetwork.IsConnected || photonView.IsMine;

        bool isOrderTaker = playerIndex == 0;
        bool isDoodleBuddy = playerIndex == 1;

        bool canSpeak = isLocalPlayer && isOrderTaker;
        bool canHear = isLocalPlayer && isDoodleBuddy;

        if (recorder != null)
        {
            recorder.RecordingEnabled = canSpeak;
            recorder.TransmitEnabled = canSpeak;
        }

        if (speaker != null)
        {
            speaker.enabled = canHear;
            speaker.gameObject.SetActive(canHear);
        }
    }
}