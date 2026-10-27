using UnityEngine;

/// <summary>
/// 高速化中だけ中央の画像3枚を順に点灯させる。
/// 制作者：sagara
/// </summary>
[DisallowMultipleComponent]
public class HightSpeedIndicator : MonoBehaviour
{
    [SerializeField, Tooltip("高速化コントローラーへの参照")]
    private HighSpeedController _controller;
    [SerializeField, Min(0.1f), Tooltip("サイクル秒数")]
    private float _cycleSeconds = 0.8f;
    [SerializeField] private UnityEngine.UI.Image[] _triangles;
    [SerializeField] private Color _tint = new Color(0.95f, 0.99f, 1f, 0.65f);
    private Vector2[] _positions;
    private float _elapsed;
    private bool _wasVisible;

    private void Awake()
    {
        _positions = new Vector2[_triangles.Length];
        for (int i = 0; i < _triangles.Length; i++)
        {
            if (_triangles[i] == null) continue;
            _positions[i] = _triangles[i].rectTransform.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        Hide();
        if (_controller != null) _controller.StateChanged += OnStateChanged;
    }

    private void OnDisable()
    {
        if (_controller != null) _controller.StateChanged -= OnStateChanged;
        Hide();
    }

    private void OnStateChanged()
    {
        if (_controller == null || !_controller.IsSpeedModified) Hide();
    }

    private void Hide()
    {
        _elapsed = 0f;
        _wasVisible = false;
        if (_triangles == null) return;
        foreach (var triangle in _triangles)
            if (triangle != null) triangle.enabled = false;
    }

    private void LateUpdate()
    {
        if (_controller == null || !_controller.IsSpeedModified)
        {
            if (_wasVisible) Hide();
            return;
        }
        _elapsed = _wasVisible ? _elapsed + Time.unscaledDeltaTime : 0f;
        _wasVisible = true;
        float phase = _elapsed / Mathf.Max(0.1f, _cycleSeconds);
        float entrance = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / 0.12f));
        for (int i = 0; i < _triangles.Length; i++)
        {
            var triangle = _triangles[i];
            if (triangle == null) continue;
            float pulse = Mathf.Pow(0.5f + 0.5f * Mathf.Cos((phase - i / 3f) * Mathf.PI * 2f), 2f);
            Color tint = _tint;
            tint.a *= entrance * Mathf.Lerp(0.35f, 1f, pulse);
            triangle.color = tint;
            triangle.rectTransform.anchoredPosition = _positions[i] + Vector2.right * (pulse * 3f);
            triangle.enabled = true;
        }
    }
}
