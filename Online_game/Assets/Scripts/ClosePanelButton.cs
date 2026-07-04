using UnityEngine;
using UnityEngine.UI;

public class ClosePanelButton : MonoBehaviour
{
    public Button closeButton;
    public GameObject panelToClose;

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    public void ClosePanel()
    {
        if (panelToClose != null)
            panelToClose.SetActive(false);
    }
}