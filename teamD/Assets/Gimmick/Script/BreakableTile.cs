using System.Collections;
using UnityEngine;

// 1セルのTileGroup用。初回進入後、点滅してグループごと消える。
[RequireComponent(typeof(TileCell))]
public class BreakableTile : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("初回進入から破壊までの秒数")]
    private float _destroyDelay = 3f;

    [SerializeField, Min(0.01f), Tooltip("点滅の切り替え間隔（秒）")]
    private float _blinkInterval = 0.15f;

    [SerializeField, Tooltip("点滅時の色")]
    private Color _blinkColor = Color.white;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    // 進入済みフラグ。再進入時に再度点滅しないようにする。
    private bool _hasTriggered;

    public void OnEnterPlayer(Player player)
    {
        if (_hasTriggered)
        {
            return;
        }

        TileCell cell = GetComponent<TileCell>();
        TileGroup group = cell.ParentGroup;
        TileGrid grid = GetComponentInParent<TileGrid>();

        // 進入済みフラグを立てる。再進入時に再度点滅しないようにする。
        _hasTriggered = true;
        StartCoroutine(BlinkAndDestroy(player, cell, group, grid));
    }

    // 点滅して破壊するコルーチン
    private IEnumerator BlinkAndDestroy(Player player, TileCell cell, TileGroup group, TileGrid grid)
    {
        Renderer tileRenderer = GetComponent<Renderer>();
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        Color originalColor = (tileRenderer != null) ? tileRenderer.sharedMaterial.GetColor(BaseColorId) : Color.white;
        float destroyAt = Time.time + Mathf.Max(0f, _destroyDelay);
        bool highlight = true;

        while (Time.time < destroyAt)
        {
            if (tileRenderer != null)
            {
                tileRenderer.GetPropertyBlock(properties);
                properties.SetColor(BaseColorId, highlight ? _blinkColor : originalColor);
                tileRenderer.SetPropertyBlock(properties);
            }

            float wait = Mathf.Min(Mathf.Max(0.01f, _blinkInterval), destroyAt - Time.time);
            yield return new WaitForSeconds(wait);
            highlight = !highlight;
        }

        // Destroyだけではグリッドの登録が残り、再配置できなくなる。
        if (grid != null && grid.RemoveTile(group))
            grid.PlayBreakEffect(group);
        if (player != null && player.IsTileReserved(group.GridPosition + cell.LocalPosition))
            player.Damage();

        Destroy(group.gameObject);
    }
}
