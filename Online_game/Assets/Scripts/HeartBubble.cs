using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spawns a small ring of hearts that orbit around the character's head for a
/// given duration, then fade out. Attach to a world-space Canvas positioned
/// at/above the head bone (parent it to the Head bone in the rig for best results).
/// </summary>
public class HeartBubble : MonoBehaviour
{
    [Header("Setup")]
    public RectTransform heartPrefab;   // a small heart Image prefab (UI)
    public RectTransform orbitCenter;   // usually this object itself

    [Header("Orbit")]
    public int heartCount = 4;
    public float orbitRadius = 60f;     // local UI units
    public float orbitSpeed = 90f;      // degrees per second
    public float bobAmount = 8f;        // small vertical wobble for a livelier feel
    public float bobSpeed = 2f;

    [Header("Timing")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.5f;

    private Coroutine _running;
    private List<RectTransform> _activeHearts = new List<RectTransform>();

    /// <summary>Plays an orbiting heart ring for the given total duration (e.g. 10 seconds).</summary>
    public void Play(float duration)
    {
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(RunOrbit(duration));
    }

    public void Stop()
    {
        if (_running != null) StopCoroutine(_running);
        _running = null;
        foreach (var h in _activeHearts)
            if (h != null) Destroy(h.gameObject);
        _activeHearts.Clear();
    }

    IEnumerator RunOrbit(float duration)
    {
        if (heartPrefab == null || orbitCenter == null) yield break;

        // Spawn the hearts, evenly spaced around the circle
        _activeHearts.Clear();
        for (int i = 0; i < heartCount; i++)
        {
            RectTransform heart = Instantiate(heartPrefab, orbitCenter.parent);
            heart.localScale = Vector3.zero; // start invisible, pop in below
            _activeHearts.Add(heart);
        }

        float angleStep = 360f / heartCount;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            // Fade in at the start, fade out near the end
            float scale = 1f;
            if (t < fadeInDuration) scale = Mathf.SmoothStep(0f, 1f, t / fadeInDuration);
            else if (t > duration - fadeOutDuration) scale = Mathf.SmoothStep(1f, 0f, (t - (duration - fadeOutDuration)) / fadeOutDuration);

            for (int i = 0; i < _activeHearts.Count; i++)
            {
                RectTransform heart = _activeHearts[i];
                if (heart == null) continue;

                float angle = (t * orbitSpeed) + (angleStep * i);
                float rad = angle * Mathf.Deg2Rad;
                float bob = Mathf.Sin(t * bobSpeed + i) * bobAmount;

                Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
                heart.anchoredPosition = orbitCenter.anchoredPosition + offset + new Vector2(0f, bob);
                heart.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        foreach (var h in _activeHearts)
            if (h != null) Destroy(h.gameObject);
        _activeHearts.Clear();
        _running = null;
    }
}