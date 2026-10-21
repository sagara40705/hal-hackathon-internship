using UnityEngine;


/// <summary>
/// TileGroupを構成する各マスに、設置エフェクトを再生する
/// 作成者 : 酒井康佑
/// </summary>
public class TilePlacementEffect : MonoBehaviour
{
    [SerializeField, Tooltip("１マス分の設置エフェクトPrefab")]
    private ParticleSystem cellEffectPrefab;

    [SerializeField, Min(0f), Tooltip("グリッド面から浮かせる量")]
    private float surfaceOffset = 0.02f;

    /// <summary>
    /// TileGroupの形に添って、各セルの位置にエフェクトを生成する
    /// </summary>>
    public void Play(TileGroup group, TileGrid grid)
    {
        if(group == null || grid == null || cellEffectPrefab == null)
        {
            Debug.LogWarning("設置エフェクトの参照が設定されていません。", this);
            return;
        }

        if (group.Cells == null)
            return;

        foreach(TileCell cell in group.Cells)
        {
            if (cell == null)
                continue;

            // グループの基準座標に、セルのローカル座標を足して実際のマスを求める
            Vector2Int cellGridPosition = group.GridPosition + cell.LocalPosition;

            // マス中心のワールド座標を求め、グリッド面から少しだけ浮かせる
            Vector3 effectPosition =
                grid.GridToWorld(cellGridPosition) + grid.transform.up * surfaceOffset;

            // 円形の粒子をグリッド面に沿わせる
            Quaternion effectRotation =
                grid.transform.rotation * Quaternion.Euler(90f, 0f, 0f);

            // 1マス分のエフェクトを生成する
            Instantiate(cellEffectPrefab, effectPosition, effectRotation);

        }
    }

}
