using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 通常の移動ツールの操作終了後に確定する。ドラッグ中の連続スナップで移動を妨げない。
[InitializeOnLoad]
internal static class GridItemAutoSnap
{
    private static readonly Dictionary<GridItem, int> Pending = new();
    private static bool _applying;

    static GridItemAutoSnap()
    {
        Undo.postprocessModifications += OnModified;
        Undo.undoRedoPerformed += Pending.Clear;
        EditorApplication.update += ApplyPending;
    }

    private static UndoPropertyModification[] OnModified(UndoPropertyModification[] modifications)
    {
        if (_applying || EditorApplication.isPlayingOrWillChangePlaymode)
            return modifications;

        foreach (var modification in modifications)
        {
            var value = modification.currentValue;
            if (value.target is Transform transform &&
                value.propertyPath.StartsWith("m_LocalPosition.") &&
                transform.TryGetComponent<GridItem>(out var item) &&
                !EditorUtility.IsPersistent(item))
            {
                Pending[item] = Undo.GetCurrentGroup();
            }
        }
        return modifications;
    }

    private static void ApplyPending()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Pending.Clear();
            return;
        }
        if (Pending.Count == 0 || GUIUtility.hotControl != 0)
            return;

        var pending = new Dictionary<GridItem, int>(Pending);
        Pending.Clear();
        _applying = true;
        try
        {
            foreach (var entry in pending)
            {
                if (entry.Key == null)
                    continue;
                // ユーザーの移動とセル更新を1回のUndoで戻す。
                bool sameGroup = Undo.GetCurrentGroup() == entry.Value;
                GridItemEditor.SetCellFromCurrentPosition(entry.Key);
                Undo.FlushUndoRecordObjects();
                if (sameGroup)
                    Undo.CollapseUndoOperations(entry.Value);
            }
        }
        finally
        {
            _applying = false;
        }
    }
}
