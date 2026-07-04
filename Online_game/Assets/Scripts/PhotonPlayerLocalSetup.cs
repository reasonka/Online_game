using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public string blackWhiteCanvasTag = "BlackWhiteCanvas";
    public string blackWhiteCanvasName = "BlackWhiteCanvas";
    public float blackWhiteCanvasPlaneDistance = 1f;
    public GameObject grayscaleVolumeObject;

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

        bool isLocalBlackWhitePlayer = isLocalPlayer && playerIndex == 2;

        if (grayscaleVolumeObject != null)
            grayscaleVolumeObject.SetActive(isLocalBlackWhitePlayer);

        if (isLocalBlackWhitePlayer)
            BindBlackWhiteCanvas();
    }

    private void BindBlackWhiteCanvas()
    {
        Canvas blackWhiteCanvas = FindBlackWhiteCanvas();

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

    private Canvas FindBlackWhiteCanvas()
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();

        foreach (Canvas canvas in canvases)
        {
            if (!canvas.gameObject.scene.IsValid())
                continue;

            if (canvas.CompareTag(blackWhiteCanvasTag) || canvas.gameObject.name == blackWhiteCanvasName)
                return canvas;
        }

        return null;
    }
}