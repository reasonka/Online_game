using Photon.Pun;
using UnityEngine;

public class DoodleBuddyOnlyUI : MonoBehaviour
{
    public string playerIndexPropertyKey = "CharacterIndex";

    private void Start()
    {
        bool showForDoodleBuddy = false;

        if (PhotonNetwork.LocalPlayer != null &&
            PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(playerIndexPropertyKey, out object value))
        {
            int localPlayerIndex = (int)value;
            showForDoodleBuddy = localPlayerIndex == 1;
        }

        gameObject.SetActive(showForDoodleBuddy);
    }
}