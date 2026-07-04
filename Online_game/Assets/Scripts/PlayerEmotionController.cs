using UnityEngine;

/// <summary>
/// Receives the chosen emotion from EmojiWheelController and plays the matching
/// effect for a fixed duration (default 10 seconds).
/// </summary>
public class PlayerEmotionController : MonoBehaviour
{
    [Header("Effects")]
    public CussBubble angryEffect;   // cussing symbols from the mouth
    public HeartBubble happyEffect;  // small hearts above the head
    public CryBubble cryingEffect;   // falling tears

    [Header("Timing")]
    public float emotionDuration = 10f;

    public void PlayEmotion(EmotionType emotion)
    {
        switch (emotion)
        {
            case EmotionType.Angry:
                if (angryEffect != null) angryEffect.ShowCuss(emotionDuration);
                break;

            case EmotionType.Happy:
                if (happyEffect != null) happyEffect.Play(emotionDuration);
                break;

            case EmotionType.Crying:
                if (cryingEffect != null) cryingEffect.Play(emotionDuration);
                break;
        }
    }
}