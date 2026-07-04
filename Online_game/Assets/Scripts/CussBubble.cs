using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Shows a quick comic-style "@#$%!" burst above the character's head to express
/// anger without needing voice/text — good fit for a mute character.
/// Call ShowCuss() from anywhere (input, game event, etc.) to trigger it.
/// </summary>
public class CussBubble : MonoBehaviour
{
    [Header("Setup")]
    public TextMeshProUGUI cussText;      // the TMP text inside the bubble
    public GameObject bubbleRoot;         // parent object to scale/shake (usually this Canvas or a child panel)

    [Header("Symbols")]
    [TextArea]
    public string[] symbolSets =
    {
        "@#$%!",
        "%!*#@",
        "#&$%*!",
        "$@!#%^",
        "*@#&!"
    };

    [Header("Timing")]
    public float popInDuration = 0.15f;
    public float holdDuration = 0.8f;
    public float shakeAmount = 8f;       // in local UI units
    public float fadeOutDuration = 0.3f;

    private CanvasGroup _canvasGroup;
    private Coroutine _running;

    void Awake()
    {
        if (bubbleRoot == null) bubbleRoot = gameObject;

        _canvasGroup = bubbleRoot.GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = bubbleRoot.AddComponent<CanvasGroup>();

        bubbleRoot.SetActive(false);
    }

    /// <summary>Call this to make her "cuss" — plays the pop/shake/fade burst.</summary>
    public void ShowCuss()
    {
        ShowCuss(holdDuration);
    }

    /// <summary>Same as ShowCuss(), but lets you override how long it holds before fading.</summary>
    public void ShowCuss(float customHoldDuration)
    {
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(PlayCussAnimation(customHoldDuration));
    }

    IEnumerator PlayCussAnimation(float holdOverride)
    {
        bubbleRoot.SetActive(true);
        cussText.text = symbolSets[Random.Range(0, symbolSets.Length)];
        _canvasGroup.alpha = 1f;

        Vector3 baseScale = Vector3.one;
        Vector3 basePos = bubbleRoot.transform.localPosition;

        // Pop in (overshoot slightly for a punchy feel)
        float t = 0f;
        while (t < popInDuration)
        {
            t += Time.deltaTime;
            float p = t / popInDuration;
            float scale = Mathf.SmoothStep(0f, 1.2f, p);
            bubbleRoot.transform.localScale = baseScale * scale;
            yield return null;
        }
        bubbleRoot.transform.localScale = baseScale * 1.2f;

        // Settle to normal scale
        t = 0f;
        while (t < 0.08f)
        {
            t += Time.deltaTime;
            bubbleRoot.transform.localScale = Vector3.Lerp(baseScale * 1.2f, baseScale, t / 0.08f);
            yield return null;
        }
        bubbleRoot.transform.localScale = baseScale;

        // Hold with a little shake
        t = 0f;
        while (t < holdOverride)
        {
            t += Time.deltaTime;
            Vector3 shakeOffset = new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                0f
            ) * 0.01f; // scaled down since world-space canvases are usually tiny
            bubbleRoot.transform.localPosition = basePos + shakeOffset;
            yield return null;
        }
        bubbleRoot.transform.localPosition = basePos;

        // Fade out
        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
            yield return null;
        }

        bubbleRoot.SetActive(false);
        _running = null;
    }
}