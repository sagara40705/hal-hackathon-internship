using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ランダムにマスを攻撃する敵のアクション
/// 作成者：石田怜
/// </summary>
public class EnemyRondomAttack : EnemyAction
{
    enum State
    {
        NONE,       // 初期状態
        STARTUP,    // 発生
        RECOVERY    // 硬直
    }

    [SerializeField, Tooltip("プレイヤー")]
    private Player _player;

    [SerializeField, Tooltip("タイルグリッド")]
    private TileGrid _tileGrid;

    [SerializeField, Tooltip("タイルグリッドコントローラー")]
    private TileGridController _tileDragController;

    [SerializeField, Tooltip("攻撃予告表示")]
    private EnemyAttackIndicator _attackIndicator;

    [SerializeField, Tooltip("生成する攻撃オブジェクト")]
    private EnemyProjectileEffect _enemyAttackPrefab;

    [SerializeField, Tooltip("アニメーターコントローラー")]
    private Animator _animator;

    [SerializeField, Tooltip("攻撃の発生時間")]
    private float _startupTime = 5.0f;

    [SerializeField, Tooltip("攻撃の硬直")]
    private float _recoveryTime = 1.0f;

    [SerializeField, Tooltip("攻撃する最大タイル数"), Min(0)]
    private int _maxAttackCount = 3;

    [SerializeField, Tooltip("フィールドに残す最小タイル数"), Min(0)]
    private int _minTileCount = 3;

    [SerializeField, Tooltip("Playerを中心とした攻撃範囲（マス数）"), Min(0)]
    private int _attackRange = 3;

    private List<TileGroup> _targetTileList;    // 攻撃対象のタイルリスト
    private State _state = State.NONE;          // 現在の状態
    private float _stateTimer = 0.0f;           // タイマー

    public override void StartAction()
    {
        _isAction = true;
        _state = State.STARTUP;
        _stateTimer = 0.0f;

        SetTarget();
    }

    private void Awake()
    {
        _targetTileList = new List<TileGroup>();
    }

    private void Update()
    {
        if (!_isAction)
            return;

        _stateTimer += Time.deltaTime;
        switch (_state)
        {
            case State.STARTUP:
                UpdateStartUp();
                break;

            case State.RECOVERY:
                UpdateRecovery();
                break;
        }
    }

    // 攻撃の発生状態を更新
    private void UpdateStartUp()
    {
        if (_stateTimer >= _startupTime)
        {
            _state = State.RECOVERY;
            _stateTimer = 0.0f;

            Attack();
        }
    }

    // 攻撃の硬直状態を更新
    private void UpdateRecovery()
    {
        if (_stateTimer >= _recoveryTime)
        {
            _state = State.NONE;
            _stateTimer = 0.0f;
            _isAction = false;
        }
    }

    // 攻撃対象を設定
    private void SetTarget()
    {
        _targetTileList.Clear();

        if (_tileGrid == null || _player == null)
            return;

        List<TileGroup> candidates = new();
        Vector2Int playerPos = _player.CurrnetPosition;

        // Playerを中心に指定範囲のタイルを取得
        for (int x = -_attackRange; x <= _attackRange; x++)
        {
            for (int y = -_attackRange; y <= _attackRange; y++)
            {
                Vector2Int targetPos = playerPos + new Vector2Int(x, y);
                if (!_tileGrid.IsInPlacementArea(targetPos))
                    continue;

                // タイルが存在しない場合や静的タイルの場合は対象外
                TileGroup tileGroup = _tileGrid.GetTileGroup(targetPos);
                if (tileGroup == null || tileGroup.IsStatic)
                    continue;

                // 攻撃候補に追加
                if (!candidates.Contains(tileGroup))
                {
                    candidates.Add(tileGroup);
                }
            }
        }

        int count = Mathf.Min(_maxAttackCount, candidates.Count);
        int tileCount = _tileGrid.GetDestructibleTileCount();
        count = Mathf.Min(count, tileCount - _minTileCount);

        // 攻撃対象をランダムに選択
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(i, candidates.Count);

            TileGroup temp = candidates[i];
            candidates[i] = candidates[randomIndex];
            candidates[randomIndex] = temp;

            _targetTileList.Add(candidates[i]);
        }

        // 攻撃予告を表示
        _attackIndicator?.ShowIndicators(_targetTileList);
    }

    private void Attack()
    {
        if (_enemyAttackPrefab == null)
            return;

        if(_targetTileList.Count == 0)
            return;

        foreach (TileGroup tileGroup in _targetTileList)
        {
            if (tileGroup == null)
                continue;

            EnemyProjectileEffect bullet = Instantiate(_enemyAttackPrefab, transform.position, Quaternion.identity);
            bullet.Launch(tileGroup.transform, () =>
            {
                // タイルグリッドからタイルグループを破壊
                if (_tileGrid != null && _tileGrid.DestroyTile(tileGroup))
                    return;

                // タイルグループが破壊できなかった場合、ドラッグ中タイルの破壊を試す
                if (_tileDragController != null)
                {
                    _tileDragController.DestroyDraggingTile(tileGroup);
                }

                // プレイヤーの足元にタイルが存在するか確認
                _player.CheckGroundTile();
            });
        }

        // 攻撃予告を非表示
        _attackIndicator?.ClearIndicators();

        _animator?.SetTrigger("Attack");

        _targetTileList.Clear();
    }
}
