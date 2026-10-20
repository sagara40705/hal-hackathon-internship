using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// マウスによるタイルグループの操作
/// </summary>
public class TileGridController : MonoBehaviour
{
	[SerializeField, Tooltip("操作に使用するカメラ")]
	private Camera _camera;

	[SerializeField, Tooltip("タイルグリッド")]
	private TileGrid _tileGrid;

	[SerializeField, Tooltip("プレイヤー")]
	private Player _player;

	[SerializeField, Tooltip("タイルのレイヤー")]
	private LayerMask _tileLayer;

	[SerializeField, Tooltip("ドラッグ時の高さ")]
	private float _dragHeight = 0.3f;

	[SerializeField, Tooltip("配置可能時のカラー")]
	private Color _validColor = Color.green;

	[SerializeField, Tooltip("配置不可能時のカラー")]
	private Color _invalidColor = Color.red;

	private TileGroup _selectedGroup;				// 選択中のタイルグループ
	private Vector2Int _originalPosition;			// 移動前のグリッド座標
	private Vector2Int _targetPosition;				// 移動先のグリッド座標
	private Vector3 _grabOffset;					// マウスとグループ基準位置の差

	private Renderer[] _selectedRenderers;			// 選択中のレンダラー
	private MaterialPropertyBlock _propertyBlock;	// マテリアルパラメータ変更用


	private void Awake()
	{
		_propertyBlock = new MaterialPropertyBlock();

		if (_camera == null)
			_camera = Camera.main;
	}

	private void Update()
	{
		Mouse mouse = Mouse.current;
		if (mouse == null)
			return;

		if (mouse.leftButton.wasPressedThisFrame)
			BeginDrag();

		if (_selectedGroup == null)
			return;

		UpdateDrag();

		if (mouse.leftButton.wasReleasedThisFrame)
			EndDrag();
	}

	// ドラッグ開始
	private void BeginDrag()
	{
		// マウス位置からレイを飛ばしてタイルを取得
		Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
		if (!Physics.Raycast(ray, out RaycastHit hit, 1000.0f, _tileLayer))
			return;

		// タイルグループを取得
		TileGroup group = hit.collider.GetComponentInParent<TileGroup>();
		if (group == null || !group.IsMovable)
			return;

		if (IsGroupReserved(group))
			return;

		// グリッドへの登録状態を確認
		foreach (TileCell cell in group.Cells)
		{
			Vector2Int cellPosition = group.GridPosition + cell.LocalPosition;
			if (_tileGrid.GetCell(cellPosition) != cell)
				return;
		}

		if (!TryGetMouseWorldPosition(out Vector3 mousePosition))
			return;

		// 選択情報を保存
		_selectedGroup = group;
		_originalPosition = group.GridPosition;
		_targetPosition = _originalPosition;
		_grabOffset = group.transform.position - mousePosition;

		_selectedRenderers = group.GetComponentsInChildren<Renderer>();

		// グリッドから登録解除
		if (!_tileGrid.RemoveTile(group))
		{
			_selectedGroup = null;
			_selectedRenderers = null;
			return;
		}

		UpdateDrag();
	}

	// ドラッグ中
	private void UpdateDrag()
	{
		if (!TryGetMouseWorldPosition(out Vector3 mousePosition))
			return;

		Vector3 targetWorldPosition = mousePosition + _grabOffset;
		_targetPosition = _tileGrid.WorldToGrid(targetWorldPosition);

		// グリッド位置へスナップ
		Vector3 position = _tileGrid.GridToWorld(_targetPosition);
		position.y += _dragHeight;
		_selectedGroup.transform.position = position;

		// 配置可能か判定
		bool canPlace = _tileGrid.CanPlaceTile(_selectedGroup, _targetPosition) &&
						!IsTargetReserved(_selectedGroup, _targetPosition);

		SetPreviewColor(canPlace ? _validColor : _invalidColor);
	}

	// ドラッグ終了
	private void EndDrag()
	{
		bool canPlace = _tileGrid.CanPlaceTile(_selectedGroup, _targetPosition) &&
						!IsTargetReserved(_selectedGroup, _targetPosition);

		if (canPlace && _tileGrid.SetTile(_selectedGroup, _targetPosition))
		{
			ClearSelection();
			return;
		}

		// 配置できなければ元に戻す
		RestoreGroup();
	}

	// 元の位置へ戻す
	private void RestoreGroup()
	{
		if (!_tileGrid.SetTile(_selectedGroup, _originalPosition))
		{
			Debug.LogError("タイルグループを元の位置へ戻せませんでした", _selectedGroup);
		}

		ClearSelection();
	}

	// グループ内のセルがプレイヤーに使用されているか判定
	private bool IsGroupReserved(TileGroup group)
	{
		foreach (TileCell cell in group.Cells)
		{
			Vector2Int cellPosition = group.GridPosition + cell.LocalPosition;

			if (_player.IsTileReserved(cellPosition))
				return true;
		}

		return false;
	}

	// 配置先がプレイヤーの使用中マスと重なるか判定
	private bool IsTargetReserved(TileGroup group, Vector2Int targetPosition)
	{
		foreach (TileCell cell in group.Cells)
		{
			Vector2Int cellPosition = targetPosition + cell.LocalPosition;

			if (_player.IsTileReserved(cellPosition))
				return true;
		}

		return false;
	}

	// マウス位置をグリッド平面上のワールド座標に変換
	private bool TryGetMouseWorldPosition(out Vector3 worldPosition)
	{
		// マウス位置からレイを飛ばしてグリッド平面との交点を取得
		Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
		Plane plane = new Plane(_tileGrid.transform.up, _tileGrid.transform.position);

		// レイと平面の交点を計算
		if (plane.Raycast(ray, out float distance))
		{
			worldPosition = ray.GetPoint(distance);
			return true;
		}

		worldPosition = default;
		return false;
	}

	// ドラッグ中のプレビューカラーを設定
	private void SetPreviewColor(Color color)
	{
		if (_selectedRenderers == null)
			return;

		foreach (Renderer renderer in _selectedRenderers)
		{
			renderer.GetPropertyBlock(_propertyBlock);
			_propertyBlock.SetColor("_BaseColor", color);
			renderer.SetPropertyBlock(_propertyBlock);
		}
	}

	// プレビューカラーを解除
	private void ClearPreviewColor()
	{
		if (_selectedRenderers == null)
			return;

		foreach (Renderer renderer in _selectedRenderers)
		{
			renderer.SetPropertyBlock(null);
		}
	}

	// 選択状態を解除
	private void ClearSelection()
	{
		ClearPreviewColor();

		_selectedGroup = null;
		_selectedRenderers = null;
	}


}
