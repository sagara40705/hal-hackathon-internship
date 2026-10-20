using UnityEngine;

/// <summary>
/// 敵の行動を管理するクラス
/// </summary>
public class EnemyRoot : MonoBehaviour
{
    [SerializeField, Tooltip("敵のアクション一覧")]
    private EnemyAction[] _actions;

    [SerializeField, Tooltip("アクション間の待機時間")]
    private float _actionInterval = 1.0f;

    private EnemyAction _currentAction;     // 現在実行中のアクション
    private float _actionTimer = 0.0f;      // アクション間の待機タイマー


    private void Awake()
    {
        // アクションを取得
        if (_actions == null || _actions.Length == 0)
            _actions = GetComponents<EnemyAction>();
    }

    private void Update()
    {
        // アクション実行中
        if (_currentAction != null)
        {
            if (_currentAction.IsAction())
                return;

            // アクション終了
            _currentAction = null;
            _actionTimer = 0.0f;
        }

        // アクション間の待機
        _actionTimer += Time.deltaTime;

        if (_actionTimer < _actionInterval)
            return;

        // 次のアクションを実行
        StartNextAction();
    }

    // 次のアクションを開始
    private void StartNextAction()
    {
        if (_actions == null || _actions.Length == 0)
            return;

        // 有効なアクションからランダムに選択
        int validCount = 0;
        foreach (EnemyAction action in _actions)
        {
            if (action != null && action.isActiveAndEnabled)
                validCount++;
        }

        if (validCount == 0)
            return;

        int randomIndex = Random.Range(0, validCount);
        foreach (EnemyAction action in _actions)
        {
            if (action == null || !action.isActiveAndEnabled)
                continue;

            if (randomIndex-- > 0)
                continue;

            _currentAction = action;
            _currentAction.StartAction();
            _actionTimer = 0.0f;
            return;
        }
    }
}
