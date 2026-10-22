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

    [SerializeField, Tooltip("攻撃予告表示")]
    private EnemyAttackIndicator _attackIndicator;

    [SerializeField, Tooltip("攻撃の発生時間")]
    private float _startupTime;

    [SerializeField, Tooltip("攻撃の硬直")]
    private float _recoveryTime = 1.0f;

    [SerializeField, Tooltip("攻撃するマス数")]
    private int _attackCount = 3;

    private List<Vector2Int> _targetPosList;    // 攻撃対象の座標リスト
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
        _targetPosList = new List<Vector2Int>();
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
        _targetPosList.Clear();

        if (_tileGrid == null)
            return;

        // グリッド内の全ての座標を候補として取得
        Vector2Int gridSize = _tileGrid.GridSize;
        List<Vector2Int> candidates = new();
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (_tileGrid.IsInPlacementArea(new Vector2Int(x, y)))
                    candidates.Add(new Vector2Int(x, y));
            }
        }

        // 攻撃対象をランダムに選択
        int count = Mathf.Min(_attackCount, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(i, candidates.Count);

            Vector2Int temp = candidates[i];
            candidates[i] = candidates[randomIndex];
            candidates[randomIndex] = temp;

            _targetPosList.Add(candidates[i]);
        }

        // 攻撃予告を表示
        _attackIndicator?.ShowIndicators(_targetPosList);
    }

    private void Attack()
    {
        if (_targetPosList == null || _targetPosList.Count == 0)
            return;

        foreach (Vector2Int pos in _targetPosList)
        {
            _tileGrid?.DestroyTile(pos);
            _player?.TryAttack(pos);
        }

        // 攻撃予告を非表示
        _attackIndicator?.ClearIndicators();
    }
}
