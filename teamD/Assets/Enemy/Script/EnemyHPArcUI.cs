using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UI;


/// <summary>
/// 既存のHP用Sliderから値を読み、U字型HPへ反映する
/// 作成者 : 酒井康佑
/// </summary>
public class EnemyHPArcUI : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Slider _sourceSlider;
    [SerializeField] private Transform _enemy;
    [SerializeField] private Camera _worldCamera;

    [Header("一区画分のダメージ")]
    [SerializeField, Min(0.01f)]
    private float _damagePerSegment = 10f;

    [Header("位置")]
    [SerializeField] private Vector3 _worldOffset;
    [SerializeField] private Vector2 _uiOffset = new Vector2(0f, -10f);

    private Image _image;
    private RectTransform _rect;
    private RectTransform _parentRect;
    private Material _originalMaterial;
    private Material _runtimeMaterial;

    private int _previousTotal = -1;
    private int _previousRemaining = -1;

    private static readonly int SegmentCountId = Shader.PropertyToID("_SegmentCount");
    private static readonly int RemainingSegmentId = Shader.PropertyToID("_RemainingSegments");

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rect = GetComponent<RectTransform>();
        _parentRect = transform.parent as RectTransform;

        if(_worldCamera == null)
            _worldCamera = Camera.main;

        _originalMaterial = _image.material;

        if(_originalMaterial == null ||
            !_originalMaterial.HasProperty(SegmentCountId) ||
            !_originalMaterial.HasProperty(RemainingSegmentId))
        {
            _image.enabled = false;
            enabled = false;
            return;
        }

        _runtimeMaterial = new Material(_originalMaterial);
        _image.material = _runtimeMaterial;
        _image.raycastTarget = false;
    }

    private void LateUpdate()
    {
        if (_sourceSlider == null || _enemy == null
            || _worldCamera == null || _parentRect == null)
        {
            _image.enabled = false;
            return;
        }

        Vector3 screenPosition = _worldCamera.WorldToScreenPoint(_enemy.position + _worldOffset);

        // カメラより後ろにいる場合は表示しない
        _image.enabled = screenPosition.z > 0;

        if (!_image.enabled)
            return;

        // 敵の位置をCanvas内の座標に変換する
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRect,
            screenPosition,
            null,
            out Vector2 localPoint))
        {
            Vector2 position = localPoint + _uiOffset;

            _rect.localPosition = new Vector3(position.x, position.y, 0f);
        }

        float damage = Mathf.Max(0.01f, _damagePerSegment);
        float maxHP = Mathf.Max(0f, _sourceSlider.maxValue - _sourceSlider.minValue);

        float currentHP = Mathf.Clamp(_sourceSlider.value - _sourceSlider.minValue, 0f ,maxHP);
        int total = Mathf.Max(1,Mathf.CeilToInt(maxHP / damage));

        int remaining = Mathf.Clamp(Mathf.CeilToInt(currentHP / damage), 0, total);

        if(total != _previousTotal || remaining != _previousRemaining)
        {
            _runtimeMaterial.SetFloat(SegmentCountId, total);
            _runtimeMaterial.SetFloat(RemainingSegmentId, remaining);

            _previousTotal = total;
            _previousRemaining = remaining;
        }

    }


    private void OnDestroy()
    {
        if (_image != null)
            _image.material = _originalMaterial;

        if (_runtimeMaterial != null)
            Destroy(_runtimeMaterial);

    }

}
