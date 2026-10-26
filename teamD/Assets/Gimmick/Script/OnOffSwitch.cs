using UnityEngine;

/// <summary>TileCellの進入イベントから、同じ盤面のON/OFFを反転する。</summary>
[DefaultExecutionOrder(-90)]
[DisallowMultipleComponent]
[RequireComponent(typeof(TileCell))]
public sealed class OnOffSwitch : MonoBehaviour
{
    [SerializeField, Tooltip("未指定なら同じTileGridのコントローラーを自動取得")]
    private OnOffController _controller;
    [SerializeField] private Renderer _buttonRenderer;
    [SerializeField] private Transform _button;
    [SerializeField] private TextMesh _stateLabel;
    [SerializeField] private Color _offColor = new(0.1f, 0.45f, 1f);
    [SerializeField] private Color _onColor = new(1f, 0.4f, 0.05f);

    private MaterialPropertyBlock _properties;
    private Vector3 _buttonRestPosition;
    private bool _started;

    private void Start()
    {
        _started = true;
        if (_button != null)
            _buttonRestPosition = _button.localPosition;
        Connect();
    }

    private void OnEnable()
    {
        if (_started)
            Connect();
    }

    private void Connect()
    {
        if (_controller == null)
            _controller = OnOffController.Resolve(this);
        if (_controller != null && _controller.isActiveAndEnabled &&
            _controller.Grid == GetComponentInParent<TileGrid>())
            _controller.Register(this);
    }

    public void OnEnterPlayer(Player player)
    {
        if (!isActiveAndEnabled || player == null || _controller == null ||
            !_controller.isActiveAndEnabled || player.Grid != _controller.Grid ||
            player.Grid.GetCell(player.CurrnetPosition) != GetComponent<TileCell>())
            return;

        _controller.Toggle();
    }

    internal void RefreshAppearance(bool isOn)
    {
        if (_buttonRenderer != null)
        {
            _properties ??= new MaterialPropertyBlock();
            _buttonRenderer.GetPropertyBlock(_properties);
            _properties.SetColor("_BaseColor", isOn ? _onColor : _offColor);
            _buttonRenderer.SetPropertyBlock(_properties);
        }
        if (_stateLabel != null)
            _stateLabel.text = isOn ? "ON" : "OFF";
        if (_button != null)
            _button.localPosition = _buttonRestPosition + (isOn ? Vector3.down * 0.06f : Vector3.zero);
    }

    private void OnDisable() => _controller?.Unregister(this);
}
