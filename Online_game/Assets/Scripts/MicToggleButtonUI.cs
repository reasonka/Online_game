using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class MicToggleButtonUI : MonoBehaviour
{
    [Header("UI")]
    public Button micButton;
    public GameObject micOffImage;
    public GameObject micOnImage;

    [Header("Voice")]
    public string playerIndexPropertyKey = "CharacterIndex";

    private PlayerVoiceRoleSetup localVoiceSetup;
    private bool micOn = false;

    private void Start()
    {
        if (micButton == null)
            micButton = GetComponent<Button>();

        if (micButton != null)
            micButton.onClick.AddListener(ToggleMic);

        FindLocalOrderTakerVoiceSetup();
        SetMicVisual(false);
    }

    private void ToggleMic()
    {
        if (localVoiceSetup == null)
            FindLocalOrderTakerVoiceSetup();

        if (localVoiceSetup == null)
            return;

        micOn = !micOn;
        localVoiceSetup.SetMicButton(micOn);
        SetMicVisual(micOn);
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
            PhotonView photonView = setup.GetComponent<PhotonView>();

            if (photonView == null)
                continue;

            if (photonView.IsMine && setup.playerIndex == 0)
            {
                localVoiceSetup = setup;
                return;
            }
        }
    }
}