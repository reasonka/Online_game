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
    private AudioChorusFilter chorus;

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

        chorus = GetComponent<AudioChorusFilter>();
        if (chorus == null)
            chorus = gameObject.AddComponent<AudioChorusFilter>();
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
            // Normal
            audioSource.pitch = 1f;
        }
        else if (filterIndex == 1)
        {
            // Cute voice
            audioSource.pitch = 1.45f;

            chorus.enabled = true;
            chorus.depth = 0.25f;
            chorus.rate = 1.5f;
            chorus.wetMix1 = 0.35f;
            chorus.wetMix2 = 0.2f;
            chorus.wetMix3 = 0.1f;
        }
        else if (filterIndex == 2)
        {
            // Strong robot / radio voice
            audioSource.pitch = 0.85f;

            highPass.enabled = true;
            highPass.cutoffFrequency = 900f;

            lowPass.enabled = true;
            lowPass.cutoffFrequency = 2200f;

            distortion.enabled = true;
            distortion.distortionLevel = 0.55f;

            chorus.enabled = true;
            chorus.depth = 0.7f;
            chorus.rate = 8f;
            chorus.wetMix1 = 0.5f;
            chorus.wetMix2 = 0.35f;
            chorus.wetMix3 = 0.25f;
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

        if (chorus != null)
            chorus.enabled = false;
    }
}