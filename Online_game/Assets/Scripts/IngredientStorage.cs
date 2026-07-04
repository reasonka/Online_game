using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StorageItemEntry
{
    [Header("UI Display")]
    public string displayName;

    [Header("Item Prefab")]
    [Tooltip(
        "可以放 Ingredient、CookingBase 或 Drink 类型的 Prefab。"
    )]
    public GameObject itemPrefab;

    [Header("Optional UI Icon")]
    public Sprite icon;
}

public class IngredientStorage : MonoBehaviour
{
    [Header("Storage Information")]
    public string storageName = "Storage";

    [Header("Available Items")]
    [Tooltip(
        "这个储物柜可以提供 Ingredient、CookingBase 或 Drink。"
    )]
    public List<StorageItemEntry> availableItems =
        new List<StorageItemEntry>();

    [Header("UI")]
    [Tooltip("玩家进入储物柜区域时使用的选择 UI。")]
    public IngredientSelectionUI selectionUI;

    [Header("Debug")]
    public bool showDebugLog = true;

    private void Start()
    {
        Collider storageCollider =
            GetComponent<Collider>();

        if (storageCollider == null)
        {
            LogWarning(
                "Storage has no Collider. Player cannot detect it."
            );
        }
        else if (!storageCollider.isTrigger)
        {
            LogWarning(
                "Storage Collider should enable Is Trigger."
            );
        }

        ValidateStorageItems();
    }

    private void ValidateStorageItems()
    {
        for (int i = 0;
             i < availableItems.Count;
             i++)
        {
            StorageItemEntry entry =
                availableItems[i];

            if (entry == null)
            {
                LogWarning(
                    "Storage item index " +
                    i +
                    " is null."
                );

                continue;
            }

            if (entry.itemPrefab == null)
            {
                LogWarning(
                    "Storage item [" +
                    i +
                    "] has no prefab."
                );

                continue;
            }

            HoldableItem holdable =
                entry.itemPrefab
                    .GetComponent<HoldableItem>();

            if (holdable == null)
            {
                holdable =
                    entry.itemPrefab
                        .GetComponentInChildren<HoldableItem>();
            }

            if (holdable == null)
            {
                LogWarning(
                    "Prefab [" +
                    entry.itemPrefab.name +
                    "] has no HoldableItem component."
                );

                continue;
            }

            bool supported =
                holdable.itemType ==
                    HoldableItemType.Ingredient ||
                holdable.itemType ==
                    HoldableItemType.CookingBase ||
                holdable.itemType ==
                    HoldableItemType.Drink;

            if (!supported)
            {
                LogWarning(
                    "Storage item [" +
                    entry.itemPrefab.name +
                    "] has unsupported type: " +
                    holdable.itemType +
                    ". Allowed types are Ingredient, CookingBase and Drink."
                );

                continue;
            }

            Log(
                "Valid storage item [" +
                entry.itemPrefab.name +
                "] type: " +
                holdable.itemType
            );
        }
    }

    public void OpenStorage(
        PlayerInventory inventory)
    {
        if (selectionUI == null)
        {
            LogWarning(
                "IngredientSelectionUI is missing."
            );

            return;
        }

        if (inventory == null)
        {
            LogWarning(
                "PlayerInventory is missing."
            );

            return;
        }

        selectionUI.OpenStorage(
            this,
            inventory
        );
    }

    public void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(
                "[IngredientStorage] " +
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
                "[IngredientStorage] " +
                message,
                this
            );
        }
    }
}