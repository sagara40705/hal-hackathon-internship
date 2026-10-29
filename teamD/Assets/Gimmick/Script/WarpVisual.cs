using UnityEngine;

/// <summary>Marks the actual landing cell and flashes only after a successful teleport.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Warp))]
public sealed class WarpVisual : MonoBehaviour
{
    [SerializeField] private GimmickPulse _departure;
    [SerializeField] private GimmickPulse _receiver;
    [SerializeField] private GameObject _receiverPrefab;
    [SerializeField] private Color _color = new Color(.72f, .42f, 1f);
    private GameObject _ownedReceiver;

    private void Start() => PositionReceiver();

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
    }

    public void PlayTransfer()
    {
        PositionReceiver();
        _departure?.Play(_color);
        _receiver?.Play(_color);
    }

    private void OnDestroy()
    {
        if (_ownedReceiver != null) Destroy(_ownedReceiver);
    }
}
