using UnityEngine;
using Photon.Pun;

public class PlayerInventory : MonoBehaviourPun
{
    [Header("Hand")]
    public Transform handPoint;

    [Header("Photon")]
    [Tooltip("目前单机测试时关闭。以后连接 Photon 后打开。")]
    public bool usePhotonSync = false;

    [Header("Debug")]
    public bool showDebugLog = true;

    private HoldableItem heldItem;

    public bool HasHeldItem => heldItem != null;
    public HoldableItem HeldItem => heldItem;

    private void Start()
    {
        if (handPoint == null)
        {
            Debug.LogWarning("[PlayerInventory] Hand Point is missing.", this);
        }
    }

    public bool CanUseLocalInput()
    {
        if (!usePhotonSync)
        {
            return true;
        }

        if (!PhotonNetwork.IsConnected)
        {
            return true;
        }

        return photonView.IsMine;
    }

    public bool TryHoldObject(GameObject targetObject)
    {
        if (!CanUseLocalInput())
        {
            return false;
        }

        if (targetObject == null)
        {
            LogWarning("TryHoldObject target is null.");
            return false;
        }

        if (HasHeldItem)
        {
            LogWarning("Player is already holding: " + heldItem.name);
            return false;
        }

        if (handPoint == null)
        {
            LogWarning("Hand Point is missing.");
            return false;
        }

        HoldableItem item = targetObject.GetComponent<HoldableItem>();

        if (item == null)
        {
            item = targetObject.GetComponentInParent<HoldableItem>();
        }

        if (item == null)
        {
            LogWarning("Object has no HoldableItem: " + targetObject.name);
            return false;
        }

        heldItem = item;

        heldItem.transform.SetParent(handPoint);
        heldItem.transform.localPosition = Vector3.zero;
        heldItem.transform.localRotation = Quaternion.identity;

        heldItem.SetHeldState(true);

        Log("Now holding: " + heldItem.name);
        return true;
    }

    public GameObject SpawnAndHold(GameObject prefab)
    {
        if (!CanUseLocalInput())
        {
            return null;
        }

        if (prefab == null)
        {
            LogWarning("Spawn prefab is null.");
            return null;
        }

        if (HasHeldItem)
        {
            LogWarning("Cannot spawn because hand is occupied.");
            return null;
        }

        if (handPoint == null)
        {
            LogWarning("Hand Point is missing.");
            return null;
        }

        GameObject spawnedObject;

        if (usePhotonSync && PhotonNetwork.IsConnected)
        {
            spawnedObject = PhotonNetwork.Instantiate(
                prefab.name,
                handPoint.position,
                handPoint.rotation
            );
        }
        else
        {
            spawnedObject = Instantiate(
                prefab,
                handPoint.position,
                handPoint.rotation
            );
        }

        if (spawnedObject == null)
        {
            LogWarning("Failed to spawn: " + prefab.name);
            return null;
        }

        TryHoldObject(spawnedObject);
        return spawnedObject;
    }

    public HoldableItem ReleaseHeldItem()
    {
        if (!HasHeldItem)
        {
            return null;
        }

        HoldableItem releasedItem = heldItem;
        heldItem = null;

        releasedItem.transform.SetParent(null);

        Log("Released held item: " + releasedItem.name);
        return releasedItem;
    }

    public bool PlaceHeldItem(
        Transform targetPoint,
        Transform newParent,
        bool keepFixed)
    {
        if (!HasHeldItem)
        {
            LogWarning("No held item to place.");
            return false;
        }

        if (targetPoint == null)
        {
            LogWarning("Target point is null.");
            return false;
        }

        HoldableItem item = heldItem;
        heldItem = null;

        item.transform.SetParent(newParent);
        item.transform.SetPositionAndRotation(
            targetPoint.position,
            targetPoint.rotation
        );

        item.SetPlacedState(keepFixed);

        Log("Placed item: " + item.name);
        return true;
    }

    public void ConsumeHeldItem()
    {
        if (!HasHeldItem)
        {
            return;
        }

        GameObject objectToDestroy = heldItem.gameObject;

        Log("Consuming held item: " + objectToDestroy.name);

        heldItem = null;

        if (usePhotonSync && PhotonNetwork.IsConnected)
        {
            PhotonView itemView = objectToDestroy.GetComponent<PhotonView>();

            if (itemView == null)
            {
                itemView = objectToDestroy.GetComponentInParent<PhotonView>();
            }

            if (itemView != null && itemView.IsMine)
            {
                PhotonNetwork.Destroy(itemView.gameObject);
            }
            else
            {
                LogWarning(
                    "Held network item has no owned PhotonView. " +
                    "Using local Destroy temporarily."
                );

                Destroy(objectToDestroy);
            }
        }
        else
        {
            Destroy(objectToDestroy);
        }
    }

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log("[PlayerInventory] " + message, this);
        }
    }

    private void LogWarning(string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning("[PlayerInventory] " + message, this);
        }
    }
}