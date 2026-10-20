using UnityEngine;

/// <summary>
/// 複数のタイルセルをまとめて管理する
/// 作成者：石田怜
/// </summary>
public class TileGroup : MonoBehaviour
{
	[SerializeField, Tooltip("移動可能フラグ")]
	private bool _isMovable = true;

	private TileCell[] _cells;			// グループ内セル情報
	private Vector2Int _gridPosition;   // グリッド上の基準座標

	public bool IsMovable => _isMovable;
	public TileCell[] Cells => _cells;
	public Vector2Int GridPosition => _gridPosition;


	// タイルグループの初期化
	public void InitGroup(float tileSize)
	{
		_cells = GetComponentsInChildren<TileCell>();
		foreach (TileCell cell in _cells)
		{
			cell.InitCell(this, tileSize);
		}
	}

	// グリッド上の基準座標を設定
	public void SetGridPosition(Vector2Int gridPosition)
	{
		_gridPosition = gridPosition;
	}
}
