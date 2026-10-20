using UnityEngine;

/// <summary>
/// 敵のアクションの基底クラス
/// 作成者：石田怜
/// </summary>
public class EnemyAction : MonoBehaviour
{
    protected bool _isAction = false;     // アクション実行中かのフラグ

    // アクション実行中かを取得する
    public bool IsAction()
    {
        return _isAction;
    }

    virtual public void StartAction()
    {
    }
}
