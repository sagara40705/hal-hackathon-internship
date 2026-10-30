using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボタン中央から輪を広げ、星を3方向へ飛ばす。
/// 作成者 : 酒井康佑
/// </summary>
public class ButtonClickEffect : MonoBehaviour
{
    [SerializeField, Tooltip("エフェクトを出すボタン")]
    private RectTransform _buttonRect;

    [SerializeField, Tooltip("同じCanvas内のButtonEffectRoot")]
    private RectTransform _effectRoot;

    [SerializeField, Min(0.01f), Tooltip("エフェクトの表示時間")]
    private float _duration = 0.45f;

    [SerializeField, Tooltip("輪の色")]
    private Color _ringColor = Color.white;

    [SerializeField, Min(1f), Tooltip("輪の開始時の直径")]
    private float _ringStartSize = 60f;

    [SerializeField, Min(1f), Tooltip("輪の終了時の直径")]
    private float _ringEndSize = 220f;

    [SerializeField, Min(0.1f), Tooltip("輪郭線の太さ")]
    private float _ringWidth = 4f;

    // 途中で無効化された場合に、生成物を片付けるための一覧。
    private readonly List<GameObject> _activeEffects = new();

    /// <summary>
    /// CooldownButtonのOn Pressedから呼び出す。
    /// </summary>
    public void Play()
    {
        PlayAt(_buttonRect);
    }

    // 指定された枠の中央から、同じ設定の輪を再生する。
    public void PlayAt(RectTransform origin)
    {
        if (!isActiveAndEnabled ||
            origin == null ||
            _effectRoot == null ||
            !_effectRoot.gameObject.activeInHierarchy)
        {
            return;
        }

        // 同じCanvas内で、ボタンの中央をエフェクト用の座標へ変換する。
        Vector3 worldCenter =
            origin.TransformPoint(origin.rect.center);

        Vector3 localCenter =
            _effectRoot.InverseTransformPoint(worldCenter);

        // 今回の輪と星をまとめる親を作る。
        RectTransform burst = CreateRect(
            "ButtonClickBurst",
            _effectRoot
        );

        burst.localPosition = localCenter;
        _activeEffects.Add(burst.gameObject);

        StartCoroutine(Animate(burst));
    }

    /// <summary>
    /// 輪を広げながら透明にして消す。
    /// </summary>
    private IEnumerator Animate(RectTransform burst)
    {
        // 輪を1つ生成する。
        ButtonEffectGraphic ring = CreateGraphic(
            "Ring",
            burst,
            true,
            _ringColor
        );

        float duration = Mathf.Max(0.01f, _duration);
        float elapsed = 0f;

        while (elapsed < duration && burst != null)
        {
            float t = Mathf.Clamp01(elapsed / duration);

            // 最初は速く、最後はゆっくり広がる。
            float movement = 1f - Mathf.Pow(1f - t, 3f);

            float ringSize = Mathf.Lerp(
                _ringStartSize,
                _ringEndSize,
                movement
            );

            // 輪の大きさと透明度を更新する。
            ring.rectTransform.sizeDelta = Vector2.one * ringSize;
            ring.color = WithAlpha(_ringColor, 1f - t);

            yield return null;

            // ゲームが一時停止していても動かす。
            elapsed += Time.unscaledDeltaTime;
        }

        // 再生が終わったら削除する。
        if (burst != null)
        {
            _activeEffects.Remove(burst.gameObject);
            Destroy(burst.gameObject);
        }
    }

    // UI用のRectTransformを作る。
    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject instance = new GameObject(
            objectName,
            typeof(RectTransform)
        );

        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(100f, 100f);

        return rect;
    }

    // 円または星を表示するUIを作る。
    private ButtonEffectGraphic CreateGraphic(
        string objectName,
        Transform parent,
        bool isRing,
        Color effectColor)
    {
        RectTransform rect = CreateRect(objectName, parent);

        ButtonEffectGraphic graphic =
            rect.gameObject.AddComponent<ButtonEffectGraphic>();

        graphic.Configure(isRing, effectColor, _ringWidth);

        return graphic;
    }

    // 元の色を維持し、透明度だけを変える。
    private Color WithAlpha(Color source, float alpha)
    {
        source.a *= alpha;
        return source;
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        // 表示途中で無効になった場合も、エフェクトを残さない。
        foreach (GameObject effect in _activeEffects)
        {
            if (effect != null)
                Destroy(effect);
        }

        _activeEffects.Clear();
    }
}
