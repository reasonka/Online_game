using Photon.Pun;
using UnityEngine;

public class OrderTakerOnlyUI : MonoBehaviour
{
    public string playerIndexPropertyKey = "CharacterIndex";

    private void Start()
    {
        bool shouldShow = false;

        if (PhotonNetwork.LocalPlayer != null &&
            PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(playerIndexPropertyKey, out object value))
        {
            int localPlayerIndex = (int)value;
            shouldShow = localPlayerIndex == 0;
        }

        gameObject.SetActive(shouldShow);
    }
}