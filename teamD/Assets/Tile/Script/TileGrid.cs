using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// タイルのグリッド管理
/// 作成者：石田怜
/// </summary>
public class TileGrid : MonoBehaviour
{
	[SerializeField, Tooltip("グリッドサイズ (奇数推奨)")]
	private Vector2Int _gridSize = new Vector2Int(11, 11);

	[SerializeField, Tooltip("1マスのサイズ"), Min(0.01f)]
	private float _tileSize = 1.0f;

	[SerializeField, Tooltip("タイルの高さ")]
	private float _tileHeight = 1.0f;

    [SerializeField, Tooltip("配置可能なグリッド座標")]
    private List<Vector2Int> _placementPositions = new();

    private HashSet<Vector2Int> _placementPositionSet;      // 配置可能なグリッド座標の高速検索用セット

    // グリッド上の各セル
    private readonly Dictionary<Vector2Int, TileCell> _cells = new();

    public Vector2Int GridSize => _gridSize;
    public float TileSize => _tileSize;
    public float TileHeight => _tileHeight;


    private void Awake()
	{
        // 配置可能なグリッド座標のハッシュセットを作成
        _placementPositionSet = new HashSet<Vector2Int>(_placementPositions);

        // 初期タイルグループを登録
        TileGroup[] groups = GetComponentsInChildren<TileGroup>();
		foreach (TileGroup group in groups)
		{
			group.InitGroup(_tileSize);
            group.SetPlaced();
            Vector2Int gridPosition = WorldToGrid(group.transform.position);

			if (!SetTile(group, gridPosition))
			{
				Debug.LogWarning($"タイルグループの登録に失敗: {group.name}", group);
			}
		}
	}

	// ワールド座標をグリッド座標に変換
	public Vector2Int WorldToGrid(Vector3 worldPosition)
	{
		Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
		float halfWidth = (_gridSize.x - 1) * _tileSize * 0.5f;
		float halfHeight = (_gridSize.y - 1) * _tileSize * 0.5f;

		return new Vector2Int(
			Mathf.RoundToInt((localPosition.x + halfWidth) / _tileSize),
			Mathf.RoundToInt((localPosition.z + halfHeight) / _tileSize)
		);
	}

	// グリッド座標をワールド座標に変換
	public Vector3 GridToWorld(Vector2Int gridPosition)
	{
		float halfWidth = (_gridSize.x - 1) * _tileSize * 0.5f;
		float halfHeight = (_gridSize.y - 1) * _tileSize * 0.5f;

		Vector3 localPosition = new Vector3(
			gridPosition.x * _tileSize - halfWidth,
			0.0f,
			gridPosition.y * _tileSize - halfHeight
		);

		return transform.TransformPoint(localPosition);
	}

	// グリッド範囲内か判定
	public bool IsInGrid(Vector2Int gridPosition)
	{
		return gridPosition.x >= 0 &&
			   gridPosition.x < _gridSize.x &&
			   gridPosition.y >= 0 &&
			   gridPosition.y < _gridSize.y;
	}

	// 指定座標のセルを取得
	public TileCell GetCell(Vector2Int gridPosition)
	{
		_cells.TryGetValue(gridPosition, out TileCell cell);
		return cell;
	}

	// 指定座標のタイルグループを取得
	public TileGroup GetTileGroup(Vector2Int gridPosition)
	{
		TileCell cell = GetCell(gridPosition);
		return cell != null ? cell.ParentGroup : null;
	}

	// タイルグループを配置できるか判定
	public bool CanPlaceTile(TileGroup group,　Vector2Int gridPosition)
	{
		if (group == null ||
			group.Cells == null ||
			group.Cells.Length == 0)
			return false;

		// セルごとに配置可能か判定
		HashSet<Vector2Int> positions = new();
		foreach (TileCell cell in group.Cells)
		{
			if (cell == null)
				return false;

			Vector2Int cellPosition = gridPosition + cell.LocalPosition;

			if (!IsInPlacementArea(cellPosition))
				return false;

			if (!positions.Add(cellPosition))
				return false;

			if (_cells.ContainsKey(cellPosition))
				return false;
		}

		return true;
	}

	// タイルグループを配置
	public bool SetTile(TileGroup group, Vector2Int gridPosition)
	{
		if (!CanPlaceTile(group, gridPosition))
			return false;

		// タイルグループのセルを登録
		foreach (TileCell cell in group.Cells)
		{
			Vector2Int cellPosition = gridPosition + cell.LocalPosition;
			_cells.Add(cellPosition, cell);
		}

		// タイルグループの位置を更新
		group.SetGridPosition(gridPosition);
		group.transform.position = GridToWorld(gridPosition);

		return true;
	}

