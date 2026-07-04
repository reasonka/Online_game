using UnityEngine;

public class PlacementSurface : MonoBehaviour
{
    [Header("Placement")]
    public Transform placementPoint;

    [Tooltip("放到桌面后是否固定物体。")]
    public bool keepPlacedObjectFixed = true;

    [Header("Allowed Types")]
    public bool allowCookingBase = true;
    public bool allowFinishedFood = true;
    public bool allowIngredients = false;

    [Header("Pickup")]
    [Tooltip("是否允许玩家按 F 拿起桌上的 FinishedFood。")]
    public bool allowPickupFinishedFood = true;

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

    private void Update()
    {
        /*
         * 如果原来的 CookingBase 被 CookingStation Destroy，
         * Unity 的引用会自动变成 null。
         */
        if (placedItem == null)
        {
            return;
        }

        if (!placedItem.gameObject.activeInHierarchy)
        {
            placedItem = null;
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
            Log(
                "Placement surface is occupied by: " +
                placedItem.name
            );

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
        if (inventory == null)
        {
            LogWarning("PlayerInventory is null.");
            return false;
        }

        if (!inventory.HasHeldItem)
        {
            LogWarning("Player is not holding anything.");
            return false;
        }

        HoldableItem item = inventory.HeldItem;

        if (!CanPlace(item))
        {
            return false;
        }

        if (placementPoint == null)
        {
            LogWarning("Placement Point is missing.");
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

        /*
         * 如果放上来的是 CookingBase，
         * 自动把当前桌子引用传给它内部的 CookingStation。
         */
        CookingStation[] stations =
            item.GetComponentsInChildren<CookingStation>(true);

        foreach (CookingStation station in stations)
        {
            if (station != null)
            {
                station.SetPlacementSurface(this);
            }
        }

        Log("Placed on table: " + placedItem.name);
        return true;
    }

    public bool CanPickupFinishedFood(PlayerInventory inventory)
    {
        if (!allowPickupFinishedFood)
        {
            Log("Pickup FinishedFood is disabled.");
            return false;
        }

        if (inventory == null)
        {
            LogWarning("PlayerInventory is null.");
            return false;
        }

        if (inventory.HasHeldItem)
        {
            Log(
                "Cannot pick up because hand is occupied by: " +
                inventory.HeldItem.name
            );

            return false;
        }

        if (!IsOccupied)
        {
            Log("There is no item on this surface.");
            return false;
        }

        if (placedItem.itemType != HoldableItemType.FinishedFood)
        {
            Log(
                "The placed item is not FinishedFood. Type: " +
                placedItem.itemType
            );

            return false;
        }

        return true;
    }

    public bool TryPickupFinishedFood(PlayerInventory inventory)
    {
        if (!CanPickupFinishedFood(inventory))
        {
            return false;
        }

        HoldableItem itemToPickup = placedItem;

        placedItem = null;

        itemToPickup.transform.SetParent(null, true);

        bool pickedUp =
            inventory.TryHoldObject(itemToPickup.gameObject);

        if (!pickedUp)
        {
            placedItem = itemToPickup;

            itemToPickup.transform.SetParent(
                placementPoint,
                true
            );

            itemToPickup.transform.SetPositionAndRotation(
                placementPoint.position,
                placementPoint.rotation
            );

            itemToPickup.SetPlacedState(
                keepPlacedObjectFixed
            );

            LogWarning(
                "Failed to pick up FinishedFood."
            );

            return false;
        }

        Log(
            "Picked up FinishedFood: " +
            itemToPickup.name
        );

        return true;
    }

    public bool RegisterPlacedItem(GameObject placedObject)
    {
        if (placedObject == null)
        {
            LogWarning(
                "RegisterPlacedItem received a null object."
            );

            return false;
        }

        HoldableItem item =
            placedObject.GetComponent<HoldableItem>();

        if (item == null)
        {
            item =
                placedObject.GetComponentInParent<HoldableItem>();
        }

        if (item == null)
        {
            item =
                placedObject.GetComponentInChildren<HoldableItem>();
        }

        if (item == null)
        {
            LogWarning(
                "Generated final product has no HoldableItem: " +
                placedObject.name
            );

            return false;
        }

        if (item.itemType != HoldableItemType.FinishedFood)
        {
            LogWarning(
                "Generated object is not marked as FinishedFood: " +
                placedObject.name
            );

            return false;
        }

        placedItem = item;

        if (placementPoint != null)
        {
            item.transform.SetParent(
                placementPoint,
                true
            );

            item.transform.SetPositionAndRotation(
                placementPoint.position,
                placementPoint.rotation
            );
        }

        item.SetPlacedState(keepPlacedObjectFixed);

        Log(
            "Registered FinishedFood on table: " +
            item.name
        );

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

        removedItem.transform.SetParent(null, true);

        Log(
            "Removed placed item: " +
            removedItem.name
        );

        return removedItem;
    }

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(
                "[PlacementSurface] " + message,
                this
            );
        }
    }

    private void LogWarning(string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning(
                "[PlacementSurface] " + message,
                this
            );
        }
    }
}