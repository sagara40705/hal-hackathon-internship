
using UnityEngine;
using UnityEditor;

/// <summary>
/// TileGridの配置可能範囲を編集するカスタムエディタ
/// </summary>
[CustomEditor(typeof(TileGrid))]
public class TileGridEditor : Editor
{
    private TileGrid _tileGrid;
    private bool _isEditMode = false;
    private bool _paintAllowed = true;
    private bool _isPainting = false;

    private int _controlID;
    private int _undoGroup;
    private Vector2Int _lastPaintPosition;


    private void OnEnable()
    {
        _tileGrid = (TileGrid)target;
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        EndPaint();
        _isEditMode = false;
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("配置可能範囲エディタ", EditorStyles.boldLabel);

        // 編集モードの切り替え
        if (GUILayout.Button(_isEditMode ? "編集を終了" : "配置範囲を編集"))
        {
            if (_isEditMode)
                EndPaint();

            _isEditMode = !_isEditMode;
            SceneView.RepaintAll();
        }

        if (!_isEditMode)
            return;

        EditorGUILayout.HelpBox(
            "左クリックで配置可否を切り替えます。\nドラッグで連続して編集できます。",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("すべて配置可能"))
        {
            Undo.RecordObject(_tileGrid, "Enable All Placement Cells");
            _tileGrid.SetAllPlacementAllowed(true);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("すべて配置禁止"))
        {
            Undo.RecordObject(_tileGrid, "Disable All Placement Cells");
            _tileGrid.SetAllPlacementAllowed(false);
            SceneView.RepaintAll();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void OnSceneGUI()
    {
        if (!_isEditMode || _tileGrid == null)
            return;

        Event currentEvent = Event.current;
        _controlID = GUIUtility.GetControlID(FocusType.Passive);

        // 編集中はSceneビューの通常選択を無効化
        if (currentEvent.type == EventType.Layout)
            HandleUtility.AddDefaultControl(_controlID);

        // ドラッグ開始
        if (currentEvent.type == EventType.MouseDown &&
            currentEvent.button == 0 &&
            !currentEvent.alt &&
            HandleUtility.nearestControl == _controlID)
        {
            if (!TryGetGridPosition(currentEvent.mousePosition, out Vector2Int gridPosition))
                return;

            GUIUtility.hotControl = _controlID;
            _isPainting = true;
            _paintAllowed = !_tileGrid.IsInPlacementArea(gridPosition);
            _lastPaintPosition = new Vector2Int(int.MinValue, int.MinValue);

            Undo.IncrementCurrentGroup();
            _undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Edit Placement Area");

            PaintCell(gridPosition);
            currentEvent.Use();
        }

        // ドラッグ中
        if (currentEvent.type == EventType.MouseDrag &&
            _isPainting &&
            GUIUtility.hotControl == _controlID)
        {
            if (TryGetGridPosition(currentEvent.mousePosition, out Vector2Int gridPosition))
                PaintCell(gridPosition);

            currentEvent.Use();
        }

        // ドラッグ終了
        if (currentEvent.type == EventType.MouseUp &&
            currentEvent.button == 0 &&
            _isPainting)
        {
            EndPaint();
            currentEvent.Use();
        }
    }

    // 指定マスの配置可能状態を変更
    private void PaintCell(Vector2Int gridPosition)
    {
        if (gridPosition == _lastPaintPosition)
            return;

        _lastPaintPosition = gridPosition;

        if (_tileGrid.IsInPlacementArea(gridPosition) == _paintAllowed)
            return;

        Undo.RecordObject(_tileGrid, "Edit Placement Area");
        _tileGrid.SetPlacementAllowed(gridPosition, _paintAllowed);

        SceneView.RepaintAll();
    }

    // ドラッグ操作を終了
    private void EndPaint()
    {
        if (!_isPainting)
            return;

        _isPainting = false;
        GUIUtility.hotControl = 0;
        Undo.CollapseUndoOperations(_undoGroup);
    }

    // Undo・Redo時の処理
    private void OnUndoRedo()
    {
        if (_tileGrid == null)
            return;

        _tileGrid.RefreshPlacementArea();
        SceneView.RepaintAll();
    }

    // マウス位置からグリッド座標を取得
    private bool TryGetGridPosition(Vector2 mousePosition, out Vector2Int gridPosition)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        Vector3 planePosition = _tileGrid.transform.position +
            _tileGrid.transform.up * (_tileGrid.TileHeight * 0.5f);

        Plane plane = new Plane(_tileGrid.transform.up, planePosition);

        if (!plane.Raycast(ray, out float distance))
        {
            gridPosition = default;
            return false;
        }

        Vector3 worldPosition = ray.GetPoint(distance);
        gridPosition = _tileGrid.WorldToGrid(worldPosition);

        return _tileGrid.IsInGrid(gridPosition);
    }
}
