using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Collider))]
public class BookTriggerZone : MonoBehaviour
{
    [Header("Book")]
    public BookOpener bookOpener;

    [Header("Player")]
    public string playerTag = "Player";

    [Header("Options")]
    public bool closeBookWhenPlayerLeaves = false;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject playerObject = GetPlayerRoot(other);

        if (playerObject == null)
            return;

        if (!IsLocalPlayer(playerObject))
            return;

        if (bookOpener == null)
        {
            Debug.LogWarning("BookTriggerZone: BookOpener reference is not assigned.");
            return;
        }

        bookOpener.OpenBook(playerObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!closeBookWhenPlayerLeaves)
            return;

        GameObject playerObject = GetPlayerRoot(other);

        if (playerObject == null)
            return;

        if (!IsLocalPlayer(playerObject))
            return;

        if (bookOpener != null)
        {
            bookOpener.CloseBook();
        }
    }

    private GameObject GetPlayerRoot(Collider other)
    {
        if (other == null)
            return null;

        Transform current = other.transform;

        while (current != null)
        {
            if (current.CompareTag(playerTag))
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return null;
    }

    private bool IsLocalPlayer(GameObject playerObject)
    {
        if (!PhotonNetwork.InRoom)
        {
            return true;
        }

        PhotonView photonView = playerObject.GetComponentInParent<PhotonView>();

        if (photonView == null)
        {
            Debug.LogWarning("BookTriggerZone: Player has no PhotonView. Ignoring in Photon mode.");
            return false;
        }

        return photonView.IsMine;
    }
}