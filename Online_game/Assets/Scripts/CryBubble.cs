using UnityEngine;
using System.Collections;

/// <summary>
/// Spawns small tears from each eye that fall straight down and fade, repeating
/// for a given duration. Attach to a world-space Canvas positioned at eye height
/// (parent it to the Head bone, same as HeartBubble).
/// </summary>
public class CryBubble : MonoBehaviour
{
    [Header("Setup")]
    public RectTransform tearPrefab;      // a small teardrop Image prefab (UI)
    public RectTransform leftEyeSpawn;    // local position under the left eye
    public RectTransform rightEyeSpawn;   // local position under the right eye

    [Header("Timing")]
    public float spawnInterval = 0.5f;
    public float fallDistance = 40f;      // local units, how far down each tear falls
    public float fallDuration = 1f;

    private Coroutine _running;

    /// <summary>Plays repeated tears from both eyes for the given total duration (e.g. 10 seconds).</summary>
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
            SpawnTear(leftEyeSpawn);
            SpawnTear(rightEyeSpawn);
            yield return new WaitForSeconds(spawnInterval);
            elapsed += spawnInterval;
        }
        _running = null;
    }

    void SpawnTear(RectTransform spawnPoint)
    {
        if (tearPrefab == null || spawnPoint == null) return;

        // Parent into the Canvas (this object), NOT the bone it's attached to —
        // UI Images only render when they're inside a Canvas hierarchy.
        RectTransform tear = Instantiate(tearPrefab, transform);
        tear.localPosition = spawnPoint.localPosition;
        tear.rotation = Quaternion.identity; // stay upright regardless of any parent tilt
        StartCoroutine(AnimateTear(tear));
    }

    IEnumerator AnimateTear(RectTransform tear)
    {
        Vector3 startPos = tear.localPosition;
        Vector3 endPos = startPos - new Vector3(0f, fallDistance, 0f);

        CanvasGroup cg = tear.GetComponent<CanvasGroup>();
        if (cg == null) cg = tear.gameObject.AddComponent<CanvasGroup>();

        float t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float p = t / fallDuration;
            tear.localPosition = Vector3.Lerp(startPos, endPos, p);
            tear.rotation = Quaternion.identity;
            cg.alpha = 1f - p;
            yield return null;
        }

        Destroy(tear.gameObject);
    }
}