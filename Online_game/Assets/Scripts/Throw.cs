using System.Collections;
using UnityEngine;
using Photon.Pun;

public class Throw : MonoBehaviour
{
    [Header("Throw Point")]
    [Tooltip("FinishedFood 会移动到这个位置，然后恢复物理并自然下落。")]
    public Transform throwPoint;

    [Header("Input")]
    [Tooltip("玩家位于 Trigger Zone 内时，按这个键释放 FinishedFood。")]
    public KeyCode throwKey = KeyCode.T;

    [Header("Player Collision Protection")]
    [Tooltip("释放后暂时忽略物品与玩家之间的碰撞，防止玩家被顶起来。")]
    public bool temporarilyIgnorePlayerCollision = true;

    [Tooltip("释放后忽略玩家碰撞的时间。")]
    public float ignorePlayerCollisionDuration = 0.75f;

    [Header("Optional Force")]
    [Tooltip("关闭时物品只会自然下落。")]
    public bool addThrowForce = false;

    [Tooltip("开启 Add Throw Force 后使用的向前力量。")]
    public float forwardForce = 2f;

    [Tooltip("开启 Add Throw Force 后使用的向上力量。")]
    public float upwardForce = 1f;

    [Header("Debug")]
    public bool showDebugLog = true;

    private PlayerInventory playerInside;

    private void Start()
    {
        Collider triggerCollider =
            GetComponent<Collider>();

        if (triggerCollider == null)
        {
            LogWarning(
                "Throw Zone has no Collider."
            );
        }
        else if (!triggerCollider.isTrigger)
        {
            LogWarning(
                "Throw Zone Collider must enable Is Trigger."
            );
        }

        if (throwPoint == null)
        {
            LogWarning(
                "Throw Point has not been assigned."
            );
        }
    }

    private void Update()
    {
        if (playerInside == null)
        {
            return;
        }

        if (!CanPlayerUseInput(playerInside))
        {
            return;
        }

        if (Input.GetKeyDown(throwKey))
        {
            TryThrowHeldItem();
        }
    }

    private bool CanPlayerUseInput(
        PlayerInventory inventory)
    {
        if (inventory == null)
        {
            return false;
        }

        if (!inventory.usePhotonSync)
        {
            return true;
        }

        if (!PhotonNetwork.IsConnected)
        {
            return true;
        }

        PhotonView playerView =
            inventory.GetComponent<PhotonView>();

        if (playerView == null)
        {
            playerView =
                inventory.GetComponentInParent<PhotonView>();
        }

        return playerView != null &&
               playerView.IsMine;
    }

    private void TryThrowHeldItem()
    {
        if (playerInside == null)
        {
            return;
        }

        if (!playerInside.HasHeldItem)
        {
            Log(
                "Player has no held item."
            );

            return;
        }

        HoldableItem heldItem =
            playerInside.HeldItem;

        if (heldItem == null)
        {
            LogWarning(
                "HeldItem reference is null."
            );

            return;
        }

        /*
         * 只有 FinishedFood 可以从这个区域释放。
         */
        if (heldItem.itemType !=
            HoldableItemType.FinishedFood)
        {
            Log(
                "Only FinishedFood can be thrown here. " +
                "Current item type: " +
                heldItem.itemType
            );

            return;
        }

        if (throwPoint == null)
        {
            LogWarning(
                "Throw Point is missing."
            );

            return;
        }

        /*
         * 在释放前保存物品和玩家的 Collider。
         * 因为释放后 PlayerInventory 的 HeldItem 会变成 null。
         */
        GameObject itemObject =
            heldItem.gameObject;

        Collider[] itemColliders =
            itemObject.GetComponentsInChildren<Collider>(
                true
            );

        Collider[] playerColliders =
            playerInside.GetComponentsInChildren<Collider>(
                true
            );

        Rigidbody releasedRigidbody =
            playerInside.ReleaseHeldItemAt(
                throwPoint
            );

        if (releasedRigidbody == null)
        {
            LogWarning(
                "Failed to release FinishedFood."
            );

            return;
        }

        /*
         * 避免刚恢复 Collider 时与玩家重叠，
         * 导致 CharacterController 被向上顶起。
         */
        if (temporarilyIgnorePlayerCollision)
        {
            SetIgnoreCollisions(
                itemColliders,
                playerColliders,
                true
            );

            StartCoroutine(
                RestorePlayerCollisionsAfterDelay(
                    itemColliders,
                    playerColliders
                )
            );
        }

        /*
         * 默认关闭，所以只会自然下落。
         */
        if (addThrowForce)
        {
            Vector3 forceDirection =
                throwPoint.forward * forwardForce +
                Vector3.up * upwardForce;

            releasedRigidbody.AddForce(
                forceDirection,
                ForceMode.Impulse
            );
        }

        Log(
            "FinishedFood released at Throw Point: " +
            itemObject.name
        );
    }

    private IEnumerator RestorePlayerCollisionsAfterDelay(
        Collider[] itemColliders,
        Collider[] playerColliders)
    {
        yield return new WaitForSeconds(
            ignorePlayerCollisionDuration
        );

        SetIgnoreCollisions(
            itemColliders,
            playerColliders,
            false
        );
    }

    private void SetIgnoreCollisions(
        Collider[] firstColliders,
        Collider[] secondColliders,
        bool ignore)
    {
        if (firstColliders == null ||
            secondColliders == null)
        {
            return;
        }

        foreach (Collider first in firstColliders)
        {
            if (first == null ||
                first.isTrigger)
            {
                continue;
            }

            foreach (Collider second in secondColliders)
            {
                if (second == null ||
                    second.isTrigger)
                {
                    continue;
                }

                Physics.IgnoreCollision(
                    first,
                    second,
                    ignore
                );
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInventory inventory =
            FindInventory(other);

        if (inventory == null)
        {
            return;
        }

        if (!CanPlayerUseInput(inventory))
        {
            return;
        }

        playerInside = inventory;

        Log(
            "Player entered Throw Zone: " +
            inventory.name
        );
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInventory inventory =
            FindInventory(other);

        if (inventory == null)
        {
            return;
        }

        if (playerInside != inventory)
        {
            return;
        }

        playerInside = null;

        Log(
            "Player exited Throw Zone: " +
            inventory.name
        );
    }

    private PlayerInventory FindInventory(
        Collider other)
    {
        if (other == null)
        {
            return null;
        }

        PlayerInventory inventory =
            other.GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            inventory =
                other.GetComponentInParent<PlayerInventory>();
        }

        if (inventory == null)
        {
            inventory =
                other.GetComponentInChildren<PlayerInventory>();
        }

        return inventory;
    }

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(
                "[Throw] " + message,
                this
            );
        }
    }

    private void LogWarning(string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning(
                "[Throw] " + message,
                this
            );
        }
    }
}