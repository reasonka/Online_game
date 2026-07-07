using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("UI Sounds")]
    public AudioClip buttonClickSound;
    public AudioClip buttonHoverSound;
    public AudioClip panelOpenSound;
    public AudioClip panelCloseSound;

    [Header("Typing / Prompt Sounds")]
    public AudioClip typingSound;
    public AudioClip enterSound;
    public AudioClip errorSound;
    public AudioClip characterSelectedSound;
    public AudioClip loadingStartSound;

    [Header("Player Food Action Sounds")]
    public AudioClip pickupFoodSound;
    public AudioClip dropFoodSound;
    public AudioClip throwFoodSound;
    public AudioClip waveFoodSound;

    [Header("Player Movement Sounds")]
    public AudioClip walkSound;
    public AudioClip runSound;

    [Header("Cooking Sounds")]
    public AudioClip addIngredientSound;
    public AudioClip foodCookedSuccessSound;
    public AudioClip foodCookedFailSound;

    [Header("Customer / Order Sounds")]
    public AudioClip newOrderSound;
    public AudioClip correctOrderServedSound;
    public AudioClip wrongOrderServedSound;
    public AudioClip customerPraiseSound;
    public AudioClip customerDisappointedSound;
    public AudioClip customerDeathSound;

    [Header("Movement Audio Source")]
    public AudioSource movementAudioSource;

    [Header("Mic / Voice Filter Sounds")]
    public AudioClip micOnSound;
    public AudioClip micOffSound;

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

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = volume;

        if (movementAudioSource == null)
            movementAudioSource = gameObject.AddComponent<AudioSource>();

        movementAudioSource.playOnAwake = false;
        movementAudioSource.loop = false;
        movementAudioSource.volume = volume;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip, volume);
    }

    public void PlayButtonClick()
    {
        PlaySound(buttonClickSound);
    }

    public void PlayButtonHover()
    {
        PlaySound(buttonHoverSound);
    }

    public void PlayPanelOpen()
    {
        PlaySound(panelOpenSound);
    }

    public void PlayPanelClose()
    {
        PlaySound(panelCloseSound);
    }

    public void PlayTyping()
    {
        PlaySound(typingSound);
    }

    public void PlayEnter()
    {
        PlaySound(enterSound);
    }

    public void PlayError()
    {
        PlaySound(errorSound);
    }

    public void PlayCharacterSelected()
    {
        PlaySound(characterSelectedSound);
    }

    public void PlayLoadingStart()
    {
        PlaySound(loadingStartSound);
    }

    public void PlayPickupFood()
    {
        PlaySound(pickupFoodSound);
    }

    public void PlayDropFood()
    {
        PlaySound(dropFoodSound);
    }

    public void PlayThrowFood()
    {
        PlaySound(throwFoodSound);
    }

    public void PlayWaveFood()
    {
        PlaySound(waveFoodSound);
    }

    public void PlayAddIngredient()
    {
        PlaySound(addIngredientSound);
    }

    public void PlayFoodCookedSuccess()
    {
        PlaySound(foodCookedSuccessSound);
    }

    public void PlayFoodCookedFail()
    {
        PlaySound(foodCookedFailSound);
    }

    public void PlayNewOrder()
    {
        PlaySound(newOrderSound);
    }

    public void PlayCorrectOrderServed()
    {
        PlaySound(correctOrderServedSound);
    }

    public void PlayWrongOrderServed()
    {
        PlaySound(wrongOrderServedSound);
    }

    public void PlayCustomerPraise()
    {
        PlaySound(customerPraiseSound);
    }

    public void PlayCustomerDisappointed()
    {
        PlaySound(customerDisappointedSound);
    }

    public void PlayCustomerDeath()
    {
        PlaySound(customerDeathSound);
    }

    public void PlayMicOn()
    {
        PlaySound(micOnSound);
    }

    public void PlayMicOff()
    {
        PlaySound(micOffSound);
    }

    public void PlayWalk()
    {
        PlayMovementSound(walkSound);
    }

    public void PlayRun()
    {
        PlayMovementSound(runSound);
    }

    private void PlayMovementSound(AudioClip clip)
    {
        if (clip == null || movementAudioSource == null)
            return;

        movementAudioSource.Stop();
        movementAudioSource.PlayOneShot(clip, volume);
    }

    public void StopMovementSound()
    {
        if (movementAudioSource != null)
            movementAudioSource.Stop();
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);

        if (audioSource != null)
            audioSource.volume = volume;

        if (movementAudioSource != null)
            movementAudioSource.volume = volume;
    }
}