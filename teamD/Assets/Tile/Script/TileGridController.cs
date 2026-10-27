using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// マウスによるタイルグループの操作
/// 作成者：石田怜
/// </summary>
public class TileGridController : MonoBehaviour
{
	/// <summary>タイルをドラッグしているか(sagara)</summary>
	public bool IsDragging => _selectedGroup != null;

	enum DragType
	{
		MOVE,		// 移動
		PLACE		// 配置
	}

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

    [SerializeField, Tooltip("アウトライン用Renderer Data")]
    private UniversalRendererData _rendererData;

    [SerializeField, Tooltip("選択可能時のアウトラインカラー")]
    private Color _hoverValidColor = Color.green;

    [SerializeField, Tooltip("選択不可能時のアウトラインカラー")]
    private Color _hoverInvalidColor = Color.red;

    private TileGroup _selectedGroup;                       // 選択中のタイルグループ
    private Vector2Int _originalPosition;                   // 移動前のグリッド座標
    private Vector2Int _targetPosition;                     // 移動先のグリッド座標
    private Vector3 _grabOffset;                            // マウスとグループ基準位置の差
    private DragType _dragType;                             // ドラッグの種類

    private Renderer[] _selectedRenderers;                  // 選択中のレンダラー
    private MaterialPropertyBlock _propertyBlock;           // マテリアルパラメータ変更用
    private UnityAction<bool> _onDragEndCallback;           // ドラッグ終了時のコールバック

    private TileGroup _hoveredGroup;                        // マウスオーバー中のグループ
    private Dictionary<GameObject, int> _originalLayers;    // 変更前のレイヤー
    private int _hoverLayer;                                // マウスオーバー用レイヤー

    private OutlineRendererFeature _outlineRendererFeature;

    private readonly string _hoverLayerName = "SelectedTileGroup"; // マウスオーバー用レイヤー名


    // 外部から指定したタイルのドラッグ開始
    public bool BeginDragTile(TileGroup group, UnityAction<bool> onEndDrag)
    {
        if (!StartDragging(group))
            return false;

        _onDragEndCallback = onEndDrag;
        return true;
    }


    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        _hoverLayer = LayerMask.NameToLayer(_hoverLayerName);

        if (_hoverLayer < 0)
            Debug.LogError($"レイヤーが存在しません：{_hoverLayerName}", this);

        if (_camera == null)
            _camera = Camera.main;

