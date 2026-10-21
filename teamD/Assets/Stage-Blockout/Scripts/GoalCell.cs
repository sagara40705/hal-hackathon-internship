using UnityEngine;
using UnityEngine.Events;

/// <summary>ゴールセルへの進入時に鍵の所持数だけを確認する。クリア処理は行わない。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TileCell))]
public sealed class GoalCell : MonoBehaviour
{
    [SerializeField, Min(1), Tooltip("ゴール条件を満たすために必要な鍵の所持数")]
    private int _requiredKeyCount = 1;

    [SerializeField, Tooltip("クリア条件を満たしたときの通知")]
    private UnityEvent<Player> _onKeyRequirementMet = new();

    public int RequiredKeyCount => Mathf.Max(1, _requiredKeyCount);

    // Playerが必要な鍵を持っているかどうかを判定する
    public bool HasRequiredKeys(Player player)
    {
        return player != null &&
            player.TryGetComponent(out PlayerInventory inventory) &&
            inventory.KeyCount >= RequiredKeyCount;
    }

    // TileCellの進入イベントから、動的なPlayer引数を渡して呼び出す。
    public void OnEnterPlayer(Player player)
    {
        if (player == null || !isActiveAndEnabled)
        {
            return;
        }
            
        // 鍵をもっている時
        if (HasRequiredKeys(player))
        {
            _onKeyRequirementMet?.Invoke(player);
            Debug.Log("クリア！！！");
        }
    }
}
