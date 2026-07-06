using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VoiceFilterSelectorUI : MonoBehaviour, IPointerEnterHandler
{
    public GameObject filterPanel;

    public Button normalButton;
    public Button cuteButton;
    public Button robotButton;

    public RectTransform micButtonArea;
    public RectTransform filterPanelArea;

    public MicToggleButtonUI micToggleButtonUI;

    public string voiceFilterPropertyKey = "VoiceFilter";

    private bool filterPanelOpen = false;

    private void Start()
    {
        if (micButtonArea == null)
            micButtonArea = GetComponent<RectTransform>();

        if (micToggleButtonUI == null)
            micToggleButtonUI = GetComponent<MicToggleButtonUI>();

        if (filterPanel != null)
        {
            filterPanelArea = filterPanel.GetComponent<RectTransform>();
            filterPanel.SetActive(false);
        }

        if (normalButton != null)
            normalButton.onClick.AddListener(() => SelectFilterAndTurnMicOn(0));

        if (cuteButton != null)
            cuteButton.onClick.AddListener(() => SelectFilterAndTurnMicOn(1));

        if (robotButton != null)
            robotButton.onClick.AddListener(() => SelectFilterAndTurnMicOn(2));
    }

    private void Update()
    {
        if (!filterPanelOpen)
            return;

        if (IsMouseOver(micButtonArea) || IsMouseOver(filterPanelArea))
            return;

        HideFilterPanel();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowFilterPanel();
    }

    public void ShowFilterPanel()
    {
        filterPanelOpen = true;

        if (filterPanel != null)
            filterPanel.SetActive(true);
    }

    public void HideFilterPanel()
    {
        filterPanelOpen = false;

        if (filterPanel != null)
            filterPanel.SetActive(false);
    }

    private void SelectFilterAndTurnMicOn(int filterIndex)
    {
        Hashtable properties = new Hashtable
        {
            { voiceFilterPropertyKey, filterIndex }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);

        if (micToggleButtonUI != null)
            micToggleButtonUI.TurnMicOn();

        HideFilterPanel();
    }

    private bool IsMouseOver(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            Input.mousePosition,
            null
        );
    }
}