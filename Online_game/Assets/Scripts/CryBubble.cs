using UnityEngine;
using System.Collections;

/// <summary>
/// Spawns small tear drops that fall and fade, repeating for a given duration.
/// Attach to a world-space Canvas positioned near the character's eyes/cheeks.
/// </summary>
public class CryBubble : MonoBehaviour
{
    [Header("Setup")]
    public RectTransform tearPrefab;      // a small teardrop Image prefab (UI)
    public RectTransform spawnPoint;      // where tears spawn from (usually this object)

    [Header("Timing")]
    public float spawnInterval = 0.5f;
    public float fallDistance = 50f;      // local UI units
    public float fallDuration = 1f;

    private Coroutine _running;

    /// <summary>Plays repeated tear drops for the given total duration (e.g. 10 seconds).</summary>
    public void Play(float duration)
    {
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(RunForDuration(duration));
    }

    public void Stop()
    {
        if (_running != null) StopCoroutine(_running);
        _running = null;
    }

    IEnumerator RunForDuration(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            SpawnTear();
            yield return new WaitForSeconds(spawnInterval);
            elapsed += spawnInterval;
        }
        _running = null;
    }

    void SpawnTear()
    {
        if (tearPrefab == null || spawnPoint == null) return;

        RectTransform tear = Instantiate(tearPrefab, spawnPoint.parent);
        tear.anchoredPosition = spawnPoint.anchoredPosition + new Vector2(Random.Range(-8f, 8f), 0f);
        StartCoroutine(AnimateTear(tear));
    }

    IEnumerator AnimateTear(RectTransform tear)
    {
        Vector2 startPos = tear.anchoredPosition;
        Vector2 endPos = startPos - new Vector2(0f, fallDistance);

        CanvasGroup cg = tear.GetComponent<CanvasGroup>();
        if (cg == null) cg = tear.gameObject.AddComponent<CanvasGroup>();

        float t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float p = t / fallDuration;
            tear.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
            cg.alpha = 1f - p;
            yield return null;
        }

        Destroy(tear.gameObject);
    }
}