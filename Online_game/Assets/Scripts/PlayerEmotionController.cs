using UnityEngine;

public class PlayerEmotionController : MonoBehaviour
{
    [Header("Effects")]
    public CussBubble angryEffect;
    public HeartBubble happyEffect;
    public CryBubble cryingEffect;

    [Header("Timing")]
    public float emotionDuration = 10f;

    private void Awake()
    {
        if (angryEffect == null)
            angryEffect = GetComponentInChildren<CussBubble>(true);

        if (happyEffect == null)
            happyEffect = GetComponentInChildren<HeartBubble>(true);

        if (cryingEffect == null)
            cryingEffect = GetComponentInChildren<CryBubble>(true);

        HideAllEffects();
    }

    private void Start()
    {
        HideAllEffects();
    }

    private void HideAllEffects()
    {
        if (angryEffect != null)
        {
            angryEffect.Stop();
            angryEffect.gameObject.SetActive(false);
        }

        if (happyEffect != null)
        {
            happyEffect.Stop();
            happyEffect.gameObject.SetActive(false);
        }

        if (cryingEffect != null)
        {
            cryingEffect.gameObject.SetActive(false);
        }
    }

    public void PlayEmotion(EmotionType emotion)
    {
        HideAllEffects();

        switch (emotion)
        {
            case EmotionType.Happy:
                if (happyEffect != null)
                {
                    happyEffect.gameObject.SetActive(true);
                    happyEffect.Play(emotionDuration);
                }
                else
                {
                    Debug.LogWarning("Happy effect missing.");
                }
                break;

            case EmotionType.Angry:
                if (angryEffect != null)
                {
                    angryEffect.gameObject.SetActive(true);
                    angryEffect.ShowCuss(emotionDuration);
                }
                else
                {
                    Debug.LogWarning("Angry effect missing.");
                }
                break;

            case EmotionType.Crying:
                if (cryingEffect != null)
                {
                    cryingEffect.gameObject.SetActive(true);
                    cryingEffect.Play(emotionDuration);
                }
                else
                {
                    Debug.LogWarning("Crying effect missing.");
                }
                break;
        }
    }
}