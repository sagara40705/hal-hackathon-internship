using UnityEngine;

public class Warp : MonoBehaviour
{
    [SerializeField, Tooltip("ワープの移動量")]
    private Vector2Int _warpAmount = Vector2Int.zero;

    public Vector2Int WarpAmount => _warpAmount;

    public void OnEnterPlayer(Player player)
    {
        player.MoveByGridOffset(_warpAmount);
        PeriodicBlockContacts.OnTeleported(player);
    }

    private void OnDrawGizmos()
    {
        TileGrid grid = GetComponentInParent<TileGrid>();
        if (grid == null)
            return;

        Vector2Int source = grid.WorldToGrid(transform.position);
        Vector3 start = grid.transform.InverseTransformPoint(grid.GridToWorld(source));
        Vector3 destination = grid.transform.InverseTransformPoint(grid.GridToWorld(source + _warpAmount));
        float tileSize = grid.transform.InverseTransformPoint(grid.GridToWorld(Vector2Int.right)).x
            - grid.transform.InverseTransformPoint(grid.GridToWorld(Vector2Int.zero)).x;

        // タイルの上面より少し上に表示する。
        start.y = destination.y = 0.6f;

        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.color = Color.magenta;
        Gizmos.matrix = grid.transform.localToWorldMatrix;
        Gizmos.DrawLine(start, destination);
        Gizmos.DrawWireCube(destination, new Vector3(tileSize * 0.9f, 0.05f, tileSize * 0.9f));
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
