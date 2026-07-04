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
    [Tooltip("本地测试时关闭，正式联网时开启。")]
    public bool usePhotonSync = false;

    [Header("Debug")]
    public bool showDebugLog = true;

    private HoldableItem heldItem;

    public HoldableItem HeldItem => heldItem;

    public bool HasHeldItem => heldItem != null;

    /// <summary>
    /// 当手上物品状态变化时触发。
    /// true 表示现在拿着物品，false 表示手为空。
    /// </summary>
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

    /// <summary>
    /// 生成一个 Prefab，并立刻拿到手上。
    /// </summary>
    public GameObject SpawnAndHold(GameObject prefab)
    {
        if (prefab == null)
        {
            LogWarning(
                "SpawnAndHold received a null prefab."
            );

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
            LogWarning(
                "Hand Point is missing."
            );

            return null;
        }

        GameObject spawnedObject;

        if (usePhotonSync &&
            PhotonNetwork.IsConnected)
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
            LogWarning(
                "Failed to spawn item: " +
                prefab.name
            );

            return null;
        }

        bool heldSuccessfully =
            TryHoldObject(spawnedObject);

        if (!heldSuccessfully)
        {
            DestroyObject(spawnedObject);
            return null;
        }

        return spawnedObject;
    }

    /// <summary>
    /// 把场景中已有的物体拿到手上。
    /// </summary>
    public bool TryHoldObject(GameObject targetObject)
    {
        if (targetObject == null)
        {
            LogWarning(
                "TryHoldObject received a null object."
            );

            return false;
        }

        if (HasHeldItem)
        {
            LogWarning(
                "Cannot hold another object because hand is occupied."
            );

            return false;
        }

        if (handPoint == null)
        {
            LogWarning(
                "Hand Point is missing."
            );

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

        Transform itemTransform =
            heldItem.transform;

        itemTransform.SetParent(
            handPoint,
            false
        );

        itemTransform.localPosition =
            Vector3.zero;

        itemTransform.localRotation =
            Quaternion.identity;

        /*
         * true 表示物品进入手持状态：
         * Collider 关闭；
         * Rigidbody 变为 Kinematic；
         * 重力关闭。
         */
        heldItem.SetHeldState(true);

        NotifyHeldItemChanged();

        Log(
            "Now holding: " +
            heldItem.name
        );

        return true;
    }

    /// <summary>
    /// 把手上的物品放到指定位置。
    /// keepFixed 为 true 时，物品放下后保持固定。
    /// </summary>
    public bool PlaceHeldItem(
        Transform parentTransform,
        Transform placementTransform,
        bool keepFixed)
    {
        if (!HasHeldItem)
        {
            LogWarning(
                "There is no held item to place."
            );

            return false;
        }

        if (placementTransform == null)
        {
            LogWarning(
                "Placement Transform is missing."
            );

            return false;
        }

        HoldableItem itemToPlace =
            heldItem;

        heldItem = null;

        Transform itemTransform =
            itemToPlace.transform;

        itemTransform.SetParent(
            parentTransform,
            true
        );

        itemTransform.SetPositionAndRotation(
            placementTransform.position,
            placementTransform.rotation
        );

        itemToPlace.SetPlacedState(
            keepFixed
        );

        NotifyHeldItemChanged();

        Log(
            "Placed item: " +
            itemToPlace.name
        );

        return true;
    }

    /// <summary>
    /// 把手上的物品移动到指定位置并释放。
    /// 物品不会固定，也不会销毁。
    /// Rigidbody、Collider 和重力会恢复。
    /// </summary>
    public Rigidbody ReleaseHeldItemAt(
        Transform releasePoint)
    {
        if (!HasHeldItem)
        {
            LogWarning(
                "There is no held item to release."
            );

            return null;
        }

        if (releasePoint == null)
        {
            LogWarning(
                "Release Point is missing."
            );

            return null;
        }

        HoldableItem itemToRelease =
            heldItem;

        heldItem = null;

        Transform itemTransform =
            itemToRelease.transform;

        /*
         * 先脱离玩家的 HandPoint。
         */
        itemTransform.SetParent(
            null,
            true
        );

        /*
         * 移动到空中的 Throw Point。
         */
        itemTransform.SetPositionAndRotation(
            releasePoint.position,
            releasePoint.rotation
        );

        /*
         * false 表示不固定物品。
         * Collider、Rigidbody 和原来的物理状态会恢复。
         */
        itemToRelease.SetPlacedState(false);

        /*
         * 通知 Animator：
         * IsCarrying 变成 false。
         */
        NotifyHeldItemChanged();

        Rigidbody releasedRigidbody =
            itemToRelease.GetComponent<Rigidbody>();

        if (releasedRigidbody == null)
        {
            releasedRigidbody =
                itemToRelease.GetComponentInChildren<Rigidbody>();
        }

        if (releasedRigidbody != null)
        {
            /*
             * 确保物品能够根据物理和重力下落。
             */
            releasedRigidbody.isKinematic = false;
            releasedRigidbody.useGravity = true;

            /*
             * 清除之前可能残留的速度。
             * 必须先把 Is Kinematic 关闭，
             * 再修改 velocity 和 angularVelocity。
             */
            releasedRigidbody.velocity =
                Vector3.zero;

            releasedRigidbody.angularVelocity =
                Vector3.zero;

            releasedRigidbody.WakeUp();
        }
        else
        {
            LogWarning(
                "Released item has no Rigidbody: " +
                itemToRelease.name
            );
        }

        Log(
            "Released held item at point: " +
            itemToRelease.name
        );

        return releasedRigidbody;
    }

    /// <summary>
    /// 销毁当前手上的物品。
    /// 用于添加 Ingredient 或扔进垃圾桶。
    /// </summary>
    public void ConsumeHeldItem()
    {
        if (!HasHeldItem)
        {
            Log(
                "There is no held item to consume."
            );

            return;
        }

        GameObject objectToDestroy =
            heldItem.gameObject;

        string itemName =
            objectToDestroy.name;

        heldItem = null;

        NotifyHeldItemChanged();

        DestroyObject(
            objectToDestroy
        );

        Log(
            "Consumed held item: " +
            itemName
        );
    }

    /// <summary>
    /// 只清除手持引用，不销毁物体。
    /// </summary>
    public void ForceClearHeldReference()
    {
        heldItem = null;

        NotifyHeldItemChanged();
    }

    private void DestroyObject(
        GameObject targetObject)
    {
        if (targetObject == null)
        {
            return;
        }

        if (usePhotonSync &&
            PhotonNetwork.IsConnected)
        {
            PhotonView targetView =
                targetObject.GetComponent<PhotonView>();

            if (targetView == null)
            {
                targetView =
                    targetObject.GetComponentInParent<PhotonView>();
            }

            if (targetView == null)
            {
                targetView =
                    targetObject.GetComponentInChildren<PhotonView>();
            }

            if (targetView != null)
            {
                PhotonNetwork.Destroy(
                    targetView.gameObject
                );
            }
            else
            {
                /*
                 * 没有 PhotonView 的普通物体，
                 * 只能在当前客户端本地销毁。
                 */
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

        HeldItemChanged?.Invoke(
            HasHeldItem
        );
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
                "[PlayerInventory] " +
                message,
                this
            );
        }
    }

    private void LogWarning(string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning(
                "[PlayerInventory] " +
                message,
                this
            );
        }
    }
}