using UnityEngine;

/// <summary>Marks the actual landing cell and flashes only after a successful teleport.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Warp))]
[ExecuteAlways]
public sealed class WarpVisual : MonoBehaviour
{
    [SerializeField] private GimmickPulse _departure;
    [SerializeField] private GimmickPulse _receiver;
    [SerializeField] private GameObject _receiverPrefab;
    [SerializeField] private Color _color = new Color(.72f, .42f, 1f);
    [SerializeField] private Renderer[] _rings;
    [SerializeField] private Renderer _inlay;
    [SerializeField] private ParticleSystem[] _ambient;
    [SerializeField] private TextMesh[] _pairLabels;
    private GameObject _ownedReceiver;
    private MaterialPropertyBlock _properties;
    private bool _refreshAppearance;

    public Color PairColor => _color;

    private void OnEnable() => ApplyPairAppearance();
    private void OnValidate() => _refreshAppearance = true;
    private void Update()
    {
        if (_refreshAppearance) ApplyPairAppearance();
    }

    private void Start()
    {
        if (Application.IsPlaying(gameObject)) PositionReceiver();
    }

    public void SetPairColor(Color color)
    {
        _color = color;
        ApplyPairAppearance();
    }

    private void Tint(Renderer target, Color color, float emission)
    {
        if (target == null) return;
        _properties ??= new MaterialPropertyBlock();
        target.GetPropertyBlock(_properties);
        _properties.SetColor("_BaseColor", color);
        _properties.SetColor("_EmissionColor", color * emission);
        target.SetPropertyBlock(_properties);
    }

    private void ApplyPairAppearance()
    {
        _refreshAppearance = false;
        if (_rings != null)
            foreach (var ring in _rings) Tint(ring, _color, 1.25f);
        Tint(_inlay, _color * .26f, .3f);
        if (_ambient != null)
            foreach (var system in _ambient)
                if (system != null)
                {
                    var main = system.main;
                    if (main.startColor.color != _color) main.startColor = _color;
                }
        if (_receiver != null)
            foreach (var ring in _receiver.GetComponentsInChildren<MeshRenderer>(true))
                Tint(ring, _color, 1.25f);
        if (_pairLabels != null)
            foreach (var label in _pairLabels)
                if (label != null)
                {
                    Color color = Color.Lerp(_color, Color.white, .32f);
                    if (label.color != color) label.color = color;
                }
    }

    private void PositionReceiver()
    {
        var grid = GetComponentInParent<TileGrid>();
        if (grid == null) return;
        var destination = grid.WorldToGrid(transform.position) + GetComponent<Warp>().WarpAmount;
        // A missing landing tile must not be advertised as a safe destination.
        if (grid.GetCell(destination) == null) return;
        if (_receiver == null && _receiverPrefab != null)
        {
            _ownedReceiver = Instantiate(_receiverPrefab, grid.transform);
            _ownedReceiver.name = "Warp arrival (visual only)";
            _receiver = _ownedReceiver.GetComponentInChildren<GimmickPulse>(true);
        }
        if (_receiver != null)
        {
            Transform root = _ownedReceiver != null ? _ownedReceiver.transform : _receiver.transform;
            root.position = grid.GridToWorld(destination);
            root.rotation = grid.transform.rotation;
        }
        ApplyPairAppearance();
    }

    public void PlayTransfer()
    {
        PositionReceiver();
        _departure?.Play(_color);
        _receiver?.Play(_color);
    }

    private void OnDestroy()
    {
        if (_ownedReceiver == null) return;
        if (Application.IsPlaying(gameObject)) Destroy(_ownedReceiver);
        else DestroyImmediate(_ownedReceiver);
    }
}
