using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("Audio Source")]
    public AudioSource sfxAudioSource;

    [Header("UI SFX")]
    public AudioClip buttonClickSFX;
    public AudioClip buttonHoverSFX;
    public AudioClip openPanelSFX;
    public AudioClip closePanelSFX;

    [Header("Gameplay SFX")]
    public AudioClip pickupSFX;
    public AudioClip dropSFX;
    public AudioClip cookingSFX;
    public AudioClip correctOrderSFX;
    public AudioClip wrongOrderSFX;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float volume = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sfxAudioSource == null)
            sfxAudioSource = GetComponent<AudioSource>();

        if (sfxAudioSource == null)
            sfxAudioSource = gameObject.AddComponent<AudioSource>();

        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.loop = false;
        sfxAudioSource.volume = volume;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxAudioSource == null)
            return;

        sfxAudioSource.PlayOneShot(clip, volume);
    }

    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSFX);
    }

    public void PlayButtonHover()
    {
        PlaySFX(buttonHoverSFX);
    }

    public void PlayOpenPanel()
    {
        PlaySFX(openPanelSFX);
    }

    public void PlayClosePanel()
    {
        PlaySFX(closePanelSFX);
    }

    public void PlayPickup()
    {
        PlaySFX(pickupSFX);
    }

    public void PlayDrop()
    {
        PlaySFX(dropSFX);
    }

    public void PlayCooking()
    {
        PlaySFX(cookingSFX);
    }

    public void PlayCorrectOrder()
    {
        PlaySFX(correctOrderSFX);
    }

    public void PlayWrongOrder()
    {
        PlaySFX(wrongOrderSFX);
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);

        if (sfxAudioSource != null)
            sfxAudioSource.volume = volume;
    }
}