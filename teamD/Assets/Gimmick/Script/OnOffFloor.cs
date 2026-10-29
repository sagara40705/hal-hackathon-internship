using UnityEngine;

/// <summary>固定TileGroupをON/OFFでグリッドから出し入れする。</summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(TileGroup))]
public sealed class OnOffFloor : MonoBehaviour
{
    [SerializeField, Tooltip("未指定なら同じTileGridのコントローラーを自動取得")]
    private OnOffController _controller;

    [SerializeField, Tooltip("OFF（青床）ならfalse、ON（オレンジ床）ならtrue")]
    private bool _solidWhenOn;

    [SerializeField, Tooltip("実体化中の見た目。TileCellはこの外に置く")]
    private GameObject _solidVisual;

    [SerializeField, Tooltip("消えているときの枠。Colliderは付けない")]
    private GameObject _inactiveVisual;

    [SerializeField] private OnOffVisual _visual;

    private TileGrid _grid;
    private TileGroup _group;
    private Collider[] _colliders;
    private Vector2Int[] _positions;
    private bool[] _placementWasAllowed;
    private bool _started;
    private bool _ready;

    public bool IsSolid { get; private set; }
    public bool SolidWhenOn => _solidWhenOn;

    private void Start()
    {
        _started = true;
        Initialize();
    }

    private void OnEnable()
    {
        if (_started)
            Initialize();
    }

    private void Initialize()
    {
        if (_ready)
            return;

        _grid = GetComponentInParent<TileGrid>();
        _group = GetComponent<TileGroup>();
        if (_grid == null || !_group.IsStatic)
        {
            Debug.LogWarning("切替床はTileGridの子に置き、TileGroupの固定フラグを有効にしてください。", this);
            return;
        }

        if (_controller == null)
            _controller = OnOffController.Resolve(this);
        if (_controller == null || !_controller.isActiveAndEnabled || _controller.Grid != _grid)
            return;

        if (!_group.IsInitialized)
            _group.InitGroup(_grid.TileSize);
        if (_group.Cells.Length == 0)
            return;

        Vector2Int origin = _grid.WorldToGrid(transform.position);
        _positions = new Vector2Int[_group.Cells.Length];
        _placementWasAllowed = new bool[_positions.Length];
        for (int i = 0; i < _positions.Length; i++)
        {
            _positions[i] = origin + _group.Cells[i].LocalPosition;
            TileCell existing = _grid.GetCell(_positions[i]);
            if (!_grid.IsInPlacementArea(_positions[i]) ||
                (existing != null && existing != _group.Cells[i]))
            {
                Debug.LogWarning("切替床は配置可能範囲内で、他のタイルと重ならない位置に配置してください。", this);
                return;
            }
            _placementWasAllowed[i] = true;
        }

        // 通常はTileGrid.Awakeで登録済み。実行中に生成したPrefabも同じ経路へ合わせる。
        if (_grid.GetCell(_positions[0]) != _group.Cells[0] && !_grid.SetTile(_group, origin))
            return;

        _group.SetPlaced();
        _colliders = GetComponentsInChildren<Collider>(true);
        _ready = true;
        _controller.Register(this);
    }

    internal void ApplyState(bool isOn)
    {
        if (!_ready)
            return;

        bool solid = isOn == _solidWhenOn;
        bool registered = _grid.GetCell(_positions[0]) == _group.Cells[0];
        if (solid)
        {
            RestorePlacementArea();
            if (!registered && !_grid.SetTile(_group, _group.GridPosition))
            {
                Debug.LogError("切替床の再登録に失敗しました。配置先の重複を確認してください。", this);
                ReservePlacementArea();
                SetAppearance(false);
                return;
            }
        }
        else
        {
            if (registered && !_grid.RemoveTile(_group))
                return;
            // 見た目が消えていても、その場所に別のタイルを置けないようにする。
            ReservePlacementArea();
        }

        bool lostSupport = IsSolid && !solid;
        SetAppearance(solid);
        if (lostSupport)
        {
            // 足元が消えた場合も、既存の落下・リトライ経路を使う。
            foreach (Player player in FindObjectsByType<Player>(FindObjectsSortMode.None))
                CheckPlayerSupport(player);
        }
    }

    private void CheckPlayerSupport(Player player)
    {
        // JumpForward中はPlayerが無効になるため、空中で足元が切り替わっても落下させない。
        if (!player.isActiveAndEnabled || player.Grid != _grid)
            return;
        foreach (Vector2Int position in _positions)
        {
            if (player.IsTileReserved(position))
            {
                player.Damage();
                return;
            }
        }
    }

    private void SetAppearance(bool solid, bool animate = true)
    {
        IsSolid = solid;
        if (_solidVisual != null)
            _solidVisual.SetActive(solid);
        if (_inactiveVisual != null)
            _inactiveVisual.SetActive(!solid);
        _visual?.ShowFloor(solid, animate && isActiveAndEnabled);
        foreach (Collider tileCollider in _colliders)
        {
            if (tileCollider != null)
                tileCollider.enabled = solid;
        }
    }

    private void ReservePlacementArea()
    {
        foreach (Vector2Int position in _positions)
            _grid.SetPlacementAllowed(position, false);
    }

    private void RestorePlacementArea()
    {
        for (int i = 0; i < _positions.Length; i++)
            _grid.SetPlacementAllowed(_positions[i], _placementWasAllowed[i]);
    }

    private void OnDisable()
    {
        if (!_ready)
            return;
        _controller?.Unregister(this);
        if (_grid != null)
        {
            RestorePlacementArea();
            if (gameObject.activeInHierarchy)
            {
                // コンポーネントだけ無効化した場合は通常の固定床へ戻す。
                if (_grid.GetCell(_positions[0]) != _group.Cells[0])
                    _grid.SetTile(_group, _group.GridPosition);
            }
            else if (_grid.GetCell(_positions[0]) == _group.Cells[0])
                _grid.RemoveTile(_group);
        }
        SetAppearance(true, false);
        _ready = false;
    }
}
