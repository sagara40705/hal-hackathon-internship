using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敵の行動を管理するクラス
/// 作成者：石田怜
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

        // 有効なアクションをリストに追加
        List<EnemyAction> validActions = new List<EnemyAction>();
        foreach (EnemyAction action in _actions)
        {
            if (action != null && action.isActiveAndEnabled)
                validActions.Add(action);
        }
        if (validActions.Count == 0)
            return;

        // 有効なアクションからランダムに選択
        int randomIndex = Random.Range(0, validActions.Count);
        _currentAction = validActions[randomIndex];
        _currentAction.StartAction();
        _actionTimer = 0.0f;
    }
}
