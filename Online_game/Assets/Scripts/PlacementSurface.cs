using UnityEngine;

public class PlacementSurface : MonoBehaviour
{
    [Header("Placement")]
    public Transform placementPoint;

    [Tooltip("放下后是否固定住，不受 Rigidbody 影响。")]
    public bool keepPlacedObjectFixed = true;

    [Header("Allowed Types")]
    public bool allowCookingBase = true;
    public bool allowFinishedFood = true;
    public bool allowIngredients = false;

    [Header("Debug")]
    public bool showDebugLog = true;

    private HoldableItem placedItem;

    public bool IsOccupied => placedItem != null;
    public HoldableItem PlacedItem => placedItem;

    private void Start()
    {
        if (placementPoint == null)
        {
            LogWarning("Placement Point is missing.");
        }
    }

    public bool CanPlace(HoldableItem item)
    {
        if (item == null)
        {
            return false;
        }

        if (IsOccupied)
        {
            Log("Placement surface is already occupied by: " + placedItem.name);
            return false;
        }

        switch (item.itemType)
        {
            case HoldableItemType.CookingBase:
                return allowCookingBase;

            case HoldableItemType.FinishedFood:
                return allowFinishedFood;

            case HoldableItemType.Ingredient:
                return allowIngredients;

            default:
                return false;
        }
    }

    public bool TryPlaceFromInventory(PlayerInventory inventory)
    {
        if (inventory == null || !inventory.HasHeldItem)
        {
            LogWarning("Inventory is empty.");
            return false;
        }

        HoldableItem item = inventory.HeldItem;

        if (!CanPlace(item))
        {
            return false;
        }

        bool placed = inventory.PlaceHeldItem(
            placementPoint,
            placementPoint,
            keepPlacedObjectFixed
        );

        if (!placed)
        {
            return false;
        }

        placedItem = item;

        Log("Placed on table: " + placedItem.name);
        return true;
    }

    public void ClearOccupiedReference()
    {
        placedItem = null;
        Log("Placement surface marked as empty.");
    }

    public HoldableItem RemovePlacedItem()
    {
        if (!IsOccupied)
        {
            return null;
        }

        HoldableItem removedItem = placedItem;
        placedItem = null;

        removedItem.transform.SetParent(null);

        Log("Removed placed item: " + removedItem.name);
        return removedItem;
    }

    private void Update()
    {
        // 如果原本放置的 CookingBase 在完成食谱后被 Destroy，
        // 自动将桌子重新标记为空。
        if (placedItem == null)
        {
            return;
        }

        if (!placedItem.gameObject.activeInHierarchy)
        {
            placedItem = null;
        }
    }

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log("[PlacementSurface] " + message, this);
        }
    }

    private void LogWarning(string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning("[PlacementSurface] " + message, this);
        }
    }
}