using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text instructionText;
    public Button backToLobbyButton;

    [Header("Input")]
    public KeyCode hostProceedKey = KeyCode.Return;
    public bool alsoAllowNumpadEnter = true;

    private int completedLevelNumber = 1;
    private bool isShown = false;

    private void Awake()
    {
        HideLevelComplete();

        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.RemoveAllListeners();
            backToLobbyButton.onClick.AddListener(BackToLobby);
        }
    }

    private void Update()
    {
        if (!isShown)
        {
            return;
        }

        if (!CanThisClientProceed())
        {
            return;
        }

        bool pressedEnter = Input.GetKeyDown(hostProceedKey);

        if (alsoAllowNumpadEnter)
        {
            pressedEnter = pressedEnter || Input.GetKeyDown(KeyCode.KeypadEnter);
        }

        if (pressedEnter)
        {
            BackToLobby();
        }
    }

    public void ShowLevelComplete(int levelNumber)
    {
        completedLevelNumber = levelNumber;
        isShown = true;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = "Level " + completedLevelNumber + " Complete!";
        }

        bool canProceed = CanThisClientProceed();

        if (instructionText != null)
        {
            if (canProceed)
            {
                instructionText.text = "Press Enter or click Back to Lobby.";
            }
            else
            {
                instructionText.text = "Waiting for the host to return everyone to the lobby...";
            }
        }

        if (backToLobbyButton != null)
        {
            backToLobbyButton.gameObject.SetActive(canProceed);
            backToLobbyButton.interactable = canProceed;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Level complete UI shown for Level " + completedLevelNumber);
    }

    public void HideLevelComplete()
    {
        isShown = false;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private bool CanThisClientProceed()
    {
        if (!PhotonNetwork.InRoom)
        {
            return true;
        }

        return PhotonNetwork.IsMasterClient;
    }

    private void BackToLobby()
    {
        if (!CanThisClientProceed())
        {
            Debug.LogWarning("Only the host can return everyone to the lobby.");
            return;
        }

        if (OurGameManager.Instance == null)
        {
            Debug.LogError("OurGameManager not found.");
            return;
        }

        OurGameManager.Instance.CompleteLevelAndReturnToLobby(completedLevelNumber);
    }
}