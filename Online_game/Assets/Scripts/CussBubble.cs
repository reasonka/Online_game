using UnityEngine;

/// <summary>
/// Fires a Particle System burst (cuss symbols) from the character's mouth to
/// express anger without needing voice/text — good fit for a mute character.
/// Call ShowCuss() from anywhere (input, game event, etc.) to trigger it.
/// Assign a Particle System configured with your cuss sprite as its texture.
/// </summary>
public class CussBubble : MonoBehaviour
{
    [Header("Setup")]
    public ParticleSystem particles;

    /// <summary>Call this to make her "cuss" — fires the particle burst.</summary>
    public void ShowCuss()
    {
        ShowCuss(0f); // duration ignored; the Particle System's own settings control lifetime
    }

    /// <summary>Same as ShowCuss(), kept for compatibility with existing callers that pass a duration.</summary>
    public void ShowCuss(float customHoldDuration)
    {
        if (particles == null) return;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.Play();
    }
}