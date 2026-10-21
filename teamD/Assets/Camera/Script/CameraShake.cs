using UnityEngine;
using System.Collections;
public class CameraShake : MonoBehaviour
{

    public static CameraShake Instance;

    [Header("揺れの設定")]
    public float duration = 0.2f;
    public float magnitude = 0.2f;

    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    void Awake()
    {
        Instance = this;
    }

    public void Shake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.localPosition = originalPosition;
        }

        shakeCoroutine = StartCoroutine(ShakeCoroutine());
    }

    IEnumerator ShakeCoroutine()
    {
        originalPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // ランダムにカメラを揺らす
            Vector3 offset = Random.insideUnitSphere * magnitude;

            transform.localPosition = originalPosition + offset;

            // ヒットストップ中でも揺れる
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }
}
