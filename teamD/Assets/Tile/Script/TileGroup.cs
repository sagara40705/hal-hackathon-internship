using UnityEngine;

/// <summary>
/// 複数のタイルセルをまとめて管理する
/// 作成者：石田怜
/// </summary>
public class TileGroup : MonoBehaviour
{
	[SerializeField, Tooltip("固定フラグ")]
	private bool _isStatic = true;

    [SerializeField, Tooltip("プレビューの配置オフセット")]
    private Vector3 _previewOffset = Vector3.zero;

	private TileCell[] _cells;			    // グループ内セル情報
	private Vector2Int _gridPosition;       // グリッド上の基準座標
    private bool _isInitialized = false;    // 初期化済みかどうかのフラグ
    private bool _isPlaced = false;         // 配置済みかどうかのフラグ

    public bool IsStatic => _isStatic;
	public TileCell[] Cells => _cells;
	public Vector2Int GridPosition => _gridPosition;
    public bool IsInitialized => _isInitialized;
    public bool IsPlaced => _isPlaced;
    public Vector3 PreviewOffset => _previewOffset;

    // タイルグループの初期化
    public void InitGroup(float tileSize)
	{
		_cells = GetComponentsInChildren<TileCell>();
		foreach (TileCell cell in _cells)
		{
			cell.InitCell(this, tileSize);
		}
        _isInitialized = true;
	}

	// グリッド上の基準座標を設定
	public void SetGridPosition(Vector2Int gridPosition)
	{
		_gridPosition = gridPosition;
    }

    // 配置済みフラグを設定
    public void SetPlaced()
    {
        _isPlaced = true;
    }
}
