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
    public string blackWhiteCanvasTag = "BlackWhiteCanvas";
    public string blackWhiteCanvasName = "BlackWhiteCanvas";
    public float blackWhiteCanvasPlaneDistance = 1f;
    public GameObject grayscaleVolumeObject;

    [Header("Level 2 Timer")]
    public string normalTimerCanvasTag = "NormalTimerCanvas";
    public string normalTimerCanvasName = "TimerCanvas_Normal";

    public string chefTimerCanvasTag = "ChefTimerCanvas";
    public string chefTimerCanvasName = "TimerCanvas_ChefBW";
    public float chefTimerCanvasPlaneDistance = 1f;

    [Header("Mic Button Canvas")]
    public string normalMicCanvasTag = "";
    public string normalMicCanvasName = "MicCanvas_Normal";

    public string chefMicCanvasTag = "";
    public string chefMicCanvasName = "MicCanvas_ChefBW";
    public float chefMicCanvasPlaneDistance = 1f;

    [Header("Settings / Info Canvas")]
    public string normalSettingsInfoCanvasTag = "";
    public string normalSettingsInfoCanvasName = "SettingsInfoCanvas_Normal";

    public string chefSettingsInfoCanvasTag = "";
    public string chefSettingsInfoCanvasName = "SettingsInfoCanvas_ChefBW";
    public float chefSettingsInfoCanvasPlaneDistance = 1f;

    private void Start()
    {
        ReadPlayerIndex();
        SetupLocalPlayer();
    }

    private void ReadPlayerIndex()
    {
        if (!readPlayerIndexFromPhotonProperties)
            return;

        if (PhotonNetwork.IsConnected && photonView != null && photonView.Owner != null)
        {
            if (photonView.Owner.CustomProperties.TryGetValue(playerIndexPropertyKey, out object value))
            {
                playerIndex = (int)value;
            }
        }
    }

    private void SetupLocalPlayer()
    {
        bool isLocalPlayer =
            !usePhotonSync ||
            !PhotonNetwork.IsConnected ||
            photonView.IsMine;

        Debug.Log(gameObject.name + " IsMine: " + photonView.IsMine + " Local: " + isLocalPlayer);

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

        bool isLocalBlackWhitePlayer =
            playerIndex == blackWhitePlayerIndex;

        if (grayscaleVolumeObject != null)
            grayscaleVolumeObject.SetActive(isLocalBlackWhitePlayer);

        SetupRoleCanvases(isLocalBlackWhitePlayer);
    }

    private void SetupRoleCanvases(bool isLocalBlackWhitePlayer)
    {
        if (isLocalBlackWhitePlayer)
        {
            BindBlackWhiteCanvas();

            SetCanvasActive(normalTimerCanvasTag, normalTimerCanvasName, false);
            BindCameraCanvas(chefTimerCanvasTag, chefTimerCanvasName, chefTimerCanvasPlaneDistance);

            SetCanvasActive(normalMicCanvasTag, normalMicCanvasName, false);
            BindCameraCanvas(chefMicCanvasTag, chefMicCanvasName, chefMicCanvasPlaneDistance);

            SetCanvasActive(normalSettingsInfoCanvasTag, normalSettingsInfoCanvasName, false);
            BindCameraCanvas(chefSettingsInfoCanvasTag, chefSettingsInfoCanvasName, chefSettingsInfoCanvasPlaneDistance);
        }
        else
        {
            SetCanvasActive(normalTimerCanvasTag, normalTimerCanvasName, true);
            SetCanvasActive(chefTimerCanvasTag, chefTimerCanvasName, false);

            SetCanvasActive(normalMicCanvasTag, normalMicCanvasName, true);
            SetCanvasActive(chefMicCanvasTag, chefMicCanvasName, false);

            SetCanvasActive(normalSettingsInfoCanvasTag, normalSettingsInfoCanvasName, true);
            SetCanvasActive(chefSettingsInfoCanvasTag, chefSettingsInfoCanvasName, false);
        }
    }

    private void BindBlackWhiteCanvas()
    {
        BindCameraCanvas(
            blackWhiteCanvasTag,
            blackWhiteCanvasName,
            blackWhiteCanvasPlaneDistance
        );
    }

    private void BindCameraCanvas(string tagName, string objectName, float planeDistance)
    {
        Canvas canvas = FindCanvas(tagName, objectName);

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

    private void SetCanvasActive(string tagName, string objectName, bool active)
    {
        Canvas canvas = FindCanvas(tagName, objectName);

        if (canvas != null)
            canvas.gameObject.SetActive(active);
    }

    private Canvas FindCanvas(string tagName, string objectName)
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null)
                continue;

            if (!canvas.gameObject.scene.IsValid())
                continue;

            bool tagMatches =
                !string.IsNullOrEmpty(tagName) &&
                canvas.gameObject.CompareTag(tagName);

            bool nameMatches =
                !string.IsNullOrEmpty(objectName) &&
                canvas.gameObject.name == objectName;

            if (tagMatches || nameMatches)
                return canvas;
        }

        return null;
    }
}