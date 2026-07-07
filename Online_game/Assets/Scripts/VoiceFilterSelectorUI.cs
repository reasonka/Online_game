using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class VoiceFilterSelectorUI : MonoBehaviour
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
        bool mouseOverMic =
            IsMouseOver(micButtonArea);

        bool mouseOverFilterPanel =
            IsMouseOver(filterPanelArea);

        if (mouseOverMic)
        {
            ShowFilterPanel();
            return;
        }

        if (filterPanelOpen && mouseOverFilterPanel)
            return;

        if (filterPanelOpen)
            HideFilterPanel();
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

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();

        Camera uiCamera = null;

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            Input.mousePosition,
            uiCamera
        );
    }
}