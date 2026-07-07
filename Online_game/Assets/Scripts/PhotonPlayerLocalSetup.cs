using System.Collections;
using Photon.Pun;
using UnityEngine;

public class PhotonPlayerLocalSetup : MonoBehaviourPun
{
    [Header("Photon")]
    public bool usePhotonSync = true;

    [Header("Player Identity")]
    public int playerIndex = 0;
    public bool readPlayerIndexFromPhotonProperties = true;
    public string playerIndexPropertyKey = "CharacterIndex";

    [Header("Local Components")]
    public Camera playerCamera;
    public AudioListener audioListener;
    public MonoBehaviour[] localOnlyInputScripts;

    [Header("Black White Player")]
    public int blackWhitePlayerIndex = 2;
    public string blackWhiteCanvasName = "BlackWhiteCanvas";
    public float blackWhiteCanvasPlaneDistance = 1f;
    public GameObject grayscaleVolumeObject;

    [Header("Level 2 Timer")]
    public string normalTimerCanvasName = "TimerCanvas_Normal";
    public string chefTimerCanvasName = "TimerCanvas_ChefBW";
    public float chefTimerCanvasPlaneDistance = 1f;

    [Header("Mic Button Canvas")]
    public string normalMicCanvasName = "MicCanvas_Normal";
    public string chefMicCanvasName = "MicCanvas_ChefBW";
    public float chefMicCanvasPlaneDistance = 1f;

    [Header("Settings / Info Canvas")]
    public string normalSettingsInfoCanvasName = "SettingsInfoCanvas_Normal";
    public string chefSettingsInfoCanvasName = "SettingsInfoCanvas_ChefBW";
    public float chefSettingsInfoCanvasPlaneDistance = 1f;

    private bool isLocalPlayer;
    private bool isLocalBlackWhitePlayer;

    private void Start()
    {
        ReadPlayerIndex();
        SetupLocalPlayer();

        if (isLocalPlayer)
            StartCoroutine(ForceCorrectCanvasStateAfterDelay());
    }

    private IEnumerator ForceCorrectCanvasStateAfterDelay()
    {
        yield return null;
        yield return new WaitForSeconds(0.2f);

        SetupRoleCanvases();
    }

    private void ReadPlayerIndex()
    {
        if (!readPlayerIndexFromPhotonProperties)
            return;

        if (PhotonNetwork.IsConnected && photonView != null && photonView.Owner != null)
        {
            if (photonView.Owner.CustomProperties.TryGetValue(playerIndexPropertyKey, out object value))
                playerIndex = (int)value;
        }
    }

    private void SetupLocalPlayer()
    {
        isLocalPlayer =
            !usePhotonSync ||
            !PhotonNetwork.IsConnected ||
            photonView.IsMine;

        if (playerCamera == null)
        {
            Debug.LogError(gameObject.name + " has no Player Camera assigned!");
            return;
        }

        playerCamera.gameObject.SetActive(true);
        playerCamera.enabled = isLocalPlayer;

        if (audioListener != null)
            audioListener.enabled = isLocalPlayer;

        foreach (MonoBehaviour inputScript in localOnlyInputScripts)
        {
            if (inputScript != null)
                inputScript.enabled = isLocalPlayer;
        }

        if (!isLocalPlayer)
            return;

        isLocalBlackWhitePlayer =
            playerIndex == blackWhitePlayerIndex;

        if (grayscaleVolumeObject != null)
            grayscaleVolumeObject.SetActive(isLocalBlackWhitePlayer);

        SetupRoleCanvases();
    }

    private void SetupRoleCanvases()
    {
        if (isLocalBlackWhitePlayer)
        {
            // Chef: close ALL normal UI.
            SetCanvasActive(normalTimerCanvasName, false);
            SetCanvasActive(normalMicCanvasName, false);
            SetCanvasActive(normalSettingsInfoCanvasName, false);

            // Chef: open only black-white UI.
            BindCameraCanvas(blackWhiteCanvasName, blackWhiteCanvasPlaneDistance);
            BindCameraCanvas(chefTimerCanvasName, chefTimerCanvasPlaneDistance);
            BindCameraCanvas(chefMicCanvasName, chefMicCanvasPlaneDistance);
            BindCameraCanvas(chefSettingsInfoCanvasName, chefSettingsInfoCanvasPlaneDistance);
        }
        else
        {
            // Non-chef: open normal UI.
            SetCanvasActive(normalTimerCanvasName, true);
            SetCanvasActive(normalMicCanvasName, true);
            SetCanvasActive(normalSettingsInfoCanvasName, true);

            // Non-chef: close chef black-white UI.
            SetCanvasActive(blackWhiteCanvasName, false);
            SetCanvasActive(chefTimerCanvasName, false);
            SetCanvasActive(chefMicCanvasName, false);
            SetCanvasActive(chefSettingsInfoCanvasName, false);
        }
    }

    private void BindCameraCanvas(string objectName, float planeDistance)
    {
        Canvas canvas = FindCanvas(objectName);

        if (canvas == null)
        {
            Debug.LogWarning("Canvas not found: " + objectName);
            return;
        }

        canvas.gameObject.SetActive(true);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = playerCamera;
        canvas.planeDistance = planeDistance;
    }

    private void SetCanvasActive(string objectName, bool active)
    {
        Canvas canvas = FindCanvas(objectName);

        if (canvas != null)
            canvas.gameObject.SetActive(active);
    }

    private Canvas FindCanvas(string objectName)
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null)
                continue;

            if (!canvas.gameObject.scene.IsValid())
                continue;

            if (canvas.gameObject.name == objectName)
                return canvas;
        }

        return null;
    }
}