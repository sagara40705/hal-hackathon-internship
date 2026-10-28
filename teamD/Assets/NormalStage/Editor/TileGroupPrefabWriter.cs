using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NormalStage.EditorTools
{
    public static class TileGroupPrefabWriter
    {
        public const string WorkFolder = "Assets/NormalStage";

        public static bool IsCellPrefab(GameObject prefab)
        {
            if (prefab == null || !EditorUtility.IsPersistent(prefab) ||
                AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(prefab)) != prefab ||
                !PrefabUtility.IsPartOfPrefabAsset(prefab) || !prefab.activeSelf)
                return false;
            var cell = prefab.GetComponent<TileCell>();
            return cell != null && cell.enabled &&
                   prefab.GetComponentsInChildren<TileCell>(true).Length == 1 &&
                   prefab.GetComponentsInChildren<TileGroup>(true).Length == 0;
        }

        public static string Validate(TileGroupLayoutData data)
        {
            if (data == null || data.cells == null || data.cells.Count == 0)
                return "セルを1つ以上配置してください。";
            if (float.IsNaN(data.tileSize) || float.IsInfinity(data.tileSize) || data.tileSize < 0.01f)
                return "1マスのサイズは0.01以上にしてください。";
            string name = data.prefabName;
            if (string.IsNullOrWhiteSpace(name) || name != name.Trim() || name.EndsWith(".") ||
                name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains('/') || name.Contains('\\'))
                return "Prefab名に空白だけの名前・パス・使用できない文字は指定できません。";
            string stem = name.Split('.')[0].ToUpperInvariant();
            if (new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(stem))
                return "このPrefab名はWindowsの予約名です。別の名前にしてください。";
            if (!IsOutputFolder(data.outputFolder))
                return "保存先はAssets/NormalStage内のフォルダーにしてください。";
            var occupied = new HashSet<Vector2Int>();
            foreach (var stamp in data.cells)
            {
                if (stamp == null || !IsCellPrefab(stamp.prefab))
                    return "参照切れ、または単独のTileCellではないPrefabがあります。該当セルを置き直してください。";
                if (!occupied.Add(stamp.position))
                    return "同じ座標に複数のセルがあります。";
                if (data.cells.Count > 1 && stamp.prefab.GetComponentInChildren<BreakableTile>(true) != null)
                    return "崩れる床はグループ全体を消すため、1セルのグループで作成してください。";
            }
            return null;
        }

        public static bool IsOutputFolder(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   (path == WorkFolder || path.StartsWith(WorkFolder + "/", StringComparison.Ordinal)) &&
                   !path.Contains("\\") && path.Split('/').All(p => p != ".." && p != "." && p.Length > 0 && p.IndexOfAny(Path.GetInvalidFileNameChars()) < 0);
        }

        public static int CountIslands(TileGroupLayoutData data)
        {
            var remaining = new HashSet<Vector2Int>(data.cells.Select(c => c.position));
            int count = 0;
            var queue = new Queue<Vector2Int>();
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (remaining.Count > 0)
            {
                var first = remaining.First();
                remaining.Remove(first);
                queue.Enqueue(first);
                count++;
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var direction in directions)
                        if (remaining.Remove(current + direction)) queue.Enqueue(current + direction);
                }
            }
            return count;
        }

        // Prefabと編集用レイアウトを必ず別名で保存。既存アセットを上書きしない。
        public static GameObject SaveAsNew(TileGroupLayoutData data, out TileGroupLayout layout)
        {
            layout = null;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Playモードを終了してから保存してください。");
            string error = Validate(data);
            if (error != null) throw new InvalidOperationException(error);
            EnsureFolder(data.outputFolder);
            string basePath = data.outputFolder + "/" + data.prefabName;
            string prefabPath = basePath + ".prefab";
            for (int suffix = 1; File.Exists(prefabPath) || File.Exists(Path.ChangeExtension(prefabPath, ".asset")); suffix++)
                prefabPath = basePath + "_" + suffix + ".prefab";
            string layoutPath = Path.ChangeExtension(prefabPath, ".asset");
            var previewScene = EditorSceneManager.NewPreviewScene();
            GameObject root = null;
            bool prefabWritten = false;
            try
            {
                root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
                SceneManager.MoveGameObjectToScene(root, previewScene);
                var group = root.AddComponent<TileGroup>();
                var serialized = new SerializedObject(group);
                serialized.FindProperty("_isStatic").boolValue = data.isStatic;
                var min = data.cells[0].position;
                var max = min;
                foreach (var stamp in data.cells)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(stamp.prefab, previewScene);
                    instance.transform.SetParent(root.transform, false);
                    Vector2Int local = stamp.position - data.origin;
                    instance.name = $"{stamp.prefab.name}_{local.x}_{local.y}";
                    instance.transform.localPosition = new Vector3(local.x * data.tileSize, stamp.prefab.transform.localPosition.y, local.y * data.tileSize);
                    instance.transform.localRotation = stamp.prefab.transform.localRotation;
                    instance.transform.localScale = Vector3.Scale(stamp.prefab.transform.localScale, new Vector3(data.tileSize, 1f, data.tileSize));
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                    min = Vector2Int.Min(min, stamp.position);
                    max = Vector2Int.Max(max, stamp.position);
                }
                // 手札プレビューで形状の中心が撮影原点に来るように補正。
                Vector2 center = ((Vector2)min + max) * 0.5f - data.origin;
                serialized.FindProperty("_previewOffset").vector3Value = new Vector3(-center.x * data.tileSize, 0, -center.y * data.tileSize);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
                if (!success || prefab == null) throw new IOException("Prefabの保存に失敗しました。");
                prefabWritten = true;
                layout = ScriptableObject.CreateInstance<TileGroupLayout>();
                layout.data = data.Copy();
                layout.data.prefabName = root.name;
                AssetDatabase.CreateAsset(layout, layoutPath);
                AssetDatabase.SaveAssetIfDirty(layout);
                return prefab;
            }
            catch
            {
                if (prefabWritten) AssetDatabase.DeleteAsset(prefabPath);
                if (layout != null)
                {
                    if (AssetDatabase.Contains(layout)) AssetDatabase.DeleteAsset(layoutPath);
                    else UnityEngine.Object.DestroyImmediate(layout);
                }
                throw;
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)) throw new IOException("保存先フォルダーを作成できません。");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
