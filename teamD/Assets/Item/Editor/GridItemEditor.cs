using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridItem))]
public sealed class GridItemEditor : Editor
{
    private SerializedProperty _position;
    private SerializedProperty _height;

    private void OnEnable()
    {
        _position = serializedObject.FindProperty("_gridPosition");
        _height = serializedObject.FindProperty("_heightOffset");
    }

    // Inspector GUI
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        GridItem item = (GridItem)target;
        GridItemMap map = item.GetComponentInParent<GridItemMap>();
        TileGrid grid = map != null ? map.Grid : null;

        using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("配置先 Grid", grid, typeof(TileGrid), true);

        EditorGUI.BeginChangeCheck();
        using (new EditorGUI.DisabledScope(Application.isPlaying)) EditorGUILayout.PropertyField(_position, new GUIContent("Grid Position（セル番号）"));

        EditorGUILayout.PropertyField(_height, new GUIContent("Height Offset（上面からの高さ）"));
        bool placementChanged = EditorGUI.EndChangeCheck();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("_itemType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_amount"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_onCollected"));
        serializedObject.ApplyModifiedProperties();

        // Inspectorの操作時だけ位置を更新する。選択や再描画ではシーンを変更しない。
        if (placementChanged && grid != null && grid.IsInPlacementArea(item.GridPosition))
        {
            SnapToCell(item);
        }
           

        if (grid != null)
        {
            Vector2Int currentCell = grid.WorldToGrid(item.transform.position);
            EditorGUILayout.LabelField("現在の見た目のセル", currentCell.ToString());
            EditorGUILayout.LabelField("セル番号の範囲",
                $"X: 0 ～ {grid.GridSize.x - 1}, Y: 0 ～ {grid.GridSize.y - 1}");

            using (new EditorGUI.DisabledScope(Application.isPlaying || !grid.IsInPlacementArea(currentCell) || EditorUtility.IsPersistent(item)))
            {
                if (GUILayout.Button("現在の位置からセルを設定"))
                {
                    SetCellFromCurrentPosition(item);
                    serializedObject.Update();
                }
            }

            using (new EditorGUI.DisabledScope(Application.isPlaying || !grid.IsInPlacementArea(item.GridPosition) || EditorUtility.IsPersistent(item)))
            {
                if (GUILayout.Button("設定したセルへ位置を合わせる"))
                {
                    SnapToCell(item);
                }

            }

            if (!grid.IsInPlacementArea(currentCell))
            {
                EditorGUILayout.HelpBox("現在の見た目の位置は配置可能範囲外です。Sceneで配置可能なセルへ移動してください。", MessageType.Warning);
            }
            else if (currentCell != item.GridPosition)
            {
                EditorGUILayout.HelpBox("現在の見た目の位置と設定したセル番号が異なります。Sceneで移動すると自動更新されます。", MessageType.Warning);
            }
        }

        DrawSetupStatus(item, map, grid);
    }

    // Scene GUI
    internal static bool SetCellFromCurrentPosition(GridItem item)
    {
        GridItemMap map = item.GetComponentInParent<GridItemMap>();
        TileGrid grid = map != null ? map.Grid : null;
        if (Application.isPlaying || EditorUtility.IsPersistent(item) || grid == null)
        {
            return false;
        }

        Vector2Int position = grid.WorldToGrid(item.transform.position);
        if (!grid.IsInPlacementArea(position) || IsOccupied(map, item, position))
        {
            // セル番号を変更せず、最後に確定したセルへ戻す。
            SnapToCell(item);
            return false;
        }

        Undo.SetCurrentGroupName("アイテムのセルを設定");
        var serializedItem = new SerializedObject(item);
        serializedItem.FindProperty("_gridPosition").vector2IntValue = position;
        serializedItem.ApplyModifiedProperties();
        SnapToCell(item);
        return true;
    }

    // 指定セルに同じMapの有効な未取得アイテムがあるか判定する。
    private static bool IsOccupied(GridItemMap map, GridItem item, Vector2Int position)
    {
        // 実行時の登録条件に合わせ、同じMapの有効な未取得アイテムを判定する。
        foreach (GridItem other in map.GetComponentsInChildren<GridItem>())
        {
            if (other != item && other.isActiveAndEnabled && !other.IsCollected &&
                other.GetComponentInParent<GridItemMap>() == map && other.GridPosition == position)
            {
                return true;
            }
               
        }
        return false;
    }

    // セル番号に合わせてワールド座標を更新する。Undo対応。
    private static void SnapToCell(GridItem item)
    {
        Undo.RecordObject(item.transform, "アイテムの配置を更新");
        item.ApplyGridPosition();
        PrefabUtility.RecordPrefabInstancePropertyModifications(item.transform);
        SceneView.RepaintAll();
    }

    // GridItemの配置状況をInspectorに表示する。エラーや警告があれば表示する。
    private static void DrawSetupStatus(GridItem item, GridItemMap map, TileGrid grid)
    {
        if (map == null)
        {
            EditorGUILayout.HelpBox("GridItemMapのある GridItems の子に配置してください。", MessageType.Error);
            return;
        }
        if (item.GetComponentInParent<TileGroup>() != null)
            EditorGUILayout.HelpBox("TileGroupの子には配置できません。GridItemsをTileGroupの外へ移動してください。", MessageType.Error);
        if (grid == null)
        {
            EditorGUILayout.HelpBox("親の GridItemMap に Tile Grid を指定してください。", MessageType.Error);
            return;
        }

        using (var serializedMap = new SerializedObject(map))
        {
            Player player = serializedMap.FindProperty("_player").objectReferenceValue as Player;
            if (player == null || player.Grid != grid)
            {
                EditorGUILayout.HelpBox("親の GridItemMap に、同じGridを使うPlayerを指定してください。", MessageType.Error);
            }
            else if (player.GetComponent<PlayerInventory>() == null)
            {
                EditorGUILayout.HelpBox("Playerと同じオブジェクトに PlayerInventory が必要です。", MessageType.Error);
            }
        }
        if (!map.isActiveAndEnabled)
        {
            EditorGUILayout.HelpBox("親の GridItemMap が無効です。", MessageType.Warning);
        }
        if (!grid.IsInPlacementArea(item.GridPosition))
        {
            EditorGUILayout.HelpBox($"セル {item.GridPosition} は配置可能範囲外のため、取得できません。", MessageType.Error);
        }
         
        foreach (GridItem other in map.GetComponentsInChildren<GridItem>())
        {
            if (other != item && other.enabled && !other.IsCollected &&
                other.GetComponentInParent<GridItemMap>() == map && other.GridPosition == item.GridPosition)
            {
                EditorGUILayout.HelpBox($"セル {item.GridPosition} に {other.name} もあります。1セルに1個だけ配置してください。", MessageType.Error);
                break;
            }
        }
    }
}
