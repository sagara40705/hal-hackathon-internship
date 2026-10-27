using UnityEngine;

/// <summary>
/// 高速化中だけ中央に表示する、アニメーションUI
/// 制作者：sagara
/// </summary>
[DisallowMultipleComponent]
public class HightSpeedIndicator : UnityEngine.UI.MaskableGraphic
{
    [SerializeField, Tooltip("高速化コントローラーへの参照")]
    private HighSpeedController _controller;

    [SerializeField, Min(0.1f), Tooltip("サイクル秒数")]
    private float _cycleSeconds = 0.8f;

    [SerializeField, Min(0f), Tooltip("アウトラインの幅")]
    private float _outlineWidth = 2f;

    [SerializeField, Tooltip("アウトラインの色")]
    private Color _outlineColor = new Color(0f, 0f, 0f, 0.4f);

    private float _elapsed;
    private bool _wasVisible;

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
        _elapsed = 0f;
        _wasVisible = false;
    }

    private void LateUpdate()
    {
        bool visible = _controller != null && _controller.isActiveAndEnabled && _controller.IsSpeedModified;
        if (visible)
        {
            _elapsed = _wasVisible ? _elapsed + Time.unscaledDeltaTime : 0f;
            SetVerticesDirty();
        }
        else if (_wasVisible)
        {
            _elapsed = 0f;
            SetVerticesDirty();
        }
        _wasVisible = visible;
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        if (_controller == null || !_controller.isActiveAndEnabled || !_controller.IsSpeedModified) return;
        Rect rect = GetPixelAdjustedRect();
        float phase = _elapsed / Mathf.Max(0.1f, _cycleSeconds);
        float entrance = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / 0.12f));
        for (int i = 0; i < 3; i++)
        {
            // 3個が左から順に発光。移動は数pxに抑えて盤面の視認性を保つ。
            float pulse = Mathf.Pow(0.5f + 0.5f * Mathf.Cos((phase - i / 3f) * Mathf.PI * 2f), 2f);
            float left = rect.xMin + rect.width * (0.06f + i * 0.31f) + pulse * 3f;
            Color tint = color;
            tint.a *= entrance * Mathf.Lerp(0.35f, 1f, pulse);
            Vector3 top = new Vector3(left, rect.yMin + rect.height * 0.9f);
            Vector3 tip = new Vector3(left + rect.width * 0.24f, rect.center.y);
            Vector3 bottom = new Vector3(left, rect.yMin + rect.height * 0.1f);
            // 塗りの下に黒を重ねず、輪郭の外側だけを描く。
            float halfHeight = rect.height * 0.4f;
            float width = rect.width * 0.24f;
            float radius = width * halfHeight / (halfHeight + Mathf.Sqrt(width * width + halfHeight * halfHeight));
            if (_outlineWidth > 0f && radius > 0.001f)
            {
                Vector3 center = new Vector3(left + radius, rect.center.y);
                float expansion = 1f + _outlineWidth / radius;
                Color border = _outlineColor;
                border.a *= entrance * Mathf.Lerp(0.35f, 1f, pulse);
                AddBorder(vh, top, tip, center + (top - center) * expansion, center + (tip - center) * expansion, border);
                AddBorder(vh, tip, bottom, center + (tip - center) * expansion, center + (bottom - center) * expansion, border);
                AddBorder(vh, bottom, top, center + (bottom - center) * expansion, center + (top - center) * expansion, border);
            }
            int start = vh.currentVertCount;
            vh.AddVert(top, tint, Vector2.zero);
            vh.AddVert(tip, tint, Vector2.zero);
            vh.AddVert(bottom, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }
    }

    private static void AddBorder(UnityEngine.UI.VertexHelper vh, Vector3 a, Vector3 b, Vector3 outerA, Vector3 outerB, Color tint)
    {
        int start = vh.currentVertCount;
        vh.AddVert(a, tint, Vector2.zero);
        vh.AddVert(outerA, tint, Vector2.zero);
        vh.AddVert(outerB, tint, Vector2.zero);
        vh.AddVert(b, tint, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
