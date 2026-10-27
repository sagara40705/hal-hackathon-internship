
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 配置可能範囲の外周エフェクト
/// 作成者：石田怜
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlacementBoundaryEffect : MonoBehaviour
{
    [Header("輪郭")]
    [SerializeField, Tooltip("輪郭のマテリアル")]
    private Material _material;

    [SerializeField, Tooltip("輪郭の幅")]
    [Min(0.001f)]
    private float _lineWidth = 0.06f;

    [SerializeField, Tooltip("輪郭の高さ（ローカル座標）")]
    private float _lineHeight = 0.03f;

    [Header("光の壁")]
    [SerializeField, Tooltip("高さ方向の光を表示する")]
    private bool _enableHeightEffect = true;

    [SerializeField, Tooltip("高さ方向の光のマテリアル")]
    private Material _lightWallMaterial;

    [SerializeField, Tooltip("高さ方向の光の高さ")]
    [Min(0.001f)]
    private float _lightWallHeight = 1.0f;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;

    private struct Edge
    {
        public Vector2Int Start;
        public Vector2Int End;

        public Edge(Vector2Int start, Vector2Int end)
        {
            Start = start;
            End = end;
        }
    }

    // 配置可能範囲から輪郭を生成
    public void Rebuild(IEnumerable<Vector2Int> positions, Vector3 offset, float tileSize)
    {
        InitializeRenderer();

        if (tileSize <= 0.0f)
            return;

        HashSet<Vector2Int> cells = new(positions);
        List<Edge> edges = new();

        // 配置可能範囲の外周を抽出
        foreach (Vector2Int pos in cells)
        {
            Vector2Int a = pos * 2 + new Vector2Int(-1, -1);
            Vector2Int b = pos * 2 + new Vector2Int(-1, 1);
            Vector2Int c = pos * 2 + new Vector2Int(1, 1);
            Vector2Int d = pos * 2 + new Vector2Int(1, -1);

            if (!cells.Contains(pos + Vector2Int.left))
                edges.Add(new Edge(a, b));

            if (!cells.Contains(pos + Vector2Int.up))
                edges.Add(new Edge(b, c));

            if (!cells.Contains(pos + Vector2Int.right))
                edges.Add(new Edge(c, d));

            if (!cells.Contains(pos + Vector2Int.down))
                edges.Add(new Edge(d, a));
        }

        // 接続点から辺を検索できるようにする
        Dictionary<Vector2Int, List<int>> edgeMap = new();

        for (int i = 0; i < edges.Count; i++)
        {
            Vector2Int start = edges[i].Start;

            if (!edgeMap.TryGetValue(start, out List<int> indices))
            {
                indices = new List<int>();
                edgeMap.Add(start, indices);
            }

            indices.Add(i);
        }

        List<Vector3> vertices = new();
        List<Vector2> uvs = new();

        List<int> lineTriangles = new();
        List<int> wallTriangles = new();

        bool[] visited = new bool[edges.Count];

        bool generateWall = _enableHeightEffect && _lightWallMaterial != null;

        // 外周を閉じたループごとに生成
        for (int i = 0; i < edges.Count; i++)
        {
            if (visited[i])
                continue;

            List<Edge> loop = new();
            int current = i;

            while (current >= 0 && !visited[current])
            {
                visited[current] = true;

                Edge edge = edges[current];
                loop.Add(edge);

                if (edge.End == loop[0].Start)
                    break;

                current = FindNextEdge(
                    edge,
                    edges,
                    edgeMap,
                    visited
                );
            }

            // 閉じていない輪郭は描画しない
            if (loop.Count == 0 || loop[^1].End != loop[0].Start)
                continue;

            // ループの全長を計算
            float perimeter = 0.0f;

            foreach (Edge edge in loop)
            {
                Vector3 start = ToLocalPosition(edge.Start, offset, tileSize);
                Vector3 end = ToLocalPosition(edge.End, offset, tileSize);

                perimeter += Vector3.Distance(start, end);
            }

            if (perimeter <= 0.0f)
                continue;

            float distance = 0.0f;

            foreach (Edge edge in loop)
            {
                Vector3 start = ToLocalPosition(edge.Start, offset, tileSize);
                Vector3 end = ToLocalPosition(edge.End, offset, tileSize);

                float length = Vector3.Distance(start, end);

                // 輪郭全体を0～1に正規化
                float uvStart = distance / perimeter;
                float uvEnd = (distance + length) / perimeter;

                // 地面の輪郭を追加
                AddEdge(
                    start,
                    end,
                    uvStart,
                    uvEnd,
                    _lineWidth,
                    vertices,
                    uvs,
                    lineTriangles
                );

                // 高さ方向の光を追加
                if (generateWall)
                {
                    AddLightWall(
                        start,
                        end,
                        uvStart,
                        uvEnd,
                        _lightWallHeight,
                        vertices,
                        uvs,
                        wallTriangles
                    );
                }

                distance += length;
            }
        }

        // 以前のメッシュを破棄
        if (_mesh != null)
        {
            _meshFilter.sharedMesh = null;

            if (Application.isPlaying)
                Destroy(_mesh);
            else
                DestroyImmediate(_mesh);
        }

        // 新しいメッシュを生成
        _mesh = new Mesh
        {
            name = "Placement Boundary"
        };

        _mesh.indexFormat = IndexFormat.UInt32;
        _mesh.SetVertices(vertices);
        _mesh.SetUVs(0, uvs);

        // 輪郭と高さ方向の光を別SubMeshにする
        _mesh.subMeshCount = generateWall ? 2 : 1;
        _mesh.SetTriangles(lineTriangles, 0);

        if (generateWall)
            _mesh.SetTriangles(wallTriangles, 1);

        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _meshFilter.sharedMesh = _mesh;

        // マテリアルを割り当てるだけでパラメータは変更しない
        if (generateWall)
        {
            _meshRenderer.sharedMaterials = new Material[]
            {
                _material,
                _lightWallMaterial
            };
        }
        else
        {
            _meshRenderer.sharedMaterials = new Material[]
            {
                _material
            };
        }
    }

    // 接続先の辺を取得
    private int FindNextEdge(
        Edge current,
        List<Edge> edges,
        Dictionary<Vector2Int, List<int>> edgeMap,
        bool[] visited)
    {
        if (!edgeMap.TryGetValue(current.End, out List<int> candidates))
            return -1;

        Vector2Int direction = current.End - current.Start;

        int bestIndex = -1;
        int bestPriority = int.MaxValue;

        foreach (int index in candidates)
        {
            if (visited[index])
                continue;

            Vector2Int nextDirection =
                edges[index].End - edges[index].Start;

            // 外積を計算
            int cross = direction.x * nextDirection.y
                - direction.y * nextDirection.x;

            // 内積を計算
            int dot = direction.x * nextDirection.x
                + direction.y * nextDirection.y;

            // 右折、直進、左折の順に優先
            int priority = cross < 0 ? 0 :
                dot > 0 ? 1 :
                cross > 0 ? 2 : 3;

            if (priority < bestPriority)
            {
                bestPriority = priority;
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    // グリッド頂点からローカル座標に変換
    private Vector3 ToLocalPosition(
        Vector2Int point,
        Vector3 offset,
        float tileSize)
    {
        return new Vector3(
            point.x * tileSize * 0.5f,
            _lineHeight,
            point.y * tileSize * 0.5f
        ) + offset;
    }

    // 地面の輪郭を生成
    private void AddEdge(
        Vector3 start,
        Vector3 end,
        float uvStart,
        float uvEnd,
        float width,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<int> triangles)
    {
        Vector3 direction = (end - start).normalized;

        // 線分に対して垂直な方向
        Vector3 side = Vector3.Cross(Vector3.up, direction);
        side *= width * 0.5f;

        // 角の隙間を防ぐため端点を延長
        Vector3 extension = direction * (width * 0.5f);

        Vector3 a = start - extension - side;
        Vector3 b = start - extension + side;
        Vector3 c = end + extension + side;
        Vector3 d = end + extension - side;

        int index = vertices.Count;

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);

        uvs.Add(new Vector2(uvStart, 0.0f));
        uvs.Add(new Vector2(uvStart, 1.0f));
        uvs.Add(new Vector2(uvEnd, 1.0f));
        uvs.Add(new Vector2(uvEnd, 0.0f));

        triangles.Add(index + 0);
        triangles.Add(index + 2);
        triangles.Add(index + 1);

        triangles.Add(index + 0);
        triangles.Add(index + 3);
        triangles.Add(index + 2);
    }

    // 上方向に伸びる光の面を生成
    private void AddLightWall(
        Vector3 start,
        Vector3 end,
        float uvStart,
        float uvEnd,
        float height,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<int> triangles)
    {
        Vector3 heightOffset = Vector3.up * height;

        Vector3 a = start;
        Vector3 b = start + heightOffset;
        Vector3 c = end + heightOffset;
        Vector3 d = end;

        int index = vertices.Count;

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);

        // UV.xは輪郭位置、UV.yは高さ
        uvs.Add(new Vector2(uvStart, 0.0f));
        uvs.Add(new Vector2(uvStart, 1.0f));
        uvs.Add(new Vector2(uvEnd, 1.0f));
        uvs.Add(new Vector2(uvEnd, 0.0f));

        triangles.Add(index + 0);
        triangles.Add(index + 1);
        triangles.Add(index + 2);

        triangles.Add(index + 0);
        triangles.Add(index + 2);
        triangles.Add(index + 3);
    }

    // 描画用オブジェクトを初期化
    private void InitializeRenderer()
    {
        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();

        _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _meshRenderer.receiveShadows = false;
    }

    private void OnDestroy()
    {
        if (_mesh == null)
            return;

        if (Application.isPlaying)
            Destroy(_mesh);
        else
            DestroyImmediate(_mesh);
    }
}
