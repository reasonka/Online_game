using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SceneBGM
{
    public string sceneName;
    public AudioClip musicClip;
}

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Scene Music List")]
    public SceneBGM[] sceneMusicList;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    public bool loopMusic = true;
    public float fadeDuration = 1f;

    private AudioClip currentClip;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = loopMusic;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip clipToPlay = GetMusicForScene(sceneName);

        if (clipToPlay == null)
        {
            Debug.LogWarning("No BGM assigned for scene: " + sceneName);
            return;
        }

        // Do not restart the same song if it is already playing
        if (currentClip == clipToPlay && audioSource.isPlaying)
        {
            return;
        }

        currentClip = clipToPlay;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeToNewMusic(clipToPlay));
    }

    private AudioClip GetMusicForScene(string sceneName)
    {
        foreach (SceneBGM sceneBGM in sceneMusicList)
        {
            if (sceneBGM != null && sceneBGM.sceneName == sceneName)
            {
                return sceneBGM.musicClip;
            }
        }

        return null;
    }

    private IEnumerator FadeToNewMusic(AudioClip newClip)
    {
        float startVolume = audioSource.volume;

        // Fade out current music
        if (audioSource.isPlaying)
        {
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
                yield return null;
            }
        }

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.loop = loopMusic;
        audioSource.Play();

        // Fade in new music
        float fadeInTimer = 0f;

        while (fadeInTimer < fadeDuration)
        {
            fadeInTimer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, volume, fadeInTimer / fadeDuration);
            yield return null;
        }

        audioSource.volume = volume;
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);

        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    public void StopMusic()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        audioSource.Stop();
        currentClip = null;
    }
}