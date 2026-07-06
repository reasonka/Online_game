using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameSettingsUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject settingsPanel;
    public Button openSettingsButton;
    public Button closeSettingsButton;

    [Header("Voice Volume")]
    public Slider voiceVolumeSlider;
    public TMP_Text voiceVolumeValueText;

    [Header("Mouse Sensitivity")]
    public Slider mouseSensitivitySlider;
    public TMP_Text mouseSensitivityValueText;

    [Header("Default Values")]
    public float defaultVoiceVolume = 1f;
    public float defaultMouseSensitivity = 2f;

    public const string VoiceVolumeKey = "VoiceVolume";
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

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void LoadSettings()
    {
        float voiceVolume =
            PlayerPrefs.GetFloat(VoiceVolumeKey, defaultVoiceVolume);

        float mouseSensitivity =
            PlayerPrefs.GetFloat(MouseSensitivityKey, defaultMouseSensitivity);

        if (voiceVolumeSlider != null)
            voiceVolumeSlider.value = voiceVolume;

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.value = mouseSensitivity;

        SetVoiceVolume(voiceVolume);
        SetMouseSensitivity(mouseSensitivity);
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
    }
}