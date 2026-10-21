using UnityEngine;
using System.Collections;
public class HitStop : MonoBehaviour
{
    public static HitStop Instance;

    [Header("停止時間")]
    public float stopDuration = 0.1f;

    private Coroutine stopCoroutine;

    void Awake()
    {
        Instance = this;
    }

    public void Stop()
    {
        if (stopCoroutine != null)
        {
            StopCoroutine(stopCoroutine);
        }

        stopCoroutine = StartCoroutine(StopCoroutine());
    }

    IEnumerator StopCoroutine()
    {
        Time.timeScale = 0f;

        // Time.timeScale = 0でも待機できる
        yield return new WaitForSecondsRealtime(stopDuration);

        Time.timeScale = 1f;
        stopCoroutine = null;
    }

    void OnDisable()
    {
        Time.timeScale = 1f;
    }
}
