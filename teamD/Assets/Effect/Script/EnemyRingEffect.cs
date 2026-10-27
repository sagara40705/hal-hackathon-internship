using TMPro;
using UnityEngine;
using UnityEngine.Rendering;


/// <summary>
/// 作成者 : 酒井康佑
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class EnemyRingEffect : MonoBehaviour
{
    public enum EffectMode
    {
        ENUM_Rotate, // 弾の周囲で回転する
        ENUM_Expand  // 外側へ広がって消える
    }

    [Header("種類")]
    [SerializeField]
    private EffectMode _mode = EffectMode.ENUM_Rotate;

    [Header("見た目")]
    [SerializeField]
    private Material _ringMaterial;

    [SerializeField]
    private Color _color = Color.white;

    [SerializeField, Range(16, 128)]
    private int _segments = 64;

    [SerializeField, Min(0.001f)]
    private float _width = 0.04f;

    [Header("回転リング")]
    [SerializeField, Min(0.01f)]
    private float _radius = 0.4f;

    [SerializeField, Tooltip("1秒間に回転する角度")]
    private Vector3 _rotationSpeed = new Vector3(120f, 180f, 0f);

    [Header("広がる衝撃波")]
    [SerializeField, Min(0.01f)]
    private float _startRadius = 0.15f;

    [SerializeField, Min(0.01f)]
    private float _endRadius = 1.5f;

    [SerializeField, Min(0.01f)]
    private float _duration = 0.45f;

    private LineRenderer _line;
    private Vector3[] _points;
    private float _elapsedTime;

    private void OnEnable()
    {
        _line = GetComponent<LineRenderer>();
        _elapsedTime = 0f;

        int count = Mathf.Max(16, _segments);
        _points = new Vector3[count];

        // 親オブジェクトの移動・回転についていく
        _line.useWorldSpace = false; 

        // 最後の点と最初の点をつないで円にする
        _line.loop = true;
        _line.positionCount = count;

        // 円を描くXY平面に沿って線を表示する
        _line.alignment = LineAlignment.TransformZ;
        _line.sharedMaterial = _ringMaterial;

        // 線を太さを均一にする
        _line.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        _line.widthMultiplier = _width;


        _line.shadowCastingMode = ShadowCastingMode.Off;
        _line.receiveShadows = false;


        SetColor(_color);

        float radius = _mode == EffectMode.ENUM_Rotate ? _radius : _startRadius;
        DrawRing(radius);
    }

    private void Update()
    {
        if(_mode == EffectMode.ENUM_Rotate)
        {
            // リングの見た目だけを回転させる
            transform.Rotate(_rotationSpeed * Time.deltaTime, Space.Self);

            return;
        }

       _elapsedTime += Time.deltaTime;

        float t = Mathf.Clamp01(_elapsedTime / Mathf.Max(0.01f, _duration));

        // 最初は早く広がり、最後はゆっくりになる
        float eased = 1f - Mathf.Pow(1f - t, 3f);

        float radius = Mathf.Lerp(_startRadius, _endRadius, eased);

        DrawRing(radius);

        // 広がるにつれて透明にする
        Color color = _color;
        color.a *= 1f - t;
        SetColor(color);

        // 線も少し細くする
        _line.widthMultiplier = Mathf.Lerp(_width, _width * 0.2f, t);

        if (t >= 1f) 
            Destroy(gameObject);
    }

    // XY平面に円を描く
    private void DrawRing(float radius)
    {
        for (int i = 0; i < _points.Length; i++) 
        {
            float angle = i / (float)_points.Length * Mathf.PI * 2f;

            _points[i] =
                new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        _line.SetPositions(_points);
    }

    // 線全体へ同じ色と透明度を設定する
    private void SetColor(Color color)
    {
        _line.startColor = color;
        _line.endColor = color;
    }

}
