using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// タイルグループを構成するセル
/// 作成者：石田怜
/// </summary>
public class TileCell : MonoBehaviour
{
	[SerializeField, Tooltip("プレイヤー進入時のイベント")]
	private UnityEvent<Player> _onEnterPlayerEvent;

	private TileGroup _parentGroup;     // 所属するタイルグループ
	private Vector2Int _localPosition;  // グループ内のローカル座標

	public Vector2Int LocalPosition => _localPosition;
	public TileGroup ParentGroup => _parentGroup;


	// セルの初期化
	public void InitCell(TileGroup tileGroup, float tileSize)
	{
		_parentGroup = tileGroup;
		Vector3 localPosition = tileGroup.transform.InverseTransformPoint(transform.position);

		_localPosition = new Vector2Int(
			Mathf.RoundToInt(localPosition.x / tileSize),
			Mathf.RoundToInt(localPosition.z / tileSize)
		);
	}

	// プレイヤーがマスに進入したときの処理
	public void OnEnterPlayer(Player player)
	{
		_onEnterPlayerEvent?.Invoke(player);
	}
}
