using System.Collections.Generic;
using UnityEngine;

/// <summary>床タイルとは別に、Grid座標ごとのアイテムを管理する。Items親オブジェクト用。</summary>
[DisallowMultipleComponent]
public sealed class GridItemMap : MonoBehaviour
{
    [SerializeField, Tooltip("アイテムを配置するGrid")]
    private TileGrid _tileGrid;

    [SerializeField, Tooltip("到着通知を受け取るPlayer。PlayerInventoryも同じオブジェクトに必要")]
    private Player _player;

    private readonly Dictionary<Vector2Int, GridItem> _items = new();
    private Player _subscribedPlayer;

    public TileGrid Grid => _tileGrid;

    private void OnEnable()
    {
        if (_tileGrid == null || _player == null || _player.Grid != _tileGrid)
        {
            Debug.LogWarning("GridItemMapに同じGridを使うTileGridとPlayerを指定してください。", this);
            return;
        }

        if (GetComponentInParent<TileGroup>() != null)
        {
            Debug.LogWarning("GridItemMapは移動するTileGroupの外に配置してください。", this);
            return;
        }

        _subscribedPlayer = _player;
        _subscribedPlayer.CellArrived += OnCellArrived;

        // 子のOnEnableとの実行順序に依存しないよう、既存アイテムも登録する。
        foreach (GridItem item in GetComponentsInChildren<GridItem>())
            TryRegister(item);
    }

    private void OnDisable()
    {
        if (_subscribedPlayer != null)
            _subscribedPlayer.CellArrived -= OnCellArrived;
        _subscribedPlayer = null;

        foreach (GridItem item in _items.Values)
        {
            if (item != null)
                item.DetachFromMap(this);
        }
        _items.Clear();
    }

    public GridItem GetItem(Vector2Int position)
    {
        return _items.TryGetValue(position, out GridItem item) ? item : null;
    }

    internal bool TryRegister(GridItem item)
    {
        if (!isActiveAndEnabled || _subscribedPlayer == null || _tileGrid == null ||
            item == null || !item.isActiveAndEnabled || item.IsCollected ||
            item.GetComponentInParent<GridItemMap>() != this)
            return false;

        if (item.GetComponentInParent<TileGroup>() != null)
        {
            Debug.LogWarning("アイテムはTileGroupの子にせず、Itemsの子に配置してください。", item);
            return false;
        }

        Vector2Int position = item.GridPosition;
        // 床の有無は問わない。後からタイルを運んで到達できる。
        if (!_tileGrid.IsInPlacementArea(position))
        {
            Debug.LogWarning($"アイテムの配置先が配置可能範囲外です: {position}", item);
            return false;
        }

        GridItem existing = GetItem(position);
        if (existing != null && existing != item)
        {
            Debug.LogWarning($"同じセルにはアイテムを1個だけ配置できます: {position}", item);
            return false;
        }

        _items[position] = item;
        item.AttachToMap(this, position);
        return true;
    }

    internal void Unregister(GridItem item)
    {
        if (GetItem(item.GridPosition) == item)
            _items.Remove(item.GridPosition);
        item.DetachFromMap(this);
    }

    private void OnCellArrived(Player player, Vector2Int position)
    {
        if (player != _subscribedPlayer || player.Grid != _tileGrid)
            return;

        GridItem item = GetItem(position);
        if (item == null || !item.isActiveAndEnabled || item.IsCollected)
            return;

        if (!player.TryGetComponent(out PlayerInventory inventory))
        {
            Debug.LogWarning("アイテム取得にはPlayerと同じオブジェクトにPlayerInventoryが必要です。", player);
            return;
        }

        item.TryCollect(player, inventory);
    }
}
