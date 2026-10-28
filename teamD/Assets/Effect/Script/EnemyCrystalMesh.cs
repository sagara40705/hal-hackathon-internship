using UnityEngine;

// 編集中も形を生成する。
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemyCrystalMesh : MonoBehaviour
{
    [SerializeField, Min(0.01f), Tooltip("中央部分の太さ（半径）")]
    private float _radius = 0.18f;

    [SerializeField, Min(0.01f), Tooltip("中心から前の先端までの長さ")]
    private float _frontLength = 0.6f;

    [SerializeField, Min(0.01f), Tooltip("中心から後ろの先端までの長さ")]
    private float _backLength = 0.3f;

    private Mesh _generatedMesh;

    private void OnEnable()
    {
        BuildMesh();
    }

    // 数値を変更したら、このメニューから形を更新する。
    [ContextMenu("形を更新")]
    private void BuildMesh()
    {
        ReleaseMesh();

        Vector3 front = new Vector3(0f, 0f, _frontLength);
        Vector3 back = new Vector3(0f, 0f, -_backLength);

        // コアの中央を囲む4つの頂点。
        Vector3[] corners =
        {
            new Vector3(_radius, 0f, 0f),
            new Vector3(0f, _radius, 0f),
            new Vector3(-_radius, 0f, 0f),
            new Vector3(0f, -_radius, 0f)
        };

        // 三角形8枚。面ごとに頂点を分けて角をはっきり見せる。
        Vector3[] vertices = new Vector3[24];
        int[] triangles = new int[24];

        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;
            int index = i * 6;

            // 前側の三角形。
            vertices[index] = front;
            vertices[index + 1] = corners[i];
            vertices[index + 2] = corners[next];

            // 後ろ側の三角形。
            vertices[index + 3] = back;
            vertices[index + 4] = corners[next];
            vertices[index + 5] = corners[i];
        }

        for (int i = 0; i < triangles.Length; i++)
            triangles[i] = i;

        _generatedMesh = new Mesh
        {
            name = "EnemyCrystal_Runtime",
            hideFlags = HideFlags.DontSave
        };

        _generatedMesh.vertices = vertices;
        _generatedMesh.triangles = triangles;
        _generatedMesh.RecalculateNormals();
        _generatedMesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = _generatedMesh;
    }

    private void OnDestroy()
    {
        ReleaseMesh();
    }

    // このスクリプトで生成したMeshだけを片付ける。
    private void ReleaseMesh()
    {
        if (_generatedMesh == null)
            return;

        if (Application.isPlaying)
            Destroy(_generatedMesh);
        else
            DestroyImmediate(_generatedMesh);

        _generatedMesh = null;
    }
}
