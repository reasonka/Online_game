using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameSettingsUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject settingsPanel;
    public Button openSettingsButton;
    public Button closeSettingsButton;

    [Header("Volume Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider voiceVolumeSlider;

    [Header("Mouse Sensitivity")]
    public Slider mouseSensitivitySlider;
    public TMP_Text mouseSensitivityValueText;

    [Header("Screen")]
    public Toggle fullscreenToggle;

    [Header("Controls Help")]
    public GameObject controlsPanel;
    public Button controlsButton;
    public Button closeControlsButton;

    [Header("Audio Sources")]
    public AudioSource[] musicSources;
    public AudioSource[] sfxSources;

    [Header("Default Values")]
    public float defaultMasterVolume = 1f;
    public float defaultMusicVolume = 0.7f;
    public float defaultSfxVolume = 0.8f;
    public float defaultVoiceVolume = 1f;
    public float defaultMouseSensitivity = 2f;

    public const string MasterVolumeKey = "MasterVolume";
    public const string MusicVolumeKey = "MusicVolume";
    public const string SfxVolumeKey = "SfxVolume";
    public const string VoiceVolumeKey = "VoiceVolume";
    public const string MouseSensitivityKey = "MouseSensitivity";
    public const string FullscreenKey = "Fullscreen";

    private void Start()
    {
        if (openSettingsButton != null)
            openSettingsButton.onClick.AddListener(OpenSettings);

        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);

        if (controlsButton != null)
            controlsButton.onClick.AddListener(OpenControls);

        if (closeControlsButton != null)
            closeControlsButton.onClick.AddListener(CloseControls);

        LoadSettings();
        AddSliderListeners();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (controlsPanel != null)
            controlsPanel.SetActive(false);
    }

    private void AddSliderListeners()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);

        if (voiceVolumeSlider != null)
            voiceVolumeSlider.onValueChanged.AddListener(SetVoiceVolume);

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    private void LoadSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        float sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);
        float voiceVolume = PlayerPrefs.GetFloat(VoiceVolumeKey, defaultVoiceVolume);
        float mouseSensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, defaultMouseSensitivity);
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = masterVolume;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = musicVolume;

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = sfxVolume;

        if (voiceVolumeSlider != null)
            voiceVolumeSlider.value = voiceVolume;

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.value = mouseSensitivity;

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = fullscreen;

        ApplyAllSettings();
    }

    private void ApplyAllSettings()
    {
        SetMasterVolume(PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume));
        SetMusicVolume(PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume));
        SetSfxVolume(PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume));
        SetVoiceVolume(PlayerPrefs.GetFloat(VoiceVolumeKey, defaultVoiceVolume));
        SetMouseSensitivity(PlayerPrefs.GetFloat(MouseSensitivityKey, defaultMouseSensitivity));
        SetFullscreen(PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        PlayerPrefs.Save();
    }

    public void OpenControls()
    {
        if (controlsPanel != null)
            controlsPanel.SetActive(true);
    }

    public void CloseControls()
    {
        if (controlsPanel != null)
            controlsPanel.SetActive(false);
    }

    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
    }

    public void SetMusicVolume(float value)
    {
        foreach (AudioSource source in musicSources)
        {
            if (source != null)
                source.volume = value;
        }

        PlayerPrefs.SetFloat(MusicVolumeKey, value);
    }

    public void SetSfxVolume(float value)
    {
        foreach (AudioSource source in sfxSources)
        {
            if (source != null)
                source.volume = value;
        }

        PlayerPrefs.SetFloat(SfxVolumeKey, value);
    }

    public void SetVoiceVolume(float value)
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();

        foreach (AudioSource source in allAudioSources)
        {
            if (source.GetComponent<Photon.Voice.Unity.Speaker>() != null)
                source.volume = value;
        }

        PlayerPrefs.SetFloat(VoiceVolumeKey, value);
    }

    public void SetMouseSensitivity(float value)
    {
        PlayerPrefs.SetFloat(MouseSensitivityKey, value);

        if (mouseSensitivityValueText != null)
            mouseSensitivityValueText.text = value.ToString("0.0");

        BasicPlayerController[] controllers = FindObjectsOfType<BasicPlayerController>();

        foreach (BasicPlayerController controller in controllers)
        {
            controller.mouseSensitivity = value;
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
    }
}