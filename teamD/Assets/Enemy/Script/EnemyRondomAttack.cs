using UnityEngine;
using System.Collections.Generic;

public class EnemyRondomAttack : EnemyAction
{
    enum State
    {
        NONE,       // 初期状態
        STARTUP,    // 発生
        RECOVERY    // 硬直
    }

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

    private void UpdateStartUp()
    {
        if (_stateTimer >= _startupTime)
        {
            _state = State.RECOVERY;
            _stateTimer = 0.0f;

            Attack();
        }
    }

    private void UpdateRecovery()
    {
        if (_stateTimer >= _recoveryTime)
        {
            _state = State.NONE;
            _stateTimer = 0.0f;
            _isAction = false;
        }
    }

    private void SetTarget()
    {
        _targetPosList.Clear();

        if (_tileGrid == null)
            return;

        // 攻撃対象をランダムに選択
        int maxAttempts = 100;
        int attempts = 0;

        while (_targetPosList.Count < _attackCount && attempts < maxAttempts)
        {
            attempts++;

            Vector2Int position = new Vector2Int(
                Random.Range(0, _tileGrid.GridSize.x),
                Random.Range(0, _tileGrid.GridSize.y)
            );

            if (_targetPosList.Contains(position))
                continue;

            _targetPosList.Add(position);
        }

        // 攻撃予告を表示
        _attackIndicator?.ShowIndicators(_targetPosList);
    }

    private void Attack()
    {
        if (_tileGrid == null)
            return;

        if (_targetPosList == null || _targetPosList.Count == 0)
            return;

        foreach (Vector2Int pos in _targetPosList)
        {
            _tileGrid.DestroyTile(pos);
        }

        // 攻撃予告を非表示
        _attackIndicator?.ClearIndicators();
    }
}
