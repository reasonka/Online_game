using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EmojiWheelController : MonoBehaviour
{
    [Header("References")]
    public BasicPlayerController playerController;
    public GameObject wheelRoot;

    [Header("Buttons Order")]
    public Button[] emotionButtons;

    [Header("Input")]
    public KeyCode openWheelKey = KeyCode.R;
    public KeyCode confirmKey = KeyCode.Return;

    [Header("Visual Selection")]
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.25f, 1.25f, 1.25f);

    private bool isOpen;
    private int selectedIndex;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponentInParent<BasicPlayerController>();

        if (playerController == null)
            playerController = FindObjectOfType<BasicPlayerController>();

        if (wheelRoot == null)
        {
            Transform wheel = transform.Find("EmojiWheel");
            if (wheel != null)
                wheelRoot = wheel.gameObject;
        }

        CloseWheel();
    }

    private void Start()
    {
        CloseWheel();
    }

    private void Update()
    {
        if (playerController == null)
            return;

        if (!playerController.CanUseLocalInput())
            return;

        if (!isOpen)
        {
            if (Input.GetKeyDown(openWheelKey))
                OpenWheel();

            return;
        }

        HandleKeyboardSelection();
    }

    private void OpenWheel()
    {
        isOpen = true;
        selectedIndex = 0;

        if (wheelRoot != null)
            wheelRoot.SetActive(true);

        playerController.SetEmojiInputBlocked(true);

        UpdateSelectionVisual();
    }

    private void CloseWheel()
    {
        isOpen = false;

        if (wheelRoot != null)
            wheelRoot.SetActive(false);

        if (playerController != null)
            playerController.SetEmojiInputBlocked(false);

        ResetSelectionVisual();
    }

    private void HandleKeyboardSelection()
    {
        if (emotionButtons == null || emotionButtons.Length == 0)
            return;

        if (Input.GetKeyDown(KeyCode.RightArrow) ||
            Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex++;

            if (selectedIndex >= emotionButtons.Length)
                selectedIndex = 0;

            UpdateSelectionVisual();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) ||
            Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex--;

            if (selectedIndex < 0)
                selectedIndex = emotionButtons.Length - 1;

            UpdateSelectionVisual();
        }

        if (Input.GetKeyDown(confirmKey) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmSelection();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseWheel();
        }
    }

    private void ConfirmSelection()
    {
        EmotionType chosenEmotion = EmotionType.Happy;

        if (selectedIndex == 0)
            chosenEmotion = EmotionType.Happy;
        else if (selectedIndex == 1)
            chosenEmotion = EmotionType.Angry;
        else if (selectedIndex == 2)
            chosenEmotion = EmotionType.Crying;

        playerController.PlayEmotion(chosenEmotion);

        CloseWheel();
    }

    private void UpdateSelectionVisual()
    {
        if (emotionButtons == null)
            return;

        for (int i = 0; i < emotionButtons.Length; i++)
        {
            if (emotionButtons[i] == null)
                continue;

            emotionButtons[i].transform.localScale =
                i == selectedIndex ? selectedScale : normalScale;
        }

        if (EventSystem.current != null &&
            selectedIndex >= 0 &&
            selectedIndex < emotionButtons.Length &&
            emotionButtons[selectedIndex] != null)
        {
            EventSystem.current.SetSelectedGameObject(
                emotionButtons[selectedIndex].gameObject
            );
        }
    }

    private void ResetSelectionVisual()
    {
        if (emotionButtons == null)
            return;

        for (int i = 0; i < emotionButtons.Length; i++)
        {
            if (emotionButtons[i] != null)
                emotionButtons[i].transform.localScale = normalScale;
        }
    }
}