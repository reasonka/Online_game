using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyOpeningCameraSpin : MonoBehaviour
{
    [Header("Camera")]
    public Camera cutsceneCamera;
    public Transform lookTarget;
    public Transform rotateCenter;

    [Header("Spin Settings")]
    public float radius = 8f;
    public float height = 3f;
    public float duration = 6f;
    public float startAngle = 0f;
    public float totalRotation = 360f;

    [Header("Objects")]
    public GameObject playerSpawner;

    [Header("Lobby Fade Cover")]
    public Image lobbyFadeImage;
    public float fadeOutDuration = 0.8f;

    [Header("After Cutscene")]
    public bool disableCameraAfterCutscene = true;
    public bool reEnableOtherCamerasAfterCutscene = true;

    private readonly List<Camera> otherCameras = new List<Camera>();
    private readonly List<AudioListener> otherAudioListeners = new List<AudioListener>();

    private bool shouldPlayOpeningCutscene;

    private void Awake()
    {
        shouldPlayOpeningCutscene =
            OpeningCutsceneState.PlayLobbyOpeningCutscene ||
            PlayerPrefs.GetInt("PlayLobbyOpeningCutscene", 0) == 1;

        OpeningCutsceneState.PlayLobbyOpeningCutscene = false;
        PlayerPrefs.SetInt("PlayLobbyOpeningCutscene", 0);
        PlayerPrefs.Save();

        if (shouldPlayOpeningCutscene)
        {
            ShowFadeCoverImmediately();
            PrepareCutsceneImmediately();
        }
        else
        {
            HideFadeCoverImmediately();
            DisableCutsceneCamera();

            if (playerSpawner != null)
                playerSpawner.SetActive(true);
        }
    }

    private void Start()
    {
        if (!shouldPlayOpeningCutscene)
            return;

        StartCoroutine(PlayCameraSpin());
    }

    private void ShowFadeCoverImmediately()
    {
        if (lobbyFadeImage == null)
            return;

        lobbyFadeImage.gameObject.SetActive(true);

        Color color = lobbyFadeImage.color;
        color.a = 1f;
        lobbyFadeImage.color = color;
    }

    private void HideFadeCoverImmediately()
    {
        if (lobbyFadeImage == null)
            return;

        lobbyFadeImage.gameObject.SetActive(false);
    }

    private IEnumerator FadeOutCover()
    {
        if (lobbyFadeImage == null)
            yield break;

        lobbyFadeImage.gameObject.SetActive(true);

        float timer = 0f;

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;

            Color color = lobbyFadeImage.color;
            color.a = Mathf.Lerp(1f, 0f, timer / fadeOutDuration);
            lobbyFadeImage.color = color;

            yield return null;
        }

        Color finalColor = lobbyFadeImage.color;
        finalColor.a = 0f;
        lobbyFadeImage.color = finalColor;

        lobbyFadeImage.gameObject.SetActive(false);
    }

    private void PrepareCutsceneImmediately()
    {
        if (cutsceneCamera == null)
        {
            Debug.LogWarning("LobbyOpeningCameraSpin: Cutscene Camera is not assigned.");
            return;
        }

        if (rotateCenter == null)
        {
            Debug.LogWarning("LobbyOpeningCameraSpin: Rotate Center is not assigned.");
            return;
        }

        if (lookTarget == null)
        {
            Debug.LogWarning("LobbyOpeningCameraSpin: Look Target is not assigned.");
            return;
        }

        if (playerSpawner != null)
            playerSpawner.SetActive(false);

        DisableOtherCameras();

        cutsceneCamera.gameObject.SetActive(true);
        cutsceneCamera.enabled = true;
        cutsceneCamera.depth = 100f;
        cutsceneCamera.transform.localScale = Vector3.one;

        AudioListener listener = cutsceneCamera.GetComponent<AudioListener>();

        if (listener != null)
            listener.enabled = true;

        SetCameraPosition(0f);
    }

    private IEnumerator PlayCameraSpin()
    {
        yield return null;

        yield return StartCoroutine(FadeOutCover());

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            SetCameraPosition(t);

            yield return null;
        }

        if (playerSpawner != null)
            playerSpawner.SetActive(true);

        if (disableCameraAfterCutscene)
            DisableCutsceneCamera();

        if (reEnableOtherCamerasAfterCutscene)
            ReEnableOtherCameras();
    }

    private void SetCameraPosition(float t)
    {
        if (cutsceneCamera == null || rotateCenter == null || lookTarget == null)
            return;

        float angle = startAngle + totalRotation * t;
        float radians = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(radians) * radius,
            height,
            Mathf.Sin(radians) * radius
        );

        cutsceneCamera.transform.position = rotateCenter.position + offset;
        cutsceneCamera.transform.LookAt(lookTarget.position);
    }

    private void DisableCutsceneCamera()
    {
        if (cutsceneCamera == null)
            return;

        AudioListener listener = cutsceneCamera.GetComponent<AudioListener>();

        if (listener != null)
            listener.enabled = false;

        cutsceneCamera.enabled = false;
        cutsceneCamera.gameObject.SetActive(false);
    }

    private void DisableOtherCameras()
    {
        otherCameras.Clear();
        otherAudioListeners.Clear();

        Camera[] cameras = FindObjectsOfType<Camera>(true);

        foreach (Camera camera in cameras)
        {
            if (camera == cutsceneCamera)
                continue;

            if (camera.enabled)
            {
                otherCameras.Add(camera);
                camera.enabled = false;
            }
        }

        AudioListener[] listeners = FindObjectsOfType<AudioListener>(true);

        foreach (AudioListener listener in listeners)
        {
            if (cutsceneCamera != null &&
                listener.gameObject == cutsceneCamera.gameObject)
            {
                continue;
            }

            if (listener.enabled)
            {
                otherAudioListeners.Add(listener);
                listener.enabled = false;
            }
        }
    }

    private void ReEnableOtherCameras()
    {
        foreach (Camera camera in otherCameras)
        {
            if (camera != null)
                camera.enabled = true;
        }

        foreach (AudioListener listener in otherAudioListeners)
        {
            if (listener != null)
                listener.enabled = true;
        }
    }
}