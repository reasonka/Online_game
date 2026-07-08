using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameSettingsUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject settingsPanel;
    public Button openSettingsButton;
    public Button closeSettingsButton;

    [Header("Voice / SFX Volume")]
    public Slider voiceVolumeSlider;
    public TMP_Text voiceVolumeValueText;

    [Header("Background Music Volume")]
    public Slider bgmVolumeSlider;
    public TMP_Text bgmVolumeValueText;

    [Header("Mouse Sensitivity")]
    public Slider mouseSensitivitySlider;
    public TMP_Text mouseSensitivityValueText;

    [Header("Default Values")]
    public float defaultVoiceVolume = 1f;
    public float defaultBGMVolume = 0.5f;
    public float defaultMouseSensitivity = 2f;

    public const string VoiceVolumeKey = "VoiceVolume";
    public const string BGMVolumeKey = "BGMVolume";
    public const string MouseSensitivityKey = "MouseSensitivity";

    private void Start()
    {
        if (openSettingsButton != null)
            openSettingsButton.onClick.AddListener(OpenSettings);

        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);

        LoadSettings();

        if (voiceVolumeSlider != null)
            voiceVolumeSlider.onValueChanged.AddListener(SetVoiceVolume);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void LoadSettings()
    {
        float voiceVolume =
            PlayerPrefs.GetFloat(VoiceVolumeKey, defaultVoiceVolume);

        float bgmVolume =
            PlayerPrefs.GetFloat(BGMVolumeKey, defaultBGMVolume);

        float mouseSensitivity =
            PlayerPrefs.GetFloat(MouseSensitivityKey, defaultMouseSensitivity);

        if (voiceVolumeSlider != null)
            voiceVolumeSlider.value = voiceVolume;

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.value = bgmVolume;

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.value = mouseSensitivity;

        SetVoiceVolume(voiceVolume);
        SetBGMVolume(bgmVolume);
        SetMouseSensitivity(mouseSensitivity);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayPanelOpen();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayPanelClose();

        PlayerPrefs.Save();
    }

    public void SetVoiceVolume(float value)
    {
        PlayerPrefs.SetFloat(VoiceVolumeKey, value);

        if (voiceVolumeValueText != null)
            voiceVolumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";

        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();

        foreach (AudioSource source in allAudioSources)
        {
            if (source.GetComponent<Photon.Voice.Unity.Speaker>() != null)
                source.volume = value;
        }

        if (SFXManager.Instance != null)
            SFXManager.Instance.SetVolume(value);
    }

    public void SetBGMVolume(float value)
    {
        PlayerPrefs.SetFloat(BGMVolumeKey, value);

        if (bgmVolumeValueText != null)
            bgmVolumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";

        if (BGMManager.Instance != null)
            BGMManager.Instance.SetVolume(value);
    }

    public void SetMouseSensitivity(float value)
    {
        PlayerPrefs.SetFloat(MouseSensitivityKey, value);

        if (mouseSensitivityValueText != null)
            mouseSensitivityValueText.text = value.ToString("0.0");

        BasicPlayerController[] basicControllers =
            FindObjectsOfType<BasicPlayerController>();

        foreach (BasicPlayerController controller in basicControllers)
            controller.mouseSensitivity = value;

        PlayerMovementController[] movementControllers =
            FindObjectsOfType<PlayerMovementController>();

        foreach (PlayerMovementController controller in movementControllers)
            controller.mouseSensitivity = value;

        PlayerOneController[] playerOneControllers =
            FindObjectsOfType<PlayerOneController>();

        foreach (PlayerOneController controller in playerOneControllers)
            controller.mouseSensitivity = value;
    }
}