	// タイルグループの登録を削除
	public bool RemoveTile(TileGroup group)
	{
		if (group == null ||
			group.Cells == null ||
			group.Cells.Length == 0)
			return false;

		// 登録状態を確認
		foreach (TileCell cell in group.Cells)
		{
			Vector2Int cellPosition = group.GridPosition + cell.LocalPosition;
			if (GetCell(cellPosition) != cell)
				return false;
		}

		// タイルグループのセルを削除
		foreach (TileCell cell in group.Cells)
		{
			Vector2Int cellPosition = group.GridPosition + cell.LocalPosition;
			_cells.Remove(cellPosition);
		}

		return true;
    }

    // 実行中に新しいタイルを生成して、グリッドへ登録する
    public bool TryCreateAndPlaceTile(GameObject prefab, Vector3 worldPosition)
    {
        if (prefab == null)
            return false;

        // 生成したタイルをグリッドの子にする
        GameObject instance = Instantiate
        (
            prefab,
            worldPosition,
            Quaternion.identity,
            transform
        );

        // PrefabのルートにTileGroupがあることを確認する
        TileGroup group = instance.GetComponent<TileGroup>();
        if (group == null)
        {
            Debug.LogWarning("PrefabのルートにTileGroupがありません。", instance);
            Destroy(instance);
            return false;
        }

        // TileCellを取得して初期化する
        group.InitGroup(_tileSize);

        Vector2Int gridPosition = WorldToGrid(worldPosition);
        if (!SetTile(group, gridPosition))
        {
            // 配置できなければ、未登録のPrefabを残さない
            Destroy(instance);
            return false;
        }
        return true;
    }

    // 指定座標のタイルグループを破壊
    public bool DestroyTile(Vector2Int gridPosition)
    {
        TileGroup group = GetTileGroup(gridPosition);

        if (group == null || group.IsStatic)
            return false;

        // グリッドから登録解除
        if (!RemoveTile(group))
            return false;

        Destroy(group.gameObject);
        return true;
    }

    // 配置可能範囲内か判定
    public bool IsInPlacementArea(Vector2Int gridPosition)
    {
        if (!IsInGrid(gridPosition))
            return false;

        if (_placementPositionSet != null)
            return _placementPositionSet.Contains(gridPosition);

        return _placementPositions.Contains(gridPosition);
    }

    // 指定マスの配置可否を設定
    public void SetPlacementAllowed(Vector2Int gridPosition, bool allowed)
    {
        if (!IsInGrid(gridPosition))
            return;

        if (allowed)
        {
            if (!_placementPositions.Contains(gridPosition))
                _placementPositions.Add(gridPosition);
        }
        else
        {
            _placementPositions.RemoveAll(pos => pos == gridPosition);
        }

        // 配置可能範囲の更新
        RefreshPlacementArea();
    }

    // 全マスの配置可否を設定
    public void SetAllPlacementAllowed(bool allowed)
    {
        _placementPositions.Clear();
        if (allowed)
        {
            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int y = 0; y < _gridSize.y; y++)
                {
                    _placementPositions.Add(new Vector2Int(x, y));
                }
            }
        }

        // 配置可能範囲の更新
        RefreshPlacementArea();
    }

    // 配置可能範囲の更新
    public void RefreshPlacementArea()
    {
        _placementPositionSet = new HashSet<Vector2Int>(_placementPositions);
    }

    private void OnDrawGizmos()
    {
        if (_tileSize <= 0.0f)
            return;

        for (int x = 0; x < _gridSize.x; x++)
        {
            for (int y = 0; y < _gridSize.y; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                Vector3 center = GridToWorld(gridPosition);
                center += transform.up * (_tileHeight * 0.5f);

                // 配置可能状態に応じて色を変更
                Gizmos.color = IsInPlacementArea(gridPosition) ?
                    Color.white : Color.gray1;

                float size = _tileSize * 0.9f;
                Gizmos.DrawWireCube(
                    center,
                    new Vector3(size, 0.01f, size)
                );
            }
        }
    }

    // タイル破壊演出
    // 制作: sagara
    public event System.Action<TileGroup> BreakEffectRequested;

    public void PlayBreakEffect(TileGroup group)
    {
        BreakEffectRequested?.Invoke(group);
    }
}
