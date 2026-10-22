using UnityEngine;

public class ItemKey : MonoBehaviour
{
    [SerializeField, Tooltip("Y軸まわりの回転速度（度/秒）")]
    private float _rotationSpeed = 90f;

    [SerializeField, Min(0f), Tooltip("上下に揺れる幅（初期位置からの片側の距離）")]
    private float _floatAmplitude = 0.08f;

    [SerializeField, Min(0f), Tooltip("上下に揺れる速さ（1秒あたりの往復回数）")]
    private float _floatFrequency = 1f;

    [Header("取得時の演出")]
    [SerializeField, Min(0.01f), Tooltip("1セル分上昇する時間（秒）")]
    private float _riseDuration = 0.25f;

    [SerializeField, Min(0f), Tooltip("上昇後、その場で待つ滞空時間（秒）")]
    private float _hoverDuration = 0.15f;

    [SerializeField, Min(0.01f), Tooltip("滞空後、画面外へ飛ぶ時間（秒）")]
    private float _flyDuration = 0.5f;

    [SerializeField, Tooltip("画面基準の飛行方向。Xが右、Yが上。(1, 1)で右上。ゼロの場合も右上")]
    private Vector2 _flyDirection = Vector2.one;

    [SerializeField, Tooltip("演出の画面方向を決めるカメラ。未指定ならMainCameraを使用")]
    private Camera _effectCamera;

    private Transform _cachedTransform;
    private float _baseHeight;
    private float _elapsedTime;
    private bool _collectionEffectPlayed;

    private void Awake()
    {
        _cachedTransform = transform;
    }

    private void Start()
    {
        // Gridへの初期配置が終わってから、見た目の基準高さを記録する。
        _baseHeight = _cachedTransform.localPosition.y;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        _cachedTransform.Rotate(0f, _rotationSpeed * deltaTime, 0f, Space.World);

        _elapsedTime += deltaTime;
        Vector3 position = _cachedTransform.localPosition;
        // 毎回基準高さから計算するため、上下移動が積み重なってずれない。
        position.y = _baseHeight + Mathf.Sin(_elapsedTime * _floatFrequency * (2f * Mathf.PI)) * _floatAmplitude;
        _cachedTransform.localPosition = position;
    }

    // GridItemのOn Collectedに、動的なPlayer引数で登録する。
    public void PlayCollectedEffect(Player player)
    {
        if (_collectionEffectPlayed)
        {
            return;
        }

        Camera effectCamera = _effectCamera != null ? _effectCamera : Camera.main;
        if (effectCamera == null)
        {
            return;
        }

        GridItemMap map = GetComponentInParent<GridItemMap>();
        TileGrid grid = (map != null) ? map.Grid : player != null ? player.Grid : null;
        Vector3 riseOffset = (grid != null) ? grid.transform.TransformVector(Vector3.up * grid.TileSize) : Vector3.up;

        // 独立したコピーで演出する。
        ItemKeyCollectEffect effect = ItemKeyCollectEffect.Create(transform, effectCamera, riseOffset, _riseDuration, _hoverDuration, _flyDuration, _flyDirection, _rotationSpeed);
        _collectionEffectPlayed = effect != null;
    }
}