        if (_rendererData != null)
        {
            // Renderer Featureを取得
            foreach (ScriptableRendererFeature feature in _rendererData.rendererFeatures)
            {
                if (feature is OutlineRendererFeature outlineFeature)
                {
                    _outlineRendererFeature = outlineFeature;
                    break;
                }
            }
        }
    }

    private void OnDisable()
    {
        ClearHover();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        if (_selectedGroup == null)
        {
            UpdateHover();

            if (mouse.leftButton.wasPressedThisFrame)
                BeginDrag();

            return;
        }

        UpdateDrag();

        if (mouse.leftButton.wasReleasedThisFrame)
            EndDrag();
    }

    // マウスオーバー中のグループを更新
    private void UpdateHover()
    {
        if (_hoverLayer < 0)
            return;

        // マウス位置からレイを飛ばしてタイルを取得
        Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());

        TileGroup group = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000.0f, _tileLayer))
        {
            group = hit.collider.GetComponentInParent<TileGroup>();

            // 静的なグループは対象外
            if (group != null && group.IsStatic)
                group = null;
        }

        // マウスオーバー対象が変更された場合のみレイヤーを更新
        if (_hoveredGroup != group)
        {
            ClearHover();

            if (group != null)
                SetHover(group);
        }

        // マウスオーバー中の状態に合わせてアウトライン色を更新
        if (_hoveredGroup != null)
            UpdateHoverColor(_hoveredGroup);
    }

    // マウスオーバー状態を設定
    private void SetHover(TileGroup group)
    {
        _hoveredGroup = group;
        _originalLayers = new Dictionary<GameObject, int>();

        // グループ内のすべてのオブジェクトのレイヤーを変更
        foreach (Transform child in group.GetComponentsInChildren<Transform>(true))
        {
            GameObject obj = child.gameObject;

            _originalLayers.Add(obj, obj.layer);
            obj.layer = _hoverLayer;
        }
    }

    // マウスオーバー中のアウトラインカラーを更新
    private void UpdateHoverColor(TileGroup group)
    {
        if (_outlineRendererFeature == null)
            return;

        bool canSelect = !IsGroupReserved(group);

        _outlineRendererFeature.SetOutlineColor(
            canSelect ? _hoverValidColor : _hoverInvalidColor
        );
    }

    // マウスオーバー状態を解除
    private void ClearHover()
    {
        // 元のレイヤーに復元
        if (_originalLayers != null)
        {
            foreach (var pair in _originalLayers)
            {
                if (pair.Key != null)
                    pair.Key.layer = pair.Value;
            }

            _originalLayers.Clear();
            _originalLayers = null;
        }

        _hoveredGroup = null;

        // アウトラインカラーを初期化
        if (_outlineRendererFeature != null)
            _outlineRendererFeature.ResetOutlineColor();
    }

    // マウスによるドラッグ開始
    private void BeginDrag()
    {
        // マウス位置からレイを飛ばしてタイルを取得
        Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, 1000.0f, _tileLayer))
            return;

        // タイルグループを取得
        TileGroup group = hit.collider.GetComponentInParent<TileGroup>();

        StartDragging(group);
    }

    // ドラッグ開始の共通処理
    private bool StartDragging(TileGroup group)
    {
        if (_selectedGroup != null || group == null || group.IsStatic)
            return false;

        // グループが初期化されていなければ初期化
        if (!group.IsInitialized)
            group.InitGroup(_tileGrid.TileSize);

        // グリッドへの登録状態を確認して、移動か新規配置かを判定
        DragType dragType = DragType.MOVE;

        foreach (TileCell cell in group.Cells)
        {
            Vector2Int cellPosition = group.GridPosition + cell.LocalPosition;

            if (_tileGrid.GetCell(cellPosition) != cell)
            {
                dragType = DragType.PLACE;
                break;
            }
        }

        // プレイヤーが使用中のマスが含まれるか判定
        if (IsGroupReserved(group))
            return false;

        if (!TryGetMouseWorldPosition(out Vector3 mousePosition))
            return false;

        // 既存タイルの場合はグリッドから登録解除
        if (dragType == DragType.MOVE)
        {
            if (!_tileGrid.RemoveTile(group))
                return false;
        }

        // マウスオーバー状態を解除
        ClearHover();

        // 選択情報を保存
        _selectedGroup = group;
        _dragType = dragType;
        _originalPosition = group.GridPosition;
        _targetPosition = _originalPosition;
        _grabOffset = group.transform.position - mousePosition;

        // 新規配置時はオフセットをゼロにする
        if (dragType == DragType.PLACE)
        {
            _grabOffset = Vector3.zero;
        }

        _selectedRenderers = group.GetComponentsInChildren<Renderer>();

        UpdateDrag();

        return true;
    }

    // ドラッグ中
    private void UpdateDrag()
    {
        if (!TryGetMouseWorldPosition(out Vector3 mousePosition))
            return;

        // ターゲット位置を計算
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
            _tileGrid.PlayPlacementEffect(_selectedGroup);

            // 新規配置時の処理
            if (_dragType == DragType.PLACE)
            {
                _selectedGroup.transform.parent = _tileGrid.transform;
                _selectedGroup.SetPlaced();

                // ドラッグ終了時のコールバックを呼び出す
                _onDragEndCallback?.Invoke(true);
                _onDragEndCallback = null;
            }

            ClearSelection();
            return;
        }

        // 配置できなければ元に戻す
        RestoreGroup();
    }

    // 元の位置へ戻す
    private void RestoreGroup()
    {
        if (_selectedGroup.IsPlaced)
        {
            if (!_tileGrid.SetTile(_selectedGroup, _originalPosition))
            {
                Debug.LogError("タイルグループを元の位置へ戻せませんでした", _selectedGroup);
            }
        }
        else
        {
            Destroy(_selectedGroup.gameObject);
        }

        // ドラッグ終了時のコールバックを呼び出す
        _onDragEndCallback?.Invoke(false);
        _onDragEndCallback = null;

        ClearSelection();
    }

    // グループ内のセルがプレイヤーに使用されているか判定
    private bool IsGroupReserved(TileGroup group)
    {
        // グループが配置されていない場合は使用中判定を行わない
        if (!group.IsPlaced)
            return false;

        // グループ内のセルがプレイヤーに使用されているか判定
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
        Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(_tileGrid.transform.up, _tileGrid.transform.position);

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
            if(renderer == null)
                continue;

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
            if (renderer == null)
                continue;

            renderer.SetPropertyBlock(null);
        }
    }

    // 指定したタイルをドラッグ中か判定
    public bool IsDraggingTile(TileGroup group)
    {
        return group != null && _selectedGroup == group;
    }

    // ドラッグ中の指定タイルを破壊
    public bool DestroyDraggingTile(TileGroup group)
    {
        if (group == null || _selectedGroup != group)
            return false;

        // ドラッグ終了時のコールバックを退避
        UnityAction<bool> callback = _onDragEndCallback;

        // タイル破壊エフェクトを再生
        _tileGrid.PlayBreakEffect(group);

        // ドラッグ状態を解除
        ClearSelection();
        ClearHover();

        // タイルグループを破壊
        Destroy(group.gameObject);

        // ドラッグ失敗として通知
        callback?.Invoke(false);

        return true;
    }

    // 選択状態を解除
    private void ClearSelection()
    {
        ClearPreviewColor();

        _selectedGroup = null;
        _selectedRenderers = null;
        _onDragEndCallback = null;
    }
}
