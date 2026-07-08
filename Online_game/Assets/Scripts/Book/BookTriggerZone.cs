using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Collider))]
public class BookTriggerZone : MonoBehaviour
{
    [Header("Book")]
    public BookOpener bookOpener;

    [Header("Player Tags")]
    public string playerTag = "Player";
    public string playerOtherTag = "PlayerOther";

    [Header("Options")]
    public bool closeBookWhenPlayerLeaves = false;

    private readonly Dictionary<GameObject, int> localPlayerTriggerCounts = new Dictionary<GameObject, int>();

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

        RegisterLocalPlayerInside(playerObject);

        if (bookOpener == null)
        {
            Debug.LogWarning("BookTriggerZone: BookOpener reference is not assigned.");
            return;
        }

        bookOpener.OpenBook(playerObject);
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject playerObject = GetPlayerRoot(other);

        if (playerObject == null)
            return;

        if (!IsLocalPlayer(playerObject))
            return;

        UnregisterLocalPlayerInside(playerObject);

        if (!closeBookWhenPlayerLeaves)
            return;

        if (IsLocalPlayerStillInside(playerObject))
            return;

        if (bookOpener != null)
        {
            bookOpener.CloseBook();
        }
    }

    private void RegisterLocalPlayerInside(GameObject playerObject)
    {
        if (playerObject == null)
            return;

        if (!localPlayerTriggerCounts.ContainsKey(playerObject))
        {
            localPlayerTriggerCounts.Add(playerObject, 0);
        }

        localPlayerTriggerCounts[playerObject]++;
    }

    private void UnregisterLocalPlayerInside(GameObject playerObject)
    {
        if (playerObject == null)
            return;

        if (!localPlayerTriggerCounts.ContainsKey(playerObject))
            return;

        localPlayerTriggerCounts[playerObject]--;

        if (localPlayerTriggerCounts[playerObject] <= 0)
        {
            localPlayerTriggerCounts.Remove(playerObject);
        }
    }

    private bool IsLocalPlayerStillInside(GameObject playerObject)
    {
        return localPlayerTriggerCounts.ContainsKey(playerObject);
    }

    private GameObject GetPlayerRoot(Collider other)
    {
        if (other == null)
            return null;

        Transform current = other.transform;

        while (current != null)
        {
            if (IsAllowedPlayerTag(current.gameObject))
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return null;
    }

    private bool IsAllowedPlayerTag(GameObject obj)
    {
        if (obj == null)
            return false;

        return obj.CompareTag(playerTag) || obj.CompareTag(playerOtherTag);
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