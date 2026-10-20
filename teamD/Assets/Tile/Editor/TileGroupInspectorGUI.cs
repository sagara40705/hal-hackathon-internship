using UnityEngine;
using UnityEditor;

/// <summary>
/// TileGroupのカスタムInspector
/// </summary>
[CustomEditor(typeof(TileGroup))]
public class TileGroupInspectorGUI : Editor
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
}