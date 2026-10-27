using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 設置したブロックの外周に沿って、光の帯を外側へ広げる。
/// 作成者 : 酒井康佑
/// </summary>
public class TileOutlineWave : MonoBehaviour
{
    [SerializeField, Tooltip("波紋用のMaterial")]
    private Material _waveMaterial;

    [SerializeField, Min(0.01f), Tooltip("広がって消えるまでの時間")]
    private float _duration = 0.45f;

    [SerializeField, Range(0.05f, 0.3f),
     Tooltip("外側へ広がる距離。1マスに対する割合")]
    private float _expandDistance = 0.25f;

    [SerializeField, Range(0.01f, 0.1f),
     Tooltip("光の線の太さ。1マスに対する割合")]
    private float _lineWidth = 0.035f;

    private static readonly int OpacityId =
        Shader.PropertyToID("_Opacity");

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.down
    };

    // 外周の1辺と、その両端が広がる方向。
    private struct Edge
    {
        public Vector2 start;
        public Vector2 end;
        public Vector2 startOffset;
        public Vector2 endOffset;
    }

    // 実行中に作ったオブジェクトとMeshを管理する。
    private class Wave
    {
        public GameObject root;
        public Mesh mesh;
    }

    private readonly List<Wave> _activeWaves = new();

    /// <summary>
    /// 指定したTileGroupの形に合わせて波紋を再生する。
    /// surfaceHeightはグリッド基準の高さ。
    /// </summary>
    public void Play(
        TileGroup group,
        TileGrid grid,
        float surfaceHeight)
    {
        if (!isActiveAndEnabled ||
            group == null ||
            grid == null ||
            group.Cells == null ||
            _waveMaterial == null)
        {
            return;
        }

        HashSet<Vector2Int> positions = new();

        // 今回設置したブロックが占めるマスを記録する。
        foreach (TileCell cell in group.Cells)
        {
            if (cell != null)
                positions.Add(group.GridPosition + cell.LocalPosition);
        }

        List<Edge> edges = new();
        float halfSize = grid.TileSize * 0.5f;

        foreach (Vector2Int position in positions)
        {
            Vector3 center3 = grid.transform.InverseTransformPoint(
                grid.GridToWorld(position)
            );

            Vector2 center = new Vector2(center3.x, center3.z);

            foreach (Vector2Int normal in Directions)
            {
                // 同じブロックのマスが隣にあれば、内部の辺なので省く。
                if (positions.Contains(position + normal))
                    continue;

                // 辺に沿って進む方向。
                Vector2Int tangent =
                    new Vector2Int(-normal.y, normal.x);

                edges.Add(new Edge
                {
                    start = center +
                        (Vector2)(normal - tangent) * halfSize,

                    end = center +
                        (Vector2)(normal + tangent) * halfSize,

                    startOffset = GetCornerOffset(
                        positions, position, normal, -tangent
                    ),

                    endOffset = GetCornerOffset(
                        positions, position, normal, tangent
                    )
                });
            }
        }

        if (edges.Count == 0)
            return;

        GameObject root = new GameObject(
            "TileOutlineWave",
            typeof(MeshFilter),
            typeof(MeshRenderer)
        );

        // グリッドの向きと大きさに合わせる。
        root.transform.SetParent(grid.transform, false);
        root.transform.localPosition =
            new Vector3(0f, surfaceHeight, 0f);

        Mesh mesh = new Mesh
        {
            name = "TileOutlineWaveMesh"
        };

        mesh.MarkDynamic();

        root.GetComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer meshRenderer = root.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = _waveMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        Wave wave = new Wave
        {
            root = root,
            mesh = mesh
        };

        _activeWaves.Add(wave);

        StartCoroutine(
            Animate(wave, meshRenderer, edges, grid.TileSize)
        );
    }

    /// <summary>
    /// 角を、直線・出っ張った角・くぼんだ角に分けて、
    /// 隣の辺とつながる移動方向を求める。
    /// </summary>
    private Vector2 GetCornerOffset(
        HashSet<Vector2Int> positions,
        Vector2Int cell,
        Vector2Int normal,
        Vector2Int side)
    {
        // 横にセルがなければ、外側へ出っ張る角。
        if (!positions.Contains(cell + side))
            return (Vector2)(normal + side);

        // 横と斜めにセルがあれば、L字などのくぼんだ角。
        if (positions.Contains(cell + side + normal))
            return (Vector2)(normal - side);

        // 直線の途中なら、辺の外向きにだけ移動する。
        return (Vector2)normal;
    }

    private IEnumerator Animate(
        Wave wave,
        MeshRenderer meshRenderer,
        List<Edge> edges,
        float tileSize)
    {
        // 1辺を4頂点、2枚の三角形で描く。
        Vector3[] vertices = new Vector3[edges.Count * 4];
        int[] triangles = new int[edges.Count * 6];

        for (int i = 0; i < edges.Count; i++)
        {
            int v = i * 4;
            int t = i * 6;

            triangles[t] = v;
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v + 2;

            triangles[t + 3] = v;
            triangles[t + 4] = v + 2;
            triangles[t + 5] = v + 3;
        }

        wave.mesh.vertices = vertices;
        wave.mesh.triangles = triangles;

        MaterialPropertyBlock properties = new();

        float duration = Mathf.Max(0.01f, _duration);
        float width = Mathf.Clamp(_lineWidth, 0.01f, 0.1f) * tileSize;
        float endDistance =
            Mathf.Clamp(_expandDistance, 0.05f, 0.3f) * tileSize;

        float elapsed = 0f;

        while (elapsed < duration && wave.root != null)
        {
            float t = Mathf.Clamp01(elapsed / duration);

            // 最初は速く、最後はゆっくり広がる。
            float movement = 1f - Mathf.Pow(1f - t, 3f);

            float innerDistance = Mathf.Lerp(
                0.005f * tileSize,
                endDistance,
                movement
            );

            float outerDistance = innerDistance + width;

            for (int i = 0; i < edges.Count; i++)
            {
                Edge edge = edges[i];
                int v = i * 4;

                vertices[v] = ToXZ(
                    edge.start + edge.startOffset * innerDistance
                );

                vertices[v + 1] = ToXZ(
                    edge.end + edge.endOffset * innerDistance
                );

                vertices[v + 2] = ToXZ(
                    edge.end + edge.endOffset * outerDistance
                );

                vertices[v + 3] = ToXZ(
                    edge.start + edge.startOffset * outerDistance
                );
            }

            wave.mesh.vertices = vertices;
            wave.mesh.RecalculateBounds();

            // 広がるにつれて透明にする。
            properties.SetFloat(OpacityId, 1f - t);
            meshRenderer.SetPropertyBlock(properties);

            yield return null;
            elapsed += Time.deltaTime;
        }

        _activeWaves.Remove(wave);
        ReleaseWave(wave);
    }

    // 2Dの座標を、床と平行なXZ平面へ変換する。
    private Vector3 ToXZ(Vector2 point)
    {
        return new Vector3(point.x, 0f, point.y);
    }

    // 自動生成したMeshも忘れずに削除する。
    private void ReleaseWave(Wave wave)
    {
        if (wave.root != null)
            Destroy(wave.root);

        if (wave.mesh != null)
            Destroy(wave.mesh);
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        foreach (Wave wave in _activeWaves)
            ReleaseWave(wave);

        _activeWaves.Clear();
    }
}
