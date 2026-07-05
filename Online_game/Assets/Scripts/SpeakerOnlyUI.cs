using System.Collections;
using Photon.Pun;
using UnityEngine;

public class SpeakerOnlyUI : MonoBehaviour
{
    public string playerIndexPropertyKey = "CharacterIndex";

    private IEnumerator Start()
    {
        yield return null;
        yield return new WaitForSeconds(0.2f);

        bool shouldShow = false;

        if (PhotonNetwork.LocalPlayer != null &&
            PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(playerIndexPropertyKey, out object value))
        {
            int playerIndex = (int)value;

            shouldShow =
                playerIndex == 0 ||
                playerIndex == 2;
        }

        gameObject.SetActive(shouldShow);
    }
}