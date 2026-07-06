using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class MicToggleButtonUI : MonoBehaviour
{
    public Button micButton;
    public GameObject micOffImage;
    public GameObject micOnImage;

    private PlayerVoiceRoleSetup localVoiceSetup;
    private bool micOn = false;

    private void Awake()
    {
        if (micButton == null)
            micButton = GetComponent<Button>();

        SetMicVisual(false);
    }

    private void Start()
    {
        if (micButton != null)
            micButton.onClick.AddListener(ClickMicButton);

        FindLocalSpeakerVoiceSetup();
        TurnMicOff();
    }

    private void ClickMicButton()
    {
        // Mic button only turns OFF.
        // It cannot turn ON directly anymore.
        if (micOn)
            TurnMicOff();
    }

    public void TurnMicOn()
    {
        micOn = true;
        SetMicVisual(true);

        if (localVoiceSetup == null)
            FindLocalSpeakerVoiceSetup();

        if (localVoiceSetup != null)
            localVoiceSetup.SetMicButton(true);
    }

    public void TurnMicOff()
    {
        micOn = false;
        SetMicVisual(false);

        if (localVoiceSetup == null)
            FindLocalSpeakerVoiceSetup();

        if (localVoiceSetup != null)
            localVoiceSetup.SetMicButton(false);
    }

    private void SetMicVisual(bool isOn)
    {
        if (micOffImage != null)
            micOffImage.SetActive(!isOn);

        if (micOnImage != null)
            micOnImage.SetActive(isOn);
    }

    private void FindLocalSpeakerVoiceSetup()
    {
        PlayerVoiceRoleSetup[] voiceSetups =
            FindObjectsOfType<PlayerVoiceRoleSetup>();

        foreach (PlayerVoiceRoleSetup setup in voiceSetups)
        {
            PhotonView view = setup.GetComponent<PhotonView>();

            if (view == null)
                continue;

            bool isLocalPlayer = view.IsMine;

            bool canSpeak =
                setup.playerIndex == 0 ||
                setup.playerIndex == 2;

            if (isLocalPlayer && canSpeak)
            {
                localVoiceSetup = setup;
                return;
            }
        }
    }
}