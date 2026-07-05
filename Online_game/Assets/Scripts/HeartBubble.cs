using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HeartBubble : MonoBehaviour
{
    [Header("Setup")]
    public RectTransform heartPrefab;
    public RectTransform orbitCenter;

    [Header("Orbit")]
    public int heartCount = 4;
    public float orbitRadius = 60f;
    public float orbitSpeed = 90f;
    public float bobAmount = 8f;
    public float bobSpeed = 2f;

    [Header("Timing")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.5f;

    private Coroutine runningCoroutine;
    private readonly List<RectTransform> activeHearts = new List<RectTransform>();

    private void Awake()
    {
        if (orbitCenter == null)
            orbitCenter = GetComponent<RectTransform>();

        if (heartPrefab != null)
            heartPrefab.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        ClearHearts();
    }

    public void Play(float duration)
    {
        gameObject.SetActive(true);

        if (runningCoroutine != null)
            StopCoroutine(runningCoroutine);

        ClearHearts();

        runningCoroutine = StartCoroutine(RunOrbit(duration));
    }

    public void Stop()
    {
        if (runningCoroutine != null)
            StopCoroutine(runningCoroutine);

        runningCoroutine = null;
        ClearHearts();
    }

    private IEnumerator RunOrbit(float duration)
    {
        if (heartPrefab == null)
        {
            Debug.LogWarning("HeartBubble: Heart Prefab is missing.");
            yield break;
        }

        if (orbitCenter == null)
        {
            Debug.LogWarning("HeartBubble: Orbit Center is missing.");
            yield break;
        }

        for (int i = 0; i < heartCount; i++)
        {
            RectTransform heart = Instantiate(heartPrefab, orbitCenter);
            heart.gameObject.SetActive(true);

            heart.localPosition = Vector3.zero;
            heart.localRotation = Quaternion.identity;
            heart.localScale = Vector3.zero;

            CanvasGroup canvasGroup = heart.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = heart.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;

            activeHearts.Add(heart);
        }

        float angleStep = 360f / heartCount;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            float scale = 1f;
            float alpha = 1f;

            if (t < fadeInDuration)
            {
                float value = t / fadeInDuration;
                scale = Mathf.SmoothStep(0f, 1f, value);
                alpha = Mathf.SmoothStep(0f, 1f, value);
            }
            else if (t > duration - fadeOutDuration)
            {
                float value = (t - (duration - fadeOutDuration)) / fadeOutDuration;
                scale = Mathf.SmoothStep(1f, 0f, value);
                alpha = Mathf.SmoothStep(1f, 0f, value);
            }

            for (int i = 0; i < activeHearts.Count; i++)
            {
                RectTransform heart = activeHearts[i];

                if (heart == null)
                    continue;

                float angle = t * orbitSpeed + angleStep * i;
                float rad = angle * Mathf.Deg2Rad;

                float bob = Mathf.Sin(t * bobSpeed + i) * bobAmount;

                // OLD STYLE DIRECTION:
                // Circle uses X/Z.
                // Y only goes up/down slightly.
                Vector3 offset =
                    new Vector3(
                        Mathf.Cos(rad),
                        0f,
                        Mathf.Sin(rad)
                    ) * orbitRadius;

                heart.localPosition = offset + new Vector3(0f, bob, 0f);
                heart.localScale = Vector3.one * scale;

                // keep heart upright
                heart.rotation = Quaternion.identity;

                CanvasGroup canvasGroup = heart.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                    canvasGroup.alpha = alpha;
            }

            yield return null;
        }

        ClearHearts();
        runningCoroutine = null;
    }

    private void ClearHearts()
    {
        for (int i = 0; i < activeHearts.Count; i++)
        {
            if (activeHearts[i] != null)
                Destroy(activeHearts[i].gameObject);
        }

        activeHearts.Clear();
    }
}