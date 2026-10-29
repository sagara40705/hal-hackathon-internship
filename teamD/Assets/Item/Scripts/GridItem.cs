using UnityEngine;
using UnityEngine.Events;

/// <summary>タイルとは独立した、Grid座標に固定される取得アイテム。</summary>
[DisallowMultipleComponent]
public sealed class GridItem : MonoBehaviour
{
    [SerializeField, Tooltip("配置するGrid座標。Play開始前に設定する")]
    private Vector2Int _gridPosition;

    [SerializeField] private ItemType _itemType = ItemType.Key;
    [SerializeField, Min(1)] private int _amount = 1;

    [SerializeField, Min(0f), Tooltip("タイル上面からモデルの原点までの高さ")]
    private float _heightOffset = 0.35f;

    [SerializeField, Tooltip("取得完了時の通知。引数は取得したPlayer")]
    private UnityEvent<Player> _onCollected = new();

    private GridItemMap _registeredMap;
    private Vector2Int _registeredPosition;

    public Vector2Int GridPosition => _registeredMap != null ? _registeredPosition : _gridPosition;
    public ItemType Type => _itemType;
    public int Amount => _amount;
    public bool IsCollected { get; private set; }

    private void OnEnable()
    {
        if (!IsCollected)
            GetComponentInParent<GridItemMap>()?.TryRegister(this);
    }

    private void OnDisable()
    {
        if (_registeredMap != null)
            _registeredMap.Unregister(this);
    }

    private void OnValidate()
    {
        _amount = Mathf.Max(1, _amount);
        _heightOffset = Mathf.Max(0f, _heightOffset);
    }

    internal void AttachToMap(GridItemMap map, Vector2Int position)
    {
        _registeredMap = map;
        _registeredPosition = position;
        ApplyGridPosition();
    }

    internal void DetachFromMap(GridItemMap map)
    {
        if (_registeredMap == map)
            _registeredMap = null;
    }

    [ContextMenu("Grid座標に位置を合わせる")]
    public void ApplyGridPosition()
    {
        GridItemMap map = _registeredMap != null ? _registeredMap : GetComponentInParent<GridItemMap>();
        TileGrid grid = map != null ? map.Grid : null;
        if (grid == null)
            return;

        transform.position = grid.GridToWorld(GridPosition) +
            grid.transform.TransformVector(Vector3.up * (grid.TileHeight * 0.5f + _heightOffset));
        transform.rotation = grid.transform.rotation;
    }

    internal bool TryCollect(Player player, PlayerInventory inventory)
    {
        if (IsCollected || !isActiveAndEnabled || inventory == null || !inventory.CanAdd(_itemType, _amount))
            return false;

        // 通知先から再入されても二重取得にならないよう、先に取得済み・登録解除にする。
        IsCollected = true;
        if (_registeredMap != null)
            _registeredMap.Unregister(this);

        try
        {
            inventory.TryAdd(_itemType, _amount);
        }
        finally
        {
            gameObject.SetActive(false);
        }
        // 取得成功後、鍵の場合だけ音を鳴らす。
        if (_itemType == ItemType.Key)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySEById(SoundId.GetKey);
        }
        _onCollected?.Invoke(player);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        GridItemMap map = GetComponentInParent<GridItemMap>();
        TileGrid grid = map != null ? map.Grid : null;
        if (grid == null)
            return;

        Vector3 surface = grid.GridToWorld(GridPosition) +
            grid.transform.TransformVector(Vector3.up * (grid.TileHeight * 0.5f));
        Color previousColor = Gizmos.color;
        Gizmos.color = grid.IsInPlacementArea(GridPosition) ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(surface, grid.TileSize * 0.15f);
        Gizmos.DrawLine(surface, transform.position);
        Gizmos.color = previousColor;
    }
}
