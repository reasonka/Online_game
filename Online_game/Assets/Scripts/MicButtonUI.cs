using UnityEngine;
using UnityEngine.EventSystems;

public class MicButtonUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private PlayerVoiceRoleSetup localVoiceSetup;

    private void Start()
    {
        FindLocalVoiceSetup();
    }

    private void FindLocalVoiceSetup()
    {
        PlayerVoiceRoleSetup[] voiceSetups =
            FindObjectsOfType<PlayerVoiceRoleSetup>();

        foreach (PlayerVoiceRoleSetup setup in voiceSetups)
        {
            if (setup.GetComponent<Photon.Pun.PhotonView>() != null &&
                setup.GetComponent<Photon.Pun.PhotonView>().IsMine &&
                setup.playerIndex == 0)
            {
                localVoiceSetup = setup;
                break;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (localVoiceSetup == null)
            FindLocalVoiceSetup();

        if (localVoiceSetup != null)
            localVoiceSetup.PressMicButton();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (localVoiceSetup != null)
            localVoiceSetup.ReleaseMicButton();
    }
}