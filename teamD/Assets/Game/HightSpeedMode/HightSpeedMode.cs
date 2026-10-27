using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>高速化ボタンの入力と描画。速度管理はHighSpeedControllerに委譲する。</summary>
[DisallowMultipleComponent]
public class HightSpeedMode : UnityEngine.UI.MaskableGraphic,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private HighSpeedController _controller;
    private int? _pointerId;
    public bool CanSpeedUp => _controller != null && _controller.CanSpeedUp;
    public bool IsSpeedModified => _controller != null && _controller.IsSpeedModified;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (_controller != null) _controller.StateChanged += OnStateChanged;
        OnStateChanged();
    }

    private void OnStateChanged()
    {
        if (_controller == null || !_controller.IsRequested) _pointerId = null;
        SetVerticesDirty();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsActive() || !CanSpeedUp || _pointerId.HasValue || eventData.button != PointerEventData.InputButton.Left) return;
        if (!_controller.BeginSpeedUp()) return;
        _pointerId = eventData.pointerId;
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

    private void Release()
    {
        if (_pointerId.HasValue && _controller != null) _controller.EndSpeedUp();
        _pointerId = null;
        SetVerticesDirty();
    }

    protected override void OnDisable()
    {
        Release();
        if (_controller != null) _controller.StateChanged -= OnStateChanged;
        base.OnDisable();
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
        if (!CanSpeedUp) background = new Color(0.35f, 0.35f, 0.35f, 0.4f);
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
            Color iconColor = CanSpeedUp ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            vh.AddVert(center + new Vector2(left, 0.47f) * radius, iconColor, Vector2.zero);
            vh.AddVert(center + new Vector2(left + 0.49f, 0f) * radius, iconColor, Vector2.zero);
            vh.AddVert(center + new Vector2(left, -0.47f) * radius, iconColor, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
