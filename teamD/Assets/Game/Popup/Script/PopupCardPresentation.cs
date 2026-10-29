using UnityEngine;

/// <summary>説明画像とボタンを同じ基準サイズで配置し、画面内に収める。</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public sealed class PopupCardPresentation : MonoBehaviour
{
    [SerializeField] private Vector2 _referenceSize = new Vector2(1920, 1080);
    [SerializeField, Range(0.5f, 1f)] private float _screenCoverage = 0.94f;
    [SerializeField, Min(0)] private float _appearDuration = 0.18f;
    private RectTransform _rect;
    private CanvasGroup _group;
    private float _elapsed;

    private void OnEnable()
    {
        _rect = (RectTransform)transform;
        _group = GetComponent<CanvasGroup>();
        _elapsed = Application.isPlaying ? 0 : _appearDuration;
        Apply();
    }

    private void LateUpdate()
    {
        _elapsed += Application.isPlaying ? Time.unscaledDeltaTime : _appearDuration;
        Apply();
    }

    private void Apply()
    {
        if (_rect == null || !(_rect.parent is RectTransform parent)) return;
        float width = Mathf.Max(1, _referenceSize.x);
        float height = Mathf.Max(1, _referenceSize.y);
        _rect.anchorMin = _rect.anchorMax = _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.anchoredPosition = Vector2.zero;
        _rect.sizeDelta = new Vector2(width, height);
        // CanvasのないPrefab単体プレビューでは基準サイズを維持する。
        float fit = parent.rect.width > 0 && parent.rect.height > 0
            ? Mathf.Min(parent.rect.width / width, parent.rect.height / height) * _screenCoverage
            : 1;
        float t = _appearDuration <= 0 ? 1 : Mathf.Clamp01(_elapsed / _appearDuration);
        float eased = 1 - Mathf.Pow(1 - t, 3);
        _rect.localScale = Vector3.one * Mathf.Max(0.001f, fit) * Mathf.Lerp(0.985f, 1, eased);
        _group.alpha = eased;
    }
}
