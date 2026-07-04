using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class IngredientSelectionUI : MonoBehaviour
{
    [Header("Main UI")]
    [Tooltip("整个储物柜 UI 的根物体。")]
    public GameObject panelRoot;

    [Header("Header")]
    public TMP_Text storageNameText;

    [Header("Dynamic Button List")]
    [Tooltip("动态生成的按钮会放到这里。")]
    public Transform buttonContainer;

    [Tooltip("带有 StorageItemButton 的按钮 Prefab。")]
    public StorageItemButton itemButtonPrefab;

    [Header("Behaviour")]
    public bool closeAfterSelectingItem = true;

    [Tooltip("打开 UI 时是否显示和解锁鼠标。")]
    public bool manageCursor = true;

    [Header("Debug")]
    public bool showDebugLog = true;

    private readonly List<StorageItemButton> spawnedButtons =
        new List<StorageItemButton>();

    private IngredientStorage currentStorage;
    private PlayerInventory currentInventory;

    public bool IsOpen =>
        panelRoot != null &&
        panelRoot.activeSelf;

    private void Awake()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseStorage();
        }
    }

    public void OpenStorage(
        IngredientStorage storage,
        PlayerInventory inventory)
    {
        if (storage == null)
        {
            LogWarning(
                "Storage is null."
            );

            return;
        }

        if (inventory == null)
        {
            LogWarning(
                "PlayerInventory is null."
            );

            return;
        }

        if (inventory.HasHeldItem)
        {
            LogWarning(
                "Cannot open storage while player is holding: " +
                inventory.HeldItem.name
            );

            return;
        }

        currentStorage = storage;
        currentInventory = inventory;

        if (storageNameText != null)
        {
            storageNameText.text =
                storage.storageName;
        }

        ClearButtons();

        CreateButtons(
            storage.availableItems
        );

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (manageCursor)
        {
            Cursor.visible = true;
            Cursor.lockState =
                CursorLockMode.None;
        }

        Log(
            "Opened storage: " +
            storage.storageName
        );
    }

    private void CreateButtons(
        List<StorageItemEntry> entries)
    {
        if (buttonContainer == null)
        {
            LogWarning(
                "Button Container is missing."
            );

            return;
        }

        if (itemButtonPrefab == null)
        {
            LogWarning(
                "Item Button Prefab is missing."
            );

            return;
        }

        if (entries == null)
        {
            return;
        }

        foreach (StorageItemEntry entry in entries)
        {
            if (entry == null ||
                entry.itemPrefab == null)
            {
                continue;
            }

            StorageItemButton newButton =
                Instantiate(
                    itemButtonPrefab,
                    buttonContainer
                );

            newButton.Setup(
                entry,
                this
            );

            spawnedButtons.Add(
                newButton
            );
        }
    }

    public void SelectItem(
        StorageItemEntry entry)
    {
        if (entry == null ||
            entry.itemPrefab == null)
        {
            LogWarning(
                "Selected storage entry is invalid."
            );

            return;
        }

        if (currentInventory == null)
        {
            LogWarning(
                "Current PlayerInventory is missing."
            );

            return;
        }

        if (currentInventory.HasHeldItem)
        {
            LogWarning(
                "Player hand is already occupied."
            );

            return;
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
                "Selected prefab has no HoldableItem: " +
                entry.itemPrefab.name
            );

            return;
        }

        /*
         * 现在储物柜支持三种类型：
         * Ingredient、CookingBase、Drink。
         */
        bool supportedType =
            holdable.itemType ==
                HoldableItemType.Ingredient ||
            holdable.itemType ==
                HoldableItemType.CookingBase ||
            holdable.itemType ==
                HoldableItemType.Drink;

        if (!supportedType)
        {
            LogWarning(
                "Storage only supports Ingredient, CookingBase or Drink. " +
                "Selected type: " +
                holdable.itemType
            );

            return;
        }

        GameObject spawnedObject =
            currentInventory.SpawnAndHold(
                entry.itemPrefab
            );

        if (spawnedObject == null)
        {
            LogWarning(
                "Failed to spawn selected item: " +
                entry.itemPrefab.name
            );

            return;
        }

        Log(
            "Spawned and held item: " +
            entry.itemPrefab.name +
            ", type: " +
            holdable.itemType
        );

        if (closeAfterSelectingItem)
        {
            CloseStorage();
        }
    }

    public void CloseStorage()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        ClearButtons();

        currentStorage = null;
        currentInventory = null;

        if (manageCursor)
        {
            Cursor.visible = false;
            Cursor.lockState =
                CursorLockMode.Locked;
        }

        Log("Storage UI closed.");
    }

    private void ClearButtons()
    {
        for (int i =
                 spawnedButtons.Count - 1;
             i >= 0;
             i--)
        {
            if (spawnedButtons[i] != null)
            {
                Destroy(
                    spawnedButtons[i].gameObject
                );
            }
        }

        spawnedButtons.Clear();
    }

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(
                "[IngredientSelectionUI] " +
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
                "[IngredientSelectionUI] " +
                message,
                this
            );
        }
    }
}