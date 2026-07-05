using TMPro;
using UnityEngine;

public class InfoPanelUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject infoPanel;
    public TMP_Text infoText;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.Tab;
    public bool showOnStart = false;

    private void Start()
    {
        if (infoText != null)
            infoText.text = GetInfoText();

        SetPanelVisible(showOnStart);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetPanelVisible(!infoPanel.activeSelf);
    }

    public void OpenPanel()
    {
        SetPanelVisible(true);
    }

    public void ClosePanel()
    {
        SetPanelVisible(false);
    }

    public void TogglePanel()
    {
        SetPanelVisible(!infoPanel.activeSelf);
    }

    private void SetPanelVisible(bool visible)
    {
        if (infoPanel != null)
            infoPanel.SetActive(visible);
    }

    private string GetInfoText()
    {
        return
            "WASD  Walk\n" +
            "E  Pick up food\n" +
            "Q  Drop food\n" +
            "R  Open emoji wheel\n" +
            "Arrow Keys  Choose emoji\n" +
            "Enter  Confirm emoji";
    }
}