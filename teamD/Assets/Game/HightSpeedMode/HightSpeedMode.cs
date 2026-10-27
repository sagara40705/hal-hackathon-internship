using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>押している間だけゲーム全体を高速化する早送りUI。</summary>
[DisallowMultipleComponent]
public class HightSpeedMode : UnityEngine.UI.MaskableGraphic,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField, Min(1f)] private float _speedMultiplier = 2.5f;
    private int? _pointerId;
    private bool _isHolding;
    private float _originalTimeScale;
    private float _appliedTimeScale;

    public bool IsSpeedModified => _isHolding && Time.timeScale > 0f
        && Mathf.Approximately(Time.timeScale, _appliedTimeScale);

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsActive() || _pointerId.HasValue || eventData.button != PointerEventData.InputButton.Left)
            return;
        _pointerId = eventData.pointerId;
        ApplySpeed();
        SetVerticesDirty();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_pointerId == eventData.pointerId) Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_pointerId == eventData.pointerId) Release();
    }

    private void Update()
    {
        if (_pointerId.HasValue) ApplySpeed();
    }

    private void ApplySpeed()
    {
        // ポーズやヒットストップは解除しない。
        if (Time.timeScale <= 0f) return;
        if (!_isHolding)
        {
            _originalTimeScale = Time.timeScale;
            _appliedTimeScale = _originalTimeScale * Mathf.Max(1f, _speedMultiplier);
            _isHolding = true;
        }
        Time.timeScale = _appliedTimeScale;
    }

    private void Release()
    {
        if (_isHolding && Mathf.Approximately(Time.timeScale, _appliedTimeScale))
            Time.timeScale = _originalTimeScale;
        _isHolding = false;
        _pointerId = null;
        SetVerticesDirty();
    }

    protected override void OnDisable()
    {
        Release();
        base.OnDisable();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) Release();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) Release();
    }

    // 円の外側はクリックを通す。
    public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
    {
        if (!base.Raycast(screenPoint, eventCamera)) return false;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 local);
        Rect rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        return (local - rect.center).sqrMagnitude <= radius * radius;
    }

    // テクスチャ不要の円と二つの三角形。Prefabだけで表示できる。
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        Color32 background = _pointerId.HasValue ? new Color(0.32f, 0.36f, 0.36f, 1f) : color;
        vh.AddVert(center, background, Vector2.zero);
        const int segments = 96;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, background, Vector2.zero);
            if (i > 0) vh.AddTriangle(0, i, i + 1);
        }
        for (int i = 0; i < 2; i++)
        {
            float left = i == 0 ? -0.46f : 0.09f;
            int start = vh.currentVertCount;
            vh.AddVert(center + new Vector2(left, 0.47f) * radius, Color.white, Vector2.zero);
            vh.AddVert(center + new Vector2(left + 0.49f, 0f) * radius, Color.white, Vector2.zero);
            vh.AddVert(center + new Vector2(left, -0.47f) * radius, Color.white, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
