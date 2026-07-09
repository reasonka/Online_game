using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text instructionText;
    public Button backToLobbyButton;

    [Header("Scene")]
    public string lobbySceneName = "Lobby";

    [Header("Input")]
    public KeyCode hostProceedKey = KeyCode.Return;
    public bool alsoAllowNumpadEnter = true;

    [Header("Auto Return")]
    public bool autoReturnToLobby = false;
    public float autoReturnDelay = 5f;

    private int completedLevelNumber = 1;
    private bool isShown = false;
    private bool isReturning = false;

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
            return;

        if (!CanThisClientProceed())
            return;

        bool pressedEnter = Input.GetKeyDown(hostProceedKey);

        if (alsoAllowNumpadEnter)
            pressedEnter = pressedEnter || Input.GetKeyDown(KeyCode.KeypadEnter);

        if (pressedEnter)
            BackToLobby();
    }

    public void ShowLevelComplete(int levelNumber)
    {
        completedLevelNumber = levelNumber;
        isShown = true;
        isReturning = false;

        if (panel != null)
            panel.SetActive(true);

        if (titleText != null)
            titleText.text = "Level " + completedLevelNumber + " Complete!";

        bool canProceed = CanThisClientProceed();

        if (instructionText != null)
        {
            if (canProceed)
            {
                if (autoReturnToLobby)
                    instructionText.text = "Returning to lobby...";
                else
                    instructionText.text = "Press Enter or click Back to Lobby.";
            }
            else
            {
                instructionText.text = "Waiting for the host to return everyone to the lobby...";
            }
        }

        if (backToLobbyButton != null)
        {
            backToLobbyButton.gameObject.SetActive(canProceed && !autoReturnToLobby);
            backToLobbyButton.interactable = canProceed && !autoReturnToLobby;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (autoReturnToLobby && canProceed)
            Invoke(nameof(BackToLobby), autoReturnDelay);
    }

    public void HideLevelComplete()
    {
        isShown = false;

        if (panel != null)
            panel.SetActive(false);
    }

    private bool CanThisClientProceed()
    {
        if (!PhotonNetwork.InRoom)
            return true;

        return PhotonNetwork.IsMasterClient;
    }

    private void BackToLobby()
    {
        if (isReturning)
            return;

        if (!CanThisClientProceed())
        {
            Debug.LogWarning("Only the host can return everyone to the lobby.");
            return;
        }

        isReturning = true;

        if (OurGameManager.Instance != null)
        {
            OurGameManager.Instance.CompleteLevelAndReturnToLobby(completedLevelNumber);
            return;
        }

        Debug.LogWarning("OurGameManager not found. Loading lobby directly.");

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.AutomaticallySyncScene = true;

            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel(lobbySceneName);
        }
        else
        {
            SceneManager.LoadScene(lobbySceneName);
        }
    }
}