using UnityEngine;

public class InfoPanelUI : MonoBehaviour
{
    public GameObject infoPanel;

    public KeyCode toggleKey = KeyCode.Tab;
    public bool showOnStart = false;

    private void Start()
    {
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