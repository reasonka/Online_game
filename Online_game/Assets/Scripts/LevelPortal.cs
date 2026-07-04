using UnityEngine;
using Photon.Pun;

public class LevelPortal : MonoBehaviour
{
    [Header("Portal Settings")]
    public string sceneToLoad = "Level1";

    [Tooltip("0 = always unlocked. 1 = unlocks after Level 1 is complete.")]
    public int requiredCompletedLevel = 0;

    [Header("Player Detection")]
    public string playerTag = "Player";

    [Header("Portal Objects")]
    public Collider portalTrigger;
    public GameObject[] lightsWhenUnlocked;
    public GameObject[] objectsWhenLocked;

    private bool isUnlocked = false;

    private void Start()
    {
        RefreshPortal();
    }

    public void RefreshPortal()
    {
        if (OurGameManager.Instance == null)
        {
            isUnlocked = requiredCompletedLevel <= 0;
        }
        else
        {
            isUnlocked = OurGameManager.Instance.IsLevelUnlocked(requiredCompletedLevel);
        }

        if (portalTrigger != null)
        {
            portalTrigger.enabled = isUnlocked;
        }

        foreach (GameObject obj in lightsWhenUnlocked)
        {
            if (obj != null)
            {
                obj.SetActive(isUnlocked);
            }
        }

        foreach (GameObject obj in objectsWhenLocked)
        {
            if (obj != null)
            {
                obj.SetActive(!isUnlocked);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isUnlocked) return;
        if (!other.CompareTag(playerTag)) return;

        PhotonView playerPhotonView = other.GetComponentInParent<PhotonView>();

        if (PhotonNetwork.InRoom)
        {
            if (playerPhotonView == null) return;
            if (!playerPhotonView.IsMine) return;
        }

        if (OurGameManager.Instance == null) return;

        // Only host loads the scene for everyone
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        OurGameManager.Instance.LoadLevelForEveryone(sceneToLoad);
    }
}