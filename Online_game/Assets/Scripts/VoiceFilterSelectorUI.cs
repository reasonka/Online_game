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
    public string playerIndexPropertyKey = "CharacterIndex";

    private bool filterPanelOpen = false;
    private int currentFilterIndex = -1;
    private bool micIsOn = false;

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
            normalButton.onClick.AddListener(() => SelectFilterToggleMic(0));

        if (cuteButton != null)
            cuteButton.onClick.AddListener(() => SelectFilterToggleMic(1));

        if (robotButton != null)
            robotButton.onClick.AddListener(() => SelectFilterToggleMic(2));
    }

    private void Update()
    {
        if (CanLocalPlayerUseMicKeys())
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                SelectFilterToggleMic(0);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                SelectFilterToggleMic(1);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                SelectFilterToggleMic(2);
                return;
            }
        }

        bool mouseOverMic = IsMouseOver(micButtonArea);
        bool mouseOverFilterPanel = IsMouseOver(filterPanelArea);

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

    private bool CanLocalPlayerUseMicKeys()
    {
        if (PhotonNetwork.LocalPlayer == null)
            return false;

        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(playerIndexPropertyKey, out object value))
            return false;

        int playerIndex = (int)value;

        return playerIndex == 0 || playerIndex == 2;
    }

    private void SelectFilterToggleMic(int filterIndex)
    {
        bool pressingSameFilter =
            micIsOn &&
            currentFilterIndex == filterIndex;

        if (pressingSameFilter)
        {
            TurnMicOff();
            return;
        }

        currentFilterIndex = filterIndex;
        micIsOn = true;

        Hashtable properties = new Hashtable
        {
            { voiceFilterPropertyKey, filterIndex }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);

        if (micToggleButtonUI != null)
            micToggleButtonUI.TurnMicOn();

        HideFilterPanel();

        Debug.Log("Voice filter on: " + filterIndex);
    }

    private void TurnMicOff()
    {
        micIsOn = false;

        if (micToggleButtonUI != null)
            micToggleButtonUI.TurnMicOff();

        HideFilterPanel();

        Debug.Log("Voice mic off.");
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

    private bool IsMouseOver(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return false;

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();

        Camera uiCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            Input.mousePosition,
            uiCamera
        );
    }
}