using UnityEngine;

public class ChangeDirection : MonoBehaviour
{
	[SerializeField, Tooltip("移動方向")]
	private Vector2Int _direction = Vector2Int.up;

	public void OnEnterPlayer(Player player)
	{
		player.SetDirection(_direction);
	}
}
