using TMPro;
using UnityEngine;

public class InfoPanelUI : MonoBehaviour
{
    public GameObject infoPanel;
    public TMP_Text infoText;

    public KeyCode toggleKey = KeyCode.Tab;
    public bool showOnStart = false;

    private void Start()
    {
        if (infoText != null)
        {
            infoText.text =
                "WASD  Walk\n" +
                "E  Pick up food\n" +
                "Q  Drop food\n" +
                "R  Open emoji menu\n" +
                "Arrow Keys  Choose emoji\n" +
                "Enter  Confirm emoji\n" +
                "Left Ctrl  Unlock cursor";
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