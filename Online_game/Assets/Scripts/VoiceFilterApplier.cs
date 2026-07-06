using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class VoiceFilterApplier : MonoBehaviourPunCallbacks
{
    public string voiceFilterPropertyKey = "VoiceFilter";

    private AudioSource audioSource;
    private AudioHighPassFilter highPass;
    private AudioLowPassFilter lowPass;
    private AudioDistortionFilter distortion;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        highPass = GetComponent<AudioHighPassFilter>();
        if (highPass == null)
            highPass = gameObject.AddComponent<AudioHighPassFilter>();

        lowPass = GetComponent<AudioLowPassFilter>();
        if (lowPass == null)
            lowPass = gameObject.AddComponent<AudioLowPassFilter>();

        distortion = GetComponent<AudioDistortionFilter>();
        if (distortion == null)
            distortion = gameObject.AddComponent<AudioDistortionFilter>();
    }

    private void Start()
    {
        ApplyOwnerVoiceFilter();
    }

    public override void OnPlayerPropertiesUpdate(
        Player targetPlayer,
        Hashtable changedProps)
    {
        if (photonView.Owner == targetPlayer &&
            changedProps.ContainsKey(voiceFilterPropertyKey))
        {
            ApplyOwnerVoiceFilter();
        }
    }

    private void ApplyOwnerVoiceFilter()
    {
        int filterIndex = 0;

        if (photonView.Owner != null &&
            photonView.Owner.CustomProperties.TryGetValue(
                voiceFilterPropertyKey,
                out object value))
        {
            filterIndex = (int)value;
        }

        ApplyFilter(filterIndex);
    }

    private void ApplyFilter(int filterIndex)
    {
        ResetFilter();

        if (audioSource == null)
            return;

        if (filterIndex == 0)
        {
            audioSource.pitch = 1f;
        }
        else if (filterIndex == 1)
        {
            audioSource.pitch = 1.25f;
        }
        else if (filterIndex == 2)
        {
            audioSource.pitch = 0.95f;

            highPass.enabled = true;
            highPass.cutoffFrequency = 600f;

            lowPass.enabled = true;
            lowPass.cutoffFrequency = 3500f;

            distortion.enabled = true;
            distortion.distortionLevel = 0.25f;
        }
    }

    private void ResetFilter()
    {
        if (audioSource != null)
            audioSource.pitch = 1f;

        if (highPass != null)
            highPass.enabled = false;

        if (lowPass != null)
            lowPass.enabled = false;

        if (distortion != null)
            distortion.enabled = false;
    }
}