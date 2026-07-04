using System;
using UnityEngine;
using Photon.Pun;

public class PlayerInventory : MonoBehaviourPun
{
    [Header("Hand")]
    [Tooltip("手持物品挂载的位置。")]
    public Transform handPoint;

    [Header("Animator")]
    [Tooltip("角色模型上的 Animator。")]
    public Animator animator;

    [Tooltip("Animator 中控制手持状态的 Bool 参数。")]
    public string isCarryingParameter = "IsCarrying";

    [Header("Photon")]
    [Tooltip("本地测试时关闭，正式联网时开启。")]
    public bool usePhotonSync = false;

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

        if (HasHeldItem)
        {
            LogWarning(
                "Cannot spawn item because hand is occupied by: " +
                heldItem.name
            );

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
            LogWarning("Failed to spawn item: " + prefab.name);
            return null;
        }

        if (!TryHoldObject(spawnedObject))
        {
            DestroyObject(spawnedObject);
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

        if (HasHeldItem)
        {
            LogWarning("Player is already holding an item.");
            return false;
        }

        if (handPoint == null)
        {
            LogWarning("Hand Point is missing.");
            return false;
        }

        HoldableItem holdable =
            targetObject.GetComponent<HoldableItem>();

        if (holdable == null)
        {
            holdable =
                targetObject.GetComponentInChildren<HoldableItem>();
        }

        if (holdable == null)
        {
            holdable =
                targetObject.GetComponentInParent<HoldableItem>();
        }

        if (holdable == null)
        {
            LogWarning(
                "Object has no HoldableItem component: " +
                targetObject.name
            );

            return false;
        }

        heldItem = holdable;

        Transform itemTransform = heldItem.transform;

        itemTransform.SetParent(handPoint, false);
        itemTransform.localPosition = Vector3.zero;
        itemTransform.localRotation = Quaternion.identity;

        /*
         * 你的 HoldableItem 版本需要 bool 参数。
         * true 表示进入手持状态。
         */
        heldItem.SetHeldState(true);

        NotifyHeldItemChanged();

        Log("Now holding: " + heldItem.name);
        return true;
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
        heldItem = null;

        Transform itemTransform = itemToPlace.transform;

        itemTransform.SetParent(parentTransform, true);

        itemTransform.SetPositionAndRotation(
            placementTransform.position,
            placementTransform.rotation
        );

        itemToPlace.SetPlacedState(keepFixed);

        NotifyHeldItemChanged();

        Log("Placed item: " + itemToPlace.name);
        return true;
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

        if (usePhotonSync && PhotonNetwork.IsConnected)
        {
            PhotonView targetView =
                targetObject.GetComponent<PhotonView>();

            if (targetView == null)
            {
                targetView =
                    targetObject.GetComponentInParent<PhotonView>();
            }

            if (targetView != null)
            {
                PhotonNetwork.Destroy(targetView.gameObject);
            }
            else
            {
                Destroy(targetObject);
            }
        }
        else
        {
            Destroy(targetObject);
        }
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

        animator.SetBool(
            isCarryingParameter,
            HasHeldItem
        );
    }

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(
                "[PlayerInventory] " + message,
                this
            );
        }
    }

    private void LogWarning(string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning(
                "[PlayerInventory] " + message,
                this
            );
        }
    }
}