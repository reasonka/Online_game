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

        bool isLocalBlackWhitePlayer = playerIndex == blackWhitePlayerIndex;

        if (grayscaleVolumeObject != null)
            grayscaleVolumeObject.SetActive(isLocalBlackWhitePlayer);

        if (isLocalBlackWhitePlayer)
        {
            BindBlackWhiteCanvas();

            SetCanvasActive(normalTimerCanvasTag, normalTimerCanvasName, false);
            BindChefTimerCanvas();
        }
        else
        {
            SetCanvasActive(normalTimerCanvasTag, normalTimerCanvasName, true);
            SetCanvasActive(chefTimerCanvasTag, chefTimerCanvasName, false);
        }
    }

    private void BindBlackWhiteCanvas()
    {
        Canvas blackWhiteCanvas = FindCanvas(blackWhiteCanvasTag, blackWhiteCanvasName);

        if (blackWhiteCanvas == null)
        {
            Debug.LogWarning("BlackWhiteCanvas not found. Check its name or tag.");
            return;
        }

        blackWhiteCanvas.gameObject.SetActive(true);
        blackWhiteCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        blackWhiteCanvas.worldCamera = playerCamera;
        blackWhiteCanvas.planeDistance = blackWhiteCanvasPlaneDistance;
    }

    private void BindChefTimerCanvas()
    {
        Canvas timerCanvas = FindCanvas(chefTimerCanvasTag, chefTimerCanvasName);

        if (timerCanvas == null)
        {
            Debug.LogWarning("Chef timer canvas not found. Check its name or tag.");
            return;
        }

        timerCanvas.gameObject.SetActive(true);
        timerCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        timerCanvas.worldCamera = playerCamera;
        timerCanvas.planeDistance = chefTimerCanvasPlaneDistance;
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
                canvas.gameObject.tag == tagName;

            bool nameMatches =
                !string.IsNullOrEmpty(objectName) &&
                canvas.gameObject.name == objectName;

            if (tagMatches || nameMatches)
                return canvas;
        }

        return null;
    }
}