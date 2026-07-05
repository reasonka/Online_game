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
            micButton.onClick.AddListener(ToggleMic);

        FindLocalOrderTakerVoiceSetup();
        SetMicVisual(false);
    }

    private void ToggleMic()
    {
        micOn = !micOn;
        SetMicVisual(micOn);

        if (localVoiceSetup == null)
            FindLocalOrderTakerVoiceSetup();

        if (localVoiceSetup != null)
            localVoiceSetup.SetMicButton(micOn);
    }

    private void SetMicVisual(bool isOn)
    {
        if (micOffImage != null)
            micOffImage.SetActive(!isOn);

        if (micOnImage != null)
            micOnImage.SetActive(isOn);
    }

    private void FindLocalOrderTakerVoiceSetup()
    {
        PlayerVoiceRoleSetup[] voiceSetups = FindObjectsOfType<PlayerVoiceRoleSetup>();

        foreach (PlayerVoiceRoleSetup setup in voiceSetups)
        {
            PhotonView view = setup.GetComponent<PhotonView>();

            if (view != null && view.IsMine && setup.playerIndex == 0)
            {
                localVoiceSetup = setup;
                return;
            }
        }
    }
}