using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PeriodicBlock : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("開通している秒数")]
    private float _openTime = 2f;

    [SerializeField, Min(0.2f), Tooltip("下降前に点滅する最短秒数（点滅1往復以上を保証）")]
    private float _warningTime = 1f;

    [SerializeField, Min(0f), Tooltip("閉鎖している秒数")]
    private float _closedTime = 2f;

    [SerializeField, Min(0.01f), Tooltip("下降・上昇それぞれにかかる秒数")]
    private float _moveTime = 0.4f;

    [SerializeField, Min(0f), Tooltip("鉄ブロックが下がる距離")]
    private float _dropDistance = 2.25f;

    [SerializeField, Min(0.1f), Tooltip("警告点滅の切り替え間隔（秒）")]
    private float _blinkInterval = 0.15f;

    [SerializeField] private Color _warningColor = new Color(1f, 0.3f, 0.05f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private readonly WaitForFixedUpdate _physicsWait = new WaitForFixedUpdate();
    private Rigidbody _body;
    private BoxCollider _solidCollider;
    private Renderer _renderer;
    private MaterialPropertyBlock _properties;
    private Vector3 _raisedPosition;
    private Color _originalColor;
    private bool _highlight;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _solidCollider = GetComponent<BoxCollider>();
        _renderer = GetComponent<Renderer>();
        _properties = new MaterialPropertyBlock();
        _raisedPosition = transform.localPosition;
        _originalColor = _renderer.sharedMaterial.GetColor(BaseColorId);
    }

    private void Start()
    {
        PeriodicBlockContacts.EnsureExists();
    }

    private void OnEnable()
    {
        _body.position = transform.parent.TransformPoint(_raisedPosition);
        PeriodicBlockContacts.Register(_solidCollider);
        StartCoroutine(Cycle());
    }

    private IEnumerator Cycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, _openTime));

            float interval = Mathf.Max(0.1f, _blinkInterval);
            float warningEnd = Time.time + Mathf.Max(_warningTime, interval * 2f);
            WaitForSeconds blinkWait = new WaitForSeconds(interval);

            // 各色で必ず描画フレームをまたぐ。時間が飛んでも点滅を省略しない。
            do
            {
                SetHighlight(true);
                yield return blinkWait;
                SetHighlight(false);
                yield return blinkWait;
            }
            while (Time.time < warningEnd);

            yield return MoveBlock(0f, 1f);
            yield return new WaitForSeconds(Mathf.Max(0f, _closedTime));
            yield return MoveBlock(1f, 0f);
        }
    }

    private IEnumerator MoveBlock(float from, float to)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(_moveTime, Time.fixedDeltaTime * 2f);
        while (elapsed < duration)
        {
            yield return _physicsWait;
            elapsed += Time.fixedDeltaTime;
            float lowered = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            Vector3 position = _raisedPosition - Vector3.up * (Mathf.Max(0f, _dropDistance) * lowered);
            _body.MovePosition(transform.parent.TransformPoint(position));
        }

        // 最終位置を物理更新に反映してから次の段階へ進む。
        yield return _physicsWait;
    }

    private void SetHighlight(bool highlight)
    {
        if (_highlight == highlight)
        {
            return;
        }


        _highlight = highlight;
        _renderer.GetPropertyBlock(_properties);
        _properties.SetColor(BaseColorId, highlight ? _warningColor : _originalColor);
        _renderer.SetPropertyBlock(_properties);
    }

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player != null)
        {
            player.Damage();
        }

    }

    private void OnDisable()
    {
        PeriodicBlockContacts.Unregister(_solidCollider);
        StopAllCoroutines();
        _renderer.SetPropertyBlock(null);
        _highlight = false;
    }
}
