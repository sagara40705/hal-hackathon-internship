using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// TileGroupのカスタムGUI
/// 作成者：石田怜
/// </summary>
[CustomEditor(typeof(TileGroup))]
public class TileGroupEditor : Editor
{
	private Material _material;

    public override void OnInspectorGUI()
	{
		// 通常のInspectorを表示
		DrawDefaultInspector();

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("マテリアル一括設定", EditorStyles.boldLabel);

		// 適用するマテリアルを選択
		_material = (Material)EditorGUILayout.ObjectField(
			"Material",
			_material,
			typeof(Material),
			false
		);

		// マテリアルの一括適用
		using (new EditorGUI.DisabledScope(_material == null))
		{
			if (GUILayout.Button("セルにマテリアルを適用"))
			{
				ApplyMaterial();
			}
		}
    }

    private void OnSceneGUI()
    {
        // 中心点を描画
        DrawCenter();
    }

    // 子オブジェクトのマテリアルを一括変更
    private void ApplyMaterial()
	{
		TileGroup group = (TileGroup)target;

        foreach (Transform child in group.transform)
		{
			// 直下の子オブジェクトのRendererのみ取得
			Renderer renderer = child.GetComponent<Renderer>();

			if (renderer == null)
				continue;

			Undo.RecordObject(renderer, "Apply Tile Group Material");

			// すべてのマテリアルスロットを変更
			Material[] materials = renderer.sharedMaterials;

			for (int i = 0; i < materials.Length; i++)
			{
				materials[i] = _material;
			}

			renderer.sharedMaterials = materials;

			// Prefabインスタンスの変更を記録
			PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
			EditorUtility.SetDirty(renderer);
		}
	}

    private void DrawCenter()
    {
        TileGroup group = (TileGroup)target;

        // 中心座標を取得
        Vector3 center = group.transform.position + group.PreviewOffset;

        // 表示サイズを計算
        float size = HandleUtility.GetHandleSize(center) * 0.1f;

        // 描画設定を保存
        Color previousColor = Handles.color;
        CompareFunction previousZTest = Handles.zTest;

        // 遮蔽されている部分を半透明で描画
        Handles.zTest = CompareFunction.Greater;
        Handles.color = new Color(1.0f, 1.0f, 0.0f, 0.25f);

        Handles.SphereHandleCap(
            0,
            center,
            Quaternion.identity,
            size,
            EventType.Repaint
        );

        // 手前に見えている部分を不透明で描画
        Handles.zTest = CompareFunction.LessEqual;
        Handles.color = Color.yellow;

        Handles.SphereHandleCap(
            0,
            center,
            Quaternion.identity,
            size,
            EventType.Repaint
        );

        // 描画設定を復元
        Handles.color = previousColor;
        Handles.zTest = previousZTest;
    }
}
