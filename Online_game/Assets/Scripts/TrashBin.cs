using UnityEngine;

public class TrashBin : MonoBehaviour
{
    [Header("Debug")]
    public bool showDebugLog = true;

    private void Start()
    {
        Collider binCollider = GetComponent<Collider>();

        if (binCollider == null)
        {
            LogWarning(
                "TrashBin has no Collider. " +
                "The player will not be able to detect it."
            );

            return;
        }

        if (!binCollider.isTrigger)
        {
            LogWarning(
                "TrashBin Collider should enable Is Trigger."
            );
        }
    }

    /// <summary>
    /// Destroys any object currently held by the player.
    /// Does not check HoldableItemType.
    /// </summary>
    public bool TryThrowAway(PlayerInventory inventory)
    {
        if (inventory == null)
        {
            LogWarning("PlayerInventory is null.");
            return false;
        }

        if (!inventory.HasHeldItem)
        {
            Log("Player is not holding anything.");
            return false;
        }

        string itemName = inventory.HeldItem.name;

        inventory.ConsumeHeldItem();

        Log("Thrown away held item: " + itemName);
        return true;
    }

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(
                "[TrashBin] " + message,
                this
            );
        }
    }

    private void LogWarning(string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning(
                "[TrashBin] " + message,
                this
            );
        }
    }
}