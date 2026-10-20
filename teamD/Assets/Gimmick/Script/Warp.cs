using UnityEngine;

public class Warp : MonoBehaviour
{
    [SerializeField, Tooltip("ワープの移動量")]
    private Vector2Int _warpAmount = Vector2Int.zero;

    public Vector2Int WarpAmount => _warpAmount;

    public void OnEnterPlayer(Player player)
    {
        player.MoveByGridOffset(_warpAmount);
    }
}
