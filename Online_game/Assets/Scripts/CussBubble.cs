using UnityEngine;
using System.Collections;

public class CussBubble : MonoBehaviour
{
    [Header("Setup")]
    public ParticleSystem particles;

    private Coroutine stopCoroutine;

    private void Awake()
    {
        if (particles == null)
            particles = GetComponentInChildren<ParticleSystem>(true);
    }

    public void ShowCuss()
    {
        ShowCuss(2f);
    }

    public void ShowCuss(float duration)
    {
        gameObject.SetActive(true);

        if (particles == null)
        {
            Debug.LogWarning("CussBubble: ParticleSystem is missing.");
            return;
        }

        particles.gameObject.SetActive(true);

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.Clear(true);
        particles.Play(true);

        if (stopCoroutine != null)
            StopCoroutine(stopCoroutine);

        if (duration > 0f)
            stopCoroutine = StartCoroutine(StopAfter(duration));
    }

    private IEnumerator StopAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        Stop();
    }

    public void Stop()
    {
        if (stopCoroutine != null)
            StopCoroutine(stopCoroutine);

        stopCoroutine = null;

        if (particles != null)
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}