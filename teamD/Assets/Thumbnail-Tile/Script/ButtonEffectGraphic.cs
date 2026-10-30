using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI上に、円の輪郭または星を描く。
/// 作成者 : 酒井康佑
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
public class ButtonEffectGraphic : MaskableGraphic
{
    private bool _isRing;
    private float _fillAmount = 1f;

    // クールタイム表示では輪を0～1の割合で描く。通常の輪は1のまま。
    public void SetFillAmount(float value)
    {
        _fillAmount = Mathf.Clamp01(value);
        SetVerticesDirty();
    }
    private float _lineWidth = 4f;

    /// <summary>
    /// 描く形・色・線の太さを設定する。
    /// </summary>
    public void Configure(bool isRing, Color effectColor, float lineWidth)
    {
        _isRing = isRing;
        _lineWidth = lineWidth;
        color = effectColor;

        // エフェクトがボタンのクリックを遮らないようにする。
        raycastTarget = false;

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;

        if (radius <= 0f)
            return;

        if (_isRing)
            DrawRing(vh, rect.center, radius);
        else
            DrawStar(vh, rect.center, radius);
    }

    // 中央が透明な、円の輪郭を描く。
    private void DrawRing(VertexHelper vh, Vector2 center, float radius)
    {
        const int segments = 64;
        float innerRadius = Mathf.Max(0f, radius - _lineWidth);

        for (int i = 0; i < segments; i++)
        {
            float angleA = Mathf.PI * 0.5f - i * Mathf.PI * 2f / segments * _fillAmount;
            float angleB = Mathf.PI * 0.5f - (i + 1) * Mathf.PI * 2f / segments * _fillAmount;

            Vector2 directionA =
                new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA));

            Vector2 directionB =
                new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB));

            int index = vh.currentVertCount;

            vh.AddVert(center + directionA * radius, color, Vector2.zero);
            vh.AddVert(center + directionB * radius, color, Vector2.zero);
            vh.AddVert(center + directionB * innerRadius, color, Vector2.zero);
            vh.AddVert(center + directionA * innerRadius, color, Vector2.zero);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }
    }

    // 外側と内側の頂点を交互に並べ、五角の星を描く。
    private void DrawStar(VertexHelper vh, Vector2 center, float radius)
    {
        const int pointCount = 10;

        vh.AddVert(center, color, Vector2.zero);

        for (int i = 0; i < pointCount; i++)
        {
            float angle = (90f + i * 36f) * Mathf.Deg2Rad;
            float pointRadius = i % 2 == 0 ? radius : radius * 0.45f;

            Vector2 direction =
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            vh.AddVert(
                center + direction * pointRadius,
                color,
                Vector2.zero
            );
        }

        for (int i = 0; i < pointCount; i++)
        {
            vh.AddTriangle(
                0,
                i + 1,
                (i + 1) % pointCount + 1
            );
        }
    }
}
