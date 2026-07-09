using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;

public static class OpeningCutsceneState
{
    public static bool PlayLobbyOpeningCutscene;
}

public class OpeningCutsceneController : MonoBehaviour
{
    [System.Serializable]
    public class CutsceneSlide
    {
        public GameObject slideObject;
        public TMP_Text storyText;

        [TextArea(3, 6)]
        public string fullText;
    }

    [Header("Cutscene Slides")]
    public CutsceneSlide[] slides;

    [Header("Canvas")]
    public GameObject cutsceneCanvasRoot;

    [Header("Fade")]
    public Image fadeImage;
    public float fadeDuration = 1f;

    [Header("Typing Settings")]
    public float typingSpeed = 0.04f;
    public float waitAfterTyping = 2f;

    [Header("Controls")]
    public KeyCode skipTypingKey = KeyCode.Space;
    public KeyCode nextSlideKey = KeyCode.Return;

    [Header("Scene After Cutscene")]
    public string nextSceneName = "Lobby";

    private bool skipTyping;
    private bool goNextSlide;
    private bool cutsceneFinished;

    private void Start()
    {
        HideAllSlides();

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);

            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
        }

        StartCoroutine(PlayCutscene());
    }

    private void Update()
    {
        if (cutsceneFinished)
            return;

        if (Input.GetKeyDown(skipTypingKey))
            skipTyping = true;

        if (Input.GetKeyDown(nextSlideKey))
            goNextSlide = true;
    }

    private IEnumerator PlayCutscene()
    {
        for (int i = 0; i < slides.Length; i++)
        {
            if (!IsValidSlide(slides[i]))
                continue;

            skipTyping = false;
            goNextSlide = false;

            ShowOnlySlide(i);

            yield return StartCoroutine(TypeText(slides[i]));

            float timer = 0f;

            while (timer < waitAfterTyping)
            {
                if (goNextSlide)
                    break;

                timer += Time.deltaTime;
                yield return null;
            }
        }

        cutsceneFinished = true;

        yield return StartCoroutine(FadeToBlack());

        HideAllSlides();

        if (cutsceneCanvasRoot != null)
            cutsceneCanvasRoot.SetActive(false);

        OpeningCutsceneState.PlayLobbyOpeningCutscene = true;
        PlayerPrefs.SetInt("PlayLobbyOpeningCutscene", 1);
        PlayerPrefs.Save();

        LoadLobbyScene();
    }

    private void LoadLobbyScene()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.AutomaticallySyncScene = true;

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(nextSceneName);
            }

            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private bool IsValidSlide(CutsceneSlide slide)
    {
        return slide != null &&
               slide.slideObject != null &&
               slide.storyText != null &&
               !string.IsNullOrWhiteSpace(slide.fullText);
    }

    private IEnumerator TypeText(CutsceneSlide slide)
    {
        slide.storyText.text = "";

        for (int i = 0; i < slide.fullText.Length; i++)
        {
            if (skipTyping || goNextSlide)
            {
                slide.storyText.text = slide.fullText;
                yield break;
            }

            slide.storyText.text += slide.fullText[i];

            if (typingSpeed > 0f)
                yield return new WaitForSeconds(typingSpeed);
            else
                yield return null;
        }
    }

    private IEnumerator FadeToBlack()
    {
        if (fadeImage == null)
            yield break;

        fadeImage.gameObject.SetActive(true);

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            Color color = fadeImage.color;
            color.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadeImage.color = color;

            yield return null;
        }

        Color finalColor = fadeImage.color;
        finalColor.a = 1f;
        fadeImage.color = finalColor;
    }

    private void ShowOnlySlide(int index)
    {
        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i] == null)
                continue;

            if (slides[i].slideObject != null)
                slides[i].slideObject.SetActive(i == index);

            if (slides[i].storyText != null)
                slides[i].storyText.text = "";
        }
    }

    private void HideAllSlides()
    {
        if (slides == null)
            return;

        foreach (CutsceneSlide slide in slides)
        {
            if (slide == null)
                continue;

            if (slide.slideObject != null)
                slide.slideObject.SetActive(false);

            if (slide.storyText != null)
                slide.storyText.text = "";
        }
    }
}