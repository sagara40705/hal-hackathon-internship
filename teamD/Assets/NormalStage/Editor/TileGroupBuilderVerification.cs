using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NormalStage.EditorTools
{
    // Unityの実Prefab保存・読み直し・TileGrid登録まで確認するEditModeの検証。
    public static class TileGroupBuilderVerification
    {
        [MenuItem("Tools/Normal Stage/Verify Tile Group Builder")]
        public static void VerifyFromMenu() => Debug.Log(Run());

        public static string Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("EditModeで実行してください。");
            var scene = SceneManager.GetActiveScene();
            bool dirty = scene.isDirty;
            int roots = scene.rootCount;
            int scenes = SceneManager.sceneCount;
            var selection = Selection.activeObject;
            string folder = "Assets/NormalStage/Editor/Verification_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/NormalStage/Editor", folder.Split('/').Last());
            int checks = 0;
            Action<bool, string> check = (passed, message) =>
            {
                if (!passed) throw new Exception("Tile Group Builder: " + message);
                checks++;
            };
            try
            {
                var floor = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tile/Prefab/TileCell/Cell.prefab");
                var arrow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tile/Prefab/TileCell/Cell_Direction_F.prefab");
                var jump = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gimmick/Prefab/TileCell/Cell_Jump.prefab");
                var broken = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gimmick/Prefab/TileCell/Cell_Breakable.prefab");
                var groupSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gimmick/Prefab/TileGroup/Group_OnOffFloor_On.prefab");
                check(TileGroupPrefabWriter.IsCellPrefab(floor) && TileGroupPrefabWriter.IsCellPrefab(arrow) && TileGroupPrefabWriter.IsCellPrefab(jump), "既存セルをパレットとして受理");
                check(!TileGroupPrefabWriter.IsCellPrefab(groupSource), "専用グループの誤入れ子を拒否");
                var data = new TileGroupLayoutData { outputFolder = folder, prefabName = "Group_Verify", origin = new Vector2Int(2, 1) };
                check(TileGroupPrefabWriter.Validate(data) != null, "空レイアウトを拒否");
                data.cells.Add(new TileGroupCellStamp(new Vector2Int(1, 1), floor));
                data.cells.Add(new TileGroupCellStamp(new Vector2Int(2, 1), arrow));
                data.cells.Add(new TileGroupCellStamp(new Vector2Int(2, 2), jump));
                check(TileGroupPrefabWriter.Validate(data) == null, "有効な複数種レイアウト");
                check(TileGroupPrefabWriter.CountIslands(data) == 1, "連結セルを判定");
                var disconnected = data.Copy();
                disconnected.cells.Add(new TileGroupCellStamp(new Vector2Int(-4, -4), floor));
                check(TileGroupPrefabWriter.CountIslands(disconnected) == 2 && TileGroupPrefabWriter.Validate(disconnected) == null, "離れた形を保持");
                var invalid = data.Copy();
                invalid.cells.Add(new TileGroupCellStamp(data.cells[0].position, floor));
                check(TileGroupPrefabWriter.Validate(invalid) != null, "重複座標を拒否");
                invalid = data.Copy(); invalid.cells[0].prefab = null;
                check(TileGroupPrefabWriter.Validate(invalid) != null, "参照切れを拒否");
                invalid = data.Copy(); invalid.cells[0].prefab = broken;
                check(TileGroupPrefabWriter.Validate(invalid) != null, "崩れる床の複数セル混在を拒否");
                invalid.cells.RemoveRange(1, 2);
                check(TileGroupPrefabWriter.Validate(invalid) == null, "崩れる床の単独配置を許可");
                invalid = data.Copy(); invalid.outputFolder = "Assets/NormalStage/../Tile";
                check(TileGroupPrefabWriter.Validate(invalid) != null, "保存範囲外へのパスを拒否");
                invalid = data.Copy(); invalid.prefabName = "../Other";
                check(TileGroupPrefabWriter.Validate(invalid) != null, "名前にパスを指定できない");
                invalid = data.Copy(); invalid.tileSize = float.NaN;
                check(TileGroupPrefabWriter.Validate(invalid) != null, "不正なマスサイズを拒否");

                var prefab = TileGroupPrefabWriter.SaveAsNew(data, out var layout);
                string path = AssetDatabase.GetAssetPath(prefab);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var group = prefab.GetComponent<TileGroup>();
                check(group != null && !group.IsStatic && prefab.GetComponentsInChildren<TileGroup>(true).Length == 1, "ルートに移動可能なTileGroupが1つ");
                check(prefab.transform.childCount == 3 && prefab.GetComponentsInChildren<TileCell>().Length == 3, "保存して読み直したセル数");
                check(group.PreviewOffset == new Vector3(0.5f, 0, -0.5f), "手札プレビューを中央に補正");
                var layoutReloaded = AssetDatabase.LoadAssetAtPath<TileGroupLayout>(AssetDatabase.GetAssetPath(layout));
                check(layoutReloaded.data.origin == data.origin && layoutReloaded.data.cells[2].prefab == jump, "レイアウトの参照と原点を保存");
                var copied = layoutReloaded.data.Copy(); copied.cells[0].position = Vector2Int.zero;
                check(layoutReloaded.data.cells[0].position == new Vector2Int(1, 1), "再編集用コピーが保存元を変えない");
                foreach (var cell in prefab.GetComponentsInChildren<TileCell>())
                {
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(cell.gameObject);
                    check(source != null && new[] { floor, arrow, jump }.Contains(source), "セルPrefabとの接続を維持");
                    var calls = new SerializedObject(cell).FindProperty("_onEnterPlayerEvent.m_PersistentCalls.m_Calls");
                    var sourceCalls = new SerializedObject(source.GetComponent<TileCell>()).FindProperty("_onEnterPlayerEvent.m_PersistentCalls.m_Calls");
                    check(calls.arraySize == sourceCalls.arraySize, "イベント数を維持");
                    for (int i = 0; i < calls.arraySize; i++)
                    {
                        var call = calls.GetArrayElementAtIndex(i);
                        var target = call.FindPropertyRelative("m_Target").objectReferenceValue as Component;
                        check(target != null && target.transform.IsChildOf(prefab.transform), "イベント参照先が生成したPrefab内");
                        check(call.FindPropertyRelative("m_MethodName").stringValue == sourceCalls.GetArrayElementAtIndex(i).FindPropertyRelative("m_MethodName").stringValue, "イベントメソッドを維持");
                    }
                }

                var preview = EditorSceneManager.NewPreviewScene();
                try
                {
                    var gridObject = new GameObject("VerificationGrid");
                    SceneManager.MoveGameObjectToScene(gridObject, preview);
                    var grid = gridObject.AddComponent<TileGrid>();
                    grid.SetAllPlacementAllowed(true);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
                    instance.transform.SetParent(gridObject.transform, false);
                    var instanceGroup = instance.GetComponent<TileGroup>();
                    instanceGroup.InitGroup(1);
                    check(instanceGroup.Cells.Select(c => c.LocalPosition).ToHashSet().SetEquals(new[] { new Vector2Int(-1, 0), Vector2Int.zero, new Vector2Int(0, 1) }), "原点を引いたXZセル座標");
                    check(grid.SetTile(instanceGroup, new Vector2Int(5, 5)), "既存TileGridに登録可能");
                    check(grid.GetCell(new Vector2Int(4, 5)) != null && grid.GetCell(new Vector2Int(5, 6)) != null, "登録先セル位置が一致");
                    var player = new GameObject("VerificationPlayer");
                    SceneManager.MoveGameObjectToScene(player, preview);
                    var playerComponent = player.AddComponent<Player>();
                    grid.GetCell(new Vector2Int(5, 5)).GetComponent<ChangeDirection>().OnEnterPlayer(playerComponent);
                    check(playerComponent.Direction == Vector2Int.up, "方向転換の動作を維持");
                    check(grid.RemoveTile(instanceGroup) && grid.CanPlaceTile(instanceGroup, new Vector2Int(6, 5)), "登録解除後に再配置可能");
                }
                finally { EditorSceneManager.ClosePreviewScene(preview); }

                var next = TileGroupPrefabWriter.SaveAsNew(data, out var nextLayout);
                check(AssetDatabase.GetAssetPath(next) != path && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null, "同名保存で既存Prefabを上書きしない");
                check(AssetDatabase.GetAssetPath(nextLayout) != AssetDatabase.GetAssetPath(layout), "同名レイアウトも上書きしない");
                var scaled = data.Copy(); scaled.isStatic = true; scaled.tileSize = 2;
                var scaledPrefab = TileGroupPrefabWriter.SaveAsNew(scaled, out _);
                check(scaledPrefab.GetComponent<TileGroup>().IsStatic, "固定フラグを保存");
                check(scaledPrefab.transform.GetChild(0).localPosition == new Vector3(-2, 0, 0) && scaledPrefab.transform.GetChild(0).localScale == new Vector3(2, 1, 2), "マスサイズをXZ位置と見た目に適用");
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
                Selection.activeObject = selection;
            }
            check(scene == SceneManager.GetActiveScene() && scene.isDirty == dirty && scene.rootCount == roots && SceneManager.sceneCount == scenes, "ユーザーの開いているシーンを変更しない");
            return $"Tile Group Builder: {checks} checks passed.";
        }
    }
}
