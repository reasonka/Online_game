using System;
using UnityEngine;
using Photon.Pun;

public class PlayerInventory : MonoBehaviourPun
{
    [Header("Hand")]
    [Tooltip("物品拿在手上时所挂载的位置。")]
    public Transform handPoint;

    [Header("Animator")]
    [Tooltip("角色模型上的 Animator。")]
    public Animator animator;

    [Tooltip("Animator 中表示是否拿着物品的 Bool 参数。")]
    public string isCarryingParameter = "IsCarrying";

    [Header("Photon")]
    [Tooltip("联网时强制使用 PhotonNetwork.Instantiate。")]
    public bool usePhotonSync = true;

    [Header("Debug")]
    public bool showDebugLog = true;

    private HoldableItem heldItem;

    public HoldableItem HeldItem => heldItem;

    public bool HasHeldItem => heldItem != null;

    public event Action<bool> HeldItemChanged;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        UpdateCarryingAnimator();
    }

    public GameObject SpawnAndHold(GameObject prefab)
    {
        if (prefab == null)
        {
            LogWarning("SpawnAndHold received a null prefab.");
            return null;
        }

        if (!CanUseThisInventory())
        {
            LogWarning("Only the local owner can spawn items from this inventory.");
            return null;
        }

        if (HasHeldItem)
        {
            LogWarning("Cannot spawn item because hand is occupied by: " + heldItem.name);
            return null;
        }

        if (handPoint == null)
        {
            LogWarning("Hand Point is missing.");
            return null;
        }

        GameObject spawnedObject;

        if (usePhotonSync && PhotonNetwork.InRoom)
        {
            spawnedObject = PhotonNetwork.Instantiate(
                prefab.name,
                handPoint.position,
                handPoint.rotation
            );

            PhotonView itemView = spawnedObject.GetComponent<PhotonView>();

            if (itemView == null)
            {
                itemView = spawnedObject.GetComponentInChildren<PhotonView>();
            }

            if (itemView == null)
            {
                LogWarning("Spawned network item has no PhotonView: " + prefab.name);
                PhotonNetwork.Destroy(spawnedObject);
                return null;
            }

            photonView.RPC(
                nameof(RPC_HoldObject),
                RpcTarget.All,
                itemView.ViewID
            );

            return spawnedObject;
        }

        spawnedObject = Instantiate(
            prefab,
            handPoint.position,
            handPoint.rotation
        );

        bool heldSuccessfully = HoldObjectLocally(spawnedObject);

        if (!heldSuccessfully)
        {
            Destroy(spawnedObject);
            return null;
        }

        return spawnedObject;
    }

    public bool TryHoldObject(GameObject targetObject)
    {
        if (targetObject == null)
        {
            LogWarning("TryHoldObject received a null object.");
            return false;
        }

        if (!CanUseThisInventory())
        {
            LogWarning("Only the local owner can hold items with this inventory.");
            return false;
        }

        if (usePhotonSync && PhotonNetwork.InRoom)
        {
            PhotonView itemView = targetObject.GetComponent<PhotonView>();

            if (itemView == null)
            {
                itemView = targetObject.GetComponentInChildren<PhotonView>();
            }

            if (itemView == null)
            {
                LogWarning("Cannot network-hold object without PhotonView: " + targetObject.name);
                return false;
            }

            photonView.RPC(
                nameof(RPC_HoldObject),
                RpcTarget.All,
                itemView.ViewID
            );

            return true;
        }

        return HoldObjectLocally(targetObject);
    }

    private bool HoldObjectLocally(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return false;
        }

        if (HasHeldItem)
        {
            LogWarning("Cannot hold another object because hand is occupied.");
            return false;
        }

        if (handPoint == null)
        {
            LogWarning("Hand Point is missing.");
            return false;
        }

        HoldableItem holdable = targetObject.GetComponent<HoldableItem>();

        if (holdable == null)
        {
            holdable = targetObject.GetComponentInChildren<HoldableItem>();
        }

        if (holdable == null)
        {
            holdable = targetObject.GetComponentInParent<HoldableItem>();
        }

        if (holdable == null)
        {
            LogWarning("Object has no HoldableItem component: " + targetObject.name);
            return false;
        }

        heldItem = holdable;

        Transform itemTransform = heldItem.transform;

        itemTransform.SetParent(handPoint, false);
        itemTransform.localPosition = Vector3.zero;
        itemTransform.localRotation = Quaternion.identity;

        heldItem.SetHeldState(true);

        NotifyHeldItemChanged();

        Log("Now holding: " + heldItem.name);

        return true;
    }

    [PunRPC]
    private void RPC_HoldObject(int itemViewId)
    {
        PhotonView itemView = PhotonView.Find(itemViewId);

        if (itemView == null)
        {
            LogWarning("RPC_HoldObject could not find PhotonView ID: " + itemViewId);
            return;
        }

        HoldObjectLocally(itemView.gameObject);
    }

    public bool PlaceHeldItem(
        Transform parentTransform,
        Transform placementTransform,
        bool keepFixed)
    {
        if (!HasHeldItem)
        {
            LogWarning("There is no held item to place.");
            return false;
        }

        if (placementTransform == null)
        {
            LogWarning("Placement Transform is missing.");
            return false;
        }

        HoldableItem itemToPlace = heldItem;

        if (usePhotonSync && PhotonNetwork.InRoom)
        {
            PhotonView itemView = itemToPlace.GetComponent<PhotonView>();

            if (itemView == null)
            {
                itemView = itemToPlace.GetComponentInParent<PhotonView>();
            }

            if (itemView == null)
            {
                itemView = itemToPlace.GetComponentInChildren<PhotonView>();
            }

            if (itemView != null)
            {
                photonView.RPC(
                    nameof(RPC_PlaceHeldObject),
                    RpcTarget.All,
                    itemView.ViewID,
                    placementTransform.position,
                    placementTransform.rotation,
                    keepFixed
                );

                return true;
            }
        }

        PlaceHeldObjectLocally(
            parentTransform,
            placementTransform.position,
            placementTransform.rotation,
            keepFixed
        );

        return true;
    }

    private void PlaceHeldObjectLocally(
        Transform parentTransform,
        Vector3 position,
        Quaternion rotation,
        bool keepFixed)
    {
        if (!HasHeldItem)
        {
            return;
        }

        HoldableItem itemToPlace = heldItem;
        heldItem = null;

        Transform itemTransform = itemToPlace.transform;

        itemTransform.SetParent(parentTransform, true);
        itemTransform.SetPositionAndRotation(position, rotation);

        itemToPlace.SetPlacedState(keepFixed);

        NotifyHeldItemChanged();

        Log("Placed item: " + itemToPlace.name);
    }

    [PunRPC]
    private void RPC_PlaceHeldObject(
        int itemViewId,
        Vector3 position,
        Quaternion rotation,
        bool keepFixed)
    {
        PhotonView itemView = PhotonView.Find(itemViewId);

        if (itemView == null)
        {
            LogWarning("RPC_PlaceHeldObject could not find PhotonView ID: " + itemViewId);
            return;
        }

        HoldableItem item = itemView.GetComponent<HoldableItem>();

        if (item == null)
        {
            item = itemView.GetComponentInChildren<HoldableItem>();
        }

        if (item == null)
        {
            return;
        }

        heldItem = item;

        PlaceHeldObjectLocally(
            null,
            position,
            rotation,
            keepFixed
        );
    }

    public Rigidbody ReleaseHeldItemAt(Transform releasePoint)
    {
        if (!HasHeldItem)
        {
            LogWarning("There is no held item to release.");
            return null;
        }

        if (releasePoint == null)
        {
            LogWarning("Release Point is missing.");
            return null;
        }

        HoldableItem itemToRelease = heldItem;

        if (usePhotonSync && PhotonNetwork.InRoom)
        {
            PhotonView itemView = itemToRelease.GetComponent<PhotonView>();

            if (itemView == null)
            {
                itemView = itemToRelease.GetComponentInParent<PhotonView>();
            }

            if (itemView == null)
            {
                itemView = itemToRelease.GetComponentInChildren<PhotonView>();
            }

            if (itemView != null)
            {
                photonView.RPC(
                    nameof(RPC_ReleaseHeldObject),
                    RpcTarget.All,
                    itemView.ViewID,
                    releasePoint.position,
                    releasePoint.rotation
                );
            }
        }
        else
        {
            ReleaseHeldObjectLocally(
                releasePoint.position,
                releasePoint.rotation
            );
        }

        Rigidbody releasedRigidbody = itemToRelease.GetComponent<Rigidbody>();

        if (releasedRigidbody == null)
        {
            releasedRigidbody = itemToRelease.GetComponentInChildren<Rigidbody>();
        }

        return releasedRigidbody;
    }

    private void ReleaseHeldObjectLocally(Vector3 position, Quaternion rotation)
    {
        if (!HasHeldItem)
        {
            return;
        }

        HoldableItem itemToRelease = heldItem;
        heldItem = null;

        Transform itemTransform = itemToRelease.transform;

        itemTransform.SetParent(null, true);
        itemTransform.SetPositionAndRotation(position, rotation);

        itemToRelease.SetPlacedState(false);

        NotifyHeldItemChanged();

        Rigidbody releasedRigidbody = itemToRelease.GetComponent<Rigidbody>();

        if (releasedRigidbody == null)
        {
            releasedRigidbody = itemToRelease.GetComponentInChildren<Rigidbody>();
        }

        if (releasedRigidbody != null)
        {
            releasedRigidbody.isKinematic = false;
            releasedRigidbody.useGravity = true;
            releasedRigidbody.velocity = Vector3.zero;
            releasedRigidbody.angularVelocity = Vector3.zero;
            releasedRigidbody.WakeUp();
        }

        Log("Released held item: " + itemToRelease.name);
    }

    [PunRPC]
    private void RPC_ReleaseHeldObject(
        int itemViewId,
        Vector3 position,
        Quaternion rotation)
    {
        PhotonView itemView = PhotonView.Find(itemViewId);

        if (itemView == null)
        {
            LogWarning("RPC_ReleaseHeldObject could not find PhotonView ID: " + itemViewId);
            return;
        }

        HoldableItem item = itemView.GetComponent<HoldableItem>();

        if (item == null)
        {
            item = itemView.GetComponentInChildren<HoldableItem>();
        }

        if (item == null)
        {
            return;
        }

        heldItem = item;

        ReleaseHeldObjectLocally(position, rotation);
    }

    public void ConsumeHeldItem()
    {
        if (!HasHeldItem)
        {
            Log("There is no held item to consume.");
            return;
        }

        GameObject objectToDestroy = heldItem.gameObject;
        string itemName = objectToDestroy.name;

        heldItem = null;

        NotifyHeldItemChanged();

        DestroyObject(objectToDestroy);

        Log("Consumed held item: " + itemName);
    }

    public void ForceClearHeldReference()
    {
        heldItem = null;
        NotifyHeldItemChanged();
    }

    private void DestroyObject(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return;
        }

        PhotonView targetView = targetObject.GetComponent<PhotonView>();

        if (targetView == null)
        {
            targetView = targetObject.GetComponentInParent<PhotonView>();
        }

        if (targetView == null)
        {
            targetView = targetObject.GetComponentInChildren<PhotonView>();
        }

        if (usePhotonSync && PhotonNetwork.InRoom && targetView != null)
        {
            PhotonNetwork.Destroy(targetView.gameObject);
            return;
        }

        Destroy(targetObject);
    }

    private bool CanUseThisInventory()
    {
        if (!usePhotonSync)
        {
            return true;
        }

        if (!PhotonNetwork.InRoom)
        {
            return true;
        }

        return photonView.IsMine;
    }

    private void NotifyHeldItemChanged()
    {
        UpdateCarryingAnimator();

        HeldItemChanged?.Invoke(HasHeldItem);
    }

    public void UpdateCarryingAnimator()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(isCarryingParameter, HasHeldItem);
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