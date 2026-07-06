using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanelUI : MonoBehaviour
{
    public GameObject infoPanel;

    [Header("Text Columns")]
    public TMP_Text keyText;
    public TMP_Text actionText;

    public KeyCode toggleKey = KeyCode.Tab;
    public bool showOnStart = false;

    private void Start()
    {
        if (keyText != null)
        {
            keyText.text =
                "WASD\n" +
                "E\n" +
                "Q\n" +
                "R\n" +
                "Arrow Keys\n" +
                "Enter\n" +
                "Left Ctrl";
        }

        if (actionText != null)
        {
            actionText.text =
                "Walk\n" +
                "Pick up food\n" +
                "Drop food\n" +
                "Open emoji menu\n" +
                "Choose emoji\n" +
                "Confirm emoji\n" +
                "Unlock cursor";
        }

        SetPanelVisible(showOnStart);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        if (infoPanel == null)
        {
            Debug.LogWarning("Info Panel is not assigned.");
            return;
        }

        infoPanel.SetActive(!infoPanel.activeSelf);
    }

    public void OpenPanel()
    {
        SetPanelVisible(true);
    }

    public void ClosePanel()
    {
        SetPanelVisible(false);
    }

    private void SetPanelVisible(bool visible)
    {
        if (infoPanel != null)
            infoPanel.SetActive(visible);
    }
}