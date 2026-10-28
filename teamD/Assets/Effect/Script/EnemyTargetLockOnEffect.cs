using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 攻撃予告の四隅を収束させ、逆三角形を浮かせる表示専用の演出。
/// 攻撃やタイル破壊の処理は行わず、親の生成・削除に従う。
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyTargetLockOnEffect : MonoBehaviour
{
    [Header("表示")]
    [SerializeField] private Material _visualMaterial;
    [SerializeField] private Camera _viewCamera;
    [SerializeField] private Color _frameColor = new Color(1f, 0.32f, 0.07f, 1f);
    [SerializeField] private Color _markerColor = new Color(1f, 0.78f, 0.35f, 1f);
    [SerializeField] private Color _outlineColor = new Color(0.09f, 0.012f, 0.025f, 0.95f);

    [Header("四隅のロックオン")]
    [SerializeField, Min(0.1f)] private float _frameSize = 1.06f;
    [SerializeField, Min(0.01f)] private float _cornerLength = 0.2f;
    [SerializeField, Min(0.001f)] private float _lineWidth = 0.035f;
    [SerializeField, Min(0.001f)] private float _outlineWidth = 0.06f;
    [SerializeField, Min(0.01f)] private float _appearDuration = 0.25f;
    [SerializeField, Min(1f)] private float _appearScale = 1.45f;

    [Header("浮かぶ逆三角形")]
    [SerializeField, Min(0.01f)] private float _markerWidth = 0.26f;
    [SerializeField, Min(0.01f)] private float _markerHeight = 0.23f;
    [SerializeField] private float _markerLift = 0.43f;
    [SerializeField, Min(0f)] private float _bobAmount = 0.035f;
    [SerializeField, Min(0f)] private float _bobSpeed = 3f;

    private readonly LineRenderer[] _frames = new LineRenderer[4];
    private readonly LineRenderer[] _outlines = new LineRenderer[4];
    private readonly Color[] _markerColors = new Color[6];
    private Transform _marker;
    private Mesh _markerMesh;
    private float _elapsed;
    private bool _ready;

    private void Awake()
    {
        if (_visualMaterial == null)
        {
            Debug.LogError("ロックオン用Materialを設定してください。", this);
            enabled = false;
            return;
        }

        // コライダーを作らないため、タイル選択のRaycastを妨げない。
        for (int i = 0; i < 4; i++)
        {
            _outlines[i] = CreateLine($"CornerOutline_{i}", 1);
            _frames[i] = CreateLine($"CornerLight_{i}", 2);
        }

        CreateMarker();
        _ready = true;
    }

    private void OnEnable()
    {
        _elapsed = 0f;
        if (_viewCamera == null)
            _viewCamera = Camera.main;

        if (_ready)
            UpdateVisual();
    }

    private LineRenderer CreateLine(string objectName, int order)
    {
        GameObject obj = new GameObject(objectName);
        obj.layer = gameObject.layer;
        obj.transform.SetParent(transform, false);
        LineRenderer line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = _visualMaterial;
        line.useWorldSpace = false;
        line.positionCount = 3;
        line.alignment = LineAlignment.View;
        line.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        line.numCornerVertices = 3;
        line.numCapVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sortingOrder = order;
        return line;
    }

    private void CreateMarker()
    {
        GameObject obj = new GameObject("FloatingTriangle");
        obj.layer = gameObject.layer;
        obj.transform.SetParent(transform, false);
        _marker = obj.transform;

        // 外側3頂点と内側3頂点で、暗い縁と明るい三角形を描く。
        Vector3 a = new Vector3(-_markerWidth * 0.5f, _markerHeight / 3f, 0f);
        Vector3 b = new Vector3(_markerWidth * 0.5f, _markerHeight / 3f, 0f);
        Vector3 c = new Vector3(0f, -_markerHeight * 2f / 3f, 0f);
        _markerMesh = new Mesh { name = "EnemyTargetTriangle_Runtime" };
        _markerMesh.vertices = new[] { a, b, c, a * 0.68f, b * 0.68f, c * 0.68f };
        _markerMesh.triangles = new[]
        {
            0, 1, 4, 0, 4, 3,
            1, 2, 5, 1, 5, 4,
            2, 0, 3, 2, 3, 5,
            3, 4, 5
        };
        _markerMesh.MarkDynamic();
        _markerMesh.RecalculateBounds();
        obj.AddComponent<MeshFilter>().sharedMesh = _markerMesh;
        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _visualMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 3;
    }

    private void LateUpdate()
    {
        if (!_ready)
            return;

        _elapsed += Time.deltaTime;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _appearDuration));
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        float half = _frameSize * 0.5f * Mathf.Lerp(_appearScale, 1f, eased);
        float arm = Mathf.Min(_cornerLength, half);
        float fade = Mathf.Clamp01(_elapsed / 0.06f);

        // 収束した瞬間だけ小さく光る。攻撃時刻を推測した点滅は行わない。
        float flashTime = (_elapsed - _appearDuration) / 0.075f;
        float flash = Mathf.Exp(-flashTime * flashTime);
        Color light = Color.Lerp(_frameColor, new Color(1f, 0.92f, 0.7f, 1f), flash);
        light.a *= fade * (0.92f + 0.08f * Mathf.Sin(_elapsed * 3f));
        Color outline = _outlineColor;
        outline.a *= fade;

        for (int i = 0; i < 4; i++)
        {
            float sx = i % 2 == 0 ? -1f : 1f;
            float sz = i < 2 ? -1f : 1f;
            Vector3 corner = new Vector3(sx * half, 0f, sz * half);
            DrawCorner(_outlines[i], corner, sx, sz, arm, _outlineWidth, outline);
            DrawCorner(_frames[i], corner, sx, sz, arm,
                _lineWidth * (1f + flash * 0.25f), light);
        }

        // ブロックに追従しながら、画面から読める向きで上下する。
        float lift = _markerLift + Mathf.Sin(_elapsed * _bobSpeed) * _bobAmount;
        _marker.localPosition = Vector3.up * (lift + (1f - eased) * 0.12f);
        _marker.localScale = Vector3.one * Mathf.Lerp(0.65f, 1f, eased);
        if (_viewCamera == null)
            _viewCamera = Camera.main;
        if (_viewCamera != null)
            _marker.rotation = _viewCamera.transform.rotation;

        Color markerColor = Color.Lerp(_markerColor, Color.white, flash * 0.65f);
        markerColor.a *= fade;
        for (int i = 0; i < 3; i++)
        {
            _markerColors[i] = outline;
            _markerColors[i + 3] = markerColor;
        }
        _markerMesh.colors = _markerColors;
    }

    private static void DrawCorner(LineRenderer line, Vector3 corner,
        float sx, float sz, float arm, float width, Color color)
    {
        line.SetPosition(0, corner - Vector3.right * sx * arm);
        line.SetPosition(1, corner);
        line.SetPosition(2, corner - Vector3.forward * sz * arm);
        line.widthMultiplier = width;
        line.startColor = color;
        line.endColor = color;
    }

    private void OnDestroy()
    {
        if (_markerMesh != null)
            Destroy(_markerMesh);
    }
}
