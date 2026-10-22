using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 設置したしたGroupの外周に光の板を並べ徐々に消す
/// 作成者 : 酒井康佑
/// </summary>
public class TilePlacementEffect : MonoBehaviour
{
    [SerializeField, Tooltip("幅1・高さ1、原点が下辺中央の光のPrefab")]
    private GameObject _edgeEffectPrefab;

    [SerializeField, Min(0.01f), Tooltip("光の板の高さ")]
    private float _effectHeight = 0.2f;

    [SerializeField, Tooltip("グリッド面から光の下辺までの高さ")]
    private float _surfaceHeight = 0.52f;

    [SerializeField, Min(0.01f), Tooltip("光が消えるまでの時間")]
    private float _duration = 0.4f;

    // Shader GraphのOpacityプロパティを指定する
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");


    // セルの右・左・屋・手前を調べるための座標。
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.down
    };

    /// <summary>
    /// 指定したブロックの外周にエフェクトを生成する。
    /// </summary>>
    public void Play(TileGroup group, TileGrid grid)
    {
        if (group == null || grid == null || group.Cells == null || _edgeEffectPrefab == null)
            return;

        // 今回設置したブロックが占めるマスを記録する
        HashSet<Vector2Int> positions = new(); 

        foreach(TileCell cell in group.Cells)
        {
            if (cell == null)
                continue;

            positions.Add(group.GridPosition + cell.LocalPosition);

        }

        if (positions.Count == 0)
            return;

        // エフェクトをまとめる親を作る
        // グリッドの子にすることで、グリッドの回転やスケールに合わせる。
        GameObject effectRoot = new GameObject("TilePlacementOutline");

        effectRoot.transform.SetParent(grid.transform, false);

        List<Renderer> renderers = new();

        foreach (Vector2Int position in positions)
        {
            // セルを中心をグリッドのローカル座標に変換する
            Vector3 center = grid.transform.InverseTransformPoint(grid.GridToWorld(position));

            foreach (Vector2Int direction in Directions)
            {
                // 同じブロックのマスが隣にあれば内部の変なので省く。
                if (positions.Contains(position + direction))
                    continue;

                // セル中心の半マス移動し、外周の偏の中心を求める
                Vector3 edgePosition = center + new Vector3(
                    direction.x * grid.TileSize * 0.5f,
                    _surfaceHeight,
                    direction.y * grid.TileSize * 0.5f
                );

                GameObject edge = Instantiate(
                    _edgeEffectPrefab,
                    effectRoot.transform,
                    false
                );

                edge.transform.localPosition = edgePosition;

                // 左右の辺は板を90度回転して並べる。
                float angle = direction.x != 0 ? 90f : 0f;
                edge.transform.localRotation = Quaternion.Euler(0f, angle, 0f);

                // 元の板の幅・高さが1であることを前提にサイズを設定する
                edge.transform.localScale = new Vector3(
                    grid.TileSize,
                    _effectHeight,
                    1f
                );

                renderers.AddRange(edge.GetComponentsInChildren<Renderer>());

            }
            
        }

        // 途中でこのコンポーネントが無効になっても、
        // 生成したエフェクトが残らないようにする。
        Destroy(effectRoot, _duration + 0.1f);
        StartCoroutine(FadeOut(effectRoot, renderers));
    }


    /// <summary>
    /// Materialを複製せずに、このエフェクトの透明度だけを変更する。
    /// </summary>>
    private IEnumerator FadeOut(GameObject effectRoot, List<Renderer> renderers)
    {
        MaterialPropertyBlock propertyBlock = new();
        float elapsed = 0f;

        while (elapsed < _duration && effectRoot != null)
        {
            // 生成直後は明るく、時間経過とともに透明にする
            float opacity = 1f - elapsed / _duration;
            propertyBlock.SetFloat(OpacityId, opacity);

            foreach(Renderer target in renderers)
            {
                if (target != null)
                    target.SetPropertyBlock(propertyBlock);
            }

            yield return null;
            elapsed += Time.deltaTime;
        }

        if (effectRoot != null)
            Destroy(effectRoot);
    }

}
