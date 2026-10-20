using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// グリッドに沿って自動前進するプレイヤー
/// 作成者：石田怜
/// </summary>
public class Player : MonoBehaviour
{
	[SerializeField, Tooltip("タイルグリッド")]
	private TileGrid _tileGrid;

	[Header("移動設定")]
	[SerializeField, Tooltip("移動速度")]
	private float _moveSpeed = 1.0f;

	[SerializeField, Tooltip("開始時の遅延")]
	private float _startDelay = 0.5f;

	[SerializeField, Tooltip("開始時の移動方向")]
	private Vector2Int _startDirection = Vector2Int.right;

	[Header("イベント")]
	[SerializeField, Tooltip("落下時のイベント")]
	private UnityEvent _onFallEvent;

    [SerializeField, Tooltip("被弾時のイベント")]
    private UnityEvent _onDamageEvent;

    private Vector2Int _currentPosition;        // 現在のグリッド座標
	private Vector2Int _nextPosition;           // 次のグリッド座標
	private Vector2Int _direction;              // 移動方向

	private float _startTimer;                  // 開始時の遅延タイマー

	private bool _isMoving;						// 移動中フラグ
	private bool _isGameOver;                   // ゲームオーバーフラグ

	public Vector2Int CurrnetPosition => _currentPosition;
	public Vector2Int Direction => _direction;


	private void Start()
	{
		// 初期座標と進行方向を設定
		_currentPosition = _tileGrid.WorldToGrid(transform.position);
		_direction = _startDirection;

		// プレイヤーの位置をグリッドに合わせる
		transform.position = _tileGrid.GridToWorld(_currentPosition);

		// 開始時の遅延を設定
		_startTimer = _startDelay;

		// 最初のセルの効果処理
		TileCell cell = _tileGrid.GetCell(_currentPosition);
		if (cell == null)
		{
			OnFall();
			return;
		}
		cell.OnEnterPlayer(this);
	}

	private void Update()
	{
		if (_isGameOver)
			return;

		// 開始時の遅延
		if (_startTimer > 0.0f)
		{
			_startTimer -= Time.deltaTime;
			return;
		}

		if (!_isMoving)
			BeginMove();

		UpdateMove();
	}

	// 次のマスへの移動を開始
	private void BeginMove()
	{
		_nextPosition = _currentPosition + _direction;
		_isMoving = true;
	}

	// 次のマスへ前進
	private void UpdateMove()
	{
		Vector3 targetPosition = _tileGrid.GridToWorld(_nextPosition);

		// 次のマスへ移動
		transform.position = Vector3.MoveTowards(
			transform.position,
			targetPosition,
			_moveSpeed * Time.deltaTime
		);

		if (Vector3.Distance(transform.position, targetPosition) > 0.0001f)
			return;

		// 次のマスへ到達
		_currentPosition = _nextPosition;
		_isMoving = false;

		TileCell cell = _tileGrid.GetCell(_currentPosition);

		// 床が存在しない場合
		if (cell == null)
		{
			OnFall();
			return;
		}

		// セルの効果を発動
		cell.OnEnterPlayer(this);
	}

	// 進行方向を変更
	public void SetDirection(Vector2Int direction)
	{
		// 上下左右の4方向だけ許可
		if (Mathf.Abs(direction.x) +
			Mathf.Abs(direction.y) != 1)
			return;

		_direction = direction;
	}

	// タイルが使用中か取得
	public bool IsTileReserved(Vector2Int gridPosition)
	{
		if (_isGameOver)
			return false;

		return gridPosition == _currentPosition;
	}

    // プレイヤーへの攻撃処理
    public void TryAttack(Vector2Int gridPos)
    {
        if (_isGameOver)
            return;

        if (gridPos == _currentPosition)
        {
            _isGameOver = true;
            _onDamageEvent?.Invoke();
        }
    }

    // 落下時の処理
    private void OnFall()
	{
		if (_isGameOver)
			return;

		_isGameOver = true;
		_isMoving = false;

		_onFallEvent?.Invoke();
	}
}
