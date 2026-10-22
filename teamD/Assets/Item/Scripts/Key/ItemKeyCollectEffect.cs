using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>取得時だけ生成される鍵の見た目。アイテム登録や所持数には関与しない。</summary>
public sealed class ItemKeyCollectEffect : MonoBehaviour
{
    private Transform _cachedTransform;
    private Camera _camera;
    private Vector3 _startPosition;
    private Vector3 _risePosition;
    private Vector2 _flyDirection;
    private float _riseDuration;
    private float _hoverDuration;
    private float _flyDuration;
    private float _rotationSpeed;
    private float _elapsedTime;

    // 取得元の鍵のTransformと、演出用のカメラを指定して生成する。
    internal static ItemKeyCollectEffect Create(Transform source, Camera camera,　Vector3 riseOffset, float riseDuration, float hoverDuration, float flyDuration,　Vector2 flyDirection, float rotationSpeed)
    {
        var root = new GameObject("Key_CollectedEffect");
        // 取得元と同じシーンで管理し、シーン切り替え時にも残さない。
        SceneManager.MoveGameObjectToScene(root, source.gameObject.scene);
        root.transform.position = source.position;
        int meshCount = 0;
        // 非アクティブ化された元の鍵からも、共有メッシュとマテリアルだけをコピーする。
        foreach (MeshFilter filter in source.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || !filter.TryGetComponent(out MeshRenderer renderer) || !renderer.enabled)
                continue;

            var visual = new GameObject(filter.name) { layer = filter.gameObject.layer };
            visual.transform.SetParent(root.transform, false);
            visual.transform.SetPositionAndRotation(filter.transform.position, filter.transform.rotation);
            visual.transform.localScale = filter.transform.lossyScale;
            visual.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var copy = visual.AddComponent<MeshRenderer>();
            copy.sharedMaterials = renderer.sharedMaterials;
            copy.renderingLayerMask = renderer.renderingLayerMask;
            copy.shadowCastingMode = ShadowCastingMode.Off;
            copy.receiveShadows = false;
            meshCount++;
        }

        if (meshCount == 0)
        {
            Destroy(root);
            return null;
        }

        var effect = root.AddComponent<ItemKeyCollectEffect>();
        effect._cachedTransform = root.transform;
        effect._camera = camera;
        effect._startPosition = source.position;
        effect._risePosition = source.position + riseOffset;
        effect._riseDuration = Mathf.Max(0.01f, riseDuration);
        effect._hoverDuration = Mathf.Max(0f, hoverDuration);
        effect._flyDuration = Mathf.Max(0.01f, flyDuration);
        effect._flyDirection = flyDirection.sqrMagnitude > 0.0001f
            ? flyDirection.normalized : Vector2.one.normalized;
        effect._rotationSpeed = rotationSpeed;
        return effect;
    }

    private void Update()
    {
        if (_camera == null)
        {
            Destroy(gameObject);
            return;
        }

        // クリア画面などでTime.timeScaleが0になっても演出は完了させる。
        float deltaTime = Time.unscaledDeltaTime;
        _elapsedTime += deltaTime;
        _cachedTransform.Rotate(0f, _rotationSpeed * deltaTime, 0f, Space.World);
        if (Animate(_elapsedTime))
        {
            Destroy(gameObject);
        }
            
    }

    // 完了時はtrueを返し、Updateから演出オブジェクトを破棄する。
    private bool Animate(float elapsed)
    {
        if (elapsed < _riseDuration)
        {
            float t = Mathf.Clamp01(elapsed / _riseDuration);
            float easeOut = 1f - (1f - t) * (1f - t);
            _cachedTransform.position = Vector3.LerpUnclamped(_startPosition, _risePosition, easeOut);
            return false;
        }

        float flyTime = elapsed - _riseDuration - _hoverDuration;
        if (flyTime < 0f)
        {
            _cachedTransform.position = _risePosition;
            return false;
        }

        // 画面上の方向を使うので、カメラの角度が変わっても指定方向へ飛ぶ。
        Vector3 viewport = _camera.WorldToViewportPoint(_risePosition);
        viewport.x += _flyDirection.x * 2f;
        viewport.y += _flyDirection.y * 2f;
        viewport.z = Mathf.Max(viewport.z, _camera.nearClipPlane + 0.01f);
        Vector3 destination = _camera.ViewportToWorldPoint(viewport);
        float flyT = Mathf.Clamp01(flyTime / _flyDuration);
        _cachedTransform.position = Vector3.LerpUnclamped(_risePosition, destination, flyT * flyT);
        return flyT >= 1f;
    }
}
