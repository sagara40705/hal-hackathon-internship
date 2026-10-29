using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 照準画像をカメラに向け、漂う→移動→縮小→固定の順で表示する。
/// 対象の選択・攻撃タイミングは既存の敵処理に任せる。
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyTargetLockOnEffect : MonoBehaviour
{
    [Header("画像の表示")]
    [SerializeField] private Material _visualMaterial;
    [SerializeField] private Camera _viewCamera;
    [SerializeField, Min(0.01f)] private float _size = 1.15f;
    [SerializeField] private Color _searchColor = new Color(0.08f, 0.65f, 1f, 0.85f);
    [SerializeField] private Color _lockedColor = new Color(1f, 0.24f, 0.06f, 1f);
    [SerializeField] private Color _outlineColor = new Color(0.035f, 0.015f, 0.035f, 0.8f);
    [Header("漂ってからロックオンするまで")]
    [SerializeField, Min(0f)] private float _searchDuration = 0.65f;
    [SerializeField, Min(0.01f)] private float _moveDuration = 0.3f;
    [SerializeField, Min(0.01f)] private float _lockDuration = 0.18f;
    [SerializeField, Min(0f)] private float _driftRadius = 0.55f;
    [SerializeField, Min(0f)] private float _searchLift = 0.45f;
    [SerializeField, Min(1f)] private float _searchScale = 1.35f;
    [SerializeField, Tooltip("ロック後の中心位置。ワールド座標の上方向")]
    private float _targetHeight = 0.02f;

    private Mesh _faceMesh;
    private Mesh _outlineMesh;
    private MeshRenderer _faceRenderer;
    private MeshRenderer _outlineRenderer;
    private readonly Vector3[] _vertices = new Vector3[4];
    private readonly Color[] _colors = new Color[4];
    private Vector3 _searchOrigin;
    private Vector3 _searchRight;
    private Vector3 _searchUp;
    private float _phase;
    private float _elapsed;
    private bool _initialized;
    private bool _lockSoundPlayed;

    private void Awake()
    {
        if (_visualMaterial == null)
        {
            Debug.LogError("照準用Materialを設定してください。", this);
            enabled = false;
            return;
        }
        _outlineMesh = CreateQuad("TargetOutline", 4, out _outlineRenderer);
        _faceMesh = CreateQuad("TargetImage", 5, out _faceRenderer);
    }

    private Mesh CreateQuad(string objectName, int order, out MeshRenderer renderer)
    {
        GameObject obj = new GameObject(objectName);
        obj.layer = gameObject.layer;
        obj.transform.SetParent(transform, false);
        Mesh mesh = new Mesh { name = objectName + "_Runtime" };
        mesh.vertices = new Vector3[4];
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1) };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.MarkDynamic();
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _visualMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = order;
        renderer.enabled = false;
        return mesh;
    }

    private void OnEnable()
    {
        _elapsed = 0f;
        _lockSoundPlayed = false;
        _initialized = false;
        if (_viewCamera == null) _viewCamera = FindViewCamera();
    }

    private Vector3 SearchPosition(float time)
    {
        return _searchOrigin + Vector3.up * _searchLift
            + _searchRight * (Mathf.Sin(_phase + time * 2.2f) * _driftRadius)
            + _searchUp * (Mathf.Cos(_phase + time * 1.7f) * _driftRadius * 0.35f);
    }

    private void LateUpdate()
    {
        if (_faceMesh == null) return;
        if (_viewCamera == null) _viewCamera = FindViewCamera();
        if (_viewCamera == null) return;

        // Instantiate後、呼び出し元が配置位置を設定し終えてから記録する。
        if (!_initialized)
        {
            _searchOrigin = transform.position;
            _searchRight = _viewCamera.transform.right;
            _searchUp = _viewCamera.transform.up;
            // ゲーム本体のRandomの状態を変更しない。
            _phase = (GetInstanceID() & 1023) * 0.017f;
            _initialized = true;
        }

        _elapsed += Time.deltaTime;
        Vector3 target = transform.position + Vector3.up * _targetHeight;
        Vector3 center;
        float scale = _searchScale;
        float roll = Mathf.Sin(_phase + _elapsed * 2f) * 8f;
        Color color = _searchColor;
        float moveTime = Mathf.Max(0.01f, _moveDuration);
        float lockTime = Mathf.Max(0.01f, _lockDuration);
        float lockStart = _searchDuration + moveTime;

        if (_elapsed < _searchDuration)
        {
            center = SearchPosition(_elapsed);
        }
        else if (_elapsed < lockStart)
        {
            float t = Mathf.Clamp01((_elapsed - _searchDuration) / moveTime);
            float eased = t * t * (3f - 2f * t);
            center = Vector3.Lerp(SearchPosition(_searchDuration), target, eased);
            roll = Mathf.Sin(_phase + _searchDuration * 2f) * 8f * (1f - eased);
            color = Color.Lerp(_searchColor, _lockedColor, eased);
        }
        else
        {
            // ロック状態に入った最初の一度だけ音を鳴らす。
            if (!_lockSoundPlayed)
            {
                _lockSoundPlayed = true;
                if (SoundManager.Instance != null) SoundManager.Instance.PlaySEById(SoundId.EnemyLockOn);
            }
            // ロック後は漂わせず、中心を対象タイルに固定する。
            center = target;
            roll = 0f;
            float t = Mathf.Clamp01((_elapsed - lockStart) / lockTime);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            scale = Mathf.Lerp(_searchScale, 1f, eased);
            float flash = Mathf.Sin(t * Mathf.PI);
            color = Color.Lerp(_lockedColor, Color.white, flash * 0.85f);
        }

        float fade = Mathf.Clamp01(_elapsed / 0.1f);
        color.a *= fade;
        Color outline = _outlineColor;
        outline.a *= fade;
        Quaternion facing = _viewCamera.transform.rotation * Quaternion.Euler(0f, 0f, roll);
        Vector3 right = facing * Vector3.right;
        Vector3 up = facing * Vector3.up;
        float size = _size * scale;

        // ワールド座標で板を作り、薄い親CubeのScaleに潰されるのを防ぐ。
        DrawQuad(_outlineMesh, center + _viewCamera.transform.forward * 0.002f,
            right, up, size * 1.045f, outline);
        DrawQuad(_faceMesh, center, right, up, size, color);
        bool visible = Vector3.Dot(center - _viewCamera.transform.position,
            _viewCamera.transform.forward) > 0f;
        _faceRenderer.enabled = visible;
        _outlineRenderer.enabled = visible;
    }

    private void DrawQuad(Mesh mesh, Vector3 center, Vector3 right,
        Vector3 up, float size, Color color)
    {
        Vector3 x = right * (size * 0.5f);
        Vector3 y = up * (size * 0.5f);
        _vertices[0] = transform.InverseTransformPoint(center - x - y);
        _vertices[1] = transform.InverseTransformPoint(center + x - y);
        _vertices[2] = transform.InverseTransformPoint(center + x + y);
        _vertices[3] = transform.InverseTransformPoint(center - x + y);
        for (int i = 0; i < 4; i++) _colors[i] = color;
        mesh.vertices = _vertices;
        mesh.colors = _colors;
        mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (_faceMesh != null) Destroy(_faceMesh);
        if (_outlineMesh != null) Destroy(_outlineMesh);
    }

    private static Camera FindViewCamera()
    {
        Camera main = Camera.main;
        // Render Texture に描画するカメラ(プレビュー用など)は除外
        if (main != null && main.targetTexture == null) return main;

        foreach (Camera cam in Camera.allCameras)
        {
            if (cam.targetTexture == null && cam.enabled) return cam;
        }
        return null;
    }
}
