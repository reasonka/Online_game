using Photon.Pun;
using Photon.Voice.Unity;
using UnityEngine;

public class PhotonVoiceDebugLogger : MonoBehaviourPun
{
    [Header("Role Info")]
    public string playerIndexPropertyKey = "CharacterIndex";

    [Header("Voice Components")]
    public Recorder recorder;
    public Speaker speaker;
    public AudioSource speakerAudioSource;

    [Header("Debug")]
    public float logEverySeconds = 1f;
    public KeyCode instantLogKey = KeyCode.F8;

    private float timer;

    private void Awake()
    {
        if (recorder == null)
            recorder = GetComponentInChildren<Recorder>(true);

        if (speaker == null)
            speaker = GetComponentInChildren<Speaker>(true);

        if (speakerAudioSource == null && speaker != null)
            speakerAudioSource = speaker.GetComponent<AudioSource>();

        if (speakerAudioSource == null)
            speakerAudioSource = GetComponentInChildren<AudioSource>(true);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= logEverySeconds || Input.GetKeyDown(instantLogKey))
        {
            timer = 0f;
            PrintVoiceDebug();
        }
    }

    private void PrintVoiceDebug()
    {
        int playerIndex = -1;

        if (photonView != null &&
            photonView.Owner != null &&
            photonView.Owner.CustomProperties.TryGetValue(playerIndexPropertyKey, out object value))
        {
            playerIndex = (int)value;
        }

        VoiceConnection voiceConnection = FindObjectOfType<VoiceConnection>();

        string recorderInfo = "No Recorder";
        if (recorder != null)
        {
            recorderInfo =
                "Recorder Found | RecordingEnabled: " + recorder.RecordingEnabled +
                " | TransmitEnabled: " + recorder.TransmitEnabled +
                " | IsActiveAndEnabled: " + recorder.isActiveAndEnabled;
        }

        string speakerInfo = "No Speaker";
        if (speaker != null)
        {
            speakerInfo =
                "Speaker Found | enabled: " + speaker.enabled +
                " | activeInHierarchy: " + speaker.gameObject.activeInHierarchy;
        }

        string audioSourceInfo = "No Speaker AudioSource";
        if (speakerAudioSource != null)
        {
            audioSourceInfo =
                "AudioSource Found | enabled: " + speakerAudioSource.enabled +
                " | mute: " + speakerAudioSource.mute +
                " | volume: " + speakerAudioSource.volume +
                " | spatialBlend: " + speakerAudioSource.spatialBlend +
                " | isPlaying: " + speakerAudioSource.isPlaying;
        }

        Debug.Log(
            "[VOICE DEBUG] " + gameObject.name +
            "\nIsMine: " + photonView.IsMine +
            "\nOwner: " + photonView.Owner.NickName +
            "\nPlayerIndex: " + playerIndex +
            "\nPhoton InRoom: " + PhotonNetwork.InRoom +
            "\nPhoton Room: " + (PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "None") +
            "\nVoiceConnection Found: " + (voiceConnection != null) +
            "\n" + recorderInfo +
            "\n" + speakerInfo +
            "\n" + audioSourceInfo
        );
    }
}