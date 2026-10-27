using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// プレイヤーの近くのタイルを攻撃するプログラム
/// 作成者：牛田大翔
/// </summary>
public class EnemyActionRandam : EnemyAction
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

    [SerializeField, Tooltip("Playerを中心とした攻撃範囲（マス数）")]
    private int _attackRange = 3;

    private List<Vector2Int> _targetPosList;    // 攻撃対象の座標リスト
    private State _state = State.NONE;          // 現在の状態
    private float _stateTimer = 0.0f;           // タイマー
    Animator animator;
    public override void StartAction()
    {
        _isAction = true;
        _state = State.STARTUP;
        _stateTimer = 0.0f;
        animator = GetComponent<Animator>();
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

        if (_tileGrid == null || _player == null)
            return;

        // Playerの現在のマス座標を取得
        Vector2Int playerPos = _player.CurrnetPosition;

        List<Vector2Int> candidates = new();

        // Playerを中心に指定範囲のタイルを取得
        for (int x = -_attackRange; x <= _attackRange; x++)
        {
            for (int y = -_attackRange; y <= _attackRange; y++)
            {
                Vector2Int pos = playerPos + new Vector2Int(x, y);

                // グリッドの範囲外は除外
                if (pos.x < 0 || pos.y < 0 ||
                    pos.x >= _tileGrid.GridSize.x ||
                    pos.y >= _tileGrid.GridSize.y)
                {
                    continue;
                }

                // 配置可能なタイルだけ候補にする
                if (!_tileGrid.IsInPlacementArea(pos))
                    continue;

                candidates.Add(pos);
            }
        }

        // 候補からランダムに攻撃対象を選択
        int count = Mathf.Min(_attackCount, candidates.Count);

        for (int i = 0; i < count; i++)
        {
           
            int randomIndex = Random.Range(i, candidates.Count);

            Vector2Int temp = candidates[i];
            candidates[i] = candidates[randomIndex];
            candidates[randomIndex] = temp;

            _targetPosList.Add(candidates[i]);
        }

        animator.SetTrigger("Attack");
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
            //_player?.TryAttack(pos);
        }
     
        // 攻撃予告を非表示
        _attackIndicator?.ClearIndicators();
    }
    //[Header("Player")]
    //public Transform player;

    //[Header("攻撃設定")]
    //public int attackCount = 5;
    //public float randomRadius = 5f;

    //[Header("攻撃時間")]
    //public float warningTime = 1f;
    //public float attackDuration = 0.2f;

    //[Header("Prefab")]
    //public GameObject warningPrefab;
    //public GameObject attackPrefab;

    //public override void StartAction()
    //{
    //    if (_isAction || player == null)
    //        return;

    //    StartCoroutine(AttackRoutine());
    //}

    //IEnumerator AttackRoutine()
    //{
    //    _isAction = true;

    //    List<Vector3> positions = new List<Vector3>();
    //    List<GameObject> warnings = new List<GameObject>();

    //    // 攻撃位置をランダムに決定
    //    for (int i = 0; i < attackCount; i++)
    //    {
    //        Vector2 random =
    //            Random.insideUnitCircle * randomRadius;

    //        Vector3 position = player.position +
    //            new Vector3(random.x, 0f, random.y);

    //        positions.Add(position);

    //        // 攻撃予告を生成
    //        if (warningPrefab != null)
    //        {
    //            GameObject warning = Instantiate(
    //                warningPrefab,
    //                position,
    //                Quaternion.identity
    //            );

    //            warnings.Add(warning);
    //        }


    //    }

    //    // 攻撃まで待機
    //    yield return new WaitForSeconds(warningTime);

    //    // 予告を削除
    //    foreach (GameObject warning in warnings)
    //    {
    //        if (warning != null)
    //            Destroy(warning);
    //    }

    //    // 攻撃オブジェクトを生成
    //    foreach (Vector3 position in positions)
    //    {
    //        if (attackPrefab != null)
    //        {
    //            GameObject attack = Instantiate(
    //                attackPrefab,
    //                position,
    //                Quaternion.identity
    //            );

    //            Destroy(attack, attackDuration);
    //        }
    //    }

    //    yield return new WaitForSeconds(attackDuration);

    //    _isAction = false;
    //}
    //private void OnTriggerEnter(Collider other)
    //{


    //        Debug.Log("Objectに攻撃が命中！");

    //        // 当たったObjectを破壊
    //        Destroy(other.gameObject);

    //}
}

