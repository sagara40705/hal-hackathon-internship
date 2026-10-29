using UnityEngine;
using UnityEngine.Events;
using System.Collections;

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

    [SerializeField, Tooltip("Stopによる停止時のイベント")]
    private UnityEvent _onStoppedEvent = new();

  

    private Vector2Int _currentPosition;        // 現在のグリッド座標
	private Vector2Int _nextPosition;           // 次のグリッド座標
	private Vector2Int _direction;              // 移動方向

	private float _startTimer;                  // 開始時の遅延タイマー

	private bool _isMoving;                     // 移動中フラグ
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
		NotifyCellArrived();
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

		// 次の足場に着地した瞬間に一度だけ再生する。
        PlayLandingSound();

		// セルの効果を発動
		NotifyCellArrived();
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

    // 足元にタイルが存在するか判定
    public void CheckGroundTile()
    {
        if (_isGameOver)
            return;

        if (_tileGrid == null)
            return;

        // 現在のプレイヤー位置にタイルが存在するか確認
        if (_tileGrid.GetCell(_currentPosition) == null &&
            _tileGrid.GetCell(_nextPosition) == null)
        {
            OnFall();
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

    // #2-gimmick(sagara)
    // ============================
    // 通常移動の処理を変更せず、進行方向の1マス先を飛び越える(sagara)
    public IEnumerator JumpForward(float duration, float height)
    {
        // 空中・着地イベント中の再ジャンプも抑止する。
        if (!isActiveAndEnabled || _isGameOver)
            yield break;

        Vector2Int destination = _currentPosition + _direction * 2;
        Vector3 start = transform.position;
        Vector3 end = _tileGrid.GridToWorld(destination);
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        height = Mathf.Max(0f, height);

        // MonoBehaviourを無効化しても、このコルーチンは継続する。
        enabled = false;
        try
        {
            // ジャンプ開始時に一度だけ再生する。
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySEById(SoundId.PlayerJump);

            while (elapsed < duration)
            {
                if (_isGameOver)
                    yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, end, t)
                    + _tileGrid.transform.up * (4f * height * t * (1f - t));
                yield return null;
            }

            if (_isGameOver)
                yield break;

            _currentPosition = destination;
            _isMoving = false;
            transform.position = end;

            // 空中に通過したセルの効果は発動せず、着地先だけを確認する。
            TileCell cell = _tileGrid.GetCell(destination);
            if (cell == null)
                OnFall();
            else
            {
                // 空中では鳴らさず、着地先に床がある場合だけ再生する。
                PlayLandingSound();
                NotifyCellArrived();
                cell.OnEnterPlayer(this);
            }
        }
        finally
        {
            enabled = true;
        }
    }

    // 初期配置・ワープの通知とは分け、実際の着地箇所から呼ぶ。
    private void PlayLandingSound()
    {
        if (!_isGameOver && SoundManager.Instance != null)
            SoundManager.Instance.PlaySEById(SoundId.PlayerLand);
    }

    // ダメージを受けたときの処理(sagara)
    public void Damage()
    {
        // ゲームオーバー確定後の重複通知では鳴らさない。
        if (!_isGameOver)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySEById(SoundId.PlayerDamage);
        }
        OnFall();
    }

    // 速度を取得(sagara)
    public float MoveSpeed => _moveSpeed;

    // 速度を変更(sagara)
    public void SetMoveSpeed(float speed)
    {
        _moveSpeed = speed;
    }

    // 移動中フラグ(sagara)
    private bool _isProcessingMove;

    // 座標を移動(sagara)
    public void MoveByGridOffset(Vector2Int gridOffset)
    {
        // 移動中は無視する
        if (_isProcessingMove)
            return;

        // 移動先の座標を計算
        Vector2Int destination = _currentPosition + gridOffset;
        TileCell cell = _tileGrid.GetCell(destination);
        if (cell == null)
            return;

        // 移動中フラグを立てる
        _isProcessingMove = true;
        try
        {
            // 座標を移動し、到着イベントを発火する
            _currentPosition = destination;
            transform.position = _tileGrid.GridToWorld(destination);
            NotifyCellArrived();
            cell.OnEnterPlayer(this);
        }
        finally
        {
            _isProcessingMove = false;
        }
    }
    // ============================

    // #9-stage-Item(sagara)
    // ============================
    // アイテム取得用の到着通知。既存の移動・床判定・セル効果は各関数に残す。
    public TileGrid Grid => _tileGrid;
    public event System.Action<Player, Vector2Int> CellArrived;

    // セル到着通知を発火する。床の存在を確認できた到着箇所からだけ呼び出す。
    private void NotifyCellArrived()
    {
        // 床の存在を確認できた到着箇所からだけ呼び出す。
        if (!_isGameOver)
            CellArrived?.Invoke(this, _currentPosition);
    }

    // Playerを停止させる
    public void Stop()
    {
        _isGameOver = true;
        _isMoving = false;
        _onStoppedEvent?.Invoke();
    }

    // ============================
}
