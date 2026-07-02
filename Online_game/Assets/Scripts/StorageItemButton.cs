using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorageItemButton : MonoBehaviour
{
    [Header("UI References")]
    public Button button;
    public TMP_Text itemNameText;
    public Image itemIconImage;

    private StorageItemEntry currentEntry;
    private IngredientSelectionUI currentUI;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    public void Setup(
        StorageItemEntry entry,
        IngredientSelectionUI selectionUI)
    {
        currentEntry = entry;
        currentUI = selectionUI;

        if (itemNameText != null)
        {
            if (!string.IsNullOrEmpty(entry.displayName))
            {
                itemNameText.text = entry.displayName;
            }
            else if (entry.itemPrefab != null)
            {
                itemNameText.text = entry.itemPrefab.name;
            }
            else
            {
                itemNameText.text = "Missing Item";
            }
        }

        if (itemIconImage != null)
        {
            itemIconImage.sprite = entry.icon;
            itemIconImage.enabled = entry.icon != null;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        if (currentUI == null || currentEntry == null)
        {
            return;
        }

        currentUI.SelectItem(currentEntry);
    }
}